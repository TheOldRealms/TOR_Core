using System.Diagnostics;
using HarmonyLib;
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
///   Prefix-skip the method for settlements the <see cref="AiThinkLod"/> classifies
///   as <see cref="AiThinkLod.Tier.Far"/>. Catch-up on approach is implicit: the
///   method's own internal 3-hour gate compares <c>ElapsedHoursUntilNow</c>, so the
///   next call after a Far→Mid/Near transition sees a large elapsed time and
///   runs the full update once. The multiplicative decay (×0.85 / ×0.8) is also
///   skipped for Far settlements, which just means the field stays at its last
///   computed value until the player approaches again.
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
        [HarmonyPrefix]
        public static bool Prefix(Settlement settlement)
        {
            // Far settlements skip entirely. Near + Mid pass through to the vanilla
            // body (which itself has a 3-hour internal gate).
            return AiThinkLod.GetTier(settlement) != AiThinkLod.Tier.Far;
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
