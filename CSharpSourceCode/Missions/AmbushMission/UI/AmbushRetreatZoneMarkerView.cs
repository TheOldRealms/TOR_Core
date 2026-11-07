using TaleWorlds.Engine;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.View.MissionViews;
using TaleWorlds.ScreenSystem;

namespace TOR_Core.Missions.AmbushMission.UI
{
    /// <summary>
    /// In-mission UI marker for the ambush extraction zone. Projects
    /// the zone's world position to screen each tick and drives a
    /// Gauntlet VM so the prefab renders a yellow dot at the zone's
    /// location. Visibility is gated on Phase 2 (initial defender
    /// patrol dead) and on-screen. Off-screen behaviour for v1 is
    /// simply "hide"; a clamped edge-arrow is follow-up polish.
    ///
    /// Per-frame guards follow the lifecycle pattern in
    /// feedback_bannerlord_post_mission_ui_thread_stall: Mission.Current
    /// null, VM null, and layer null all early-return so mission
    /// teardown can't wedge the UI thread.
    /// </summary>
    [DefaultView]
    public class AmbushRetreatZoneMarkerView : MissionView
    {
        private AmbushMissionController _controller;
        private bool _controllerResolved;
        private AmbushRetreatZoneMarkerVM _vm;
        private GauntletLayer _layer;

        public override void OnMissionScreenInitialize()
        {
            base.OnMissionScreenInitialize();
            _vm = new AmbushRetreatZoneMarkerVM();
            _layer = new GauntletLayer("GauntletLayer", ViewOrderPriority, false);
            _layer.LoadMovie("AmbushRetreatZoneMarker", _vm);
            ((ScreenBase)MissionScreen).AddLayer(_layer);
        }

        public override void OnMissionScreenFinalize()
        {
            if (_layer != null)
            {
                ((ScreenBase)MissionScreen).RemoveLayer(_layer);
                _layer = null;
            }
            _vm?.OnFinalize();
            _vm = null;
            base.OnMissionScreenFinalize();
        }

        public override void OnMissionScreenTick(float dt)
        {
            base.OnMissionScreenTick(dt);
            if (Mission.Current == null || _vm == null) return;
            if (!_controllerResolved)
            {
                _controller = Mission.Current.GetMissionBehavior<AmbushMissionController>();
                _controllerResolved = true;
            }
            if (_controller == null) { _vm.IsVisible = false; return; }
            if (!_controller.IsInitialPatrolDefeated) { _vm.IsVisible = false; return; }

            // Pick the primary zone: first authored, else procedural fallback.
            // Multi-zone scenes would want a list rendered via an MBBindingList
            // on the VM; v1 scope ships one marker.
            var zones = AmbushRetreatZone.ActiveZones;
            Vec3 worldPos;
            if (zones.Count > 0 && zones[0] != null)
                worldPos = zones[0].Position;
            else if (AmbushRetreatZone.ProceduralFallbackPosition.HasValue)
                worldPos = AmbushRetreatZone.ProceduralFallbackPosition.Value;
            else { _vm.IsVisible = false; return; }

            float screenX = 0f, screenY = 0f, screenW = 0f;
            MBWindowManager.WorldToScreen(MissionScreen.CombatCamera, worldPos,
                                           ref screenX, ref screenY, ref screenW);
            // Negative w means the point is behind the camera; hide.
            if (screenW <= 0f) { _vm.IsVisible = false; return; }
            float resW = Screen.RealScreenResolutionWidth;
            float resH = Screen.RealScreenResolutionHeight;
            if (screenX < 0f || screenY < 0f || screenX > resW || screenY > resH)
            {
                _vm.IsVisible = false;
                return;
            }
            _vm.ScreenX = screenX;
            _vm.ScreenY = screenY;
            _vm.IsVisible = true;
        }
    }
}
