using HarmonyLib;
using TaleWorlds.CampaignSystem.SceneInformationPopupTypes;
using TOR_Core.CampaignMechanics.FactionScenes;

namespace TOR_Core.HarmonyPatches;

[HarmonyPatch]
public static class JoinKingdomSceneIDPatch
{
    private const string Trigger = "FactionJoin";

    [HarmonyPrefix]
    [HarmonyPatch(typeof(JoinKingdomSceneNotificationItem), "get_SceneID")]
    private static bool Prefix(JoinKingdomSceneNotificationItem __instance, ref string __result)
    {
        var overrideId = TORFactionSceneRegistry.Resolve(Trigger, __instance.KingdomToUse);
        if (overrideId == null) return true;

        __result = overrideId;
        return false;
    }
}
