using System;
using System.Collections.Generic;
using System.Diagnostics;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.BarterSystem;
using TaleWorlds.CampaignSystem.BarterSystem.Barterables;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.CampaignBehaviors.BarterBehaviors;
using TOR_Core.CampaignMechanics.Diplomacy;
using TOR_Core.Utilities.Profiling;

namespace TOR_Core.HarmonyPatches;

/// <summary>
/// Measurement-only Harmony probes around vanilla + TOR kingdom/clan daily-tick
/// methods. Scopes added here isolate the "Kingdom decisions" family of cost that
/// the BCA report labelled "TOR Kingdom Decisions 41.7 ms" and "Diplomatic Barters
/// 44.3 ms" without us knowing whether the time sat on the TOR side, the vanilla
/// side, or in both pipelines running back-to-back on every clan-day.
/// </summary>
/// <remarks>
/// Targets picked from <c>performance-improvements/vanilla-kingdom-decisions-notes.md</c>
/// (research pass 2026-11-26). Signatures and namespaces verified via
/// <c>ilspycmd</c> against the installed CampaignSystem.dll.
///
/// Zero behaviour change: each patch is a prefix+postfix stopwatch pair gated on
/// <see cref="TORCampaignProfiler.Enabled"/>. Postfix runs even when a parallel
/// gating prefix elsewhere returns false, so the recorded count equals total event
/// dispatches; the time contribution for dispatch-but-no-body calls is near zero,
/// which keeps the mean a useful indicator of real cost.
///
/// Delete this file (and its csproj entry) to remove all six probes.
/// </remarks>
public static class KingdomTickProbes
{
    // ----- 1. Vanilla kingdom decision pipeline (clan-side) -----

    [HarmonyPatch(typeof(KingdomDecisionProposalBehavior), "DailyTickClan")]
    public static class KingdomDecisionProposalDailyTickClanPatch
    {
        [HarmonyPrefix]
        public static void Prefix(out long __state)
            => __state = TORCampaignProfiler.Enabled ? Stopwatch.GetTimestamp() : 0L;

        [HarmonyPostfix]
        public static void Postfix(long __state)
        {
            if (__state == 0L) return;
            var elapsedMs = (Stopwatch.GetTimestamp() - __state) * 1000.0 / Stopwatch.Frequency;
            TORCampaignProfiler.Record("Vanilla.KingdomDecisionProposal.DailyTickClan", elapsedMs);
        }
    }

    // ----- 2. Vanilla common-case decision update split out -----

    [HarmonyPatch(typeof(KingdomDecisionProposalBehavior), "UpdateKingdomDecisions")]
    public static class KingdomDecisionProposalUpdateKingdomDecisionsPatch
    {
        [HarmonyPrefix]
        public static void Prefix(out long __state)
            => __state = TORCampaignProfiler.Enabled ? Stopwatch.GetTimestamp() : 0L;

        [HarmonyPostfix]
        public static void Postfix(long __state)
        {
            if (__state == 0L) return;
            var elapsedMs = (Stopwatch.GetTimestamp() - __state) * 1000.0 / Stopwatch.Frequency;
            TORCampaignProfiler.Record("Vanilla.KingdomDecisionProposal.UpdateKingdomDecisions", elapsedMs);
        }
    }

    // ----- 3. Vanilla clan-finance + governor + settlement-payment combo -----

    [HarmonyPatch(typeof(ClanVariablesCampaignBehavior), "DailyTickClan")]
    public static class ClanVariablesDailyTickClanPatch
    {
        [HarmonyPrefix]
        public static void Prefix(out long __state)
            => __state = TORCampaignProfiler.Enabled ? Stopwatch.GetTimestamp() : 0L;

        [HarmonyPostfix]
        public static void Postfix(long __state)
        {
            if (__state == 0L) return;
            var elapsedMs = (Stopwatch.GetTimestamp() - __state) * 1000.0 / Stopwatch.Frequency;
            TORCampaignProfiler.Record("Vanilla.ClanVariables.DailyTickClan", elapsedMs);
        }
    }

    // ----- 4. Vanilla diplomatic-barter proposals -----

    [HarmonyPatch(typeof(DiplomaticBartersBehavior), "DailyTickClan")]
    public static class DiplomaticBartersDailyTickClanPatch
    {
        [HarmonyPrefix]
        public static void Prefix(out long __state)
            => __state = TORCampaignProfiler.Enabled ? Stopwatch.GetTimestamp() : 0L;

        [HarmonyPostfix]
        public static void Postfix(long __state)
        {
            if (__state == 0L) return;
            var elapsedMs = (Stopwatch.GetTimestamp() - __state) * 1000.0 / Stopwatch.Frequency;
            TORCampaignProfiler.Record("Vanilla.DiplomaticBarters.DailyTickClan", elapsedMs);
        }
    }

    // ----- 5. TOR's compounding trade-agreement decisions -----

    [HarmonyPatch(typeof(TORTradeAgreementAIBehavior), "ConsiderTradeAgreements")]
    public static class TORTradeAgreementConsiderTradeAgreementsPatch
    {
        [HarmonyPrefix]
        public static void Prefix(out long __state)
            => __state = TORCampaignProfiler.Enabled ? Stopwatch.GetTimestamp() : 0L;

        [HarmonyPostfix]
        public static void Postfix(long __state)
        {
            if (__state == 0L) return;
            var elapsedMs = (Stopwatch.GetTimestamp() - __state) * 1000.0 / Stopwatch.Frequency;
            TORCampaignProfiler.Record("TOR.TORTradeAgreement.ConsiderTradeAgreements", elapsedMs);
        }
    }

    // ----- 6. Vanilla AI barter execution -----

    // ExecuteAiBarter has two overloads; the IEnumerable one does the real work
    // and is called by the single-Barterable wrapper, so patching just this one
    // catches both paths' cost.
    [HarmonyPatch(typeof(BarterManager), "ExecuteAiBarter",
        new Type[] { typeof(IFaction), typeof(IFaction), typeof(Hero), typeof(Hero), typeof(IEnumerable<Barterable>) })]
    public static class BarterManagerExecuteAiBarterPatch
    {
        [HarmonyPrefix]
        public static void Prefix(out long __state)
            => __state = TORCampaignProfiler.Enabled ? Stopwatch.GetTimestamp() : 0L;

        [HarmonyPostfix]
        public static void Postfix(long __state)
        {
            if (__state == 0L) return;
            var elapsedMs = (Stopwatch.GetTimestamp() - __state) * 1000.0 / Stopwatch.Frequency;
            TORCampaignProfiler.Record("Vanilla.BarterManager.ExecuteAiBarter", elapsedMs);
        }
    }
}
