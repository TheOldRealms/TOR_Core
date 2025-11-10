using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Library;
using TOR_Core.Extensions;

namespace TOR_Core.HarmonyPatches;

[HarmonyPatch]
public static class MobilePartyPatches
{
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
}