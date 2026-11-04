using System.Collections.Generic;
using TaleWorlds.Library;

namespace TOR_Core.Missions.AmbushMission.UI
{
    /// <summary>
    /// Root view model for the ambusher deployment screen. Carries the header
    /// and the list of scene-spot markers. Sidebar panels are vanilla's OoB
    /// panels augmented via AmbushFormationPanelExtension.
    /// </summary>
    public class AmbushDeploymentVM : ViewModel
    {
        private string _headerText;
        private MBBindingList<AmbushSpotMarkerVM> _spotMarkers;

        public AmbushDeploymentVM(AmbushLayoutResult layout)
        {
            _headerText = "Ambush Deployment";
            _spotMarkers = new MBBindingList<AmbushSpotMarkerVM>();
            PopulateMarkers(layout);
        }

        private void PopulateMarkers(AmbushLayoutResult layout)
        {
            if (layout == null) return;
            var seen = new HashSet<Vec3>();
            int number = 1;
            foreach (var kv in layout.DefaultAssignments)
            {
                var spot = kv.Value;
                if (!seen.Add(spot.Position)) continue;
                SpotMarkers.Add(new AmbushSpotMarkerVM(spot, number));
                number++;
            }
        }

        [DataSourceProperty]
        public string HeaderText
        {
            get => _headerText;
            set
            {
                if (value == _headerText) return;
                _headerText = value;
                OnPropertyChangedWithValue(value, nameof(HeaderText));
            }
        }

        [DataSourceProperty]
        public MBBindingList<AmbushSpotMarkerVM> SpotMarkers
        {
            get => _spotMarkers;
            set
            {
                if (value == _spotMarkers) return;
                _spotMarkers = value;
                OnPropertyChangedWithValue(value, nameof(SpotMarkers));
            }
        }
    }
}
