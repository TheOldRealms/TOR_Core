using TaleWorlds.Engine;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.View.MissionViews;

namespace TOR_Core.Missions.AmbushMission.UI
{
    /// <summary>
    /// Makes the vanilla objective-marker extraction point clickable
    /// for formation orders. Mirrors the pattern
    /// <c>MissionFormationTargetSelectionHandler</c> uses for engaging
    /// enemy formations: project the target's world position to the
    /// screen each frame; when the cursor is within a focus radius of
    /// the projection AND the player is in command mode AND left-click
    /// is released, issue a Move order on the currently selected
    /// formations to the target position.
    ///
    /// The vanilla objective-marker widget itself is pure display
    /// (no Command.Click plumbing), and <c>IOrderable</c> requires a
    /// <c>MissionObject</c> which a static zone isn't. Going through
    /// the screen-cursor path lets us ride the same command-mode UX
    /// vanilla uses for formation-target clicks without an
    /// implementation of IOrderable.
    ///
    /// Scope: Phase 2 (patrol defeated) + command mode (ALT held or
    /// order menu open) + there is at least one selected formation.
    /// </summary>
    [DefaultView]
    public class AmbushExtractionOrderHandler : MissionView
    {
        private const int ShowIndicatorsGameKey = 5; // GameKeyDefinition.ShowIndicators -> LeftAlt
        private const float FocusPixelRadius = 70f;
        private static readonly Vec3 HeightOffset = new Vec3(0f, 0f, 1.5f, -1f);

        private AmbushMissionController _controller;
        private bool _controllerResolved;

        public override void OnMissionScreenTick(float dt)
        {
            base.OnMissionScreenTick(dt);
            if (Mission.Current == null || MissionScreen == null) return;
            if (Mission.Current.MissionEnded) return;

            if (!_controllerResolved)
            {
                _controller = Mission.Current.GetMissionBehavior<AmbushMissionController>();
                _controllerResolved = true;
            }
            if (_controller == null || !_controller.IsInitialPatrolDefeated) return;

            bool commandMode = Input.IsGameKeyDown(ShowIndicatorsGameKey)
                               || Mission.Current.IsOrderMenuOpen;
            if (!commandMode) return;

            if (!TryGetExtractionWorldPos(out var worldPos)) return;

            var orderController = Mission.Current.PlayerTeam?.PlayerOrderController;
            if (orderController == null || orderController.SelectedFormations.Count == 0) return;

            // Project the zone to screen. Behind-camera guards from
            // the earlier custom marker view apply.
            float screenX = 0f, screenY = 0f, screenW = 0f;
            MBWindowManager.WorldToScreen(MissionScreen.CombatCamera,
                                           worldPos + HeightOffset,
                                           ref screenX, ref screenY, ref screenW);
            if (!MathF.IsValidValue(screenW) || screenW <= 0f
                || !MathF.IsValidValue(screenX) || !MathF.IsValidValue(screenY))
                return;

            // Cursor-vs-marker focus distance. Vanilla formation marker
            // uses a similar pixel-radius check keyed on screen center;
            // we use the actual cursor position because our target is
            // a specific point, not the center-of-screen attention
            // cone.
            var mouse = Input.GetMousePositionPixel();
            float dx = mouse.x - screenX;
            float dy = mouse.y - screenY;
            float distSq = dx * dx + dy * dy;
            if (distSq > FocusPixelRadius * FocusPixelRadius) return;

            // InputKey.LeftMouseButton = 112. IsKeyReleased matches
            // vanilla's GauntletOrderUIBase.TickInput pattern — fires
            // on button-up so a click-drag doesn't double-apply.
            if (!Input.IsKeyReleased(InputKey.LeftMouseButton)) return;

            var worldPosition = new WorldPosition(Mission.Current.Scene, worldPos);
            orderController.SetOrderWithPosition(OrderType.Move, worldPosition);
        }

        private static bool TryGetExtractionWorldPos(out Vec3 position)
        {
            var zones = AmbushRetreatZone.ActiveZones;
            if (zones.Count > 0 && zones[0] != null)
            {
                position = zones[0].Position;
                return true;
            }
            if (AmbushRetreatZone.ProceduralFallbackPosition.HasValue)
            {
                position = AmbushRetreatZone.ProceduralFallbackPosition.Value;
                return true;
            }
            position = Vec3.Zero;
            return false;
        }
    }
}
