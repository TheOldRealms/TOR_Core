using HarmonyLib;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TOR_Core.Models;

namespace TOR_Core.HarmonyPatches;

[HarmonyPatch]
[HarmonyPatchCategory("LatePatches")]
public static class ModelPatches
{

    // this patch ensures that the randomly AI hired  mercenary costs , while being a vassal in a kingdom, are not getting crazy high. Especially in smaller kingdoms.
    [HarmonyPrefix]
    [HarmonyPatch(typeof(DefaultClanFinanceModel), "CalculateShareFactor")]
    public static bool Prefix(ref float __result, Clan clan)
    {
        if (clan == Clan.PlayerClan && clan.Kingdom != null && clan.Kingdom.RulingClan != Clan.PlayerClan)
        {
            __result = 0;
            return false;
        }

        return true;
    }

    // removes auto recruitment hard cap
    [HarmonyPrefix]
    [HarmonyPatch(typeof(DefaultSettlementGarrisonModel), nameof(DefaultSettlementGarrisonModel.GetMaximumDailyAutoRecruitmentCount))]
    private static bool Prefix_UncapDailyGarrisonAutoRecruitment(ref int __result)
    {
        __result = int.MaxValue; 
        return false; 
    }

    // right after a settlement is captured it's garrison can end up with extremely low morale due to last defeat's penalties, causing desertion over the first 2 weeks so this adds a temporary morale floor
    internal static class CapturedSettlementGarrisonMoraleFloor
    {
        internal const float MIN_GARRISON_MORALE = 40f;
        private const float DURATION_DAYS = 14f;

        private static readonly Dictionary<string, double> _activeUntilDayBySettlementId = new();

        public static void Start(Settlement settlement)
        {
            _activeUntilDayBySettlementId[settlement.StringId] = CampaignTime.DaysFromNow(DURATION_DAYS).ToDays;
        }

        public static bool IsActiveForGarrison(MobileParty mobileParty)
        {
            var settlement = mobileParty.CurrentSettlement;
            if (settlement == null)
            {
                return false;
            }

            return _activeUntilDayBySettlementId.TryGetValue(settlement.StringId, out var activeUntilDay) &&
                   CampaignTime.Now.ToDays < activeUntilDay;
        }
    }

    [HarmonyPatch(typeof(CampaignEvents), "OnSettlementOwnerChanged")]
    internal static class CapturedSettlementGarrisonMoraleFloorMarkerPatch
    {
        [HarmonyPostfix]
        private static void Postfix(
            Settlement settlement,
            bool openToClaim,
            Hero newOwner,
            Hero oldOwner,
            Hero capturerHero,
            ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail detail)
        {
            if (settlement == null || !settlement.IsFortification)
            {
                return;
            }

            if (detail != ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail.BySiege)
            {
                return;
            }

            CapturedSettlementGarrisonMoraleFloor.Start(settlement);
        }
    }

    [HarmonyPatch(typeof(TORPartyMoraleModel), nameof(TORPartyMoraleModel.GetEffectivePartyMorale))]
    internal static class CapturedSettlementGarrisonMoraleFloorMoraleModelPatch
    {
        [HarmonyPostfix]
        private static void Postfix(MobileParty mobileParty, ref ExplainedNumber __result)
        {
            if (mobileParty == null || !mobileParty.IsGarrison)
            {
                return;
            }

            if (!CapturedSettlementGarrisonMoraleFloor.IsActiveForGarrison(mobileParty))
            {
                return;
            }

            __result.LimitMin(CapturedSettlementGarrisonMoraleFloor.MIN_GARRISON_MORALE);
        }
    }

}