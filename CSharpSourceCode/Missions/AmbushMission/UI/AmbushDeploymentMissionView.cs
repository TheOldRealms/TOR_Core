using NLog;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.InputSystem;
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
    /// </summary>
    public class AmbushDeploymentMissionView : DeploymentMissionView
    {
        private bool _isAmbusherPath;
        private int _suspendLogCounter;
        private AmbushDeploymentVM _vm;
        private GauntletLayer _layer;

        public override void AfterStart()
        {
            base.AfterStart();
            var controller = Mission.GetMissionBehavior<AmbushMissionController>();
            _isAmbusherPath = controller != null && controller.IsPlayerAmbusher;
            TORCommon.Log(LogLevel.Info,
                "[AmbushDeploymentView] AfterStart: ambusherPath=" + _isAmbusherPath
                + " orderTroopPlacer=" + (_orderTroopPlacer == null ? "null" : "set"));

            if (!_isAmbusherPath) return;
            _vm = new AmbushDeploymentVM(controller.Layout);
            _layer = new GauntletLayer("GauntletLayer", 50);
            _layer.LoadMovie("AmbushDeployment", _vm);
            MissionScreen.AddLayer(_layer);
        }

        public override void OnRemoveBehavior()
        {
            if (_layer != null)
            {
                MissionScreen.RemoveLayer(_layer);
                _layer = null;
            }
            _vm = null;
            base.OnRemoveBehavior();
        }

        public override void OnMissionScreenTick(float dt)
        {
            base.OnMissionScreenTick(dt);
            if (!_isAmbusherPath) return;
            // Only suppress during the deployment phase. Once FinishDeployment
            // flips the mission into Battle mode, give orders via terrain
            // click must work normally.
            if (Mission.Mode != TaleWorlds.Core.MissionMode.Deployment) return;
            var placer = _orderTroopPlacer
                ?? Mission.GetMissionBehavior<TaleWorlds.MountAndBlade.View.MissionViews.Order.OrderTroopPlacer>();
            if (placer != null && !placer.SuspendTroopPlacer)
            {
                placer.SuspendTroopPlacer = true;
                if (_suspendLogCounter++ < 3)
                {
                    TORCommon.Log(LogLevel.Info,"[AmbushDeploymentView] SuspendTroopPlacer flipped true (deployment)");
                }
            }
            UpdateSpotMarkers();
        }

        private void UpdateSpotMarkers()
        {
            if (_vm?.SpotMarkers == null) return;
            var cam = MissionScreen.CombatCamera;
            if (cam == null) return;
            foreach (var marker in _vm.SpotMarkers)
            {
                var world = marker.Spot.Position;
                float screenX = 0f;
                float screenY = 0f;
                float depth = 0f;
                MBWindowManager.WorldToScreen(cam, world, ref screenX, ref screenY, ref depth);
                bool onScreen = depth > 0f;
                marker.IsVisible = onScreen;
                if (!onScreen) continue;
                marker.ScreenX = screenX;
                marker.ScreenY = screenY;
            }
        }
    }
}
