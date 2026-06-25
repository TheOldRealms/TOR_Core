using TaleWorlds.CampaignSystem;

namespace TOR_Core.CampaignMechanics.TORCustomSettlement.Component;

public class WorldRootsComponent : TORBaseSettlementComponent
{
    public override IFaction MapFaction => Settlement.Owner.Clan;
}