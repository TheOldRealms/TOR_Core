using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem.Party;
using TOR_Core.Extensions;

namespace TOR_Core.CampaignMechanics.TORCustomSettlement.Component;

public abstract class BaseRaiderSpawnerComponent : TORBaseSettlementComponent
{
    public virtual int BattlePartySize => 250;
    public int RaidingPartyCount => MobileParty.All.Where(x => x.IsRaidingParty() && x.HomeSettlement == Settlement).Count(); //Sly : that's a lot of parties being checked
    public abstract string BattleSceneName { get; }
    public bool IsBattleUnderway { get; set; }
    public abstract List<string> RewardItemIds { get; }
    public abstract void SpawnNewParty(out MobileParty party, Settlement initialTarget);
}