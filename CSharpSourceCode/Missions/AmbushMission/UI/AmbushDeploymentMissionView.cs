using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.View.MissionViews.Singleplayer;
using TOR_Core.Utilities;

namespace TOR_Core.Missions.AmbushMission.UI
{
    /// <summary>
    /// Replaces vanilla's <see cref="DeploymentMissionView"/> for the ambush
    /// mission type only. Keeps vanilla's deployment-mode freeze (owned by
    /// <c>BattleDeploymentMissionController</c> / <c>BattleDeploymentHandler</c>)
    /// but swaps out the drag-polygon UI for ours.
    ///
    /// When the player is the ambusher, this view is a placeholder — a
    /// subsequent task fills the sidebar + overlay + Confirm UI. For now the
    /// drag-polygon UI is gone (we replaced the view) and vanilla's freeze
    /// stays in effect until something triggers end-of-deployment.
    ///
    /// When the player is the ambush victim, <see cref="AmbushMissionController"/>
    /// auto-triggers end-of-deployment via the vanilla controller's
    /// OnAfterSetupTeams event — this view has nothing to do on that path.
    ///
    /// On a non-ambush mission (defensive — shouldn't happen since we can't
    /// register this view conditionally) the override is a no-op pass-through.
    /// </summary>
    [OverrideView(typeof(DeploymentMissionView))]
    public class AmbushDeploymentMissionView : DeploymentMissionView
    {
        public override void AfterStart()
        {
            base.AfterStart();
            // The victim-side auto-skip lives in AmbushMissionController via
            // BattleDeploymentMissionController.OnAfterSetupTeams. On the
            // ambusher side, suppress vanilla's terrain click-to-place input.
            // Our mechanic is spot-assignment via the sidebar UI only; letting
            // the player nudge formations by clicking the ground breaks the
            // layout (no global "forward" direction) and can glitch troops.
            var controller = Mission.GetMissionBehavior<AmbushMissionController>();
            if (controller == null)
            {
                TORCommon.Say("[AmbushDeploymentView] no controller, passthrough");
                return;
            }
            if (controller.IsPlayerAmbusher && _orderTroopPlacer != null)
            {
                _orderTroopPlacer.SuspendTroopPlacer = true;
            }
        }
    }
}
