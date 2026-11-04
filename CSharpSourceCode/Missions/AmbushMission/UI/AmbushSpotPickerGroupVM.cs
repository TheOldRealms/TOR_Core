using TaleWorlds.Library;

namespace TOR_Core.Missions.AmbushMission.UI
{
    /// <summary>
    /// One column in the spot-picker grid. Header carries the localized
    /// group name (e.g. "Hill", "Street"); Items is the vertical list of
    /// numbered spot buttons that belong to that group.
    /// </summary>
    public class AmbushSpotPickerGroupVM : ViewModel
    {
        private string _header;
        private MBBindingList<AmbushSpotPickerItemVM> _items
            = new MBBindingList<AmbushSpotPickerItemVM>();

        public AmbushSpotPickerGroupVM(string header)
        {
            _header = header;
        }

        [DataSourceProperty]
        public string Header
        {
            get => _header;
            set
            {
                if (value == _header) return;
                _header = value;
                OnPropertyChangedWithValue(value, nameof(Header));
            }
        }

        [DataSourceProperty]
        public MBBindingList<AmbushSpotPickerItemVM> Items
        {
            get => _items;
            set
            {
                if (value == _items) return;
                _items = value;
                OnPropertyChangedWithValue(value, nameof(Items));
            }
        }
    }
}
