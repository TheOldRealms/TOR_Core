using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace TOR_Core.Missions.AmbushMission.UI
{
    /// <summary>
    /// Data source for the retreat-zone marker prefab. Driven every tick
    /// by <see cref="AmbushRetreatZoneMarkerView.OnMissionScreenTick"/>:
    /// the view projects the zone's world position through the combat
    /// camera and writes a single <see cref="ScreenPosition"/> Vec2,
    /// matching the vanilla FormationMarker prefab idiom (one Vec2
    /// positions the widget via Position=@Binding, not two offsets on
    /// an aligned widget). IsVisible is the ALT-held + alive-zone gate
    /// (see view for the full predicate list).
    ///
    /// Click routes through ButtonWidget.Command.Click →
    /// <see cref="ExecuteMoveToExtraction"/>, which asks the
    /// PlayerOrderController to issue a Move order on the currently
    /// selected formations toward the world position the view last
    /// cached on <see cref="_zoneWorldPosition"/>. Selection comes from
    /// whatever the player has picked via the vanilla order UI; if
    /// nothing is selected the Move call is a no-op.
    /// </summary>
    public class AmbushRetreatZoneMarkerVM : ViewModel
    {
        private bool _isVisible;
        private Vec2 _screenPosition;
        private string _labelText;
        private string _distanceText;
        private Vec3 _zoneWorldPosition;

        [DataSourceProperty]
        public bool IsVisible
        {
            get => _isVisible;
            set
            {
                if (_isVisible == value) return;
                _isVisible = value;
                OnPropertyChangedWithValue(value, nameof(IsVisible));
            }
        }

        [DataSourceProperty]
        public Vec2 ScreenPosition
        {
            get => _screenPosition;
            set
            {
                if (_screenPosition.x == value.x && _screenPosition.y == value.y) return;
                _screenPosition = value;
                OnPropertyChangedWithValue(value, nameof(ScreenPosition));
            }
        }

        [DataSourceProperty]
        public string LabelText
        {
            get => _labelText;
            set
            {
                if (_labelText == value) return;
                _labelText = value;
                OnPropertyChangedWithValue(value, nameof(LabelText));
            }
        }

        [DataSourceProperty]
        public string DistanceText
        {
            get => _distanceText;
            set
            {
                if (_distanceText == value) return;
                _distanceText = value;
                OnPropertyChangedWithValue(value, nameof(DistanceText));
            }
        }

        /// <summary>
        /// Written by the view each tick with the active retreat zone's
        /// world position. Consumed by <see cref="ExecuteMoveToExtraction"/>
        /// when the player clicks the marker.
        /// </summary>
        public void SetZoneWorldPosition(Vec3 position) => _zoneWorldPosition = position;

        /// <summary>
        /// Bound to the prefab's ButtonWidget Command.Click. Resolves
        /// the player team's OrderController and issues a Move order to
        /// the cached zone position on every currently selected
        /// formation. Vanilla's <c>SetOrderWithPosition(OrderType.Move, ...)</c>
        /// iterates <c>SelectedFormations</c> internally; no-op when the
        /// player hasn't selected any.
        /// </summary>
        public void ExecuteMoveToExtraction()
        {
            var mission = Mission.Current;
            if (mission?.PlayerTeam == null || mission.Scene == null) return;
            var orderController = mission.PlayerTeam.PlayerOrderController;
            if (orderController == null) return;
            var worldPosition = new WorldPosition(mission.Scene, _zoneWorldPosition);
            orderController.SetOrderWithPosition(OrderType.Move, worldPosition);
        }
    }
}
