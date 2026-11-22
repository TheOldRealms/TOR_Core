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
        private bool _isEnabled;

        public AmbushSpotPickerItemVM(int number, bool isCurrent, bool isEnabled,
                                       Action<AmbushSpotPickerItemVM> onSelected)
        {
            _number = number;
            _isCurrent = isCurrent;
            _isEnabled = isEnabled;
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

        [DataSourceProperty]
        public bool IsEnabled
        {
            get => _isEnabled;
            set
            {
                if (value == _isEnabled) return;
                _isEnabled = value;
                OnPropertyChangedWithValue(value, nameof(IsEnabled));
            }
        }

        public void ExecuteSelect()
        {
            if (!_isEnabled) return;
            _onSelected?.Invoke(this);
        }
    }
}
