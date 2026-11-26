using System.Diagnostics;
using HarmonyLib;
using SandBox.ViewModelCollection.Nameplate;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.CampaignBehaviors.AiBehaviors;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Library;
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

    /// <summary>
    /// Tier-based cadence gate on vanilla's per-party AI think. Mid and Far parties
    /// still think, just at a wider cadence than vanilla's own <c>HourCounter % 6</c>
    /// stable-lord gate.
    /// </summary>
    /// <remarks>
    /// Vanilla's PartyHourlyAiTick increments <see cref="MobilePartyAi.HourCounter"/>
    /// at the end of its body (whole-assembly decompile verified). Prefix-return-false
    /// skips the increment too, so HourCounter only advances on calls that pass my
    /// gate. Combined with vanilla's own <c>HourCounter % 6 == 0</c> check for stable
    /// lords, this means effective work cadence = my-period × 6:
    ///
    /// <list type="bullet">
    ///   <item>Near: pass every call. Effective ~6h (vanilla default for stable lords).</item>
    ///   <item>Mid:  pass 1-in-4 calls. Effective ~24h.</item>
    ///   <item>Far:  pass 1-in-8 calls. Effective ~48h.</item>
    /// </list>
    ///
    /// Catch-up on Far → Near (player approaches a distant party): when the tier
    /// flips, the next hour my gate opens (immediately for Near), and vanilla's
    /// HourCounter counts up at most 5 hours before hitting a multiple of 6. So the
    /// max latency between a Far party becoming Near and resuming full-fidelity
    /// think is ~5 in-game hours.
    ///
    /// Also respects <see cref="MobilePartyAi.RethinkAtNextHourlyTick"/>: any code
    /// path (vanilla or TOR) that sets this flag bypasses the gate so the explicit
    /// catch-up signal still works for battles, encounters, war declarations, etc.
    ///
    /// MainParty is never gated (vanilla's internal check already excludes it from
    /// the think, but letting the full method run keeps HourCounter evolving
    /// predictably for the player's party).
    /// </remarks>
    /// <summary>
    /// Measurement-only probe on <c>MapEvent.Update</c>. Companion to the LOD gate
    /// below. Only records calls that actually ran the vanilla body (postfix is
    /// skipped when the gate returned false).
    /// </summary>
    [HarmonyPatch(typeof(MapEvent), "Update")]
    public static class MapEventUpdateProfilerPatch
    {
        [HarmonyPrefix]
        public static void Prefix(out long __state)
            => __state = TORCampaignProfiler.Enabled ? Stopwatch.GetTimestamp() : 0L;

        [HarmonyPostfix]
        public static void Postfix(long __state)
        {
            if (__state == 0L) return;
            var elapsedMs = (Stopwatch.GetTimestamp() - __state) * 1000.0 / Stopwatch.Frequency;
            TORCampaignProfiler.Record("Vanilla.MapEvent.Update", elapsedMs);
        }
    }

    /// <summary>
    /// Tier-based cadence gate on <c>MapEvent.Update</c>. Called per-frame per-event
    /// by <c>MapEventManager.Tick</c>. The expensive combat work inside vanilla's
    /// body is already time-gated via <c>_nextSimulationTime.IsPast</c>, so this
    /// gate saves the cheap state-check overhead that fires every frame per event.
    /// </summary>
    /// <remarks>
    /// Cadence by tier (frame-based, not in-game-time-based):
    /// <list type="bullet">
    ///   <item>Near: pass every frame. Full vanilla responsiveness.</item>
    ///   <item>Mid:  pass 1-in-4 frames staggered by event hash.</item>
    ///   <item>Far:  pass 1-in-16 frames staggered.</item>
    /// </list>
    ///
    /// In-game battle duration is preserved because vanilla's internal
    /// <c>_nextSimulationTime</c> uses <c>CampaignTime</c>, which advances
    /// regardless of whether <c>Update</c> fired. When the gate finally opens, the
    /// vanilla body sees "sim time is well past" and runs the overdue sim session
    /// in one go. Casualties, retreats, outcomes all identical. Only the
    /// wall-clock latency between "sim is due" and "sim actually runs" shifts.
    ///
    /// Safety: skips gating for player-involved map events so the player's own
    /// battles behave exactly like vanilla.
    /// </remarks>
    [HarmonyPatch(typeof(MapEvent), "Update")]
    public static class MapEventUpdateLodPatch
    {
        private const int MidPeriodFrames = 4;
        private const int FarPeriodFrames = 16;

        [HarmonyPrefix]
        public static bool Prefix(MapEvent __instance)
        {
            if (__instance == null) return true;
            // Never gate the player's own battle. MapEventManager.Tick already
            // short-circuits for the non-raid player event, but IsRaid cases still
            // reach here — keep them full speed.
            if (MobileParty.MainParty != null && __instance == MobileParty.MainParty.MapEvent) return true;

            var tier = AiThinkLod.GetTier(__instance);
            if (tier == AiThinkLod.Tier.Near) return true;

            int frame = AiThinkLod.CurrentFrame;
            int offset = __instance.GetHashCode() & 0x7FFFFFFF;
            int period = tier == AiThinkLod.Tier.Mid ? MidPeriodFrames : FarPeriodFrames;
            return ((frame + offset) % period) == 0;
        }
    }

    /// <summary>
    /// Camera-distance cull on the per-settlement nameplate update. Called from
    /// <c>SettlementNameplatesVM.Update</c> inside a <c>TWParallel.For</c> over all
    /// ~1,100 settlement nameplates every frame. Vanilla does no visibility
    /// pre-filter at the aggregator level, so settlements on the far side of the
    /// continent run full update treatment every frame even though they will
    /// never be on screen. This probe caught it at ~10 ms mean per aggregator
    /// call = ~62% of a 60 FPS frame budget on every frame of campaign-map play.
    /// </summary>
    /// <remarks>
    /// Gate: skip the update when the settlement's world-plane distance to the
    /// camera exceeds the cull radius. The method parameter <c>cameraPosition</c>
    /// is the live camera Vec3; we compare to the settlement's gate position.
    ///
    /// 300f threshold chosen to generously cover default zoom (~150 units visible
    /// radius) plus pan + zoom-out extremes, while still cutting ~70% of
    /// settlements on any given frame when the player is in a populated region.
    ///
    /// Safety notes:
    /// - Skipped settlements retain their last-computed bind values. Their UI
    ///   simply stops updating until they re-enter the cull radius. Since they
    ///   are not on screen, the stale values are invisible.
    /// - Siege/raid state transitions fire via separate event handlers
    ///   (OnSiegeEventStartedOnSettlement, OnMapEventStartedOnSettlement) that
    ///   update the nameplate directly, so icon state propagates immediately
    ///   even for culled settlements. When the player later approaches, the next
    ///   in-range Update picks up the already-correct state.
    /// - Sequential pass (RefreshBindValues) still runs for all nameplates but
    ///   only reads the already-computed _bind fields — cheap field assignments.
    /// </remarks>
    [HarmonyPatch(typeof(SettlementNameplateVM), "UpdateNameplateMT")]
    public static class SettlementNameplateUpdateMTLodPatch
    {
        private const float CullRadiusSq = 300f * 300f;

        [HarmonyPrefix]
        public static bool Prefix(SettlementNameplateVM __instance, Vec3 cameraPosition)
        {
            if (__instance?.Settlement == null) return true;

            var sp = __instance.Settlement.GatePosition;
            float dx = cameraPosition.x - sp.X;
            float dy = cameraPosition.y - sp.Y;
            return (dx * dx + dy * dy) < CullRadiusSq;
        }
    }

    [HarmonyPatch(typeof(AiPartyThinkBehavior), "PartyHourlyAiTick")]
    public static class AiPartyThinkPartyHourlyAiTickLodPatch
    {
        private const int MidPeriodHours = 4;
        private const int FarPeriodHours = 8;

        [HarmonyPrefix]
        public static bool Prefix(MobileParty mobileParty)
        {
            if (mobileParty == null || mobileParty == MobileParty.MainParty) return true;
            // Respect vanilla's explicit rethink request. Battles, encounters,
            // settlement entry, and war-related events set this flag so the AI
            // reacts within one tick; don't gate past it.
            if (mobileParty.Ai != null && mobileParty.Ai.RethinkAtNextHourlyTick) return true;

            var tier = AiThinkLod.GetTier(mobileParty);
            if (tier == AiThinkLod.Tier.Near) return true;

            int hour = (int)CampaignTime.Now.ToHours;
            int offset = mobileParty.StringId.GetHashCode() & 0x7FFFFFFF;
            int period = tier == AiThinkLod.Tier.Mid ? MidPeriodHours : FarPeriodHours;
            return ((hour + offset) % period) == 0;
        }
    }
}
