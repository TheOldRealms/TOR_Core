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

                MBTextManager.SetTextVariable("INFLUENCE_ICON", $"{{=!}}<img src=\"{TORPaths.NormalizeAssetPath("General\\Icons\\Influence@2x")}\" extend=\"7\">");
                MBTextManager.SetTextVariable("GOLD_ICON", $"{{=!}}<img src=\"{TORPaths.NormalizeAssetPath("General\\Icons\\Coin@2x")}\" extend=\"7\">");
            }

            void AddPrestigeNobleDialogLines(CampaignGameStarter cgs)
            {
                //not empire culture
                cgs.AddDialogLine("noble_foreign", "start", "close_window", TORTextHelper.GetText("tor_empire_prestigenoble_wrongculture", "You do not serve the Empire, stranger, begone. This implies the player's faction despite the conditional checking culture - will need clarification."),
                    () => EmpirePrestigeNobleStartCondition() && !IsEmpireCulture(), null, 200);
                // not clan level 2+
                cgs.AddDialogLine("noble_missRank", "start", "close_window", TORTextHelper.GetText("tor_empire_prestigenoble_lowclanlevel", "I do not do business with nobodies, stranger, and I do not know you. Now begone. (Low Clan Tier)."),
                    () => EmpirePrestigeNobleStartCondition() && !ClanLevel2(), null, 200);

                //never met player
                cgs.AddDialogLine("noble_introduction_1", "start", "noble_introduction_2", TORTextHelper.GetTextObject("tor_empire_prestigenoble_nobleintroduction1", "It is a pleasure to make your acquaintance, I am {SHORTTITLE}{FIRSTNAME} {LASTNAME}{TITLE}. You have been making quite a name for yourself it would seem, many amongst the Imperial courts know of your exploits.").SetTextVariable("SHORTTITLE", TORTextHelper.GetTextObject("tor_empire_prestigenoble_shorttitle", "Esteemed")).SetTextVariable("FIRSTNAME", TORTextHelper.GetTextObject("tor_empire_prestigenoble_firstname", "Berthold")).SetTextVariable("LASTNAME", TORTextHelper.GetTextObject("tor_empire_prestigenoble_lastname", "Wendehals")).SetTextVariable("TITLE", TORTextHelper.GetTextObject("tor_empire_prestigenoble_title", " of Altdorf Nobles")).ToString(),
                    () => !_knowsPlayer && EmpirePrestigeNobleStartCondition(), null, 200);
                cgs.AddDialogLine("noble_introduction_2", "noble_introduction_2",
                    "noble_introduction_3", TORTextHelper.GetText("tor_empire_prestigenoble_nobleintroduction2", "I make it my business to know important, and useful, people. Connections are my trade you see, I do believe a relationship between us could prove to be very lucrative."),
                    () => !_knowsPlayer, null, 200);
                cgs.AddDialogLine("noble_introduction_3", "noble_introduction_3",
                    "prestige_noble_main_hub", TORTextHelper.GetText("tor_empire_prestigenoble_nobleintroduction3", "I can help you further your power and influence within the courts of the Empire and all I ask in return is that you return the favour when needed."),
                    () => !_knowsPlayer, () => _knowsPlayer = true, 200);

                //knows player, hub start
                cgs.AddDialogLine("noble_hub_intro", "start", "prestige_noble_main_hub", TORTextHelper.GetText("tor_empire_prestigenoble_hubintro", "There are a number of projects that could be of interest to you. What should we consider?"),
                    () => EmpirePrestigeNobleStartCondition() && _knowsPlayer, null, 200);

                //return here when completing a branch
                cgs.AddDialogLine("noble_hub_intro_repeat", "noble_hub_intro_repeat", "prestige_noble_main_hub", TORTextHelper.GetText("tor_empire_prestigenoble_hubintrorepeat", "Is there something else what I can do for you?"),
                    () => EmpirePrestigeNobleStartCondition() && _knowsPlayer, null, 200);

                cgs.AddPlayerLine("prestige_items", "prestige_noble_main_hub", "noble_prestige_items_intro", TORTextHelper.GetText("tor_empire_prestigenoble_itemsask_p", "Are there any items of interest you might have for me?"),
                    null, null, 200);

                InitPrestigeItemDialog();

                cgs.AddPlayerLine("infrastructure_projects", "prestige_noble_main_hub",
                    "noble_prestige_infrastructure_hub", TORTextHelper.GetText("tor_empire_prestigenoble_buildingask_p", "I would like to invest in infrastructure, what are my options?"),
                    null, null, 200);

                InitInfrastructureProjectsDialog();

                cgs.AddPlayerLine("influence_projects_ask", "prestige_noble_main_hub", "noble_prestige_political_power_hub", TORTextHelper.GetText("tor_empire_prestigenoble_influenceask_p", "I have an interest in the many organisations of the Empire, are there any who I could aid?"),
                    null, null, 200);

                InitPoliticalPowerProjects();

                cgs.AddPlayerLine("noble_hub_exit", "prestige_noble_main_hub", "close_window", TORTextHelper.GetText("tor_empire_prestigenoble_exitconvo_p", "Thanks, I will come back to you."),
                    null, null, 200);

                //prestige items : eg. demigrpyh

                void InitPrestigeItemDialog()
                {
                    cgs.AddDialogLine("noble_prestige_items_intro", "noble_prestige_items_intro",
                        "noble_prestige_item_hub", TORTextHelper.GetText("tor_empire_prestigenoble_itemsintro", "Hmm.. currently there is only one thing I have on hand, but it is very unique. A beast from the Imperial Menagerie, should you be interested? - If he only has one thing on hand, why are you prompted to select among demigryphs?"),
                        null, null, 200);

                    cgs.AddPlayerLine("noble_prestige_item_selection_mount", "noble_prestige_item_hub",
                        "noble_prestige_item_explain_mount", TORTextHelper.GetText("tor_empire_prestigenoble_mountinquiry_p", "What sort of beast?"),
                        null, null, 200);

                    cgs.AddPlayerLine("prestige_item_noble_hub_selection_back", "noble_prestige_item_hub",
                        "noble_hub_intro_repeat", TORTextHelper.GetText("tor_empire_prestigenoble_nointerestitems_p", "Maybe something different.(back)"),
                        null, null, 200);

                    InitSelectionMount();

                    //selection Mount
                    void InitSelectionMount()
                    {
                        cgs.AddDialogLine("noble_prestige_item_explain_mount",
                            "noble_prestige_item_explain_mount",
                            "noble_prestige_item_choice", TORTextHelper.GetText("tor_empire_prestigenoble_demigryphexplain", "Due to some rather unfortunate circumstances, we have a monstrous steed without a rider. A Demigryph, to be precise. While the rider will be missed, the keeper of the Imperial Menagerie doesn't know what to do with it. Luckily, I have contacts within the Order of the fallen Knight and they may be willing to entrust the mount to you… ({DEMIGRYPH_COST}{PRESTIGE_ICON})"),
                            null, null, 200);

                        cgs.AddPlayerLine("noble_prestige_item_choice_agree", "noble_prestige_item_choice",
                            "noble_hub_intro_repeat", TORTextHelper.GetText("tor_empire_prestigenoble_demigryphbuy_p", "Price is no issue, such a mighty steed would be worth it. ({DEMIGRYPH_COST}{PRESTIGE_ICON})"),
                            HasEnoughPrestigeForMount, SelectDemiGryphen, 200);

                        cgs.AddPlayerLine("noble_prestige_item_choice_decline", "noble_prestige_item_choice",
                            "noble_hub_intro_repeat", TORTextHelper.GetText("tor_empire_prestigenoble_demigryphrefuse_p", "Not at this time, perhaps later."), null, null, 200);
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
                        "noble_prestige_infrastructure_hub_selection", TORTextHelper.GetText("tor_empire_prestigenoble_buildingprojecthub", "An interesting choice, there are a number of projects slated for the future… but with the right amount of coin I can ensure you are known as the magnimonous benefactor behind their expedited construction."),
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
                            buildingPrestigeExplain + index, TORTextHelper.GetText("tor_empire_prestigenoble_buildingoption", index.ToString(), "Building Option", skipValidation: true),
                            () => !_constructedBuildings.Any(x => x.Contains("building" + index)), null, 200);

                        cgs.AddDialogLine(buildingPrestigeExplain + index, buildingPrestigeExplain + index,
                            $"buildingPrestigeSelection{index}_choice",
                             TORTextHelper.GetTextObject("tor_empire_prestigenoble_buildingexplain", index.ToString(), "Building Explanation", skipValidation: true).SetTextVariable("BUILDING_COST", buildingCosts[index]).ToString(),
                            null, null, 200);

                        cgs.AddPlayerLine($"noble_prestige_item_selection_building_{index}_agree",
                            $"buildingPrestigeSelection{index}_choice",
                            "noble_hub_intro_repeat", TORTextHelper.GetTextObject("tor_empire_prestigenoble_buildingpay_p", "That sounds good, I will send you the funding. ({BUILDING_COST} {GOLD_ICON}) This can be reformated to seem less awkward with restating the cost - the player should see the npc's previous description on the left with this confirmation option on the right.").SetTextVariable("BUILDING_COST", buildingCosts[index]).ToString(),
                            () => HasEnoughGold(buildingCosts[index]),
                            () => StartTransaction(buildingCosts[index], index), 200);

                        cgs.AddPlayerLine($"noble_prestige_item_selection_building_{index}_decline",
                            $"buildingPrestigeSelection{index}_choice",
                            "noble_hub_intro_repeat", TORTextHelper.GetText("tor_empire_prestigenoble_buildingrefuse_p", "Not at this time, perhaps later."),
                            null, null, 200);
                    }

                    cgs.AddPlayerLine("noble_prestige_infrastructure_hub_back",
                        "noble_prestige_infrastructure_hub_selection",
                        "noble_hub_intro_repeat", TORTextHelper.GetText("tor_empire_prestigenoble_nointerestbuildings_p", "Maybe something different.(back)"),
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
                        "noble_prestige_political_power_hub_selection", TORTextHelper.GetText("tor_empire_prestigenoble_influenceprojecthub", "Many parties and organisations throught the empire need your support. Are you willing to provide them the power they need? - power is awkward here, the player is providing political support. They are swaying attention towards the various possibilities so that additional resources are diverted to them - power isn't an object to give."),
                        null, null, 200);

                    for (var i = 0; i < politicalPowerProjects; i++)
                    {
                        var index = i;
                        cgs.AddPlayerLine(politicalPowerSelection + index,
                            "noble_prestige_political_power_hub_selection",
                            politicalPowerExplain + index, TORTextHelper.GetText("tor_empire_prestigenoble_influenceoption", index.ToString(), "Influence Option", skipValidation: true),
                            () => !_politicalPowerProjects.Any(x => x.Contains("powerProject" + index)), null, 200);

                        cgs.AddDialogLine(politicalPowerExplain + index, politicalPowerExplain + index,
                            $"powerSelection{index}_choice", TORTextHelper.GetTextObject("tor_empire_prestigenoble_influenceexplain", index.ToString(), "Influence Explanation", skipValidation: true).SetTextVariable("INFLUENCE_COST", costs[index]).ToString(),
                            null, null, 200);

                        cgs.AddPlayerLine($"powerSelection_choice{index}_agree",
                            $"powerSelection{index}_choice",
                            "noble_hub_intro_repeat", TORTextHelper.GetTextObject("tor_empire_prestigenoble_influencepay_p", "This sounds good, I will support this. ({INFLUENCE_COST} {INFLUENCE_ICON})").SetTextVariable("INFLUENCE_COST", costs[index]).ToString(),
                            () => HasEnoughInfluence(costs[index]),
                            () =>
                            {
                                ExchangeInfluenceForPrestige(costs[index], costs[index]);
                                _politicalPowerProjects.Add("powerProject" + index);
                            }, 200);

                        cgs.AddPlayerLine($"powerSelection_choice{index}_decline",
                            $"powerSelection{index}_choice",
                            "noble_hub_intro_repeat", TORTextHelper.GetText("tor_empire_prestigenoble_influencerefuse_p", "Not at this time, perhaps later."),
                            null, null, 200);
                    }

                    cgs.AddPlayerLine(politicalPowerSelection + "repeatable", "noble_prestige_political_power_hub_selection",
                        "noble_prestige_political_power_hub", TORTextHelper.GetText("tor_empire_prestigenoble_influenceoptionrepeatable", "[Enlarge your Influence throughout the Empire (Repeatable)]"),
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
                        "noble_hub_intro_repeat", TORTextHelper.GetText("tor_empire_prestigenoble_nointerestinfluence_p", "Maybe something different.(back)"),
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

            var inq = new MultiSelectionInquiryData(TORTextHelper.GetText("tor_empire_prestigenoble_demigryphinquirytitle", "Choose your demigryph!"), TORTextHelper.GetText("tor_empire_prestigenoble_demigryphinquirydescription", "Choose one of the available demigryphens."),
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
                    name.SetTextVariable("SHORTTITLE", TORTextHelper.GetTextObject("tor_empire_prestigenoble_shorttitle", "Esteemed"));
                    name.SetTextVariable("FIRSTNAME", TORTextHelper.GetTextObject("tor_empire_prestigenoble_firstname", "Berthold"));
                    name.SetTextVariable("LASTNAME", TORTextHelper.GetTextObject("tor_empire_prestigenoble_lastname", "Wendehals"));
                    name.SetTextVariable("TITLE", TORTextHelper.GetTextObject("tor_empire_prestigenoble_title", " of Altdorf Nobles"));
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