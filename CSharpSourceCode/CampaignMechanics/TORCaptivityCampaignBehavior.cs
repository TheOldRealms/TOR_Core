using Helpers;
using System;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TOR_Core.Extensions;
using TOR_Core.Utilities;

namespace TOR_Core.CampaignMechanics
{
    public class TORCaptivityCampaignBehavior : CampaignBehaviorBase
    {
        private const float _maximumDaysInCaptivity = 10;

        public override void RegisterEvents()
        {
            CampaignEvents.DailyTickHeroEvent.AddNonSerializedListener(this, OnDailyTick);
            CampaignEvents.CanHeroDieEvent.AddNonSerializedListener(this, CanHeroDie);
        }

        
        private void CanHeroDie(Hero dyingHero, KillCharacterAction.KillCharacterActionDetail causeOfDeath, ref bool result)
        {
            //Sly : prevents some hero deaths; I still need to look up what npc-npc execution details are. (maybe execution after battle?)
            //Lost is for deleting notables (village raided, no workshops) and wanderers (companions fired).
            //ExecutionAfterMapEvent is when the player kills someone via dialog option in the conversation after a map event where they choose to take a lord prisoner or not.
            if (!dyingHero.IsWanderer && !dyingHero.IsLord && !dyingHero.IsAICompanion())
            {
                return;
            }
            if (causeOfDeath != KillCharacterAction.KillCharacterActionDetail.Executed && causeOfDeath != KillCharacterAction.KillCharacterActionDetail.Lost && causeOfDeath != KillCharacterAction.KillCharacterActionDetail.ExecutionAfterMapEvent)
            {
                result = false;
            }
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

            ////Sly: spawning the lord with a party right next to where they just fought a siege battle and lost but weren't taken prisoner is not a great solution
            ////This looks like it was intended to fix the issues of special ai companions ending up stuck in the original party and preventing its destruction, but opting to spawn a new party for every noble the day after they are released is far too large of a fix, and is likely unnecessary now that ai companions don't get stuck in parties.
            //if (!hero.IsPrisoner &&
            //    hero.IsLord &&
            //    hero.GovernorOf == null &&
            //    hero.PartyBelongedTo == null &&
            //    hero.PartyBelongedToAsPrisoner == null &&
            //    hero.CanLeadParty() &&
            //    hero.IsAlive &&
            //    hero.LastKnownClosestSettlement != null &&
            //    hero.IsActive &&
            //    hero.Age > Campaign.Current.Models.AgeModel.HeroComesOfAge)
            //{
            //    //bugged? wtf, how can this even happen?
            //    if (TryFindMatchingLordParty(hero, out var party))
            //    {
            //        if (party.LeaderHero == null)
            //        {
            //            if (party.MemberRoster.Contains(hero.CharacterObject))
            //            {
            //                TORCommon.Log("Captivity Behavior : Lord's party contained themself despite not being the leader hero : " + hero.Name.ToString(), NLog.LogLevel.Info);
            //                party.ChangePartyLeader(hero);
            //                return;
            //            }
            //            else
            //            {
            //                TORCommon.Log("Captivity Behavior : Lord's party existed, but the lord wasn't in it : " + hero.Name.ToString(), NLog.LogLevel.Info);
            //                party.MemberRoster.AddToCounts(hero.CharacterObject, 1, true);
            //                party.ChangePartyLeader(hero);
            //                return;
            //            }
            //        }
            //    }

            //    MobilePartyHelper.SpawnLordParty(hero, hero.LastKnownClosestSettlement);
            //}


            //If a hero is executed in conversation after a map event when the player has the option to take them prisoner, they are marked for death. Native handles the actual killing on a DailyTickHeroEvent in AgingCampaignBehavior, which we disable, and so those heroes aren't actually executed. This replaces that missing functionality.
            if (!hero.IsTemplate && hero.IsAlive && hero.DeathMark == KillCharacterAction.KillCharacterActionDetail.ExecutionAfterMapEvent && hero.CanDie(KillCharacterAction.KillCharacterActionDetail.ExecutionAfterMapEvent))
            {
                    TORCommon.Log("TORCaptivityCampaignBehavior : killing a hero with a death mark of type ExecutionAfterMapEvent : " + hero.Name, NLog.LogLevel.Info);
                    KillCharacterAction.ApplyByDeathMark(hero, false);
            }
        }

        //Sly : this can be deleted in the future with the code that used to call it as ai companions should no longer be preventing the destruction of their lord's original party.
        private bool TryFindMatchingLordParty(Hero hero, out MobileParty party)
        {
            party = MobileParty.AllLordParties.FirstOrDefault(x => x.StringId == hero.CharacterObject.StringId + "_party_1");
            return party != null;
        }

        public override void SyncData(IDataStore dataStore) { }
    }
}