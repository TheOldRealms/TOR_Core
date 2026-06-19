using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;

namespace TOR_Core.CampaignMechanics.TORCustomSettlement.Component;

public class ShrineComponent : TORBaseSettlementComponent
{
    public override IFaction MapFaction => Settlement.Owner.Clan;
}
