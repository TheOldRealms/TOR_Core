using System;
using System.Data;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.AgentOrigins;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.Core;
using TaleWorlds.InputSystem;
using TOR_Core.Extensions;
using TOR_Core.Utilities;

namespace TOR_Core.CampaignMechanics.AICompanions
{
    public class TORAICompanionCampaignBehavior : CampaignBehaviorBase
    {
        private const string Keep = "lordshall";
        private bool _init;
        public override void RegisterEvents()
        {
            //CampaignEvents.DailyTickEvent.AddNonSerializedListener(this,InitializeHeroes);
            //CampaignEvents.OnGameLoadFinishedEvent.AddNonSerializedListener(this,OnNewGameStarted);
            CampaignEvents.SettlementEntered.AddNonSerializedListener(this,SetHeroesInKeep);
            CampaignEvents.OnSettlementLeftEvent.AddNonSerializedListener(this,RemoveHeroesFromKeep);
            CampaignEvents.OnPrisonerReleasedEvent.AddNonSerializedListener(this, HandleHeroEscape);
            CampaignEvents.OnPartyDisbandedEvent.AddNonSerializedListener(this, ReturnHeroToTown);
            CampaignEvents.BeforeHeroKilledEvent.AddNonSerializedListener(this,PreventKill);
            CampaignEvents.HeroWounded.AddNonSerializedListener(this,MoveWoundedHeroHome);
        }

        private void MoveWoundedHeroHome(Hero hero)
        {
            if(hero.PartyBelongedTo.IsMainParty) return;
            
            if (hero.IsAICompanion())
            {
                MoveCompanionToHomeTown(hero);
            }
        }

        private void HandleHeroEscape(FlattenedTroopRoster obj)
        {
            if(obj==null|| (obj != null && !obj.Troops.FirstOrDefault().IsHero))
            {
                return;
            }
               
            var characterObject = obj.Troops.FirstOrDefault(x => x.HeroObject.IsAICompanion());
            if(characterObject==null) return;
       //     if(characterObject.HeroObject.PartyBelongedTo.IsMainParty) return;
            
            if(characterObject.HeroObject.Clan!=null)
                TORCommon.Say("test");
        }

        private void PreventKill(Hero victim, Hero killer, KillCharacterAction.KillCharacterActionDetail killCharacterActionDetail, bool notification)
        {
            if(killer==null) return;
            if(killer.IsHumanPlayerCharacter) return;
            
                if (victim.Clan != null && victim.IsAICompanion())
                {
                    MoveCompanionToHomeTown(victim);
                }
        }

        private void MoveCompanionToHomeTown(Hero hero)
        {
            var home = hero.Clan.Leader.HomeSettlement;
            var location = home.LocationComplex.GetLocationWithId(Keep);
            AddLocationCharacterToLocation(hero,location);
        }

        private void ReturnHeroToTown(MobileParty mobileParty, Settlement settlement)
        {
            foreach (var hero in mobileParty.GetMemberHeroes())
            {
                if (hero.IsAICompanion())
                {
                    MoveCompanionToHomeTown(hero);
                }
            }
           
        }

        private void RemoveHeroesFromKeep(MobileParty mobileParty, Settlement settlement)
        {
            
            if(mobileParty==null||mobileParty.LeaderHero==null)return;
            
            
            
            if (mobileParty.LeaderHero != mobileParty.LeaderHero.Clan.Leader) return;
            if (mobileParty == MobileParty.MainParty) return;
            var leader = mobileParty.LeaderHero;



            if (!(settlement.IsCastle || settlement.IsTown)) return;
            var location = settlement.LocationComplex.GetLocationWithId(Keep);
            foreach (var companion in mobileParty.GetMemberHeroes())
            {
                if(companion==leader) continue;
                if(companion.Occupation == Occupation.Lord) continue;

                if (location.ContainsCharacter(companion))
                {
                    var locationCharacter = location.GetLocationCharacter(companion);
                    location.RemoveLocationCharacter(locationCharacter);
                }
                   
                
                
            }
            
            AddHeroesToClanLeader(leader);
        }


        private void AddHeroesToClanLeader(Hero hero)
        {
            var companions = hero.Clan.Heroes.Where(x => x.IsAICompanion());
            if (!companions.Any()) return;
            
            foreach (var companion in companions)
            {
               // if (!companion.IsHealthFull()) continue;
                if(companion.IsPrisoner) continue;
                
                if(!hero.PartyBelongedTo.GetMemberHeroes().Contains(companion))
                    AddHeroToPartyAction.Apply(companion,hero.PartyBelongedTo,false);
            }
        }
        
        private void SetHeroesInKeep(MobileParty mobileParty, Settlement settlement, Hero hero)
        {
            if(mobileParty==null || mobileParty.LeaderHero==null)return;
            
            if (hero != hero.Clan.Leader) return;
            
            if (mobileParty == MobileParty.MainParty) return;

            if (!(settlement.IsCastle || settlement.IsTown)) return;
        
            var location = settlement.LocationComplex.GetLocationWithId(Keep);
            if (location == null) return;
            foreach (var companion in mobileParty.GetMemberHeroes())
            {
                if(companion==hero) continue;
                if(companion.Occupation == Occupation.Lord) continue;
                AddLocationCharacterToLocation(companion,location);
            }
        }

        private static void AddLocationCharacterToLocation(Hero hero, Location location)
        {
            LocationCharacter locationCharacter = new LocationCharacter(new AgentData(new SimpleAgentOrigin(hero.CharacterObject)).Monster(Game.Current.DefaultMonster),
                SandBoxManager.Instance.AgentBehaviorManager.AddWandererBehaviors, "npc_common", true, LocationCharacter.CharacterRelations.Neutral, null, true);
            location.AddCharacter(locationCharacter);
        }
        
        

        public override void SyncData(IDataStore dataStore)
        {
            dataStore.SyncData<bool>("_init", ref _init);
        }
        
        
        
        
    }
}