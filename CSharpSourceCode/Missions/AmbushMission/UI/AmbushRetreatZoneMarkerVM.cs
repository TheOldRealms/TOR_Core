using TaleWorlds.Library;

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
    /// </summary>
    public class AmbushRetreatZoneMarkerVM : ViewModel
    {
        private bool _isVisible;
        private Vec2 _screenPosition;

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
    }
}
