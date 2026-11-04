using System;
using TaleWorlds.Library;

namespace TOR_Core.Missions.AmbushMission.UI
{
    /// <summary>
    /// One cell under a group column in the spot-picker. Displays the
    /// global spot number and fires the selection callback on click.
    /// </summary>
    public class AmbushSpotPickerItemVM : ViewModel
    {
        private readonly Action<AmbushSpotPickerItemVM> _onSelected;
        private int _number;
        private bool _isCurrent;

        public AmbushSpotPickerItemVM(int number, bool isCurrent,
                                       Action<AmbushSpotPickerItemVM> onSelected)
        {
            _number = number;
            _isCurrent = isCurrent;
            _onSelected = onSelected;
        }

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
        public bool IsCurrent
        {
            get => _isCurrent;
            set
            {
                if (value == _isCurrent) return;
                _isCurrent = value;
                OnPropertyChangedWithValue(value, nameof(IsCurrent));
            }
        }

        public void ExecuteSelect() => _onSelected?.Invoke(this);
    }
}
