using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TOR_Core.Extensions;

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

    // removing influence changes for the hirelings
    [HarmonyPrefix]
    [HarmonyPatch(typeof(ChangeClanInfluenceAction), nameof(ChangeClanInfluenceAction.Apply))]
    private static void Prefix_FreezeHirelingInfluence(Clan clan, ref float amount)
    {
        if (clan != Clan.PlayerClan || !Hero.MainHero.IsEnlisted())
        {
            return;
        }
        amount = -clan.Influence;
    }

    // removes auto recruitment hard cap
    [HarmonyPrefix]
    [HarmonyPatch(typeof(DefaultSettlementGarrisonModel), nameof(DefaultSettlementGarrisonModel.GetMaximumDailyAutoRecruitmentCount))]
    private static bool Prefix_UncapDailyGarrisonAutoRecruitment(ref int __result)
    {
        __result = int.MaxValue; 
        return false; 
    }

    // prevent ai armies from leaving more troops in a garrison than the settlements garrison capacity
    [HarmonyPatch(typeof(DefaultSettlementGarrisonModel), nameof(DefaultSettlementGarrisonModel.FindNumberOfTroopsToLeaveToGarrison))]
    internal static class ClampTroopsLeftToGarrisonCapacityPatch
    {
        [HarmonyPostfix]
        private static void Postfix(MobileParty mobileParty, Settlement settlement, ref int __result)
        {
            if (settlement == null || !settlement.IsFortification)
            {
                return;
            }

            var garrisonParty = settlement.Town.GarrisonParty;
            var currentGarrisonCount = garrisonParty.Party.NumberOfAllMembers;

            var garrisonCapacityExplained = Campaign.Current.Models.PartySizeLimitModel.CalculateGarrisonPartySizeLimit(settlement);
            var garrisonCapacity = (int)garrisonCapacityExplained.ResultNumber;

            var freeSlots = garrisonCapacity - currentGarrisonCount;
            if (freeSlots < 0)
            {
                freeSlots = 0;
            }

            if (__result > freeSlots)
            {
                __result = freeSlots;
            }

            if (__result < 0)
            {
                __result = 0;
            }
        }
    }
}