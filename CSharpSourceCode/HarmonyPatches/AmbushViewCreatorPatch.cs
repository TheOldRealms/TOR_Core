using HarmonyLib;
using SandBox.View.Missions;
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
    /// Problem: on an ambush mission the deployment phase needs custom behaviour
    /// (suppress terrain click-to-place for the ambusher, auto-finish deployment
    /// for the victim) that only our AmbushDeploymentMissionView implements.
    /// Vanilla's SandBoxMissionViews.OpenBattleMission builds the view list by
    /// doing `new DeploymentMissionView()` directly instead of routing through
    /// ViewCreatorManager.CreateMissionView&lt;T&gt;(), so the [OverrideView]
    /// attribute on our subclass is silently ignored.
    ///
    /// Fix: postfix the view-list factory. If the mission owns an
    /// AmbushMissionController, find the vanilla DeploymentMissionView instance
    /// in the returned array and substitute our subclass. Scoped to ambush
    /// missions only so no other battle-mission flow is affected.
    /// </summary>
    [HarmonyPatch(typeof(SandBoxMissionViews), nameof(SandBoxMissionViews.OpenBattleMission))]
    public static class AmbushViewCreatorPatch
    {
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
    /// Problem: during the ambusher's deployment phase we set
    /// OrderTroopPlacer.SuspendTroopPlacer = true, which skips the mouse-click
    /// input path inside OnMissionScreenTick. It does NOT skip the preceding
    /// `ActiveCursorState = GetCursorState()` line, so vanilla still computes
    /// a Ground cursor state over valid terrain, which in turn renders the
    /// formation-placement flag indicator and changes the cursor icon. The
    /// player sees a UI that visually suggests they can place troops even
    /// though the click is discarded, which is misleading and was raised as
    /// a visual bug.
    ///
    /// Fix: prefix GetCursorState so that when the placer is suspended, it
    /// short-circuits to CursorState.Normal. No flag, no cursor change.
    /// Deliberately NOT gated on ambush context: a suspended placer should
    /// behave as suspended in every respect, so any future caller that
    /// sets SuspendTroopPlacer for its own reason gets the same visual
    /// behaviour for free.
    /// </summary>
    [HarmonyPatch(typeof(OrderTroopPlacer), "GetCursorState")]
    public static class OrderTroopPlacerCursorPatch
    {
        public static bool Prefix(OrderTroopPlacer __instance, ref OrderTroopPlacer.CursorState __result)
        {
            if (!__instance.SuspendTroopPlacer) return true;
            __result = OrderTroopPlacer.CursorState.Normal;
            return false;
        }
    }

    /// <summary>
    /// Problem: our view sets OrderTroopPlacer.SuspendTroopPlacer = true in
    /// OnMissionScreenTick, but the vanilla GauntletUI order-wheel handler
    /// forces it back to false whenever its layer becomes enabled (notably
    /// after the player selects the first formation). That race lets the
    /// flag cursor + placement preview render for a frame or two before our
    /// view ticks again and re-suppresses it. Log shows three or more
    /// "flipped true" events per deployment phase, matching the user's
    /// report that the flag only appears after a formation is selected.
    ///
    /// Fix: prefix the SuspendTroopPlacer setter. When the mission has an
    /// AmbushMissionController, the player is the ambusher, and the mission
    /// mode is Deployment, force the incoming value to true regardless of
    /// what the caller tried. Vanilla's side effects (HideOrderPositionEntities
    /// and Reset) still run as intended. Scoped tightly so combat orders
    /// outside the ambusher deployment phase are untouched.
    ///
    /// Not a setter-block-trap case ([[feedback_harmony_setter_block_trap]]):
    /// we keep the setter running and only coerce the parameter, so no caller
    /// retries in a loop and no input layer freezes.
    /// </summary>
    [HarmonyPatch(typeof(OrderTroopPlacer), "SuspendTroopPlacer", MethodType.Setter)]
    public static class OrderTroopPlacerSuspendSetterPatch
    {
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
    /// Problem: the 3D order-flag banner (MissionScreen.OrderFlag, a world-
    /// space entity rendered on the ground where a formation would be sent)
    /// is visibility-controlled by MissionScreen.SetOrderFlagVisibility,
    /// not by OrderTroopPlacer.SuspendTroopPlacer. The vanilla gauntlet
    /// order-wheel handler calls SetOrderFlagVisibility(true) on its own
    /// schedule (e.g. when a formation is selected and the order wheel
    /// slows time). Our setter-pin keeps the input gate closed and the
    /// cursor-prefix pins the icon, but the 3D banner still renders because
    /// it answers to a completely separate flag.
    ///
    /// Fix: prefix SetOrderFlagVisibility. During the ambusher deployment
    /// phase, coerce the incoming value to false. Vanilla's side effect
    /// (OrderFlag.IsVisible setter which may clear FocusedOrderableObject)
    /// still runs with the coerced value, so no caller is left in an
    /// inconsistent state.
    /// </summary>
    [HarmonyPatch(typeof(MissionScreen), nameof(MissionScreen.SetOrderFlagVisibility))]
    public static class MissionScreenOrderFlagVisibilityPatch
    {
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
