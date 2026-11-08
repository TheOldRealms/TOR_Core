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
    /// In-mission UI marker for the ambush extraction zone. Projects the
    /// zone's world position to screen each tick and drives a Gauntlet
    /// VM so the prefab renders a yellow dot at the zone's location.
    ///
    /// Gates (all must pass for the marker to render):
    /// 1. <c>Mission.Current</c> + <c>MissionScreen</c> are alive.
    /// 2. <see cref="AmbushMissionController.IsInitialPatrolDefeated"/>
    ///    (Phase 2 entered: the escape fiction is live).
    /// 3. <c>Mission.MissionEnded == false</c> (a clean wipe ends the
    ///    mission; the retreat zone has no purpose after victory).
    /// 4. ALT-held OR order menu open. Same GameKey ID (5 =
    ///    ShowIndicators, bound to LeftAlt) that
    ///    <c>MissionGauntletFormationMarker</c> uses to decide when
    ///    vanilla formation plates are visible. Overlaying on that
    ///    gate gives the user a single tactical-overview hotkey for
    ///    both the formation markers and our retreat-zone marker.
    /// 5. The world point projects to a point in front of the camera
    ///    and inside the viewport.
    ///
    /// Projection follows vanilla's pattern from
    /// <c>MissionGauntletFormationMarker.UpdateMarkerPositions</c>:
    /// read the <c>CombatCamera</c> frame and feed it to
    /// <c>MBWindowManager.WorldToScreen</c>. The returned screen pixels
    /// go into the VM as a Vec2; the prefab binds its root widget's
    /// <c>Position</c> to that Vec2 (no HorizontalAlignment / corner
    /// offset shenanigans), which is what pins the sprite to the
    /// projected world point instead of the HUD corner.
    /// </summary>
    [DefaultView]
    public class AmbushRetreatZoneMarkerView : MissionView
    {
        private const int ShowIndicatorsGameKey = 5; // GameKeyDefinition.ShowIndicators -> LeftAlt
        private static readonly Vec3 HeightOffset = new Vec3(0f, 0f, 1.5f, -1f);

        private AmbushMissionController _controller;
        private bool _controllerResolved;
        private AmbushRetreatZoneMarkerVM _vm;
        private GauntletLayer _layer;

        public override void OnMissionScreenInitialize()
        {
            base.OnMissionScreenInitialize();
            _vm = new AmbushRetreatZoneMarkerVM();
            _layer = new GauntletLayer("AmbushRetreatZoneMarker", ViewOrderPriority, false);
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
            if (Mission.Current.MissionEnded) { _vm.IsVisible = false; return; }

            if (!_controllerResolved)
            {
                _controller = Mission.Current.GetMissionBehavior<AmbushMissionController>();
                _controllerResolved = true;
            }
            if (_controller == null) { _vm.IsVisible = false; return; }
            if (!_controller.IsInitialPatrolDefeated) { _vm.IsVisible = false; return; }

            // Command-mode gate: visible only while ALT is held (same
            // GameKey vanilla's formation markers use) or the order
            // menu is open. Rendering the zone during normal gameplay
            // was the v1 bug the user flagged.
            bool commandMode = Input.IsGameKeyDown(ShowIndicatorsGameKey)
                               || Mission.Current.IsOrderMenuOpen;
            if (!commandMode) { _vm.IsVisible = false; return; }

            // Pick the primary zone: first authored, else procedural
            // fallback. Multi-zone scenes would want a list rendered
            // via an MBBindingList on the VM; v2 scope ships one
            // marker.
            var zones = AmbushRetreatZone.ActiveZones;
            Vec3 worldPos;
            if (zones.Count > 0 && zones[0] != null)
                worldPos = zones[0].Position;
            else if (AmbushRetreatZone.ProceduralFallbackPosition.HasValue)
                worldPos = AmbushRetreatZone.ProceduralFallbackPosition.Value;
            else { _vm.IsVisible = false; return; }

            float screenX = 0f, screenY = 0f, screenW = 0f;
            MBWindowManager.WorldToScreen(MissionScreen.CombatCamera,
                                           worldPos + HeightOffset,
                                           ref screenX, ref screenY, ref screenW);
            // screenW < 0 means the point is behind the camera. Also
            // guard against NaN/Inf from degenerate projections.
            if (!MathF.IsValidValue(screenW) || screenW <= 0f
                || !MathF.IsValidValue(screenX) || !MathF.IsValidValue(screenY))
            {
                _vm.IsVisible = false;
                return;
            }
            float resW = TaleWorlds.Engine.Screen.RealScreenResolutionWidth;
            float resH = TaleWorlds.Engine.Screen.RealScreenResolutionHeight;
            if (screenX < 0f || screenY < 0f || screenX > resW || screenY > resH)
            {
                _vm.IsVisible = false;
                return;
            }
            _vm.ScreenPosition = new Vec2(screenX, screenY);
            _vm.IsVisible = true;
        }
    }
}
