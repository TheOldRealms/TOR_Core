using TaleWorlds.Library;

namespace TOR_Core.Missions.AmbushMission.UI
{
    /// <summary>
    /// Root view model for the ambusher deployment screen. Carries the header
    /// only. Sidebar panels are vanilla's OoB panels augmented via
    /// AmbushFormationPanelExtension; scene spot markers live on the next
    /// slice.
    /// </summary>
    public class AmbushDeploymentVM : ViewModel
    {
        private string _headerText;

        public AmbushDeploymentVM()
        {
            _headerText = "Ambush Deployment";
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
    }
}
