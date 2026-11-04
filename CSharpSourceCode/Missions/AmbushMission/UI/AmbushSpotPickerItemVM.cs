using System;
using TaleWorlds.Library;

namespace TOR_Core.Missions.AmbushMission.UI
{
    /// <summary>
    /// One row in the spot-picker dropdown. Carries the localized group
    /// label the player sees and fires the selection callback on click.
    /// The grid layout variant will add a <c>GroupTag</c> / <c>IndexInGroup</c>
    /// pair; for now this stays flat and the chip label is what differentiates.
    /// </summary>
    public class AmbushSpotPickerItemVM : ViewModel
    {
        private readonly Action<AmbushSpotPickerItemVM> _onSelected;
        private string _label;
        private bool _isCurrent;

        public AmbushSpotPickerItemVM(string label, bool isCurrent,
                                       Action<AmbushSpotPickerItemVM> onSelected)
        {
            _label = label;
            _isCurrent = isCurrent;
            _onSelected = onSelected;
        }

        [DataSourceProperty]
        public string Label
        {
            get => _label;
            set
            {
                if (value == _label) return;
                _label = value;
                OnPropertyChangedWithValue(value, nameof(Label));
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
