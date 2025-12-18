using Helpers;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Core.ImageIdentifiers;
using TaleWorlds.Localization;
using TaleWorlds.Localization.TextProcessor;
using TaleWorlds.ObjectSystem;
using TOR_Core.CampaignMechanics.CustomResources;
using TOR_Core.Extensions;
using TOR_Core.Utilities;

namespace TOR_Core.CampaignMechanics.Menagery
{
    public class PrestigeNobleTownBehavior : CampaignBehaviorBase
    {
        private const string _nobleID = "tor_prestige_noble_empire";
        private const int DemigryphCost = 1000;
        private const int RepeatableInfluenceCosts = 25;
        private const int RepeatablePrestigeGain = 15;
        private Settlement _altdorf;
        private Hero _empireNoble;
        private bool _knowsPlayer;
        private bool _receivedDemiGryphen;
        private List<string> _constructedBuildings = new List<string>();
        private readonly List<string> _politicalPowerProjects = new List<string>();

        public override void RegisterEvents()
        {
            CampaignEvents.OnNewGameCreatedEvent.AddNonSerializedListener(this, OnNewGameStarted);
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
        }

        private void OnSessionLaunched(CampaignGameStarter obj)
        {
            InitializeVariables();
            AddPrestigeNobleDialogLines(obj);

            //Setting variables like this puts them into the TextProcessingContext _variables dictionary which is the global back-up if a TextObject contains no local value for a variable
            //for common variables, these should probably be grouped together in a single location rather than each class defining global values separately that may conflict for the same variable name
            void InitializeVariables()
            {
                MBTextManager.SetTextVariable("PRESTIGE_ICON",
                    CustomResourceManager.GetResourceObject("Prestige").GetCustomResourceIconAsText());
                MBTextManager.SetTextVariable("DEMIGRYPH_COST", DemigryphCost.ToString());

                MBTextManager.SetTextVariable("INFLUENCE_ICON", "{=!}<img src=\"General\\Icons\\Influence@2x\" extend=\"7\">");
                MBTextManager.SetTextVariable("GOLD_ICON", "{=!}<img src=\"General\\Icons\\Coin@2x\" extend=\"7\">");
            }

            void AddPrestigeNobleDialogLines(CampaignGameStarter cgs)
            {
                //not empire culture
                cgs.AddDialogLine("noble_foreign", "start", "close_window", TORTextHelper.GetText("tor_empire_prestigeNoble_wrongCulture", "You are not of the Empire culture."),
                    () => EmpirePrestigeNobleStartCondition() && !IsEmpireCulture(), null, 200);
                // not clan level 2+
                cgs.AddDialogLine("noble_missRank", "start", "close_window", TORTextHelper.GetText("tor_empire_prestigeNoble_lowClanLevel", "Your clan is not prestigious enough."),
                    () => EmpirePrestigeNobleStartCondition() && !ClanLevel2(), null, 200);

                //never met player
                cgs.AddDialogLine("noble_introduction_1", "start", "noble_introduction_2", TORTextHelper.GetTextObject("tor_empire_prestigeNoble_nobleIntroduction1", "Welcome, I am {SHORTTITLE} {FIRSTNAME} {LASTNAME}, {TITLE}.").SetTextVariable("SHORTTITLE", TORTextHelper.GetTextObject("tor_empire_prestigeNoble_shorttitle", "Lord")).SetTextVariable("FIRSTNAME", TORTextHelper.GetTextObject("tor_empire_prestigeNoble_firstname", "Wilhelm")).SetTextVariable("LASTNAME", TORTextHelper.GetTextObject("tor_empire_prestigeNoble_lastname", "von Holswig-Schliestein")).SetTextVariable("TITLE", TORTextHelper.GetTextObject("tor_empire_prestigeNoble_title", "Ambassador")).ToString(),
                    () => !_knowsPlayer && EmpirePrestigeNobleStartCondition(), null, 200);
                cgs.AddDialogLine("noble_introduction_2", "noble_introduction_2",
                    "noble_introduction_3", TORTextHelper.GetText("tor_empire_prestigeNoble_nobleIntroduction2", "Introduction continued."),
                    () => !_knowsPlayer, null, 200);
                cgs.AddDialogLine("noble_introduction_3", "noble_introduction_3",
                    "prestige_noble_main_hub", TORTextHelper.GetText("tor_empire_prestigeNoble_nobleIntroduction3", "How may I assist you?"),
                    () => !_knowsPlayer, () => _knowsPlayer = true, 200);

                //knows player, hub start
                cgs.AddDialogLine("noble_hub_intro", "start", "prestige_noble_main_hub", TORTextHelper.GetText("tor_empire_prestigeNoble_hubIntro", "Welcome back. How may I help you?"),
                    () => EmpirePrestigeNobleStartCondition() && _knowsPlayer, null, 200);

                //return here when completing a branch
                cgs.AddDialogLine("noble_hub_intro_repeat", "noble_hub_intro_repeat", "prestige_noble_main_hub", TORTextHelper.GetText("tor_empire_prestigeNoble_hubIntroRepeat", "Anything else?"),
                    () => EmpirePrestigeNobleStartCondition() && _knowsPlayer, null, 200);

                cgs.AddPlayerLine("prestige_items", "prestige_noble_main_hub", "noble_prestige_items_intro", TORTextHelper.GetText("tor_empire_prestigeNoble_itemsAsk_p", "I am interested in prestige items."),
                    null, null, 200);

                InitPrestigeItemDialog();

                cgs.AddPlayerLine("infrastructure_projects", "prestige_noble_main_hub",
                    "noble_prestige_infrastructure_hub", TORTextHelper.GetText("tor_empire_prestigeNoble_buildingAsk_p", "I would like to invest in infrastructure."),
                    null, null, 200);

                InitInfrastructureProjectsDialog();

                cgs.AddPlayerLine("influence_projects_ask", "prestige_noble_main_hub", "noble_prestige_political_power_hub", TORTextHelper.GetText("tor_empire_prestigeNoble_influenceAsk_p", "I have an interest in the many organisations of the Empire."),
                    null, null, 200);

                InitPoliticalPowerProjects();

                cgs.AddPlayerLine("noble_hub_exit", "prestige_noble_main_hub", "close_window", TORTextHelper.GetText("tor_empire_prestigeNoble_exitConvo_p", "Thanks, I will come back to you."),
                    null, null, 200);

                //prestige items : eg. demigrpyh

                void InitPrestigeItemDialog()
                {
                    cgs.AddDialogLine("noble_prestige_items_intro", "noble_prestige_items_intro",
                        "noble_prestige_item_hub", TORTextHelper.GetText("tor_empire_prestigeNoble_itemsIntro", "I have some prestigious items available."),
                        null, null, 200);

                    cgs.AddPlayerLine("noble_prestige_item_selection_mount", "noble_prestige_item_hub",
                        "noble_prestige_item_explain_mount", TORTextHelper.GetText("tor_empire_prestigeNoble_mountInquiry_p", "I am interested in mounts."),
                        null, null, 200);

                    cgs.AddPlayerLine("prestige_item_noble_hub_selection_back", "noble_prestige_item_hub",
                        "noble_hub_intro_repeat", TORTextHelper.GetText("tor_empire_prestigeNoble_noInterestItems_p", "Maybe something different."),
                        null, null, 200);

                    InitSelectionMount();

                    //selection Mount
                    void InitSelectionMount()
                    {
                        cgs.AddDialogLine("noble_prestige_item_explain_mount",
                            "noble_prestige_item_explain_mount",
                            "noble_prestige_item_choice", TORTextHelper.GetText("tor_empire_prestigeNoble_demigryphExplain", "The demigryph is a prestigious mount."),
                            null, null, 200);

                        cgs.AddPlayerLine("noble_prestige_item_choice_agree", "noble_prestige_item_choice",
                            "noble_hub_intro_repeat", TORTextHelper.GetText("tor_empire_prestigeNoble_demigryphBuy_p", "Price is no issue."),
                            HasEnoughPrestigeForMount, SelectDemiGryphen, 200);

                        cgs.AddPlayerLine("noble_prestige_item_choice_decline", "noble_prestige_item_choice",
                            "noble_hub_intro_repeat", TORTextHelper.GetText("tor_empire_prestigeNoble_demigryphRefuse_p", "Not at this time."), null, null, 200);
                    }

                    bool HasEnoughPrestigeForMount()
                    {
                        var available = Hero.MainHero.GetCustomResourceValue("Prestige");
                        return available >= DemigryphCost;
                    }
                }

                //projects converting gold to prestige
                void InitInfrastructureProjectsDialog()
                {
                    cgs.AddDialogLine("noble_prestige_infrastructure_hub",
                        "noble_prestige_infrastructure_hub",
                        "noble_prestige_infrastructure_hub_selection", TORTextHelper.GetText("tor_empire_prestigeNoble_buildingProjectHub", "There are a number of projects available."),
                        null, null, 200);

                    var buildingPrestigeSelection = "noble_prestige_building_selection_";
                    var buildingPrestigeExplain = "noble_prestige_building_explain_";


                    var buildingCosts = new[]
                    {
                        200000,
                        400000,
                        500000,
                        250000,
                        500000
                    };


                    for (var i = 0; i < buildingCosts.Length; i++)
                    {
                        var index = i; //using i will cause index out of bounds exceptions during gameplay because the Line conditionals aren't evaluated until the line is about to be displayed at which point i = buildingCosts.Length
                        cgs.AddPlayerLine(buildingPrestigeSelection + index,
                            "noble_prestige_infrastructure_hub_selection",
                            buildingPrestigeExplain + index, TORTextHelper.GetText("tor_empire_prestigeNoble_buildingOption", index.ToString(), "Building Option", skipValidation: true),
                            () => !_constructedBuildings.Any(x => x.Contains("building" + index)), null, 200);

                        cgs.AddDialogLine(buildingPrestigeExplain + index, buildingPrestigeExplain + index,
                            $"buildingPrestigeSelection{index}_choice",
                             TORTextHelper.GetTextObject("tor_empire_prestigeNoble_buildingExplain", index.ToString(), "Building Explanation", skipValidation: true).SetTextVariable("BUILDING_COST", buildingCosts[index]).ToString(),
                            null, null, 200);

                        cgs.AddPlayerLine($"noble_prestige_item_selection_building_{index}_agree",
                            $"buildingPrestigeSelection{index}_choice",
                            "noble_hub_intro_repeat", TORTextHelper.GetTextObject("tor_empire_prestigeNoble_buildingPay_p", "That sounds good, I will send you the funding.").SetTextVariable("BUILDING_COST", buildingCosts[index]).ToString(),
                            () => HasEnoughGold(buildingCosts[index]),
                            () => StartTransaction(buildingCosts[index], index), 200);

                        cgs.AddPlayerLine($"noble_prestige_item_selection_building_{index}_decline",
                            $"buildingPrestigeSelection{index}_choice",
                            "noble_hub_intro_repeat", TORTextHelper.GetText("tor_empire_prestigeNoble_buildingRefuse_p", "Not at this time."),
                            null, null, 200);
                    }

                    cgs.AddPlayerLine("noble_prestige_infrastructure_hub_back",
                        "noble_prestige_infrastructure_hub_selection",
                        "noble_hub_intro_repeat", TORTextHelper.GetText("tor_empire_prestigeNoble_noInterestBuildings_p", "Maybe something different."),
                        null, null, 200);

                    bool HasEnoughGold(int price)
                    {
                        var current = Hero.MainHero.Gold;
                        return current >= price;
                    }

                    void StartTransaction(int price, int id)
                    {
                        Hero.MainHero.ChangeHeroGold(-price);
                        Hero.MainHero.AddCustomResource("Prestige", price / 500);
                        _constructedBuildings.Add("building" + id);
                    }
                }

                //projects converting influence to prestige
                void InitPoliticalPowerProjects()
                {
                    var politicalPowerSelection = "noble_prestige_power_projects_selection_";
                    var politicalPowerExplain = "noble_prestige_explain_selection_";

                    var politicalPowerProjects = 4;


                    var costs = new[]
                    {
                        200,
                        400,
                        200,
                        400
                    };


                    cgs.AddDialogLine("noble_prestige_political_power_hub",
                        "noble_prestige_political_power_hub",
                        "noble_prestige_political_power_hub_selection", TORTextHelper.GetText("tor_empire_prestigeNoble_influenceProjectHub", "Many organisations need your support."),
                        null, null, 200);

                    for (var i = 0; i < politicalPowerProjects; i++)
                    {
                        var index = i;
                        cgs.AddPlayerLine(politicalPowerSelection + index,
                            "noble_prestige_political_power_hub_selection",
                            politicalPowerExplain + index, TORTextHelper.GetText("tor_empire_prestigeNoble_influenceOption", index.ToString(), "Influence Option", skipValidation: true),
                            () => !_politicalPowerProjects.Any(x => x.Contains("powerProject" + index)), null, 200);

                        cgs.AddDialogLine(politicalPowerExplain + index, politicalPowerExplain + index,
                            $"powerSelection{index}_choice", TORTextHelper.GetTextObject("tor_empire_prestigeNoble_influenceExplain", index.ToString(), "Influence Explanation", skipValidation: true).SetTextVariable("INFLUENCE_COST", costs[index]).ToString(),
                            null, null, 200);

                        cgs.AddPlayerLine($"powerSelection_choice{index}_agree",
                            $"powerSelection{index}_choice",
                            "noble_hub_intro_repeat", TORTextHelper.GetTextObject("tor_empire_prestigeNoble_influencePay_p", "This sounds good, I will support this.").SetTextVariable("INFLUENCE_COST", costs[index]).ToString(),
                            () => HasEnoughInfluence(costs[index]),
                            () =>
                            {
                                ExchangeInfluenceForPrestige(costs[index], costs[index]);
                                _politicalPowerProjects.Add("powerProject" + index);
                            }, 200);

                        cgs.AddPlayerLine($"powerSelection_choice{index}_decline",
                            $"powerSelection{index}_choice",
                            "noble_hub_intro_repeat", TORTextHelper.GetText("tor_empire_prestigeNoble_influenceRefuse_p", "Not at this time."),
                            null, null, 200);
                    }

                    cgs.AddPlayerLine(politicalPowerSelection + 4, "noble_prestige_political_power_hub_selection",
                        "noble_prestige_political_power_hub", TORTextHelper.GetText("tor_empire_prestigeNoble_influenceOptionRepeatable", "[Enlarge your Influence throughout the Empire (Repeatable)]"),
                        () => HasEnoughInfluence(RepeatableInfluenceCosts), () => ExchangeInfluenceForPrestige(RepeatableInfluenceCosts, RepeatablePrestigeGain),
                        200);

                    bool HasEnoughInfluence(int cost)
                    {
                        return Hero.MainHero.Clan.Influence >= cost;
                    }

                    void ExchangeInfluenceForPrestige(int cost, int exchange)
                    {
                        Hero.MainHero.AddInfluenceWithKingdom(-cost);
                        Hero.MainHero.AddCultureSpecificCustomResource(exchange);
                    }

                    cgs.AddPlayerLine("noble_prestige_politicalpower_hub_back",
                        "noble_prestige_political_power_hub_selection",
                        "noble_hub_intro_repeat", TORTextHelper.GetText("tor_empire_prestigeNoble_noInterestInfluence_p", "Maybe something different."),
                        null, null, 200);
                }

                bool EmpirePrestigeNobleStartCondition()
                {
                    var partner = CharacterObject.OneToOneConversationCharacter;
                    if (partner != null) return partner.IsHero && IsPrestigeNoble(partner.HeroObject);

                    return false;
                }

                bool IsPrestigeNoble(Hero hero)
                {
                    if (hero != null)
                        return hero.Occupation == Occupation.Special && hero.HasAttribute("PrestigeNoble");
                    return false;
                }
            }
        }

        private bool IsEmpireCulture()
        {
            return Hero.MainHero.Culture.StringId == TORConstants.Cultures.EMPIRE;
        }

        private bool ClanLevel2()
        {
            return Clan.PlayerClan.Tier >= 2;
        }

        private void SelectDemiGryphen()
        {
            var demigryphens = MBObjectManager.Instance.GetObjectTypeList<ItemObject>().Where(x =>
                x.IsMountable && x.StringId.Contains("tor_empire_mount_demigryph")
            );

            var list = new List<InquiryElement>();

            foreach (var item in demigryphens)
                list.Add(new InquiryElement(item, item.Name.ToString(), new ItemImageIdentifier(item)));

            var inq = new MultiSelectionInquiryData(TORTextHelper.GetText("tor_empire_prestigeNoble_demigryphInquiryTitle", "Choose your demigryph!"), TORTextHelper.GetText("tor_empire_prestigeNoble_demigryphInquiryDescription", "Choose one of the available demigryphens."),
                list, false, 1, 1, TORTextHelper.GetText("tor_confirm", "Confirm"), null, OnGryphRewardClaimed, null);
            MBInformationManager.ShowMultiSelectionInquiry(inq);
        }

        private void OnGryphRewardClaimed(List<InquiryElement> obj)
        {
            Hero.MainHero.AddCustomResource("Prestige", -DemigryphCost);
            var item = obj[0].Identifier as ItemObject;
            Hero.MainHero.PartyBelongedTo.Party.ItemRoster.AddToCounts(item, 1);
            _receivedDemiGryphen = true;
        }

        private void OnNewGameStarted(CampaignGameStarter obj)
        {
            foreach (var town in Town.AllTowns)
            {
                if (town.StringId == "town_comp_RL1")
                {
                    _altdorf = town.Settlement;
                    CreateNobleOfTheEmpire();
                    break;
                }
            }

            void CreateNobleOfTheEmpire()
            {
                var template = MBObjectManager.Instance.GetObject<CharacterObject>(_nobleID);
                if (template != null)
                {
                    _empireNoble = HeroCreator.CreateSpecialHero(template, _altdorf, null, null, 50);
                    _empireNoble.SupporterOf = _altdorf.OwnerClan;
                    var name = _empireNoble.Template.Name;
                    name.SetTextVariable("SHORTTITLE", TORTextHelper.GetTextObject("tor_empire_prestigeNoble_shorttitle", "Lord"));
                    name.SetTextVariable("FIRSTNAME", TORTextHelper.GetTextObject("tor_empire_prestigeNoble_firstname", "Wilhelm"));
                    name.SetTextVariable("LASTNAME", TORTextHelper.GetTextObject("tor_empire_prestigeNoble_lastname", "von Holswig-Schliestein"));
                    name.SetTextVariable("TITLE", TORTextHelper.GetTextObject("tor_empire_prestigeNoble_title", "Ambassador"));
                    _empireNoble.SetName(_empireNoble.Template.Name, _empireNoble.Template.Name);
                    _empireNoble.CharacterObject.HiddenInEncyclopedia = true;
                    HeroHelper.SpawnHeroForTheFirstTime(_empireNoble, _altdorf);
                }
            }
        }

        private void OnBeforeMissionStart()
        {
            EnforcePrestigeNobleLocation();
        }

        private void OnGameMenuOpened(MenuCallbackArgs obj)
        {
            EnforcePrestigeNobleLocation();
        }

        private void EnforcePrestigeNobleLocation()
        {
            if (Settlement.CurrentSettlement == null || Settlement.CurrentSettlement != _altdorf) return;
            var locationchar = _altdorf.LocationComplex.GetLocationCharacterOfHero(_empireNoble);
            var lordsHall = _altdorf.LocationComplex.GetLocationWithId("lordshall");
            var currentloc = _altdorf.LocationComplex.GetLocationOfCharacter(locationchar);
            if (locationchar is null || lordsHall is null || currentloc is null) return;
            if (currentloc != lordsHall) _altdorf.LocationComplex.ChangeLocation(locationchar, currentloc, lordsHall);
        }


        public override void SyncData(IDataStore dataStore)
        {
            dataStore.SyncData("_knowsPlayer", ref _knowsPlayer);
            dataStore.SyncData("_receivedDemiGryphen", ref _receivedDemiGryphen);
            dataStore.SyncData("_constructedBuildings", ref _constructedBuildings);
            dataStore.SyncData("_politicalPowerProjects", ref _constructedBuildings);
        }
    }
}