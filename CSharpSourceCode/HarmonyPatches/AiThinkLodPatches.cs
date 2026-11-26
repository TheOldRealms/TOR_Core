using System.Diagnostics;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.Settlements;
using TOR_Core.CampaignMechanics;
using TOR_Core.Utilities.Profiling;

namespace TOR_Core.HarmonyPatches;

/// <summary>
/// Harmony patches that apply <see cref="AiThinkLod"/>-based gating to vanilla
/// per-entity tick methods, so distant entities don't pay the full per-tick cost.
/// </summary>
/// <remarks>
/// Problem:
///   The Nov 2026 BCA capture showed vanilla methods dominating campaign-map CPU,
///   with <c>PoliticalStagnationAndBorderIncidentCampaignBehavior.HourlyTickSettlement</c>
///   at 9.88 s cumulative / 362k calls. The report labelled this "Political
///   Stagnation" but the actual work is a spatial scan via
///   <c>MobileParty.StartFindingLocatablesAroundPosition</c> around every settlement
///   to compute nearby threat/ally intensities that feed defensive AI decisions.
///   Those fields only matter where the player can observe or interact with the
///   downstream AI behaviour, i.e. for settlements near the player.
///
/// Fix:
///   Prefix coarsens the call cadence by distance tier, instead of skipping
///   entirely. Vanilla's own 3-hour internal gate still applies on top, so our
///   gate only takes effect when its period exceeds 3 hours.
///
///   * Near: pass every call. Effective work cadence = 3h (vanilla default).
///   * Mid:  pass 1-in-6 calls, staggered by settlement hash. Effective ~6h.
///   * Far:  pass 1-in-12 calls, staggered. Effective ~12h.
///
///   Threat/ally values for Mid/Far still evolve, just coarsely. Catch-up on
///   player approach is natural: when the tier flips to Near the gate opens
///   and vanilla's internal elapsed-time check runs the full update on the
///   next matching tick.
///
/// Measurement:
///   Separate prefix+postfix pair records executed calls under
///   <c>Vanilla.PoliticalStagnation.HourlyTickSettlement</c>. Count × mean after
///   this patch lands indicates post-gating cost; compare against the pre-patch
///   baseline (9.88 s / 362k from the BCA report, or a fresh capture before this
///   patch is deployed) to measure the win.
///
/// Scope:
///   First application of AiThinkLod gating. If this validates, the same pattern
///   extends to <c>AiPartyThinkBehavior.PartyHourlyAiTick</c>,
///   <c>RecruitmentCampaignBehavior.HourlyTickParty</c>, and similar vanilla hot
///   paths. Delete this file (and <c>AiThinkLod.cs</c>) to remove.
/// </remarks>
public static class AiThinkLodPatches
{
    [HarmonyPatch(typeof(PoliticalStagnationAndBorderIncidentCampaignBehavior), "HourlyTickSettlement")]
    public static class PoliticalStagnationHourlyTickSettlementLodPatch
    {
        private const int MidPeriodHours = 6;
        private const int FarPeriodHours = 12;

        [HarmonyPrefix]
        public static bool Prefix(Settlement settlement)
        {
            if (settlement == null) return true;
            var tier = AiThinkLod.GetTier(settlement);
            if (tier == AiThinkLod.Tier.Near) return true;

            int hour = (int)CampaignTime.Now.ToHours;
            // Stagger via settlement hash so Mid/Far ticks don't synchronise onto
            // the same in-game hour. Masking the sign bit keeps modulo non-negative.
            int offset = settlement.StringId.GetHashCode() & 0x7FFFFFFF;
            int period = tier == AiThinkLod.Tier.Mid ? MidPeriodHours : FarPeriodHours;
            return ((hour + offset) % period) == 0;
        }
    }

    [HarmonyPatch(typeof(PoliticalStagnationAndBorderIncidentCampaignBehavior), "HourlyTickSettlement")]
    public static class PoliticalStagnationHourlyTickSettlementProfilerPatch
    {
        [HarmonyPrefix]
        public static void Prefix(out long __state)
            => __state = TORCampaignProfiler.Enabled ? Stopwatch.GetTimestamp() : 0L;

        [HarmonyPostfix]
        public static void Postfix(long __state)
        {
            // Postfix is skipped when the gating prefix above returns false, so this
            // only records calls that actually ran the vanilla body.
            if (__state == 0L) return;
            var elapsedMs = (Stopwatch.GetTimestamp() - __state) * 1000.0 / Stopwatch.Frequency;
            TORCampaignProfiler.Record("Vanilla.PoliticalStagnation.HourlyTickSettlement", elapsedMs);
        }
    }
}
