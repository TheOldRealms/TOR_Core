using TaleWorlds.CampaignSystem;
using TaleWorlds.Core.ViewModelCollection;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.ViewModelCollection.HUD.FormationMarker;
using TOR_Core.Extensions.UI;

namespace TOR_Core.CampaignMechanics.Ambush
{
    /// <summary>Adds the IsBluffCountHidden flag to formation markers so the troop count hides during a bluff.</summary>
    [ViewModelExtension(typeof(MissionFormationMarkerTargetVM), "Refresh")]
    public class AmbushBluffFormationMarkerExtension : BaseViewModelExtension
    {
        private const int ThresholdTroops = 5;

        private bool _isBluffCountHidden;

        [DataSourceProperty]
        public bool IsBluffCountHidden
        {
            get => _isBluffCountHidden;
            set
            {
                if (value == _isBluffCountHidden) return;
                _isBluffCountHidden = value;
                _vm?.OnPropertyChangedWithValue(value, nameof(IsBluffCountHidden));
            }
        }

        public AmbushBluffFormationMarkerExtension(ViewModel vm) : base(vm) { }

        public override void RefreshValues()
        {
            base.RefreshValues();
            IsBluffCountHidden = ComputeBluffHide();
        }

        private bool ComputeBluffHide()
        {
            var behavior = Campaign.Current?.GetCampaignBehavior<AmbushCampaignBehavior>();
            if (behavior == null || !behavior.IsCurrentMissionBluffed) return false;

            var vm = _vm as MissionFormationMarkerTargetVM;
            var formation = vm?.Formation;
            if (formation == null) return false;
            if (formation.CountOfUnits <= ThresholdTroops) return false;

            var playerTeam = Mission.Current?.PlayerTeam;
            if (formation.Team == playerTeam) return false;

            return true;
        }
    }
}
