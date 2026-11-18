using SandBox.ViewModelCollection.Nameplate;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;
using TOR_Core.Extensions.UI;

namespace TOR_Core.CampaignMechanics.Ambush
{
    /// <summary>
    /// Surfaces <see cref="AiAmbushBehavior.IsRecentlyPlayerDetected"/>
    /// as a boolean on <see cref="PartyNameplateVM"/> so the campaign-map
    /// nameplate can show an on-party status icon for the duration of
    /// the detection window. Replaces the earlier
    /// <c>AmbushDetectedMapNotification</c> feed-tray entry.
    ///
    /// Hooks <see cref="PartyNameplateVM.RefreshDynamicProperties"/> —
    /// the same callback that writes vanilla's <c>_isDisorganizedBind</c>.
    /// Choosing this hook means the icon updates on the exact same
    /// cadence as every other per-party status flag on the nameplate
    /// (per map frame at active cadence, slower when the map is paused),
    /// so there is no bespoke refresh timer to maintain.
    ///
    /// Save/load safety: <see cref="AiAmbushBehavior._playerDetectedParties"/>
    /// is already SyncData'd. On reload the dict repopulates; the first
    /// nameplate refresh after load reads this extension's RefreshValues
    /// and the icon reappears on the correct parties with correct
    /// remaining window.
    /// </summary>
    [ViewModelExtension(typeof(PartyNameplateVM), nameof(PartyNameplateVM.RefreshDynamicProperties))]
    public class AmbushDetectedNameplateExtension : BaseViewModelExtension
    {
        private bool _isAmbushDetected;

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
