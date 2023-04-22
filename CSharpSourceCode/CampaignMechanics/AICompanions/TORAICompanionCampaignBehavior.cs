using System;
using System.Data;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.AgentOrigins;
using TaleWorlds.CampaignSystem.Party;
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
        private bool _init;
        public override void RegisterEvents()
        {
            //CampaignEvents.DailyTickEvent.AddNonSerializedListener(this,InitializeHeroes);
            CampaignEvents.OnGameLoadFinishedEvent.AddNonSerializedListener(this,OnNewGameStarted);
            CampaignEvents.SettlementEntered.AddNonSerializedListener(this,SetHeroesInKeep2);
            CampaignEvents.OnSettlementLeftEvent.AddNonSerializedListener(this,RemoveHeroesFromKeep);
            CampaignEvents.OnPrisonerReleasedEvent.AddNonSerializedListener(this, );
        }

        private void RemoveHeroesFromKeep(MobileParty mobileParty, Settlement settlement)
        {
            
            if(mobileParty==null||mobileParty.LeaderHero==null)return;
            
            
            
            if (mobileParty.LeaderHero != mobileParty.LeaderHero.Clan.Leader) return;
            if (mobileParty == MobileParty.MainParty) return;
            var leader = mobileParty.LeaderHero;
            
            if(leader.Name.ToString() != "Emperor Karl Franz von Holswig-Schliestein")
                return;
            
            if (!(settlement.IsCastle || settlement.IsTown)) return;
            var location = settlement.LocationComplex.GetLocationWithId("lordshall");
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
            
            foreach (var companion in hero.Clan.Heroes)
            {
                if (companion.Occupation == Occupation.Special)
                {
                    if (companion.IsHealthFull())
                    {
                        if(!hero.PartyBelongedTo.GetMemberHeroes().Contains(companion))
                            AddHeroToPartyAction.Apply(companion,hero.PartyBelongedTo,false);
                    }
                }
            }
        }
        
        private void SetHeroesInKeep2(MobileParty mobileParty, Settlement settlement, Hero hero)
        {
            if(mobileParty==null || mobileParty.LeaderHero==null)return;
            
            if (hero != hero.Clan.Leader) return;

            if(hero.Name.ToString() != "Emperor Karl Franz von Holswig-Schliestein")
                return;
            
            if (mobileParty == MobileParty.MainParty) return;

            if (!(settlement.IsCastle || settlement.IsTown)) return;
        
            var location = settlement.LocationComplex.GetLocationWithId("lordshall");
            if (location == null) return;
            foreach (var companion in mobileParty.GetMemberHeroes())
            {
                if(companion==hero) continue;
                if(companion.Occupation == Occupation.Lord) continue;
                LocationCharacter locationCharacter = new LocationCharacter(new AgentData(new SimpleAgentOrigin(companion.CharacterObject, -1, null, default(UniqueTroopDescriptor))).Monster(Game.Current.DefaultMonster),
                    new LocationCharacter.AddBehaviorsDelegate(SandBoxManager.Instance.AgentBehaviorManager.AddWandererBehaviors), "npc_common", true, LocationCharacter.CharacterRelations.Neutral, null, true, false, null, false, false, true);
                location.AddCharacter(locationCharacter);
            }


            //SpawnHeroesIfNeeded();
        }
        
        private void SetHeroesInKeep(MobileParty mobileParty, Settlement settlement, Hero hero)
        {
            if (hero != Hero.MainHero) return;
            if(Hero.MainHero.CurrentSettlement == settlement) return;
            
            SpawnHeroesIfNeeded();
        }
        
        private void SpawnHeroesIfNeeded()
        {
            if (Settlement.CurrentSettlement != null && Settlement.CurrentSettlement.IsTown)
            {
                
                if (IsLordPartyInTown(Settlement.CurrentSettlement, out Hero lord))
                {
                    foreach (var hero in lord.CompanionsInParty)
                    {
                        SpawnHeroInKeep(hero,Settlement.CurrentSettlement);
                    }

                    
                }
            }
        }
        
        private bool IsLordPartyInTown(Settlement settlement, out Hero clanLeader)
        {
            var location = settlement.LocationComplex.GetLocationWithId("lordshall");
            foreach (var hero in from hero in Campaign.Current.AliveHeroes where hero.Occupation == Occupation.Lord where hero.Clan.Leader == hero where location.ContainsCharacter(hero) select hero)
            {
                clanLeader = hero;
                return true;
            }
            clanLeader = null;
            return false;
        }
        
        
        private void SpawnHeroInKeep(Hero hero,Settlement settlement)
        {
            
            var keep = settlement.LocationComplex.GetLocationWithId("lordshall");
            
            if (hero != null && keep != null)
            {
                LocationCharacter locationCharacter = new LocationCharacter(new AgentData(new SimpleAgentOrigin(hero.CharacterObject, -1, null, default(UniqueTroopDescriptor))).
                    Monster(Game.Current.DefaultMonster), 
                    new LocationCharacter.AddBehaviorsDelegate(SandBoxManager.Instance.AgentBehaviorManager.AddWandererBehaviors), "npc_common", true, LocationCharacter.CharacterRelations.Neutral, null, true, false, null, false, false, true);
            }
        }
        

        private void OnNewGameStarted()
        {
            
            
        }
        
        private void OnNewGameCreated(CampaignGameStarter obj)
        {
           
        }



        
        

        public override void SyncData(IDataStore dataStore)
        {
            dataStore.SyncData<bool>("_init", ref _init);
        }
        
        
        
        
    }
}