using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TOR_Core.CampaignMechanics.Religion;
using TOR_Core.Extensions;

namespace TOR_Core.CampaignMechanics.TORCustomSettlement.Component;

public class ShrineComponent : TORBaseSettlementComponent
{
    public static int DEVOTION_FOLLOWER_TROOPS_MIN = 3;
    public static int DEVOTION_FOLLOWER_TROOPS_MAX = 7;
    public static int DEVOTION_DEVOTED_TROOPS_MIN = 7;
    public static int DEVOTION_DEVOTED_TROOPS_MAX = 11;
    public static int DEVOTION_FANATIC_TROOPS_MIN = 11;
    public static int DEVOTION_FANATIC_TROOPS_MAX = 15;

    public override IFaction MapFaction => Settlement.Owner.Clan;

    
    public override void OnPartyEntered(MobileParty party)
    {
        base.OnPartyEntered(party);

        if (party == null || party.LeaderHero == null || party == MobileParty.MainParty) return;
        var leaderHero = party.LeaderHero;

        //with no check for the religion of the hero, a dedicated player can take nobles into their army and slowly change their religion by taking them to shrines that will give influence from a different religion than their current dominant one
        party.AddBlessingToParty(Religion.StringId);

        // AI religious troop recruitment from shrines
        if (Religion.ReligiousTroops == null || Religion.ReligiousTroops.Count <= 0)
        {
            return;
        }

        var heroReligion = leaderHero.GetDominantReligion();

        if (heroReligion != Religion)
        {
            return;
        }

        var freeSlots = party.Party.PartySizeLimit - party.MemberRoster.TotalManCount;
        if (freeSlots <= 0)
        {
            return;
        }

        var troop = Religion.ReligiousTroops.FirstOrDefault(x => x.IsBasicTroop && x.Occupation == Occupation.Soldier);
        if (troop == null)
        {
            return;
        }

        var devotion = leaderHero.GetDevotionLevelForReligion(heroReligion);
        int troopCount = GetTroopCountByDevotion(devotion);
        if (troopCount > 0)
        {
            if (freeSlots < troopCount) troopCount = freeSlots;
            party.MemberRoster.AddToCounts(troop, troopCount);
            CampaignEventDispatcher.Instance.OnTroopRecruited(leaderHero, Settlement, null, troop, troopCount);
        }
    }

    private int GetTroopCountByDevotion(DevotionLevel devotionLevel)
    {
        return devotionLevel switch
        {
            DevotionLevel.Follower => MBRandom.RandomInt(DEVOTION_FOLLOWER_TROOPS_MIN, DEVOTION_FOLLOWER_TROOPS_MAX),
            DevotionLevel.Devoted => MBRandom.RandomInt(DEVOTION_DEVOTED_TROOPS_MIN, DEVOTION_DEVOTED_TROOPS_MAX),
            DevotionLevel.Fanatic => MBRandom.RandomInt(DEVOTION_FANATIC_TROOPS_MIN, DEVOTION_FANATIC_TROOPS_MAX),
            _ => 0, // No troops for Skeptic/Believer
        };
    }
}
