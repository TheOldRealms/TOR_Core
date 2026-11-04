using TaleWorlds.Library;

namespace TOR_Core.Missions.AmbushMission.UI
{
    /// <summary>
    /// One scene-overlay marker, rendered as a Gauntlet widget at the
    /// projected screen position of an ambush spot. The view's
    /// OnMissionScreenTick updates ScreenX / ScreenY and IsVisible each
    /// frame. Click handling is a follow-up slice.
    /// </summary>
    public class AmbushSpotMarkerVM : ViewModel
    {
        private readonly AmbushSpot _spot;
        private int _number;
        private float _screenX;
        private float _screenY;
        private bool _isVisible;
        private string _formationLabel;

        public AmbushSpotMarkerVM(AmbushSpot spot, int number)
        {
            _spot = spot;
            _number = number;
            _formationLabel = "";
        }

        public AmbushSpot Spot => _spot;

        [DataSourceProperty]
        public int Number
        {
            get => _number;
            set
            {
                if (value == _number) return;
                _number = value;
                OnPropertyChangedWithValue(value, nameof(Number));
            }
        }

        [DataSourceProperty]
        public float ScreenX
        {
            get => _screenX;
            set
            {
                if (value == _screenX) return;
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
                if (value == _screenY) return;
                _screenY = value;
                OnPropertyChangedWithValue(value, nameof(ScreenY));
            }
        }

        [DataSourceProperty]
        public bool IsVisible
        {
            get => _isVisible;
            set
            {
                if (value == _isVisible) return;
                _isVisible = value;
                OnPropertyChangedWithValue(value, nameof(IsVisible));
            }
        }

        [DataSourceProperty]
        public string FormationLabel
        {
            get => _formationLabel;
            set
            {
                if (value == _formationLabel) return;
                _formationLabel = value;
                OnPropertyChangedWithValue(value, nameof(FormationLabel));
            }
        }
    }
}
