using SandBox.ViewModelCollection.Nameplate;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;
using TOR_Core.Extensions.UI;

namespace TOR_Core.CampaignMechanics.Ambush.UI
{
    /// <summary>
    /// Surfaces <see cref="AiAmbushBehavior.IsRecentlyPlayerDetected"/>
    /// as a boolean on <see cref="PartyNameplateVM"/> so the nameplate
    /// can show an on-party status icon while the detection window lasts.
    /// Refreshes on the same callback vanilla uses for the disorganized flag.
    /// </summary>
    [ViewModelExtension(typeof(PartyNameplateVM), nameof(PartyNameplateVM.RefreshDynamicProperties))]
    public class AmbushDetectedNameplateExtension : BaseViewModelExtension
    {
        private bool _isAmbushDetected;

        /// <summary>
        /// True while the ambush-detection window is active for this party.
        /// </summary>
        [DataSourceProperty]
        public bool IsAmbushDetected
        {
            get => _isAmbushDetected;
            set
            {
                if (value == _isAmbushDetected) return;
                _isAmbushDetected = value;
                _vm?.OnPropertyChangedWithValue(value, nameof(IsAmbushDetected));
            }
        }

        public AmbushDetectedNameplateExtension(ViewModel vm) : base(vm) { }

        /// <summary>
        /// Reads the detection registry and updates the flag.
        /// </summary>
        public override void RefreshValues()
        {
            base.RefreshValues();
            var party = (_vm as PartyNameplateVM)?.Party;
            var ai = Campaign.Current?.GetCampaignBehavior<AiAmbushBehavior>();
            IsAmbushDetected = party != null && ai != null
                               && ai.IsRecentlyPlayerDetected(party);
        }
    }
}
