using TaleWorlds.CampaignSystem;
using TOR_Core.CampaignMechanics.TORCustomSettlement.Settlement;

namespace TOR_Core.CampaignMechanics.TORCustomSettlement
{
    public class WorldRootsComponent : TORBaseSettlementComponent
    {
        public override IFaction MapFaction => Settlement.Owner.Clan;
    }
}