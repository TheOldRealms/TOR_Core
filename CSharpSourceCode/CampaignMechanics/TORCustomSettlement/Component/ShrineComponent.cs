using TaleWorlds.CampaignSystem;

namespace TOR_Core.CampaignMechanics.TORCustomSettlement.Component;

//Sly : OnPartyEntered can be implemented as an override here to handle blessing on Ai parties directly rather than making use of the SettlementEntered events
public class ShrineComponent : TORBaseSettlementComponent
{
    public override IFaction MapFaction => Settlement.Owner.Clan;
}
