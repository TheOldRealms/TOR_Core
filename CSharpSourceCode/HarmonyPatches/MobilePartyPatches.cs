using HarmonyLib;
using Helpers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TOR_Core.Extensions;
using TOR_Core.Utilities;

namespace TOR_Core.HarmonyPatches;

[HarmonyPatch]
public static class MobilePartyPatches
{
    private static bool ShouldUseChaosLordSettlementOverride(Hero hero)
    {
        return hero != null &&
               hero.IsLord &&
               hero.Culture?.StringId == TORConstants.Cultures.CHAOS &&
               hero.Clan != null &&
               !hero.Clan.IsOutlaw;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(Clan), "get_DefaultPartyTemplate")]
    public static void UseCultureTemplateForBrokenClanDefaultTemplate(Clan __instance, ref PartyTemplateObject __result)
    {
        if (__result?.Stacks != null && __result.ShipHulls != null)
        {
            return;
        }

        var culturePartyTemplate = __instance.Culture?.DefaultPartyTemplate;
        if (culturePartyTemplate?.Stacks == null || culturePartyTemplate.ShipHulls == null)
        {
            return;
        }
        __result = culturePartyTemplate;
    }

    //Fill available cultures
    [HarmonyPrefix]
    [HarmonyPatch(typeof(PartyBase), "UpdateVisibilityAndInspected", MethodType.Normal)]
    public static bool PreIsVisible(ref PartyBase __instance)
    {
        if (!__instance.IsMobile || !__instance.MobileParty.IsMainParty)
        {
            return true;
        }

        if (__instance.LeaderHero.IsEnlisted())
        {
            __instance.MobileParty.IsVisible = false;

            return false;
        }

        return true;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(HeroSpawnCampaignBehavior), "CalculateScoreToCreateParty")]
    public static bool GiveHighScore(ref float __result)
    {
        __result = 999f;
        return false;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(HeroHelper), "FindASuitableSettlementToTeleportForHero")]
    public static bool ChaosHeroTeleportSettlementPrefix(Hero hero, float minimumScore, ref Settlement __result)
    {
        if (!ShouldUseChaosLordSettlementOverride(hero))
        {
            return true;
        }

        var preferredSettlement = hero.Clan.HomeSettlement ?? hero.Clan.InitialHomeSettlement;
        if (preferredSettlement == null)
        {
            return true;
        }

        __result = preferredSettlement;
        return false;
    }
}