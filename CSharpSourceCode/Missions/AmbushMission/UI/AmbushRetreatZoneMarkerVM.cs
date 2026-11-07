using TaleWorlds.Library;

namespace TOR_Core.Missions.AmbushMission.UI
{
    /// <summary>
    /// Data source for the Gauntlet prefab that renders the retreat-zone
    /// marker on screen. Driven every tick by
    /// <see cref="AmbushRetreatZoneMarkerView.OnMissionScreenTick"/>:
    /// the view projects the zone's world position through the combat
    /// camera and sets <see cref="ScreenX"/> / <see cref="ScreenY"/>.
    /// Visibility is gated on Phase 2 + on-screen.
    /// </summary>
    public class AmbushRetreatZoneMarkerVM : ViewModel
    {
        private bool _isVisible;
        private float _screenX;
        private float _screenY;

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
        public float ScreenX
        {
            get => _screenX;
            set
            {
                if (_screenX == value) return;
                _screenX = value;
                OnPropertyChangedWithValue(value, nameof(ScreenX));
            }
        }

        [DataSourceProperty]
        public float ScreenY
        {
            get => _screenY;
            set
            {
                if (_screenY == value) return;
                _screenY = value;
                OnPropertyChangedWithValue(value, nameof(ScreenY));
            }
        }
    }
}
