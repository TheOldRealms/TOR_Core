using SandBox;
using SandBox.GauntletUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
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
using TOR_Core.Models;
using TOR_Core.Utilities;

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

        private CustomResourceManager() { }

        public static void Initialize()
        {
            Instance = new CustomResourceManager();
            Instance._resources.Clear();
            Instance._resources.Add("Prestige",
                new CustomResource("Prestige", "Prestige",
                    "Is used for upgrading special units of the Empire and special actions.", "prestige_icon_45",
                    TORConstants.Cultures.EMPIRE));
            Instance._resources.Add("Chivalry",
                new CustomResource("Chivalry", "Chivalry",
                    "Is used for upgrading special units of Bretonnia and special actions.", "chivalry_icon_45", TORConstants.Cultures.BRETONNIA,
                    ChivalryHelper.GetChivalryInfo));
            Instance._resources.Add("DarkEnergy",
                new CustomResource("DarkEnergy", "Dark Energy",
                    "Dark Energy is used by practitioners of necromancy to raise and upkeep their undead minions.",
                    "darkenergy_icon_45", [TORConstants.Cultures.SYLVANIA, "mousillon"]));
            Instance._resources.Add("ForestHarmony",
                new CustomResource("ForestHarmony", "Forest Harmony",
                    "Forest Binding is used to upgrade and maintain troops of the woodelves, as well as retrieve upgrades at the Oak of Ages.", "harmony_icon_45", TORConstants.Cultures.ASRAI, ForestHarmonyHelper.GetForestHarmonyInfo));
            Instance._resources.Add("CouncilFavor",
                new CustomResource("CouncilFavor", "Eonir Council Favor",
                    "Retrieve power in the Eonir council and use it to your benefit.", "favor_icon_45", TORConstants.Cultures.EONIR,FavorHelper.GetFavorInfo));
            Instance._resources.Add("WindsOfMagic",
                new CustomResource("WindsOfMagic", "Winds of Magic",
                    "Winds of Magic is used by spellcasters to cast spells.", "winds_icon_45"));
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
            CampaignEvents.HeroLevelledUp.AddNonSerializedListener(this,OnHeroLevelUp);
            CampaignEvents.OnIssueUpdatedEvent.AddNonSerializedListener(this, OnIssueSolved);
        }

        private void OnIssueSolved(IssueBase issue, IssueBase.IssueUpdateDetails issueState, Hero hero)
        {
            if(issueState != IssueBase.IssueUpdateDetails.IssueFinishedWithSuccess) return;
            
            if (hero.Clan!=null && hero.Clan == Clan.PlayerClan)
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
            if (hero.Clan!=null && hero.Clan == Clan.PlayerClan)
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
                if (winner.Culture.StringId == TORConstants.Cultures.BRETONNIA)
                {
                    if (settlement.Culture.StringId == TORConstants.Cultures.BRETONNIA)
                    {
                        Hero.MainHero.AddCultureSpecificCustomResource(40);
                    }
                }
            }
        }

        private void PrisonerReleasedChange(Hero prisoner, PartyBase party, IFaction faction, EndCaptivityDetail detail)
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

                var resource = Hero.MainHero.GetCultureSpecificCustomResource();


                var explainedNumber = new ExplainedNumber();

                explainedNumber.Add(20);


                if (Hero.MainHero.Culture.StringId == TORConstants.Cultures.BRETONNIA)
                {
                    var settlement = TORCommon.FindNearestSettlement(MobileParty.MainParty, 150f,
                        x => x.IsTown && x.IsBretonnianMajorSettlement());

                    if (settlement != null)
                    {
                        explainedNumber.AddFactor(1);
                    }
                }
                
                if (Hero.MainHero.Culture.StringId == TORConstants.Cultures.EMPIRE)
                {
                    
                    var settlement = TORCommon.FindNearestSettlement(MobileParty.MainParty, 150f,
                        x => x.IsTown && x.Culture.StringId==TORConstants.Cultures.EMPIRE);

                    if (settlement != null)
                    {
                        explainedNumber.AddFactor(0.5f);
                    }
                }
                
                if (Hero.MainHero.Culture.StringId == TORConstants.Cultures.ASRAI)
                {
                    var settlement = TORCommon.FindNearestSettlement(MobileParty.MainParty, 500f,
                        x =>  x.IsOakOfTheAges());

                    if (settlement != null)
                    {
                        explainedNumber.AddFactor(2);
                    }
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
        
        private void CalculateCustomResourceGainFromBattles(MapEvent mapEvent)
        {
            mapEvent.GetBattleRewards(MobileParty.MainParty.Party, out var renownChange, out _, out _, out _, out _);

            if (MobileParty.MainParty.LeaderHero.GetCultureSpecificCustomResource() == GetResourceObject("Prestige") ||
                MobileParty.MainParty.LeaderHero.GetCultureSpecificCustomResource() == GetResourceObject("Chivalry") ||
                MobileParty.MainParty.LeaderHero.GetCultureSpecificCustomResource() == GetResourceObject("CouncilFavor")||
                MobileParty.MainParty.LeaderHero.GetCultureSpecificCustomResource() == GetResourceObject("ForestHarmony"))
            {
                var fairBattleOrPlayerInferior = _initialCombatRatio < 1.1f;

                if (MobileParty.MainParty.HasBlessing("cult_of_sigmar")) renownChange *= 1.2f;


                if (Hero.MainHero.HasCareerChoice("HolyPurgePassive2"))
                {
                    var eventSide = mapEvent.GetMapEventSide(mapEvent.DefeatedSide);
                    foreach (var party in eventSide.Parties)
                        if (party.Party.LeaderHero!=null && (party.Party.LeaderHero.IsChaos() || party.Party.LeaderHero.IsVampire()))
                        {
                            var choice = TORCareerChoices.GetChoice("HolyPurgePassive2");
                            var value = choice.Passive.EffectMagnitude;
                            if (choice.Passive.InterpretAsPercentage) value /= 100;
                            renownChange *= value;

                            break;
                        }
                }

                if (Hero.MainHero.HasCareerChoice("UnrestrictedMagicPassive3"))
                {
                    renownChange *= 1.2f;
                }
                
                if (Hero.MainHero.HasCareerChoice("CollegeOrdersPassive3"))
                {
                    var eventSide = mapEvent.GetMapEventSide(mapEvent.DefeatedSide);

                    var heroes = Hero.MainHero.PartyBelongedTo.GetMemberHeroes();
                    heroes.Remove(Hero.MainHero);
                    heroes.RemoveAll(x => x.Culture.StringId != "empire");
                    
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
                
                if (Hero.MainHero.Culture.StringId == TORConstants.Cultures.ASRAI && mapEvent.GetMapEventSide(BattleSideEnum.Defender).Parties
                        .AnyQ(x =>
                        {
                            if (x.Party.Culture.StringId != TORConstants.Cultures.BEASTMEN) return false;
                            var oak = Settlement.FindFirst(x => x.StringId == "oak_of_ages");
                            var distance = mapEvent.Position.Distance(oak.Position2D);

                            if (distance <= 250)
                            {
                                return true;
                            }
                            
                            return false;
                        }))
                {
                    renownChange *= 3;
                }

                if (fairBattleOrPlayerInferior)
                {
                    if (Hero.MainHero.HasCareerChoice("FuryOfWarPassive3")) renownChange *= 2f;
                    if (Hero.MainHero.HasCareerChoice("FlameOfUlricPassive3"))
                    {
                        var model = Campaign.Current.Models.GetFaithModel();

                        model.AddBlessingToParty(MobileParty.MainParty, "cult_of_ulric");
                    }
                }

                MobileParty.MainParty.LeaderHero.AddCultureSpecificCustomResource((int)(1 + renownChange));
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
                Instance._resourceChanges.Clear();
                PartyScreenManager.PartyScreenLogic.PartyScreenClosedEvent += PartyScreenLogic_PartyScreenClosedEvent;
                PartyScreenManager.PartyScreenLogic.AfterReset += PartyScreenLogic_AfterReset;
            }
        }

        private static void PartyScreenLogic_AfterReset(PartyScreenLogic partyScreenLogic, bool fromCancel)
        {
            Instance._resourceChanges.Clear();
            Instance._currentPartyVM?.GetExtensionInstance().RefreshValues();
        }

        private static void PartyScreenLogic_PartyScreenClosedEvent(PartyBase leftOwnerParty,
            TroopRoster leftMemberRoster, TroopRoster leftPrisonRoster, PartyBase rightOwnerParty,
            TroopRoster rightMemberRoster, TroopRoster rightPrisonRoster, bool fromCancel)
        {
            PartyScreenManager.PartyScreenLogic.PartyScreenClosedEvent -= PartyScreenLogic_PartyScreenClosedEvent;
            PartyScreenManager.PartyScreenLogic.AfterReset -= PartyScreenLogic_AfterReset;
            if (!fromCancel)
            {
                foreach (var tuple in Instance._resourceChanges)
                {
                    Hero.MainHero.AddCustomResource(tuple.Item1, -tuple.Item2);
                }
            }

            Instance._resourceChanges.Clear();
            if (PartyScreenManager.Instance.CurrentMode != PartyScreenMode.Loot) return;
            if (PlayerEncounter.Current == null) return;
            
            var prisoners = leftPrisonRoster.TotalManCount;
            var result = 0f;

            if ((Hero.MainHero.IsVampire() || Hero.MainHero.CanRaiseDead()) &&
                PartyScreenManager.Instance.CurrentMode == PartyScreenMode.Loot)
            {
                if (Hero.MainHero.PartyBelongedTo.MapEvent != null)
                {
                    var totalCausalties = Hero.MainHero.PartyBelongedTo.MapEvent.GetMapEventSide(BattleSideEnum.Defender).Casualties;
                    result += Math.Max(0,totalCausalties - prisoners);
                }
                
                if (leftMemberRoster != null && leftMemberRoster.Count > 0)
                {
                    result += AdjustBattleSpoilsForDarkEnergy(leftMemberRoster);
                }

                if (leftPrisonRoster != null && leftPrisonRoster.Count > 0)
                {
                    result += AdjustBattleSpoilsForDarkEnergy(leftPrisonRoster, true);
                }

                Hero.MainHero.AddCultureSpecificCustomResource(result);
                return;
            }

            if (Hero.MainHero.Culture.StringId == TORConstants.Cultures.BRETONNIA &&
                PartyScreenManager.Instance.CurrentMode == PartyScreenMode.Loot)
            {
                var prisonerRoster = leftPrisonRoster.ToFlattenedRoster().ToList();
                if (prisonerRoster.Any())
                {
                    foreach (var element in prisonerRoster)
                    {
                        if (!element.Troop.IsHuman()) return;

                        if (element.Troop.Culture.IsBandit)
                        {
                            result += 1;
                            continue;
                        }

                        result += 2f;

                        if (element.Troop.IsKnightUnit())
                        {
                            result += 2;
                        }
                    }
                }

                Hero.MainHero.AddCultureSpecificCustomResource(result);
            }
        }


        private static float AdjustBattleSpoilsForDarkEnergy(TroopRoster leftUnits, bool isPrisoner = false)
        {
            var explainedNumber = new ExplainedNumber();
            float reduction = 5;

            foreach (var troop in leftUnits.GetTroopRoster().ToList())
            {
                if (troop.Character.IsHero) continue;

                var level = troop.Character.Level;

                explainedNumber.Add(level * troop.Number);
            }

            return explainedNumber.ResultNumber / reduction;
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
                    Instance._resourceChanges.Add(new Tuple<string, int>(requirement.Item1.StringId,
                        requirement.Item2 * command.TotalNumber));
                    partyVM.GetExtensionInstance().RefreshValues();
                }
            }
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
    }
}