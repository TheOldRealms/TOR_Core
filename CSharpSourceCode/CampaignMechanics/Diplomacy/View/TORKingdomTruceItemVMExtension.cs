using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ViewModelCollection.KingdomManagement.Diplomacy;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;
using TOR_Core.Extensions.UI;

namespace TOR_Core.CampaignMechanics.Diplomacy.View
{
    [ViewModelExtension(typeof(KingdomTruceItemVM))]
    public class TORKingdomTruceItemVMExtension : BaseViewModelExtension
    {
        [DataSourceProperty]
        public bool IsAllianceVisible { get; set; }
        [DataSourceProperty]
        public bool IsAllianceAvailable { get; set; }
        [DataSourceProperty]
        public int AllianceInfluenceCost { get; set; }
        [DataSourceProperty]
        public int AllianceGoldCost { get; set; }
        [DataSourceProperty]
        public string AllianceActionName = GameTexts.FindText("str_diplomacy_form_alliance_label").ToString();

        [DataSourceProperty]
        public HintViewModel AllianceHint {  get; set; }
        [DataSourceProperty]
        public BasicTooltipViewModel AllianceScoreHint { get; set; }

        protected readonly Kingdom _faction1;
        protected readonly Kingdom _faction2;
        protected readonly bool _isAlliance;

        public TORKingdomTruceItemVMExtension(ViewModel vm) : base(vm)
        {
            var view = (KingdomTruceItemVM)vm;
            _faction1 = (Kingdom)view.Faction1;
            _faction2 = (Kingdom)view.Faction2;

            _isAlliance = _faction1.GetStanceWith(_faction2).IsAllied;
        }
    }
}
