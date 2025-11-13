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
                cgs.AddDialogLine("noble_foreign", "start", "close_window", GameTexts.FindText("tor_empire_prestigeNoble_wrongCulture").ToString(),
                    () => EmpirePrestigeNobleStartCondition() && !IsEmpireCulture(), null, 200);
                // not clan level 2+
                cgs.AddDialogLine("noble_missRank", "start", "close_window", GameTexts.FindText("tor_empire_prestigeNoble_lowClanLevel").ToString(),
                    () => EmpirePrestigeNobleStartCondition() && !ClanLevel2(), null, 200);

                //never met player
                cgs.AddDialogLine("noble_introduction_1", "start", "noble_introduction_2", GameTexts.FindText("tor_empire_prestigeNoble_nobleIntroduction1").SetTextVariable("SHORTTITLE", GameTexts.FindText("tor_empire_prestigeNoble_shorttitle")).SetTextVariable("FIRSTNAME", GameTexts.FindText("tor_empire_prestigeNoble_firstname")).SetTextVariable("LASTNAME", GameTexts.FindText("tor_empire_prestigeNoble_lastname")).SetTextVariable("TITLE", GameTexts.FindText("tor_empire_prestigeNoble_title")).ToString(),
                    () => !_knowsPlayer && EmpirePrestigeNobleStartCondition(), null, 200);
                cgs.AddDialogLine("noble_introduction_2", "noble_introduction_2",
                    "noble_introduction_3", GameTexts.FindText("tor_empire_prestigeNoble_nobleIntroduction2").ToString(),
                    () => !_knowsPlayer, null, 200);
                cgs.AddDialogLine("noble_introduction_3", "noble_introduction_3",
                    "prestige_noble_main_hub", GameTexts.FindText("tor_empire_prestigeNoble_nobleIntroduction3").ToString(),
                    () => !_knowsPlayer, () => _knowsPlayer = true, 200);

                //knows player, hub start
                cgs.AddDialogLine("noble_hub_intro", "start", "prestige_noble_main_hub", GameTexts.FindText("tor_empire_prestigeNoble_hubIntro").ToString(),
                    () => EmpirePrestigeNobleStartCondition() && _knowsPlayer, null, 200);

                //return here when completing a branch
                cgs.AddDialogLine("noble_hub_intro_repeat", "noble_hub_intro_repeat", "prestige_noble_main_hub", GameTexts.FindText("tor_empire_prestigeNoble_hubIntroRepeat").ToString(),
                    () => EmpirePrestigeNobleStartCondition() && _knowsPlayer, null, 200);

                cgs.AddPlayerLine("prestige_items", "prestige_noble_main_hub", "noble_prestige_items_intro", GameTexts.FindText("tor_empire_prestigeNoble_itemsAsk_p").ToString(),
                    null, null, 200);

                InitPrestigeItemDialog();

                cgs.AddPlayerLine("infrastructure_projects", "prestige_noble_main_hub",
                    "noble_prestige_infrastructure_hub", GameTexts.FindText("tor_empire_prestigeNoble_buildingAsk_p").ToString(),
                    null, null, 200);

                InitInfrastructureProjectsDialog();

                cgs.AddPlayerLine("influence_projects_ask", "prestige_noble_main_hub", "noble_prestige_political_power_hub", GameTexts.FindText("tor_empire_prestigeNoble_influenceAsk_p").ToString(),
                    null, null, 200);

                InitPoliticalPowerProjects();

                cgs.AddPlayerLine("noble_hub_exit", "prestige_noble_main_hub", "close_window", GameTexts.FindText("tor_empire_prestigeNoble_exitConvo_p").ToString(),
                    null, null, 200);

                //prestige items : eg. demigrpyh

                void InitPrestigeItemDialog()
                {
                    cgs.AddDialogLine("noble_prestige_items_intro", "noble_prestige_items_intro",
                        "noble_prestige_item_hub", GameTexts.FindText("tor_empire_prestigeNoble_itemsIntro").ToString(),
                        null, null, 200);

                    cgs.AddPlayerLine("noble_prestige_item_selection_mount", "noble_prestige_item_hub",
                        "noble_prestige_item_explain_mount", GameTexts.FindText("tor_empire_prestigeNoble_mountInquiry_p").ToString(),
                        null, null, 200);

                    cgs.AddPlayerLine("prestige_item_noble_hub_selection_back", "noble_prestige_item_hub",
                        "noble_hub_intro_repeat", GameTexts.FindText("tor_empire_prestigeNoble_noInterestItems_p").ToString(),
                        null, null, 200);

                    InitSelectionMount();

                    //selection Mount
                    void InitSelectionMount()
                    {
                        cgs.AddDialogLine("noble_prestige_item_explain_mount",
                            "noble_prestige_item_explain_mount",
                            "noble_prestige_item_choice", GameTexts.FindText("tor_empire_prestigeNoble_demigryphExplain").ToString(),
                            null, null, 200);

                        cgs.AddPlayerLine("noble_prestige_item_choice_agree", "noble_prestige_item_choice",
                            "noble_hub_intro_repeat", GameTexts.FindText("tor_empire_prestigeNoble_demigryphBuy_p").ToString(),
                            HasEnoughPrestigeForMount, SelectDemiGryphen, 200);

                        cgs.AddPlayerLine("noble_prestige_item_choice_decline", "noble_prestige_item_choice",
                            "noble_hub_intro_repeat", GameTexts.FindText("tor_empire_prestigeNoble_demigryphRefuse_p").ToString(), null, null, 200);
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
                        "noble_prestige_infrastructure_hub_selection", GameTexts.FindText("tor_empire_prestigeNoble_buildingProjectHub").ToString(),
                        null, null, 200);

                    var buildingPrestigeSelection = "noble_prestige_building_selection_";
                    var buildingPrestigeExplain = "noble_prestige_building_explain_";

                    var buildingsTexts = new[]
                    {
                        "[Dedicate a statue to Sigmar Heldenhammer]",
                        "[Help fund the construction of a Temple of Shallya]",
                        "[Construct a new dry dock]",
                        "[Imperial Training Grounds]",
                        "[Renovate the Heldenhammer]"
                    };

                    var buildingCosts = new[]
                    {
                        200000,
                        400000,
                        500000,
                        250000,
                        500000
                    };

                    var buildingExplainTexts = new[]
                    {
                        $"The Cult of Sigmar within Altdorf has ambitions to build a rather sizeable statue of our most glorious Sigmar, upon a hill with a prestigous view of his beloved Empire. They are lacking in some funding to ensure it's timely completion, I can ensure your name is written upon such a wondrous memorial for a measly cost. ({buildingCosts[0]} {{GOLD_ICON}})",
                        $"There are many who suffer across the Empire and Shallya's priestesses are needed more than ever, however funding a Temple to the Lady in White is no small endeavour. You can be sure that, for a little coin, your name will be praised for generations for aiding in such a selfless act.({buildingCosts[1]} {{GOLD_ICON}})",
                        $"The threat we face from Norsca is ever present, the Elector Count of Nordland has called for the construction of a new dry dock to empower the Imperial Navy. It has already been funded by the local lords...however for a little extra I can ensure you are known as the prime contributor. ({buildingCosts[2]} {{GOLD_ICON}})",
                        $"The forests and the hills of the Empire need constant patrols to ensure the safety of it's citizens, one of the local garrisons is in dire need of training equipment and you have an opportunity to provide it for them. ({buildingCosts[3]} {{GOLD_ICON}})",
                        $"It takes a lot of coin to keep the mighty Heldenhammer afloat, the Grand Theogonist and the Cult of Sigmar will look very favourably upon any who donate to ensure it is battle ready at all times. ({buildingCosts[4]} {{GOLD_ICON}})"
                    };

                    for (var i = 0; i < buildingCosts.Length; i++)
                    {
                        var index = i; //using i will cause index out of bounds exceptions during gameplay because the Line conditionals aren't evaluated until the line is about to be displayed at which point i = buildingCosts.Length
                        cgs.AddPlayerLine(buildingPrestigeSelection + index,
                            "noble_prestige_infrastructure_hub_selection",
                            buildingPrestigeExplain + index, GameTexts.FindText("tor_empire_prestigeNoble_buildingOption" + index).ToString(),
                            () => !_constructedBuildings.Any(x => x.Contains("building" + index)), null, 200);

                        cgs.AddDialogLine(buildingPrestigeExplain + index, buildingPrestigeExplain + index,
                            $"buildingPrestigeSelection{index}_choice",
                             GameTexts.FindText("tor_empire_prestigeNoble_buildingExplain" + index).SetTextVariable("BUILDING_COST", buildingCosts[index]).ToString(),
                            null, null, 200);

                        cgs.AddPlayerLine($"noble_prestige_item_selection_building_{index}_agree",
                            $"buildingPrestigeSelection{index}_choice",
                            "noble_hub_intro_repeat", GameTexts.FindText("tor_empire_prestigeNoble_buildingPay_p").SetTextVariable("BUILDING_COST", buildingCosts[index]).ToString(),
                            () => HasEnoughGold(buildingCosts[index]),
                            () => StartTransaction(buildingCosts[index], index), 200);

                        cgs.AddPlayerLine($"noble_prestige_item_selection_building_{index}_decline",
                            $"buildingPrestigeSelection{index}_choice",
                            "noble_hub_intro_repeat", GameTexts.FindText("tor_empire_prestigeNoble_buildingRefuse_p").ToString(),
                            null, null, 200);
                    }

                    cgs.AddPlayerLine("noble_prestige_infrastructure_hub_back",
                        "noble_prestige_infrastructure_hub_selection",
                        "noble_hub_intro_repeat", GameTexts.FindText("tor_empire_prestigeNoble_noInterestBuildings_p").ToString(),
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

                    var politicalPowerProjectTexts = new[]
                    {
                        "[Find support for a diplomatic mission to Ulthuan to establish a new trade route.]",
                        "[Support the Traders Guild envoy on a mission to establish gunpowder trade with Cathay]",
                        "[Support the Engineers Guild technological innovation.]",
                        "[Help fund a Huntsmen Expedition to Lustria]"
                    };

                    var costs = new[]
                    {
                        200,
                        400,
                        200,
                        400
                    };

                    var explainPowerProjectTexts = new[]
                    {
                        $"A diplomatic mission is set to leave for Ulthuan, but they fear they are lacking in gifts elegent enough to appease the knife ears...elves. Perhaps you could help them? ({costs[0]} {{INFLUENCE_ICON}})",
                        $"It is with faith, steel and gunpowder that we protect the Empire and we need a lot of gunpowder. Cathay also also makes use of black powder and in a hope to bring our two peoples closer, the Trade Guild has sent an envoy to establish trade ties. Some Elector Counts are not convinced yet. ({costs[1]} {{INFLUENCE_ICON)}})",
                        $"The wonders of the Engineers Guild are many but they are equally as costly to invent, test and so forth. Funding their experimental endeavours would earn you an ample amount of public opinion. ({costs[2]} {{INFLUENCE_ICON}})",
                        $"Beasts are myriad within the jungles of Lustria, the Huntsmen are set to go on a hunt soon but could use help acquiring provisions for such an arduous journey. ({costs[3]} {{INFLUENCE_ICON}})"
                    };

                    cgs.AddDialogLine("noble_prestige_political_power_hub",
                        "noble_prestige_political_power_hub",
                        "noble_prestige_political_power_hub_selection", GameTexts.FindText("tor_empire_prestigeNoble_influenceProjectHub").ToString(),
                        null, null, 200);

                    for (var i = 0; i < politicalPowerProjects; i++)
                    {
                        var index = i;
                        cgs.AddPlayerLine(politicalPowerSelection + index,
                            "noble_prestige_political_power_hub_selection",
                            politicalPowerExplain + index, GameTexts.FindText("tor_empire_prestigeNoble_influenceOption" + index).ToString(),
                            () => !_politicalPowerProjects.Any(x => x.Contains("powerProject" + index)), null, 200);

                        cgs.AddDialogLine(politicalPowerExplain + index, politicalPowerExplain + index,
                            $"powerSelection{index}_choice", GameTexts.FindText("tor_empire_prestigeNoble_influenceExplain" + index).SetTextVariable("INFLUENCE_COST", costs[index]).ToString(),
                            null, null, 200);

                        cgs.AddPlayerLine($"powerSelection_choice{index}_agree",
                            $"powerSelection{index}_choice",
                            "noble_hub_intro_repeat", GameTexts.FindText("tor_empire_prestigeNoble_influencePay_p").SetTextVariable("INFLUENCE_COST", costs[index]).ToString(),
                            () => HasEnoughInfluence(costs[index]),
                            () =>
                            {
                                ExchangeInfluenceForPrestige(costs[index], costs[index]);
                                _politicalPowerProjects.Add("powerProject" + index);
                            }, 200);

                        cgs.AddPlayerLine($"powerSelection_choice{index}_decline",
                            $"powerSelection{index}_choice",
                            "noble_hub_intro_repeat", GameTexts.FindText("tor_empire_prestigeNoble_influenceRefuse_p").ToString(),
                            null, null, 200);
                    }

                    cgs.AddPlayerLine(politicalPowerSelection + 4, "noble_prestige_political_power_hub_selection",
                        "noble_prestige_political_power_hub", GameTexts.FindText("tor_empire_prestigeNoble_influenceOptionRepeatable").ToString(),
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
                        "noble_hub_intro_repeat", GameTexts.FindText("tor_empire_prestigeNoble_noInterestInfluence_p").ToString(),
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

            var inq = new MultiSelectionInquiryData(GameTexts.FindText("tor_empire_prestigeNoble_demigryphInquiryTitle").ToString(), GameTexts.FindText("tor_empire_prestigeNoble_demigryphInquiryDescription").ToString(),
                list, false, 1, 1, new TextObject("{=5Unqsx3N}Confirm", null).ToString(), null, OnGryphRewardClaimed, null);
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
                    name.SetTextVariable("SHORTTITLE", GameTexts.FindText("tor_empire_prestigeNoble_shorttitle"));
                    name.SetTextVariable("FIRSTNAME", GameTexts.FindText("tor_empire_prestigeNoble_firstname"));
                    name.SetTextVariable("LASTNAME", GameTexts.FindText("tor_empire_prestigeNoble_lastname"));
                    name.SetTextVariable("TITLE", GameTexts.FindText("tor_empire_prestigeNoble_title"));
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