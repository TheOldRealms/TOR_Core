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
using TaleWorlds.LinQuick;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;
using TOR_Core.CharacterDevelopment;
using TOR_Core.Extensions;
using TOR_Core.Utilities;
using FaceGen = TaleWorlds.Core.FaceGen;

namespace TOR_Core.CampaignMechanics.RaiseDead
{
    public class PostBattleCampaignBehavior : CampaignBehaviorBase
    {
        private List<CharacterObject> _raiseableCharacters = new List<CharacterObject>();
        private List<CharacterObject> _treeSpiritUnits = new();

        public override void RegisterEvents()
        {
            CampaignEvents.OnAfterSessionLaunchedEvent.AddNonSerializedListener(this, InitializeRaiseableCharacters);
            CampaignEvents.OnPlayerBattleEndEvent.AddNonSerializedListener(this, PostBattleEvent);                //Those events are never executed when the player lose a battle!
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
                    var choice =  TORCareerChoices.GetChoice("BookOfSigmarPassive3");
                    
                    hero.Heal((int) choice.GetPassiveValue(),false);
                }
            }
        }


        private void PostBattleEvent(MapEvent mapEvent)
        {
            
            if (Hero.MainHero.Culture.StringId == TORConstants.Cultures.ASRAI)
            {
                
                var heroes = Hero.MainHero.PartyBelongedTo.GetMemberHeroes();

                if (!heroes.Any(x => x.IsSpellCaster() && x.HasKnownLore("LoreOfLife")))
                {
                    return;
                }
                var settlements = TORCommon.FindSettlementsAroundPosition(MobileParty.MainParty.Position2D, 200);
                if (!settlements.AnyQ(x => x.Culture.StringId == TORConstants.Cultures.ASRAI && x.StringId .Contains("_AL")))
                {
                       return;
                }

                var t = GetAthelLorenTreeSpiritUnits();

                foreach (var character in t)
                {
                    PlayerEncounter.Current.RosterToReceiveLootMembers.AddToCounts(character,1);
                }
               

            }
            
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
            _raiseableCharacters = characters.Where(character => character.IsUndead()&& !character.HasAttribute("NecromancerChampion") && character.IsBasicTroop && character.Race == TaleWorlds.Core.FaceGen.GetRaceOrDefault("skeleton")).ToList();

            _treeSpiritUnits = characters.Where(x => x.Culture.StringId == TORConstants.Cultures.ASRAI && x.Race != FaceGen.GetRaceOrDefault("elf") && x.Race != FaceGen.GetRaceOrDefault("human")).ToList();
        }

        private List<CharacterObject> CalculateBloodKnightsCandidates(MapEvent mapEvent, out int reduced)
        {
            reduced = 0;
            List<CharacterObject> elements = new List<CharacterObject>();

            CharacterObject BloodKnightTemplate = MBObjectManager.Instance.GetObject<CharacterObject>("tor_bd_blooddragon_initiate");        //I assume that needs change, beware

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
                        if (MBRandom.RandomFloat >= 0.05f) continue;
                        elements.Add(BloodKnightTemplate);
                        reduced++;
                    }
                    else
                    {
                        if (MBRandom.RandomFloat >= 0.1f) continue;
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
            double raiseDeadChance = 0;

            raiseDeadChance= Hero.MainHero.PartyBelongedTo.GetMemberHeroes().Select(hero => hero.GetRaiseDeadChance()).Max();
            
            for (int i = 0; i <= num; i++)
            {
                if(MBRandom.RandomFloat <= raiseDeadChance)
                {
                    elements.Add(_raiseableCharacters.GetRandomElement());
                }
            }
            return elements;
        }
        
        private List<CharacterObject> GetAthelLorenTreeSpiritUnits(int reduction = 0){
            List<CharacterObject> elements = new List<CharacterObject>();

            var num = 5;

            var gainChance = 0.5f;
           // var gainChance= Hero.MainHero.PartyBelongedTo.GetMemberHeroes().Select(hero => hero.GetRaiseDeadChance()).Max();
            
            for (int i = 0; i <= num; i++)
            {
                if(MBRandom.RandomFloat <= gainChance)
                {
                    elements.Add(_treeSpiritUnits.GetRandomElement());
                }
            }

            return elements;
        }

        public override void SyncData(IDataStore dataStore)
        {
            
        }
    }
}
