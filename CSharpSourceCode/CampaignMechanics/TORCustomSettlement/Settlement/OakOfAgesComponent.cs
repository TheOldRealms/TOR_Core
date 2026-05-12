using TaleWorlds.CampaignSystem;

namespace TOR_Core.CampaignMechanics.TORCustomSettlement.Settlement;

public class OakOfAgesComponent : TORBaseSettlementComponent
{
    public override IFaction MapFaction => Settlement.Owner.Clan;
}