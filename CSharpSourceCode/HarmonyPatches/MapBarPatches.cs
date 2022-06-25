using HarmonyLib;
using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.CampaignSystem.ViewModelCollection.Map.MapBar;
using TOR_Core.CampaignMechanics;

namespace TOR_Core.HarmonyPatches
{
    [HarmonyPatch]
    public class MapBarPatches
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(MapBarVM), MethodType.Constructor, typeof(INavigationHandler), typeof(IMapStateHandler), typeof(Func<MapBarShortcuts>), typeof(Action))]
        public static void ReplaceMapVM(MapBarVM __instance)
        {
            __instance.MapInfo = new TORMapInfoVM();
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(MapBarVM), "OnRefresh")]
        public static void RefreshExtraProperties(MapBarVM __instance)
        {
            var info = __instance.MapInfo as TORMapInfoVM;
            if (info != null) info.RefreshExtraProperties();
        }
    }
}
