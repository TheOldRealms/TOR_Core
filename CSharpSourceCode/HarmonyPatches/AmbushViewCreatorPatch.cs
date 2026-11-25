using HarmonyLib;
using SandBox.View.Missions;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View.MissionViews;
using TaleWorlds.MountAndBlade.View.MissionViews.Order;
using TaleWorlds.MountAndBlade.View.MissionViews.Singleplayer;
using TaleWorlds.MountAndBlade.View.Screens;
using TOR_Core.Missions.AmbushMission;
using TOR_Core.Missions.AmbushMission.UI;

namespace TOR_Core.HarmonyPatches
{
    /// <summary>
    /// Problem: vanilla's OpenBattleMission constructs DeploymentMissionView directly, bypassing the ViewCreatorManager so our [OverrideView] subclass never runs.
    /// Fix: postfix the view-list factory and substitute AmbushDeploymentMissionView for the vanilla instance when the mission owns an AmbushMissionController.
    /// </summary>
    [HarmonyPatch(typeof(SandBoxMissionViews), nameof(SandBoxMissionViews.OpenBattleMission))]
    public static class AmbushViewCreatorPatch
    {
        /// <summary>Swaps the deployment view when the mission is an ambush.</summary>
        public static void Postfix(Mission mission, ref MissionView[] __result)
        {
            if (mission?.GetMissionBehavior<AmbushMissionController>() == null) return;
            for (int i = 0; i < __result.Length; i++)
            {
                if (__result[i].GetType() == typeof(DeploymentMissionView))
                {
                    __result[i] = new AmbushDeploymentMissionView();
                    return;
                }
            }
        }
    }

    /// <summary>
    /// Problem: a suspended OrderTroopPlacer still computes a Ground cursor state and renders the placement flag, suggesting clicks will work when they are discarded.
    /// Fix: prefix GetCursorState to return CursorState.Normal whenever the placer is suspended. Not gated on ambush, any suspended placer gets the same behaviour.
    /// </summary>
    [HarmonyPatch(typeof(OrderTroopPlacer), "GetCursorState")]
    public static class OrderTroopPlacerCursorPatch
    {
        /// <summary>Short-circuits the ground-cursor computation when the placer is suspended.</summary>
        public static bool Prefix(OrderTroopPlacer __instance, ref OrderTroopPlacer.CursorState __result)
        {
            if (!__instance.SuspendTroopPlacer) return true;
            __result = OrderTroopPlacer.CursorState.Normal;
            return false;
        }
    }

    /// <summary>
    /// Problem: the vanilla order-wheel handler flips SuspendTroopPlacer back to false when its layer becomes enabled, letting the flag render for a frame or two before our view re-suppresses it.
    /// Fix: prefix the setter. During the ambusher deployment phase, coerce the incoming value to true so the side effects still run but the suspension stays.
    /// </summary>
    [HarmonyPatch(typeof(OrderTroopPlacer), "SuspendTroopPlacer", MethodType.Setter)]
    public static class OrderTroopPlacerSuspendSetterPatch
    {
        /// <summary>Forces SuspendTroopPlacer true during the ambusher deployment phase.</summary>
        public static void Prefix(ref bool value)
        {
            if (value) return;
            var mission = Mission.Current;
            if (mission == null) return;
            if (mission.Mode != TaleWorlds.Core.MissionMode.Deployment) return;
            var controller = mission.GetMissionBehavior<AmbushMissionController>();
            if (controller == null || !controller.IsPlayerAmbusher) return;
            value = true;
        }
    }

    /// <summary>
    /// Problem: the 3D order-flag banner is controlled by MissionScreen.SetOrderFlagVisibility, not by SuspendTroopPlacer, so vanilla still renders the banner during ambusher deployment.
    /// Fix: prefix SetOrderFlagVisibility to coerce the incoming value to false during the ambusher deployment phase.
    /// </summary>
    [HarmonyPatch(typeof(MissionScreen), nameof(MissionScreen.SetOrderFlagVisibility))]
    public static class MissionScreenOrderFlagVisibilityPatch
    {
        /// <summary>Hides the 3D order-flag banner during the ambusher deployment phase.</summary>
        public static void Prefix(ref bool value)
        {
            if (!value) return;
            var mission = Mission.Current;
            if (mission == null) return;
            if (mission.Mode != TaleWorlds.Core.MissionMode.Deployment) return;
            var controller = mission.GetMissionBehavior<AmbushMissionController>();
            if (controller == null || !controller.IsPlayerAmbusher) return;
            value = false;
        }
    }
}
