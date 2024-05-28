using Helpers;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.ObjectSystem;
using TOR_Core.Extensions;
using TOR_Core.Utilities;

namespace TOR_Core.CampaignMechanics.RegimentsOfRenown
{
    public class RORCampaignBehavior : CampaignBehaviorBase
    {
        public override void RegisterEvents()
        {
            CampaignEvents.SettlementEntered.AddNonSerializedListener(this, SettlementEntered);
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
        }

        private void OnSessionLaunched(CampaignGameStarter obj)
        {
            
        }

        private void SettlementEntered(MobileParty mobileParty, Settlement settlement, Hero hero)
        {
            if (mobileParty != null &&
                mobileParty.IsLordParty &&
                mobileParty != MobileParty.MainParty &&
                !mobileParty.IsDisbanding &&
                mobileParty.LeaderHero != null &&
                mobileParty.LeaderHero.CanPlaceArtillery() &&
                mobileParty.LeaderHero.Culture.StringId == TORConstants.Cultures.EMPIRE &&
                !mobileParty.Party.IsStarving &&
                mobileParty.MapFaction.IsKingdomFaction &&
                mobileParty.Party.NumberOfAllMembers < mobileParty.LimitedPartySize &&
                mobileParty.CanPayMoreWage() &&
                mobileParty.LeaderHero.Gold > HeroHelper.StartRecruitingMoneyLimit(mobileParty.LeaderHero) &&
                (mobileParty.LeaderHero == mobileParty.LeaderHero.Clan.Leader || mobileParty.LeaderHero.Clan.Gold > HeroHelper.StartRecruitingMoneyLimitForClanLeader(mobileParty.LeaderHero)))
            {
                var troop = MBObjectManager.Instance.GetObject<CharacterObject>("tor_empire_veteran_artillery_crew");
                var targetNumTroop = MBRandom.RandomInt(2, 4);
                var crewInParty = mobileParty.MemberRoster.GetTroopRoster().Where(x => x.Character.GetAttributes().Contains("ArtilleryCrew"));
                if (mobileParty.Party.NumberOfAllMembers + targetNumTroop <= mobileParty.LimitedPartySize && troop != null && crewInParty.Count() < targetNumTroop)
                {
                    var cost = Campaign.Current.Models.PartyWageModel.GetTroopRecruitmentCost(troop, mobileParty.LeaderHero, false) * targetNumTroop;
                    if (mobileParty.LeaderHero.Gold > cost)
                    {
                        GiveGoldAction.ApplyBetweenCharacters(mobileParty.LeaderHero, null, cost);
                        mobileParty.AddElementToMemberRoster(troop, targetNumTroop);
                        CampaignEventDispatcher.Instance.OnTroopRecruited(mobileParty.LeaderHero, settlement, settlement.Notables.FirstOrDefault(), troop, targetNumTroop);
                    }
                }
            }
        }

        public override void SyncData(IDataStore dataStore) { }
    }
}
