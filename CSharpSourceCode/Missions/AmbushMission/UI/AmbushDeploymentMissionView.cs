using NLog;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View.MissionViews.Singleplayer;
using TOR_Core.Utilities;

namespace TOR_Core.Missions.AmbushMission.UI
{
    /// <summary>
    /// Replaces vanilla's <see cref="DeploymentMissionView"/> for ambush
    /// missions. Keeps vanilla's deployment-mode freeze (owned by
    /// <c>BattleDeploymentMissionController</c> + <c>BattleDeploymentHandler</c>)
    /// but suppresses terrain click-to-place input so the only placement
    /// mechanism is spot-assignment via the sidebar UI.
    ///
    /// Registered by Harmony-postfixing <c>SandBox.View.ViewCreator.OpenBattleMission</c>:
    /// vanilla instantiates DeploymentMissionView directly with <c>new</c>
    /// rather than via the ViewCreatorManager override registry, so the
    /// <c>[OverrideView]</c> attribute has no effect on it. The postfix
    /// removes vanilla's instance and adds ours when the mission is an
    /// ambush.
    ///
    /// Previous iteration also owned a GauntletLayer with on-screen spot
    /// markers (the brown "1"/"2" chips). They were misaligned, stayed
    /// visible into the battle, and the per-frame projection loop had no
    /// mission-end guard — a plausible contributor to the post-TotalVictory
    /// freeze. Removed entirely; the spot-picker in the OoB sidebar is
    /// enough.
    /// </summary>
    public class AmbushDeploymentMissionView : DeploymentMissionView
    {
        private bool _isAmbusherPath;
        private int _suspendLogCounter;

        public override void AfterStart()
        {
            base.AfterStart();
            var controller = Mission.GetMissionBehavior<AmbushMissionController>();
            _isAmbusherPath = controller != null && controller.IsPlayerAmbusher;
            TORCommon.Log(LogLevel.Info,
                "[AmbushDeploymentView] AfterStart: ambusherPath=" + _isAmbusherPath
                + " orderTroopPlacer=" + (_orderTroopPlacer == null ? "null" : "set"));

            // Point the deployment camera at the scener-authored anchor
            // so the ambusher starts looking at the engagement area
            // instead of whatever default vanilla picks (often scene
            // corner or sp_player on a far hill).
            //
            // Bannerlord camera frames must be right-handed (s = u x f).
            // The earlier spelling used s = (facing.Y, -facing.X, 0),
            // which is the LEFT-handed version; the engine responded by
            // rotating the camera to look straight up (+Z) instead of
            // along the authored horizontal facing.
            if (_isAmbusherPath
                && controller.TryGetCameraAnchor(out var pos, out var facing)
                && MissionScreen != null)
            {
                var frame = MatrixFrame.Identity;
                frame.origin = pos;
                frame.rotation.f = new Vec3(facing.X, facing.Y, 0f);
                frame.rotation.s = new Vec3(-facing.Y, facing.X, 0f);
                frame.rotation.u = new Vec3(0f, 0f, 1f);
                MissionScreen.UpdateFreeCamera(frame);
            }
        }

        public override void OnMissionScreenTick(float dt)
        {
            base.OnMissionScreenTick(dt);
            if (!_isAmbusherPath) return;
            // Only suppress during the deployment phase. Once FinishDeployment
            // flips the mission into Battle mode, give-orders via terrain
            // click must work normally.
            if (Mission.Mode != TaleWorlds.Core.MissionMode.Deployment) return;
            var placer = _orderTroopPlacer
                ?? Mission.GetMissionBehavior<TaleWorlds.MountAndBlade.View.MissionViews.Order.OrderTroopPlacer>();
            if (placer != null && !placer.SuspendTroopPlacer)
            {
                placer.SuspendTroopPlacer = true;
                if (_suspendLogCounter++ < 3)
                {
                    TORCommon.Log(LogLevel.Info,
                        "[AmbushDeploymentView] SuspendTroopPlacer flipped true (deployment)");
                }
            }
        }
    }
}
