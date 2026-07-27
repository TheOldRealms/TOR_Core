using HarmonyLib;
using Helpers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.CampaignBehaviors.BarterBehaviors;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TOR_Core.CampaignMechanics.UniqueSpawns;
using TOR_Core.Extensions;
using TOR_Core.Models;
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
    [HarmonyPatch(typeof(HeroSpawnCampaignBehavior), "FindASuitableSettlementToTeleportForHero")]
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

    [HarmonyPrefix]
    [HarmonyPatch(typeof(DiplomaticBartersBehavior), "DailyTickClan")]
    public static bool KeepUniqueSpawnClansOutOfVanillaDiplomacy(Clan clan)
    {
        return UniqueSpawnCampaignBehavior.ShouldRunVanillaDiplomacy(clan);
    }

    // post calculation mobile party attack and avoidance decisions
    [HarmonyPostfix]
    [HarmonyPatch(typeof(DefaultMobilePartyAIModel), "CalculateInitiativeScoresForEnemy")]
    private static void PostProcessEnemyInitiativeScores(
        DefaultMobilePartyAIModel __instance,
        MobileParty mobileParty,
        MobileParty enemyParty,
        float localAdvantage,
        float maxAggressiveness,
        ref float avoidScore,
        ref float attackScore)
    {
        if (__instance is not TORMobilePartyAIModel torMobilePartyAIModel)
        {
            return;
        }

        torMobilePartyAIModel.AdjustEnemyInitiativeScores(
            mobileParty,
            enemyParty,
            localAdvantage,
            maxAggressiveness,
            ref avoidScore,
            ref attackScore);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(MobileParty), "GetBehaviorText")]
    public static void UseOrionWarPlanBehaviorText(MobileParty __instance, ref TextObject __result)
    {
        if (__instance?.GetUniqueSpawnComponent()?.UniqueSpawnId != "tor_unique_orion")
        {
            return;
        }

        var orionBehavior = Campaign.Current?.GetCampaignBehavior<OrionCampaignBehavior>();
        var behaviorText = orionBehavior?.GetOrionBehaviorText(__instance);
        if (behaviorText == null || behaviorText.IsEmpty())
        {
            return;
        }

        __result = behaviorText;
    }

}