using SandBox.ViewModelCollection;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TOR_Core.Extensions.UI;
using TOR_Core.Missions.AmbushMission;

namespace TOR_Core.CampaignMechanics.Ambush
{
    /// <summary>Scoreboard overlay for ambush missions. Hides the result banner + Done button until the resolution is terminal.</summary>
    [ViewModelExtension(typeof(SPScoreboardVM), "OnTick")]
    public class AmbushScoreboardVMExtension : BaseViewModelExtension
    {
        private bool _isAmbushResultVisible = true;

        /// <summary>True when the result banner + Done button should render.</summary>
        [DataSourceProperty]
        public bool IsAmbushResultVisible
        {
            get => _isAmbushResultVisible;
            set
            {
                if (value == _isAmbushResultVisible) return;
                _isAmbushResultVisible = value;
                _vm?.OnPropertyChangedWithValue(value, nameof(IsAmbushResultVisible));
            }
        }

        public AmbushScoreboardVMExtension(ViewModel vm) : base(vm) { }

        public override void RefreshValues()
        {
            base.RefreshValues();
            IsAmbushResultVisible = ComputeVisibility();
        }

        private static bool ComputeVisibility()
        {
            var mission = Mission.Current;
            if (mission == null) return true;

            bool isAmbush = mission.GetMissionBehavior<AmbushEndLogic>() != null
                            || mission.GetMissionBehavior<AmbushedEndLogic>() != null;
            if (!isAmbush) return true;

            var behavior = Campaign.Current?.GetCampaignBehavior<AmbushCampaignBehavior>();
            if (behavior == null) return false;

            var resolution = behavior.LastResolution;
            if (resolution == AmbushResolution.AmbusherTotalVictory) return true;
            if (resolution == AmbushResolution.AmbusherDefeated
                && !behavior.LastAmbusherLeaderDown) return true;
            if (resolution == AmbushResolution.VictimDefeated
                && !behavior.LastVictimLeaderDown) return true;
            return false;
        }
    }
}
