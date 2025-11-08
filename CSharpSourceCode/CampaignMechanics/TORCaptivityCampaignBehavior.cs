using Helpers;
using System;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TOR_Core.Utilities;

namespace TOR_Core.CampaignMechanics
{
    public class TORCaptivityCampaignBehavior : CampaignBehaviorBase
    {
        private readonly float _maximumDaysInCaptivity = 10;

        public override void RegisterEvents()
        {
            CampaignEvents.DailyTickHeroEvent.AddNonSerializedListener(this, OnDailyTick);
        }

        private void OnDailyTick(Hero hero)
        {
            if (hero.IsPrisoner && hero.PartyBelongedToAsPrisoner != null &&
                hero != Hero.MainHero &&
                hero.PartyBelongedToAsPrisoner.Owner != null &&
                hero.PartyBelongedToAsPrisoner.Owner.Clan != Clan.PlayerClan &&
                hero.CaptivityStartTime != null &&
                hero.IsLord)
            {
                var time = CampaignTime.Now;
                var duration = time - hero.CaptivityStartTime;
                if (duration.ToDays > _maximumDaysInCaptivity) EndCaptivityAction.ApplyByEscape(hero, null);
            }

            if(!hero.IsPrisoner && 
                hero.IsLord &&
                hero.GovernorOf == null && 
                hero.PartyBelongedTo == null && 
                hero.PartyBelongedToAsPrisoner == null && 
                hero.CanLeadParty() && 
                hero.IsAlive &&
                hero.LastKnownClosestSettlement != null &&
                hero.IsActive &&
                hero.Age > Campaign.Current.Models.AgeModel.HeroComesOfAge)
            {
                //bugged? wtf, how can this even happen?
                if (TryFindMatchingLordParty(hero, out var party))
                {
                    if (party.LeaderHero == null)
                    {
                        if (party.MemberRoster.Contains(hero.CharacterObject))
                        {
                            TORCommon.Log("Captivity Behavior : Lord's party contained themself despite not being the leader hero : " + hero.Name.ToString(), NLog.LogLevel.Info);
                            party.ChangePartyLeader(hero);
                            return;
                        }
                        else
                        {
                            TORCommon.Log("Captivity Behavior : Lord's party existed, but the lord wasn't in it : " + hero.Name.ToString(), NLog.LogLevel.Info);
                            party.MemberRoster.AddToCounts(hero.CharacterObject, 1, true);
                            party.ChangePartyLeader(hero);
                            return;
                        }
                    }
                }

                MobilePartyHelper.SpawnLordParty(hero, hero.LastKnownClosestSettlement);
            }
        }


        private bool TryFindMatchingLordParty(Hero hero, out MobileParty party)
        {
            party = MobileParty.AllLordParties.FirstOrDefault(x => x.StringId == hero.CharacterObject.StringId + "_party_1");
            return party != null;
        }

        public override void SyncData(IDataStore dataStore) { }
    }
}
