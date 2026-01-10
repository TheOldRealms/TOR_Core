using Helpers;
using SandBox;
using SandBox.GauntletUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Issues;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.ViewModelCollection.Party;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.LinQuick;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ScreenSystem;
using TaleWorlds.TwoDimension;
using TOR_Core.CharacterDevelopment;
using TOR_Core.CharacterDevelopment.CareerSystem;
using TOR_Core.Extensions;
using TOR_Core.Extensions.UI;
using TOR_Core.Models;
using TOR_Core.Utilities;
using static Helpers.PartyScreenHelper;

namespace TOR_Core.CampaignMechanics.CustomResources
{
    public class CustomResourceManager
    {
        public static CustomResourceManager Instance { get; private set; }
        private readonly Dictionary<string, CustomResource> _resources = [];
        private ScreenBase _currentPartyScreen;
        private PartyVM _currentPartyVM;
        private readonly List<Tuple<string, int>> _resourceChanges = [];
        private float _initialCombatRatio;
        private readonly Dictionary<string, int> _massBudget = new(); // naber: budget has to be calculated beforehand

        private CustomResourceManager() { }

        public static void Initialize()
        {
            Instance = new CustomResourceManager();
            Instance._resources.Clear();
            Instance._resources.Add("Prestige",
                new CustomResource("Prestige", "prestige_icon_45", TORConstants.Cultures.EMPIRE));
            Instance._resources.Add("Chivalry",
                new CustomResource("Chivalry", "chivalry_icon_45", TORConstants.Cultures.BRETONNIA, ChivalryHelper.GetChivalryInfo));
            Instance._resources.Add("DarkEnergy",
                new CustomResource("DarkEnergy", "darkenergy_icon_45", [TORConstants.Cultures.SYLVANIA, TORConstants.Cultures.MOUSILLON]));
            Instance._resources.Add("ForestHarmony",
                new CustomResource("ForestHarmony", "harmony_icon_45", TORConstants.Cultures.ASRAI, ForestHarmonyHelper.GetForestHarmonyInfo));
            Instance._resources.Add("CouncilFavor",
                new CustomResource("CouncilFavor", "favor_icon_45", TORConstants.Cultures.EONIR, FavorHelper.GetFavorInfo));
            Instance._resources.Add("OathGold",
                new CustomResource("OathGold", "oathgold_icon_45", TORConstants.Cultures.DAWI, OathGoldHelper.GetOathGoldInfo));
            Instance._resources.Add("Teef",
                new CustomResource("Teef", "teef_icon_45", TORConstants.Cultures.GREENSKIN, TeefHelper.GetTeefInfo));
            Instance._resources.Add("Meat",
                new CustomResource("Meat", "meat_icon_45"));
            Instance._resources.Add("Waaagh",
                new CustomResource("Waaagh", "winds_icon_45", null)); //do not associate helper resources to a culture otherwise using hero.'GetCultureResource' can return the wrong one - it expects a single resource per culture
            Instance._resources.Add("WindsOfMagic",
                new CustomResource("WindsOfMagic", "winds_icon_45"));
        }

        public static CustomResource GetResourceObject(string id)
        {
            if (Instance._resources.TryGetValue(id, out CustomResource resource))
            {
                return resource;
            }
            else return null;
        }

        public static CustomResource GetResourceObject(Func<List<CustomResource>, CustomResource> query)
        {
            return query([.. Instance._resources.Values]);
        }

        public static bool DoesResourceObjectExist(string id)
        {
            return Instance._resources.TryGetValue(id, out _);
        }

        public static void RegisterEvents()
        {
            ScreenManager.OnPushScreen += ScreenManager_OnPushScreen;
            ScreenManager.OnPopScreen += ScreenManager_OnPopScreen;
            Instance.RegisterCampaignEvents();
        }

        private void RegisterCampaignEvents()
        {
            CampaignEvents.OnMissionStartedEvent.AddNonSerializedListener(this, InitialCombatStrengthCalculation);
            CampaignEvents.OnPlayerBattleEndEvent.AddNonSerializedListener(this, CalculateCustomResourceGainFromBattles);
            CampaignEvents.OnHideoutBattleCompletedEvent.AddNonSerializedListener(this, CalculateHideOutCompletedGain);
            CampaignEvents.HeroPrisonerReleased.AddNonSerializedListener(this, PrisonerReleasedChange);
            CampaignEvents.TournamentFinished.AddNonSerializedListener(this, TournamentFinishedChange);
            CampaignEvents.HeroLevelledUp.AddNonSerializedListener(this, OnHeroLevelUp);
            CampaignEvents.OnIssueUpdatedEvent.AddNonSerializedListener(this, OnIssueSolved);
        }

        private void OnIssueSolved(IssueBase issue, IssueBase.IssueUpdateDetails issueState, Hero hero)
        {
            if (issueState != IssueBase.IssueUpdateDetails.IssueFinishedWithSuccess) return;

            if (hero.Clan != null && hero.Clan == Clan.PlayerClan)
            {
                var customResourceGain = new ExplainedNumber(15);

                if (Hero.MainHero.Culture.StringId == TORConstants.Cultures.SYLVANIA || Hero.MainHero.Culture.StringId == TORConstants.Cultures.MOUSILLON)
                {
                    customResourceGain.AddFactor(0.25f);
                }

                Hero.MainHero.AddCultureSpecificCustomResource(customResourceGain.ResultNumber);
            }
        }

        private void OnHeroLevelUp(Hero hero, bool shouldNotify)
        {
            if (hero.Clan != null && hero.Clan == Clan.PlayerClan)
            {
                var customResourceGain = new ExplainedNumber(10);

                if (Hero.MainHero.Culture.StringId == TORConstants.Cultures.SYLVANIA || Hero.MainHero.Culture.StringId == TORConstants.Cultures.MOUSILLON)
                {
                    customResourceGain.AddFactor(0.25f);
                }

                Hero.MainHero.AddCultureSpecificCustomResource(customResourceGain.ResultNumber);
            }
        }

        private void TournamentFinishedChange(CharacterObject winner, MBReadOnlyList<CharacterObject> participants,
            Town settlement, ItemObject wonItem)
        {
            if (winner.IsPlayerCharacter)
            {
                // Trigger arena fight won event for quest tracking
                TORCampaignEvents.Instance.OnArenaFightWon(Hero.MainHero);

                if (winner.Culture.StringId == TORConstants.Cultures.BRETONNIA)
                {
                    if (settlement.Culture.StringId == TORConstants.Cultures.BRETONNIA)
                    {
                        Hero.MainHero.AddCultureSpecificCustomResource(40);
                    }
                }
                if (winner.Culture.StringId == TORConstants.Cultures.DAWI)
                {
                    if (settlement.Culture.StringId == TORConstants.Cultures.DAWI)
                    {
                        if (winner.HasAttribute("DwarfWarriorIII"))
                        {
                            Hero.MainHero.AddCultureSpecificCustomResource(25);
                        }
                        else if (winner.HasAttribute("DwarfWarriorII"))
                        {
                            Hero.MainHero.AddCultureSpecificCustomResource(20);
                        }
                        else if (winner.HasAttribute("DwarfWarriorI"))
                        {
                            Hero.MainHero.AddCultureSpecificCustomResource(15);
                        }
                    }
                }
            }
        }

        private void PrisonerReleasedChange(Hero prisoner, PartyBase party, IFaction faction, EndCaptivityDetail detail, bool arg5)
        {
            var explainedNumber = new ExplainedNumber();
            if (party == PartyBase.MainParty && detail == EndCaptivityDetail.ReleasedByChoice)
            {
                if (Hero.MainHero.Culture.StringId == TORConstants.Cultures.BRETONNIA)
                {
                    explainedNumber.Add(50);

                    if (prisoner.Culture.StringId == TORConstants.Cultures.BRETONNIA)
                    {
                        explainedNumber.AddFactor(1);
                    }
                }

                Hero.MainHero.AddCultureSpecificCustomResource(explainedNumber.ResultNumber);
            }
        }

        private void CalculateHideOutCompletedGain(BattleSideEnum battleSideEnum, HideoutEventComponent eventComponent)
        {
            var hideout = eventComponent.MapEvent.MapEventSettlement;
            if (eventComponent.MapEvent.PlayerSide == eventComponent.MapEvent.WinningSide)
            {
                if (Hero.MainHero.Culture.StringId == TORConstants.Cultures.SYLVANIA) return;

                var explainedNumber = new ExplainedNumber();

                explainedNumber.Add(20);

                if (Hero.MainHero.Culture.StringId == TORConstants.Cultures.BRETONNIA)
                {
                    var settlement = TORCommon.FindNearestSettlement(MobileParty.MainParty, 50f,
                        x => x.IsTown && x.IsBretonnianMajorSettlement());

                    if (settlement != null)
                    {
                        explainedNumber.AddFactor(1);
                    }
                }

                if (Hero.MainHero.Culture.StringId == TORConstants.Cultures.EMPIRE)
                {

                    var settlement = TORCommon.FindNearestSettlement(MobileParty.MainParty, 50f,
                        x => x.IsTown && x.Culture.StringId == TORConstants.Cultures.EMPIRE);

                    if (settlement != null)
                    {
                        explainedNumber.AddFactor(0.5f);
                    }
                }

                if (Hero.MainHero.Culture.StringId == TORConstants.Cultures.ASRAI && MobileParty.MainParty.InAthelLoren())
                {
                    explainedNumber.AddFactor(2);
                }

                Hero.MainHero.AddCultureSpecificCustomResource(explainedNumber.ResultNumber);
            }
        }

        private void InitialCombatStrengthCalculation(IMission mission)
        {
            if (Campaign.Current != null)
            {
                _initialCombatRatio = 0;
                var playerEvent = Campaign.Current.MainParty.MapEvent;

                if (playerEvent == null) return;

                playerEvent.GetStrengthsRelativeToParty(playerEvent.PlayerSide, out float playerStrength,
                    out float enemyStrength);

                if (enemyStrength > 0)
                {
                    _initialCombatRatio = playerStrength / enemyStrength;
                }
            }
        }

        private void CalculateCustomResourceGainFromBattles(MapEvent mapEvent) //PlayerBattleEndEvent
        {
            var playerHero = Hero.MainHero;
            var playerParty = MobileParty.MainParty;
            var playerCulture = playerHero.Culture;
            var defeatedSide = mapEvent.GetMapEventSide(mapEvent.DefeatedSide); //crash when defeated in hideout battle
            mapEvent.GetBattleRewards(playerParty.Party, out var renownChange, out _, out _, out _, out _);


            //Sly : each resource is assigned to a single culture, and if they weren't, GetCultureSpecificCustomResource using FirstOrDefault could cause issues by returning a secondary resource rather than a primary one
            if (playerCulture.StringId == TORConstants.Cultures.EMPIRE ||
                playerCulture.StringId == TORConstants.Cultures.BRETONNIA ||
                playerCulture.StringId == TORConstants.Cultures.EONIR ||
                playerCulture.StringId == TORConstants.Cultures.ASRAI ||
                playerCulture.StringId == TORConstants.Cultures.DAWI)
            {
                var fairBattleOrPlayerInferior = _initialCombatRatio < 1.1f;

                if (playerParty.HasBlessing("cult_of_sigmar")) renownChange *= 1.2f;



                if (playerHero.HasCareerChoice("SquiresPassive4"))
                {
                    foreach (var party in defeatedSide.Parties)
                    {
                        if (party.Party.LeaderHero != null && (party.Party.LeaderHero.CharacterObject.Race != 0))
                        {
                            if (renownChange > 0)
                            {
                                renownChange *= 2;
                            }

                            break;
                        }
                    }
                }

                if (playerHero.HasCareerChoice("HolyPurgePassive2"))
                {
                    foreach (var party in defeatedSide.Parties)
                        if (party.Party.LeaderHero != null && (party.Party.LeaderHero.IsChaos() || party.Party.LeaderHero.IsVampire()))
                        {
                            var choice = TORCareerChoices.GetChoice("HolyPurgePassive2");
                            var value = choice.Passive.EffectMagnitude;
                            if (choice.Passive.InterpretAsPercentage) value /= 100;
                            renownChange *= value;

                            break;
                        }
                }

                if (playerHero.HasCareerChoice("HuntTheWickedPassive2"))
                {
                    var evilCultures = new[] { TORConstants.Cultures.CHAOS, TORConstants.Cultures.BEASTMEN, TORConstants.Cultures.SYLVANIA, TORConstants.Cultures.MOUSILLON };
                    foreach (var party in defeatedSide.Parties)
                    {
                        if (evilCultures.Contains(party.Party.Culture.StringId))
                        {
                            var choice = TORCareerChoices.GetChoice("HuntTheWickedPassive2");
                            var value = choice.GetPassiveValue() / 100f;
                            renownChange *= (1 + value);
                            break;
                        }
                    }
                }

                if (playerHero.HasCareerChoice("UnrestrictedMagicPassive3"))
                {
                    var choice = TORCareerChoices.GetChoice("UnrestrictedMagicPassive3");
                    if (choice != null)
                    {
                        var bonus = choice.GetPassiveValue();
                        if (choice.Passive.InterpretAsPercentage) bonus /= 100;
                        renownChange *= (1 + bonus);
                    }
                }

                if (playerHero.HasCareerChoice("CollegeOrdersPassive3"))
                {
                    var heroes = playerParty.GetMemberHeroes();
                    heroes.Remove(playerHero);
                    heroes.RemoveAll(x => x.Culture.StringId != TORConstants.Cultures.EMPIRE);

                    var bonus = 1f;
                    if (heroes.Any(x => x.HasKnownLore("LoreOfMetal")))
                        bonus += 0.1f;

                    if (heroes.Any(x => x.HasKnownLore("LoreOfFire")))
                        bonus += 0.1f;

                    if (heroes.Any(x => x.HasKnownLore("LoreOfHeavens")))
                        bonus += 0.1f;

                    if (heroes.Any(x => x.HasKnownLore("LoreOfLife")))
                        bonus += 0.1f;

                    if (heroes.Any(x => x.HasKnownLore("LoreOfBeasts")))
                        bonus += 0.1f;

                    if (heroes.Any(x => x.HasKnownLore("LoreOfLight")))
                        bonus += 0.1f;

                    renownChange *= bonus;

                }

                if (playerCulture.StringId == TORConstants.Cultures.ASRAI && playerParty.InAthelLoren() && defeatedSide.Parties.AnyQ(x => x.Party.Culture.StringId == TORConstants.Cultures.BEASTMEN))
                {
                    renownChange *= 3;
                }

                if (fairBattleOrPlayerInferior)
                {
                    if (playerHero.HasCareerChoice("FuryOfWarPassive3")) renownChange *= 2f;
                    if (playerHero.HasCareerChoice("FlameOfUlricPassive3"))
                    {
                        var model = Campaign.Current.Models.GetFaithModel();

                        model.AddBlessingToParty(playerParty, "cult_of_ulric");
                    }
                }

                if (playerCulture.StringId == TORConstants.Cultures.DAWI)
                {
                    var hasgrudge = false;
                    foreach (var party in defeatedSide.Parties) //the attribute could be checked first, then loop (or call external method to do it) rather than checking each party for every attribute
                    {
                        if (playerHero.HasAttribute("HumanGrudge"))
                        {
                            var humanCultures = new[] { TORConstants.Cultures.EMPIRE, TORConstants.Cultures.BRETONNIA };

                            if (party.Party.MobileParty != null && party.Party.MobileParty.IsBandit)
                            {
                                if (party.Troops.Any(elem => elem.Troop.Race == 0))
                                {
                                    hasgrudge = true;
                                }

                            }
                            else
                            {
                                if (humanCultures.Contains(party.Party.Culture.StringId))
                                {
                                    hasgrudge = true;
                                    break;
                                }
                            }
                        }

                        if (playerHero.HasAttribute("GreenskinGrudge"))
                        {
                            var greenskinCultures = new[] { TORConstants.Cultures.GREENSKIN };
                            if (greenskinCultures.Contains(party.Party.Culture.StringId))
                            {
                                hasgrudge = true;
                                break;
                            }
                        }

                        if (playerHero.HasAttribute("UndeadGrudge"))
                        {
                            var undeadCultures = new[] { TORConstants.Cultures.SYLVANIA, TORConstants.Cultures.MOUSILLON };
                            if (undeadCultures.Contains(party.Party.Culture.StringId))
                            {
                                hasgrudge = true;
                                break;
                            }
                        }

                        if (playerHero.HasAttribute("ElfGrudge"))
                        {
                            var elfCultures = new[] { TORConstants.Cultures.ASRAI, TORConstants.Cultures.EONIR, TORConstants.Cultures.DRUCHII };
                            if (elfCultures.Contains(party.Party.Culture.StringId))
                            {
                                hasgrudge = true;
                                break;
                            }
                        }

                        if (playerHero.HasAttribute("SkavenGrudge"))     //Temporary ;)
                        {
                            hasgrudge = TORCommon.FindSettlementsAroundPosition(mapEvent.Position.ToVec2(), 30, (x) => (x.Culture.StringId == TORConstants.Cultures.GREENSKIN && x.IsTown || x.IsVillage) ||
                                x.Culture.StringId == TORConstants.Cultures.DAWI && x.IsDwarfKarak()).AnyQ();
                        }

                        if (hasgrudge)
                        {
                            renownChange *= 2;
                        }

                        if (playerHero.HasAttribute("DwarfWarriorIII"))
                        {
                            renownChange *= 3f;
                        }
                        else if (playerHero.HasAttribute("DwarfWarriorII"))
                        {
                            renownChange *= 2;
                        }
                        else if (playerHero.HasAttribute("DwarfWarriorI"))
                        {
                            renownChange *= 1.5f;
                        }
                    }
                }

                if (playerHero.HasCareerChoice("MercenaryLordPassive4"))
                {
                    var leadershipSkill = playerHero.GetSkillValue(DefaultSkills.Leadership);
                    var leadershipFactor = 1f + (leadershipSkill / 300f);
                    renownChange *= leadershipFactor;
                }

                playerHero.AddCultureSpecificCustomResource((int)(1 + renownChange));
            }
        }

        private static void ScreenManager_OnPopScreen(ScreenBase poppedScreen)
        {
            if (poppedScreen == Instance._currentPartyScreen) Instance._currentPartyScreen = null;
        }

        private static void ScreenManager_OnPushScreen(ScreenBase pushedScreen)
        {
            if (pushedScreen is GauntletPartyScreen)
            {
                Instance._currentPartyScreen = pushedScreen;
                ResetResourcesAndRefillInitially();
                PartyScreenHelper.GetActivePartyState().PartyScreenLogic.PartyScreenClosedEvent += PartyScreenLogic_PartyScreenClosedEvent;
                PartyScreenHelper.GetActivePartyState().PartyScreenLogic.AfterReset += PartyScreenLogic_AfterReset;
            }
        }
        
        private static int CalculateTeefFromBattle(MapEvent mapEvent)
        {
            if (Hero.MainHero.Culture.StringId != TORConstants.Cultures.GREENSKIN)
            {
                return 0;
            }
            
            int totalTeef = 0;
            const int BaseTeefPerKill = 2;

            var partiesOnSide = mapEvent.PartiesOnSide(mapEvent.DefeatedSide);
            
            foreach (var party in partiesOnSide)
            {
                // Get killed troops from the defeated side
                var killedTroops = party.Troops.Where(x => x.IsKilled);

                foreach (var killed in killedTroops)
                {
                    // Teef based on tier: higher tier enemies = more valuable loot
                    int teefFromTroop = BaseTeefPerKill * Math.Min(1, killed.Troop.Tier);

                    if (killed.Troop.IsHero)
                    {
                        teefFromTroop += 50;
                    }
                    totalTeef += teefFromTroop;
                }
            }
            
            if (mapEvent.GetLeaderParty(mapEvent.PlayerSide) != PartyBase.MainParty)
            {
                var partyCount = mapEvent.PartiesOnSide(mapEvent.PlayerSide).Count;
                totalTeef /= partyCount;
            }

            return totalTeef;
        }

        private static void ResetResourcesAndRefillInitially()
        {
            Instance._resourceChanges.Clear();
            Instance._massBudget.Clear();
            AddInitialChange();
        }

        private static void AddInitialChange()
        {

            var mapEvent = MapEvent.PlayerMapEvent;

            if (mapEvent == null) return;

            if (PartyScreenHelper.GetActivePartyState().PartyScreenLogic == null) return;

            if (PartyScreenHelper.GetActivePartyState().PartyScreenMode != PartyScreenMode.Loot) return;


            var result = 0f;

            var prisoners = PartyScreenHelper.GetActivePartyState().PartyScreenLogic.PrisonerRosters[0];

            var leftMemberRoster = PartyScreenHelper.GetActivePartyState().PartyScreenLogic.MemberRosters[0];

            if (Hero.MainHero.Culture.StringId == TORConstants.Cultures.ASRAI)
            {
                foreach (var element in prisoners.GetTroopRoster())
                {
                    if (element.Character.Culture.StringId != TORConstants.Cultures.BEASTMEN) continue;

                    result += 3 * element.Number;
                }
            }

            if (Hero.MainHero.Culture.StringId == TORConstants.Cultures.SYLVANIA || Hero.MainHero.Culture.StringId == TORConstants.Cultures.MOUSILLON)
            {
                if ((Hero.MainHero.IsNecromancer() || Hero.MainHero.CanRaiseDead()))
                {
                    if (Hero.MainHero.PartyBelongedTo is MobileParty playerParty && playerParty.MapEvent != null) //Sly : not sure of the goal of determining the player's party via MainHero when MapEvent.PlayerMapEvent was previously checked
                    {
                        var totalCasualties = playerParty.MapEvent.GetMapEventSide(playerParty.MapEvent.DefeatedSide).TroopCasualties;
                        result += Math.Max(0, totalCasualties - prisoners.ToFlattenedRoster().Count());

                        // Bonus dark energy from killing greenskins (they can't be raised, but fuel dark magic)
                        result += CalculateGreenskinDarkEnergyBonus(playerParty.MapEvent);
                    }

                    if (leftMemberRoster != null && leftMemberRoster.Count > 0)
                    {
                        result += AdjustBattleSpoilsForDarkEnergy(leftMemberRoster);
                    }

                    if (prisoners != null && prisoners.Count > 0)
                    {
                        result += AdjustBattleSpoilsForDarkEnergy(prisoners, true);
                    }
                }
            }


            if (Hero.MainHero.Culture.StringId == TORConstants.Cultures.BRETONNIA)
            {
                var prisonerRoster = prisoners.GetTroopRoster();
                if (prisonerRoster.Any())
                {
                    var explainedNumber = new ExplainedNumber();
                    foreach (var element in prisonerRoster)
                    {
                        AddChivarlyForUnit(ref explainedNumber, element.Character, element.Number);
                    }
                }
            }

            // Meat from killed troops
            var meatGained = CalculateMeatFromBattle(mapEvent);
            if (meatGained > 0)
            {
                var meatResource = GetResourceObject("Meat");
                if (meatResource != null)
                {
                    AddResourceChanges(meatResource, -meatGained);
                }
            }

            // Teef from killed troops (Greenskins only)
            if (Hero.MainHero.Culture.StringId == TORConstants.Cultures.GREENSKIN)
            {
                var teefGained = CalculateTeefFromBattle(mapEvent);
                if (teefGained > 0)
                {
                    var teefResource = GetResourceObject("Teef");
                    if (teefResource != null)
                    {
                        AddResourceChanges(teefResource, -teefGained);
                    }
                }
            }

            AddResourceChanges(Hero.MainHero.GetCultureSpecificCustomResource(), -(int)result);
        }

        private static void PartyScreenLogic_AfterReset(PartyScreenLogic partyScreenLogic, bool fromCancel)
        {
            ResetResourcesAndRefillInitially();
            PartyVMExtension.ViewModelInstance?.GetExtensionInstance()?.RefreshValues();
        }

        private static void PartyScreenLogic_PartyScreenClosedEvent(PartyBase leftOwnerParty,
            TroopRoster leftMemberRoster, TroopRoster leftPrisonRoster, PartyBase rightOwnerParty,
            TroopRoster rightMemberRoster, TroopRoster rightPrisonRoster, bool fromCancel)
        {
            PartyScreenHelper.GetActivePartyState().PartyScreenLogic.PartyScreenClosedEvent -= PartyScreenLogic_PartyScreenClosedEvent;
            PartyScreenHelper.GetActivePartyState().PartyScreenLogic.AfterReset -= PartyScreenLogic_AfterReset;
            if (!fromCancel)
            {
                foreach (var tuple in Instance._resourceChanges)
                {
                    if (tuple.Item1 == "Meat")
                    {
                        var meatItem = DefaultItems.Meat;
                        Hero.MainHero.PartyBelongedTo.ItemRoster.AddToCounts(meatItem, -tuple.Item2);
                        Hero.MainHero.AddCustomResource("Meat", -1000);
                        continue;
                    }
                    Hero.MainHero.AddCustomResource(tuple.Item1, -tuple.Item2);
                    var value = Hero.MainHero.GetCustomResourceValue("Meat");
                    Hero.MainHero.AddCustomResource("Meat", -value);
                }
            }
            Instance._resourceChanges.Clear();
            Instance._massBudget.Clear();
        }


        private static float AdjustBattleSpoilsForDarkEnergy(TroopRoster leftUnits, bool isPrisoner = false)
        {
            var explainedNumber = new ExplainedNumber();
            foreach (var troop in leftUnits.GetTroopRoster().ToList())
            {
                if (troop.Character.IsHero) continue;

                AddDarkEnergyForUnit(ref explainedNumber, troop.Character, troop.Number);
            }
            return explainedNumber.ResultNumber;
        }

        private static void AddDarkEnergyForUnit(ref ExplainedNumber number, CharacterObject characterObject, int amount)
        {
            if (characterObject.IsHero) return;

            var tier = characterObject.Tier;
            var value = amount * tier;
            number.Add(value);
        }

        private static float CalculateGreenskinDarkEnergyBonus(MapEvent mapEvent)
        {
            float bonus = 0f;
            var partiesOnSide = mapEvent.PartiesOnSide(mapEvent.DefeatedSide);

            foreach (var party in partiesOnSide)
            {
                var killedTroops = party.Troops.Where(x => x.IsKilled);
                foreach (var rosterMember in killedTroops)
                {
                    if (rosterMember.Troop.IsGreenskin())
                    {
                        bonus += rosterMember.Troop.Tier;
                    }
                }
            }
            return bonus;
        }

        private static void AddChivarlyForUnit(ref ExplainedNumber number, CharacterObject characterObject, int amount)
        {
            if (!characterObject.IsHuman()) return;

            if (characterObject.Culture.IsBandit)
            {
                number.Add(1 * amount);
                return;
            }

            number.Add(2 * amount);

            if (characterObject.IsKnightUnit())
            {
                number.Add(2 * amount);
            }
        }

        private static int CalculateMeatFromBattle(MapEvent mapEvent)
        {
            if (Hero.MainHero.Culture.StringId != TORConstants.Cultures.GREENSKIN)
                return 0;
            
            int rawMeat = 0;

            var partiesOnSide = mapEvent.PartiesOnSide(mapEvent.DefeatedSide);

            // First collect all potential meat
            foreach (var party in partiesOnSide)
            {
                var killedTroops = party.Troops.Where(x => x.IsKilled);

                foreach (var killed in killedTroops)
                {
                    // No meat from undead
                    if (killed.Troop.IsUndead())
                        continue;

                    // 5 meat chances for large targets, 1 for regular
                    if (killed.Troop.IsLargeTarget())
                    {
                        rawMeat += 5;
                        continue;
                    }

                    if (killed.Troop.IsBeastman())
                    {
                        rawMeat += 2;
                        continue;
                    }

                    rawMeat += 1;
                    continue;
                }
            }

            // Apply random 15%-30% of meat chances
            float meatPercent = MBRandom.RandomFloatRanged(0.15f, 0.35f);
            int totalMeat = (int)(rawMeat * meatPercent);

            return totalMeat;
        }

        public static void OnPartyScreenTroopUpgrade(PartyVM partyVM, PartyScreenLogic.PartyCommand command)
        {
            if (Instance._currentPartyVM != partyVM) Instance._currentPartyVM = partyVM;
            if (command.Code == PartyScreenLogic.PartyCommandCode.UpgradeTroop)
            {
                CharacterObject troopToUpgrade = command.Character;
                CharacterObject upgradeTarget = troopToUpgrade.UpgradeTargets[command.UpgradeTarget];
                var requirement = upgradeTarget.GetCustomResourceRequiredForUpgrade(true);

                if (requirement != null)
                {
                    AddResourceChanges(requirement.Item1, requirement.Item2 * command.TotalNumber);
                    partyVM.GetExtensionInstance().RefreshValues();
                }
            }
        }

        public static void AddResourceChanges(CustomResource resource, int amount)
        {
            Instance._resourceChanges.Add(new Tuple<string, int>(resource.StringId, amount));
            PartyVMExtension.ViewModelInstance?.GetExtensionInstance()?.RefreshValues();
        }


        public static Dictionary<CustomResource, int> GetPendingResources()
        {
            var dictionary = new Dictionary<CustomResource, int>();
            foreach (var item in Instance._resourceChanges)
            {
                var resource = GetResourceObject(item.Item1);
                if (dictionary.ContainsKey(resource))
                {
                    dictionary[resource] += item.Item2;
                }
                else dictionary.Add(resource, item.Item2);
            }

            return dictionary;
        }

        public static void OnTroopTransferred(PartyVM partyVm, PartyScreenLogic.PartyRosterSide fromSide, CharacterObject troop, int transferAmount, bool isPrisoner = false)
        {
            int sign = fromSide == PartyScreenLogic.PartyRosterSide.Left ? 1 : -1;
            var explainedNumber = new ExplainedNumber();

            if (Hero.MainHero.Culture.StringId == TORConstants.Cultures.ASRAI)
            {
                if (troop.Culture.StringId == TORConstants.Cultures.BEASTMEN)
                {
                    explainedNumber.Add(3 * transferAmount);
                }
            }

            if (Hero.MainHero.IsNecromancer() || Hero.MainHero.CanRaiseDead())
            {
                //rosterElement.Number = 1;
                AddDarkEnergyForUnit(ref explainedNumber, troop, transferAmount);
            }

            if (Hero.MainHero.Culture.StringId == TORConstants.Cultures.BRETONNIA && isPrisoner)
            {
                AddChivarlyForUnit(ref explainedNumber, troop, transferAmount);
            }

            AddResourceChanges(Hero.MainHero.GetCultureSpecificCustomResource(), sign * (int)explainedNumber.ResultNumber);

            partyVm.GetExtensionInstance().RefreshValues();
        }

        public static void OverridePendingResources(Dictionary<CustomResource, int> spendMap)
        {
            Instance._resourceChanges.Clear();
            foreach (var entry in spendMap)
            {
                var resourceId = entry.Key.StringId;
                var amount = entry.Value;
                Instance._resourceChanges.Add(new Tuple<string, int>(resourceId, amount));
            }
            PartyVMExtension.ViewModelInstance?.GetExtensionInstance()?.RefreshValues();
        }

        public static int GetPendingFor(string resourceId)
        {
            int sum = 0;
            foreach (var change in Instance._resourceChanges)
                if (change.Item1 == resourceId) sum += change.Item2;
            return sum;
        }

        public static int ClampMassUpgradeAndConsume(string resourceId, int unitCost, int requestedCount)
        {
            // remaining budget
            if (!Instance._massBudget.TryGetValue(resourceId, out var remainingBudget))
            {
                int currentBalance = (int)Hero.MainHero.GetCustomResourceValue(resourceId);
                int pendingReserved = GetPendingFor(resourceId);
                remainingBudget = currentBalance - pendingReserved;
            }

            int affordableUnits = remainingBudget / unitCost;           // int division, floor
            int appliedUnits = Math.Max(0, Math.Min(requestedCount, affordableUnits));
            int appliedCost = appliedUnits * unitCost;

            // update it
            remainingBudget -= appliedCost;
            Instance._massBudget[resourceId] = remainingBudget;

            if (appliedCost > 0)
            {
                var resource = GetResourceObject(resourceId);
                if (resource != null) AddResourceChanges(resource, appliedCost);    // +cost, pending, will be subtracted on closing the screen
            }

            return appliedUnits;
        }

        public static void ResetMassBudget() => Instance._massBudget.Clear();



    }
}