using System.Diagnostics;
using HarmonyLib;
using TaleWorlds.CampaignSystem.CampaignBehaviors.AiBehaviors;
using TOR_Core.Utilities.Profiling;

namespace TOR_Core.HarmonyPatches.Profiling;

/// <summary>
/// Measurement-only Harmony patches around vanilla AI-tick methods. Their only purpose
/// is to feed the TOR profiler so we can see where vanilla spends time on the campaign
/// map. No prefix-return, no transpile, no field writes: these patches cannot change
/// vanilla behaviour.
/// </summary>
/// <remarks>
/// Problem:
///   The TOR profiler only sees methods we wrap with <c>using var _ = Scope(name)</c>.
///   The report's biggest cumulative costs are on vanilla methods (AI Party Think,
///   AiVisitSettlement) that we cannot edit directly. Without instrumentation here,
///   steady-state cost analysis on the campaign map has a blind spot.
///
/// Fix:
///   Attach a trivial Prefix/Postfix pair to each target vanilla method. Prefix snapshots
///   the stopwatch; Postfix computes elapsed and feeds <see cref="TORCampaignProfiler.Record"/>.
///   Both branches gate on <see cref="TORCampaignProfiler.Enabled"/>: with the profiler off
///   the overhead is one bool read + one int assignment per call.
///
/// Scope:
///   Two methods only. Both are the per-party inner-loop entry on vanilla's AI think path
///   (per the decompile research in performance-improvements/vanilla-ai-lod-notes.md):
///   - AiPartyThinkBehavior.PartyHourlyAiTick       (dispatches AiHourlyTickEvent)
///   - AiVisitSettlementBehavior.AiHourlyTick       (dominant subscriber)
///
///   Delete this file to remove both patches cleanly. No other code depends on them.
/// </remarks>
public static class VanillaAiProfilerPatches
{
    [HarmonyPatch(typeof(AiPartyThinkBehavior), "PartyHourlyAiTick")]
    public static class PartyHourlyAiTickPatch
    {
        [HarmonyPrefix]
        public static void Prefix(out long __state)
            => __state = TORCampaignProfiler.Enabled ? Stopwatch.GetTimestamp() : 0L;

        [HarmonyPostfix]
        public static void Postfix(long __state)
        {
            if (__state == 0L) return;
            var elapsedMs = (Stopwatch.GetTimestamp() - __state) * 1000.0 / Stopwatch.Frequency;
            TORCampaignProfiler.Record("Vanilla.AiPartyThink.PartyHourlyAiTick", elapsedMs);
        }
    }

    [HarmonyPatch(typeof(AiVisitSettlementBehavior), "AiHourlyTick")]
    public static class AiVisitSettlementAiHourlyTickPatch
    {
        [HarmonyPrefix]
        public static void Prefix(out long __state)
            => __state = TORCampaignProfiler.Enabled ? Stopwatch.GetTimestamp() : 0L;

        [HarmonyPostfix]
        public static void Postfix(long __state)
        {
            if (__state == 0L) return;
            var elapsedMs = (Stopwatch.GetTimestamp() - __state) * 1000.0 / Stopwatch.Frequency;
            TORCampaignProfiler.Record("Vanilla.AiVisitSettlement.AiHourlyTick", elapsedMs);
        }
    }
}
