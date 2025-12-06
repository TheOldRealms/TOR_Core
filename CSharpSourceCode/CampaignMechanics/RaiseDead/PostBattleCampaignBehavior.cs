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
using TaleWorlds.ObjectSystem;
using TaleWorlds.TwoDimension;
using TOR_Core.CharacterDevelopment;
using TOR_Core.Extensions;
using TOR_Core.Utilities;

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

            // Greenskin recruitment
            if (mapEvent.PlayerSide == mapEvent.WinningSide && Hero.MainHero.Culture.StringId == TORConstants.Cultures.GREENSKIN)
            {
                var greenskinTroops = CalculateGreenskinRecruitment(mapEvent);
                foreach (var troop in greenskinTroops)
                {
                    PlayerEncounter.Current.RosterToReceiveLootMembers.AddToCounts(troop.Character, troop.Number);

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

        private List<TroopRosterElement> CalculateGreenskinRecruitment(MapEvent mapEvent)
        {
            List<TroopRosterElement> recruits = new List<TroopRosterElement>();

            var partiesOnSide = mapEvent.PartiesOnSide(mapEvent.DefeatedSide);

            foreach (var party in partiesOnSide)
            {

                bool isGreenskinParty = party.Party?.Culture?.StringId == TORConstants.Cultures.GREENSKIN;
                bool isBanditParty = party.Party?.MobileParty?.IsBandit ?? false; //village defense parties don't have a MobileParty

                if (!isGreenskinParty && !isBanditParty)
                    continue;

                var survivors = PlayerEncounter.Current.RosterToReceiveLootPrisoners.GetTroopRoster().Where(x => x.Character.IsGoblin() || x.Character.IsOrc()).ToMBList();

                // For bandits, spawn basic orc/goblin troops
                if (isBanditParty)
                {
                    var totalManCount = PlayerEncounter.Current.RosterToReceiveLootPrisoners.TotalManCount;

                    var troopRoster = party.Troops.ToList();
                    var count = 0;
                    foreach (var element in survivors)
                    {

                        var troopId = "";
                        if (element.Character.IsOrc())
                        {
                            troopId = "tor_gs_orc_boy";
                        }

                        if (element.Character.IsGoblin())
                        {
                            troopId = "tor_gs_goblin";
                        }

                        var troop = MBObjectManager.Instance.GetObject<CharacterObject>(troopId);

                        if (troop == null)
                            continue;

                        var newElement = new TroopRosterElement(troop);
                        newElement.Number = element.Number;
                        recruits.Add(newElement);
                        PlayerEncounter.Current.RosterToReceiveLootPrisoners.AddToCounts(element.Character, -element.Number);
                    }
                }
                else
                {
                    foreach (var rosterMember in survivors)
                    {
                        float recruitChance = 0.25f; // 25% base chance of regular greenskin troops
                        int recruited = 0;

                        for (int i = 0; i < rosterMember.Number; i++)
                        {
                            if (MBRandom.RandomFloat <= recruitChance)
                            {
                                recruited++;
                            }

                        }

                        // Remove recruited prisoners from the prisoner roster
                        if (recruited > 0)
                        {
                            var troop = new TroopRosterElement(rosterMember.Character);
                            troop.Number = recruited;
                            recruits.Add(troop);
                            PlayerEncounter.Current.RosterToReceiveLootPrisoners.AddToCounts(rosterMember.Character, -recruited);
                        }
                    }

                }
            }

            return recruits;
        }

        public override void SyncData(IDataStore dataStore)
        {

        }
    }
}