using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.ViewModelCollection.Missions.Objective;
using TOR_Core.Extensions.UI;

namespace TOR_Core.Missions.AmbushMission.UI
{
    /// <summary>
    /// Hover highlight for the extraction objective marker. Vanilla's
    /// <c>MissionObjectiveMarkerVM</c> is display-only; we add an
    /// <see cref="IsExtractionFocused"/> DataSourceProperty the
    /// overridden <c>MissionObjectiveMarkers.xml</c> prefab reads to
    /// render the hover visual.
    ///
    /// Focus state comes from the static
    /// <see cref="AmbushRetreatZoneObjectiveTarget.IsFocused"/> flag
    /// which <see cref="AmbushExtractionOrderHandler"/> sets per tick
    /// based on cursor-vs-marker distance. The extension gates on the
    /// VM's <c>Target</c> type so markers belonging to non-ambush
    /// objectives (quest markers etc.) always return false.
    /// </summary>
    [ViewModelExtension(typeof(MissionObjectiveMarkerVM), "UpdatePosition")]
    public class AmbushExtractionMarkerVMExtension : BaseViewModelExtension
    {
        private bool _isExtractionFocused;

        [DataSourceProperty]
        public bool IsExtractionFocused
        {
            get => _isExtractionFocused;
            set
            {
                if (value == _isExtractionFocused) return;
                _isExtractionFocused = value;
                _vm?.OnPropertyChangedWithValue(value, nameof(IsExtractionFocused));
            }
        }

        public AmbushExtractionMarkerVMExtension(ViewModel vm) : base(vm) { }

        public override void RefreshValues()
        {
            base.RefreshValues();
            var vm = _vm as MissionObjectiveMarkerVM;
            IsExtractionFocused = vm?.Target is AmbushRetreatZoneObjectiveTarget
                                  && AmbushRetreatZoneObjectiveTarget.IsFocused;
        }
    }
}
