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
    [HarmonyPatch(typeof(MobilePartyAi), "ComputePath")]
    public static bool AccountForRestrictionZones(ref bool __result, 
        MobilePartyAi __instance, 
        MobileParty ____mobileParty, 
        ref PathFaceRecord ____targetAiFaceIndex,
        ref bool ____aiPathMode,
        Vec2 newTargetPosition)
    {
        bool hasValidPathBeenFound = false;
        if (____mobileParty.CurrentNavigationFace.IsValid())
        {
            ____targetAiFaceIndex = Campaign.Current.MapSceneWrapper.GetFaceIndex(newTargetPosition);
            if (____targetAiFaceIndex.IsValid())
            {
                Vec2 position2D = ____mobileParty.Position2D;
                hasValidPathBeenFound = Campaign.Current.MapSceneWrapper.GetPathBetweenAIFaces(____mobileParty.CurrentNavigationFace, ____targetAiFaceIndex, position2D, newTargetPosition, 0.1f, __instance.Path, ____mobileParty.GetExclusionFaceIds());
            }
            else
            {
                Debug.FailedAssert("Path finding target is not valid", "C:\\Develop\\MB3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem\\Party\\MobilePartyAi.cs", "ComputePath", 2017);
            }
        }
        AccessTools.Property(typeof(MobilePartyAi), "PathBegin").SetValue(__instance, 0);
        if (!hasValidPathBeenFound)
        {
            ____aiPathMode = false;
        }
        __result = hasValidPathBeenFound;
        return false;
    }
}