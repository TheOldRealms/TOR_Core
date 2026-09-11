using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TOR_Core.Extensions;

namespace TOR_Core.CampaignMechanics.TORCustomSettlement.Component;

public abstract class BaseRaiderSpawnerComponent : TORBaseSettlementComponent
{
    public virtual int BattlePartySize => 250;
    public int RaidingPartyCount => OwnerClan.WarPartyComponents.Count;//Sly : this should have their parties as the raiding party comp derives from war party comp
    public abstract string BattleSceneName { get; }
    public bool IsBattleUnderway { get; set; }
    public abstract List<string> RewardItemIds { get; }
    public abstract MobileParty SpawnNewParty(Settlement initialTarget);
}