using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.LinQuick;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;
using TaleWorlds.TwoDimension;
using TOR_Core.CampaignMechanics.PostBattleLoot;
using TOR_Core.CharacterDevelopment;
using TOR_Core.Extensions;
using TOR_Core.Utilities;

namespace TOR_Core.CampaignMechanics.RaiseDead
{
    public class PostBattleCampaignBehavior : CampaignBehaviorBase
    {
        private const float GreenskinRecruitChance = 0.25f;

        private List<CharacterObject> _raiseableCharacters = new List<CharacterObject>();
        private List<CharacterObject> _treeSpiritUnits = new();

        public override void RegisterEvents()
        {
            CampaignEvents.OnAfterSessionLaunchedEvent.AddNonSerializedListener(this, InitializeRaiseableCharacters);
            CampaignEvents.OnPlayerBattleEndEvent.AddNonSerializedListener(this, PostBattleEvent);
        }

        private void PostBattleEvent(MapEvent mapEvent)
        {
            if (Hero.MainHero.IsEnlisted())
            {
                return;
            }

            if (Hero.MainHero.Culture.StringId == TORConstants.Cultures.ASRAI)
            {
                //Sly : I feel like a lot of these helpers should put out the member hero list rather than generating it once to check the condition, then again after to make use of it
                if (!TreeSpiritHelpers.CanBindTreeSpirits())
                {
                    return;
                }

                if (!MobileParty.MainParty.InElfForest()) return;

                var heroes = Hero.MainHero.PartyBelongedTo.GetMemberHeroes();
                var spellsinger = heroes.Where(x => x.IsSpellSinger()).MaxBy(x => x.GetSkillValue(TORSkills.Spellcraft));
                var spiritCount = GetTreeSpiritCounts(spellsinger);

                if (spiritCount > 0)
                {
                    if (_treeSpiritUnits.FirstOrDefault() is CharacterObject treeSpirit)
                        PlayerEncounter.Current.RosterToReceiveLootMembers.AddToCounts(treeSpirit, spiritCount);
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

            // Greenskin recruitment and Teef rewards - store for later application in DoLootParty
            // We can't modify prisoner roster here because prisoners don't exist yet
            // PendingLootManager automatically clears old modifications when a new battle starts
            if (mapEvent.PlayerSide == mapEvent.WinningSide && Hero.MainHero.Culture.StringId == TORConstants.Cultures.GREENSKIN)
            {
                var entries = CalculateGreenskinRecruitment(mapEvent);

                // Store troops to add as party members
                foreach (var troop in entries.received.GetTroopRoster())
                {
                    if (troop.Number > 0)
                    {
                        PendingLootedTroopManager.AddPendingMember(troop.Character, troop.Number);
                    }
                }

                // Store troops to remove from prisoner roster
                foreach (var removal in entries.removed)
                {
                    if (removal.Number > 0)
                    {
                        PendingLootedTroopManager.RemovePendingPrisoner(removal.Character, removal.Number);
                    }
                }
            }

        }

        private void InitializeRaiseableCharacters(CampaignGameStarter starter)
        {
            var characters = MBObjectManager.Instance.GetObjectTypeList<CharacterObject>();

            _raiseableCharacters = characters.Where(character => character.IsBasicTroop && character.IsUndead() && character.Race == TaleWorlds.Core.FaceGen.GetRaceOrDefault("skeleton") && !character.HasAttribute("NecromancerChampion")).ToList();

            //extension later
            _treeSpiritUnits.Add(MBObjectManager.Instance.GetObject<CharacterObject>("tor_we_dryad"));
            //Sly : removed for now and fetching directly as there's no point in iterating thousands for a single, known one
            //_treeSpiritUnits = characters.Where(x => x.Culture.StringId == TORConstants.Cultures.ASRAI && x.StringId.Contains("dryad")).ToList();
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
                    if (rosterMember.Troop.IsUndead() || rosterMember.Troop.IsBeastman())
                        continue;

                    if (rosterMember.Troop.Tier < 4)
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

        private List<CharacterObject> CalculateRaiseDeadTroops(MapEvent mapEvent, int reduction = 0)
        {
            List<CharacterObject> elements = new List<CharacterObject>();
            var num = mapEvent.GetMapEventSide(mapEvent.DefeatedSide).TroopCasualties - reduction;
            double raiseDeadChance = 0;

            raiseDeadChance = Hero.MainHero.PartyBelongedTo.GetMemberHeroes().Select(hero => hero.GetRaiseDeadChance()).Max();

            for (int i = 0; i <= num; i++)
            {
                if (MBRandom.RandomFloat <= raiseDeadChance)
                {
                    elements.Add(_raiseableCharacters.GetRandomElement());
                }
            }
            return elements;
        }

        /// <summary>
        /// Returns an int for the number of dryads gained derived from the spellsinger's spellcraft level.
        /// </summary>
        /// <remarks>
        /// If other tree spirits are going to be granted in the future, return a tuple or something. Be aware of the contents of _treeSpiritUnits which is where the character object for the unit is pulled from.
        /// </remarks>
        private int GetTreeSpiritCounts(Hero spellsinger)
        {
            List<CharacterObject> elements = new List<CharacterObject>();
            var maximumNumber = spellsinger.Level / 2;
            var gainChance = TreeSpiritHelpers.GetSuccessChance(spellsinger);
            var spiritsBound = 0;

            for (int i = 0; i <= maximumNumber; i++)
            {
                if (MBRandom.RandomFloat <= gainChance)
                {
                    spiritsBound++;
                }
            }

            return spiritsBound;
        }

        private (TroopRoster received, List<TroopRosterElement> removed) CalculateGreenskinRecruitment(MapEvent mapEvent)
        {
            TroopRoster recruits = TroopRoster.CreateDummyTroopRoster();
            List<TroopRosterElement> removed = [];

            var partiesOnSide = mapEvent.PartiesOnSide(mapEvent.DefeatedSide);

            foreach (var party in partiesOnSide)
            {
                bool isGreenskinParty = party.Party?.Culture?.StringId == TORConstants.Cultures.GREENSKIN;
                bool isBanditParty = party.Party?.MobileParty?.IsBandit ?? false;

                if (!isGreenskinParty && !isBanditParty)
                    continue;

                // Get wounded troops from mapEvent (wounded troops become prisoners)
                var defeatedGreenskins = party.Troops
                    .Where(x => (x.IsWounded  || x.IsRouted) && (x.Troop.IsGoblin() || x.Troop.IsOrc()))
                    .ToList();
                

                foreach (var wounded in defeatedGreenskins)
                {
                    // For bandits, spawn basic orc/goblin troops
                    if (isBanditParty)
                    {
                        var troopId = wounded.Troop.IsOrc() ? "tor_gs_orc_boy" : "tor_gs_goblin";
                        var troop = MBObjectManager.Instance.GetObject<CharacterObject>(troopId);

                        if (troop == null)
                            continue;

                        var newElement = new TroopRosterElement(troop);
                        newElement.Number = 1;
                        recruits.Add(newElement);
                        
                        var removedElement = new TroopRosterElement(wounded.Troop);
                   

                        if (wounded.IsWounded)
                        {
                            removedElement.WoundedNumber = 1;
                        }
                        else
                        {
                            removedElement.Number = 1;
                        }
                        removed.Add(removedElement);

                    }
                    else
                    {
                        if (MBRandom.RandomFloat <= GreenskinRecruitChance)
                        {
                            var troop = new TroopRosterElement(wounded.Troop);
                            troop.Number = 1;
                            recruits.Add(troop);
                            removed.Add(troop);
                        }
                    }
                }
                
                

            }

            // Consolidate removed list - count total troops per character type
            var newRemoved = new List<TroopRosterElement>();
            foreach (var element in removed)
            {
                if (newRemoved.Any(x => x.Character == element.Character))
                    continue;

                // Count total troops to remove (each element in 'removed' represents 1 troop)
                var totalCount = removed.WhereQ(x => x.Character == element.Character).Count();

                var newElement = new TroopRosterElement(element.Character);
                newElement.Number = totalCount;  // Store total count in Number for removal

                newRemoved.Add(newElement);
            }

            return (recruits, newRemoved);

        }



        public override void SyncData(IDataStore dataStore)
        {

        }
    }
}