using TaleWorlds.CampaignSystem.Settlements;
using TOR_Core.CampaignMechanics.Religion;

namespace TOR_Core.CampaignMechanics.TORCustomSettlement.SettlementTypes
{
    public class Shrine : BaseSettlementType
    {
        private ReligionObject _religion;
        private Settlement _settlement;
        private TORCustomSettlementComponent _component;
        public ReligionObject Religion => _religion;

        public override void OnInit(Settlement settlement, ReligionObject religion)
        {
            _settlement = settlement;
            _component = settlement.SettlementComponent as TORCustomSettlementComponent;
            _religion = religion;
        }
    }
}
