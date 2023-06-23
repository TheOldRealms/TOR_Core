using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;
using TOR_Core.Extensions;
using TOR_Core.Utilities;

namespace TOR_Core.CampaignMechanics.RaiseDead
{
    public class PostBattleCampaignBehavior : CampaignBehaviorBase
    {
        private List<CharacterObject> _raiseableCharacters = new List<CharacterObject>();

        public override void RegisterEvents()
        {
            CampaignEvents.OnAfterSessionLaunchedEvent.AddNonSerializedListener(this, InitializeRaiseableCharacters);
            CampaignEvents.OnPlayerBattleEndEvent.AddNonSerializedListener(this, RaiseDead);                //Those events are never executed when the player lose a battle!
            CampaignEvents.OnPlayerBattleEndEvent.AddNonSerializedListener(this, CheckWarriorPriestPerks);
        }
        

        private void CheckWarriorPriestPerks(MapEvent mapEvent)
        {
            if (Hero.MainHero.HasCareerChoice("BookOfSigmarPassive3"))
            {
                var playerParty = mapEvent.PartiesOnSide(mapEvent.PlayerSide).FirstOrDefault(x => x.Party.LeaderHero == Hero.MainHero); //TODO Main party check might suffies
                if (playerParty == null) return;
                var heroes = playerParty.Party.MobileParty.GetMemberHeroes();
                foreach (var hero in heroes.Where(hero => !hero.IsHealthFull())) 
                {
                    hero.Heal(20,false);
                }
            }
        }


        private void RaiseDead(MapEvent mapEvent)
        {
            if (mapEvent.PlayerSide == mapEvent.WinningSide && Hero.MainHero.CanRaiseDead())
            {
                List<CharacterObject> troops = new List<CharacterObject>();
                var reduction = 0;
                if (Hero.MainHero.HasAnyCareer())
                {
                    if (Hero.MainHero.GetAllCareerChoices().Contains("DoomRiderPassive4"))
                    {
                        var bloodKnights = CalculateBloodKnightsCandidates(mapEvent, out reduction);
                        troops.AddRange(bloodKnights);
                    }
                }
                
                var undeadTroops = CalculateRaiseDeadTroops(mapEvent, reduction);
                troops.AddRange(undeadTroops);
                for (int i = 0; i < troops.Count; i++)
                {
                    PlayerEncounter.Current.RosterToReceiveLootMembers.AddToCounts(troops[i], 1);
                }
            }
        }

        private void InitializeRaiseableCharacters(CampaignGameStarter starter)
        {
            var characters = MBObjectManager.Instance.GetObjectTypeList<CharacterObject>();
            var basic = characters.Where(character => character.IsBasicTroop).ToList();
            foreach(var c in basic)
            {
                var info = c.GetAttributes();
            }
            _raiseableCharacters = characters.Where(character => character.IsUndead() && character.IsBasicTroop).ToList();
        }

        private List<CharacterObject> CalculateBloodKnightsCandidates(MapEvent mapEvent, out int reduced)
        {
            reduced = 0;
            List<CharacterObject> elements = new List<CharacterObject>();

            CharacterObject BloodKnightTemplate = MBObjectManager.Instance.GetObject<CharacterObject>("tor_ror_dragon_knight_initiate");        //I assume that needs change, beware

            var partiesOnSide = mapEvent.PartiesOnSide(mapEvent.DefeatedSide);


            foreach (var party in partiesOnSide)
            {
                var roster = party.Troops.Where(x => x.IsKilled);
                foreach (var rosterMember in roster)
                {
                    if(rosterMember.Troop.IsUndead()||rosterMember.Troop.IsBeastman())
                        continue;
                    
                    if (rosterMember.Troop.Tier<4)
                    {
                        if (MBRandom.RandomFloat >= 0.15f) continue;
                        elements.Add(BloodKnightTemplate);
                        reduced++;
                    }
                    else
                    {
                        if (MBRandom.RandomFloat >= 0.4f) continue;
                        elements.Add(BloodKnightTemplate);
                        reduced++;
                    }
                }
            }
            
            return elements;
        }

        private List<CharacterObject> CalculateRaiseDeadTroops(MapEvent mapEvent, int reduction=0)
        {
            List<CharacterObject> elements = new List<CharacterObject>();
            var num = mapEvent.GetMapEventSide(mapEvent.DefeatedSide).Casualties- reduction;
            double raiseDeadChance = Hero.MainHero.GetRaiseDeadChance();
            for (int i = 0; i <= num; i++)
            {
                if(MBRandom.RandomFloat <= raiseDeadChance)
                {
                    elements.Add(_raiseableCharacters.GetRandomElement());
                }
            }
            return elements;
        }

        public override void SyncData(IDataStore dataStore)
        {
            //throw new NotImplementedException();
        }
    }
}
