using System.Collections.Generic;
using System.Linq;
using Helpers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;
using TOR_Core.CampaignMechanics.CustomResources;
using TOR_Core.Extensions;
using TOR_Core.Utilities;

namespace TOR_Core.CampaignMechanics.Menagery
{
    public class PrestigeNobleTownBehavior : CampaignBehaviorBase
    {
        private const string _nobleID = "tor_zoologist_empire";
        private const int DemigryphCost = 1000;
        private Settlement _altdorf;
        private Hero _empireNoble;
        private bool _knowsPlayer;
        private bool _receivedDemiGryphen;
        private List<string> _constructedBuildings = new List<string>();
        private List<string> _politicalPowerProjects = new List<string>();

        public override void RegisterEvents()
        {
            CampaignEvents.OnNewGameCreatedEvent.AddNonSerializedListener(this, OnNewGameStarted);
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
        }

        private void OnSessionLaunched(CampaignGameStarter obj)
        {
            initializeVariables();
            AddZoologistDialogLines(obj);

            void initializeVariables()
            {
                MBTextManager.SetTextVariable("PRESTIGE_ICON",
                    CustomResourceManager.GetResourceObject("Prestige").GetCustomResourceIconAsText());
                MBTextManager.SetTextVariable("PRESTIGE_COST", DemigryphCost.ToString());
            }
            
            void AddZoologistDialogLines(CampaignGameStarter campaignGameStarter)
            {
                //not empire
                campaignGameStarter.AddDialogLine("noble_foreign", "start", "close_window",
                    "You do not serve the Empire stranger, begone.",
                    () => EmpirePrestigeNobleStartCondition() && !isPartOfEmpire(), null, 200);
                // not rank 2
                campaignGameStarter.AddDialogLine("noble_missRank", "start", "close_window",
                    " I do not do business with nobodies’ stranger, and I do not know you. Now begone. (Low Renown).",
                    () => EmpirePrestigeNobleStartCondition() && !hasRenown2(), null, 200);

                //knows player false
                campaignGameStarter.AddDialogLine("noble_introduction_1", "start", "noble_introduction_2",
                    "It is a pleasure to make your acquaintance, I am Lord Berthold Wendehals. You have been making quite a name for yourself it would seem, many amongst the Imperial courts know of your exploits.",
                    () => !_knowsPlayer && EmpirePrestigeNobleStartCondition(), null, 200);
                campaignGameStarter.AddDialogLine("noble_introduction_2", "noble_introduction_2",
                    "noble_introduction_3",
                    "I make it my business to know important, and useful, people. Connections are my trade you see, I do believe a relationship between us could prove to be very lucrative.",
                    () => !_knowsPlayer, null, 200);
                campaignGameStarter.AddDialogLine("noble_introduction_3", "noble_introduction_3",
                    "prestige_noble_main_hub",
                    "I can help you further your power and influence within the courts of the Empire and all I ask in return is that you return the favour when needed.",
                    () => !_knowsPlayer, () => _knowsPlayer = true, 200);

                //knows player, hub start
                campaignGameStarter.AddDialogLine("noble_hub_intro", "start", "prestige_noble_main_hub",
                    "There are a number of projects that could be of interest to you. What should we consider?",
                    () => EmpirePrestigeNobleStartCondition() && _knowsPlayer, null, 200);

                campaignGameStarter.AddDialogLine("noble_hub_intro", "noble_hub_intro2", "prestige_noble_main_hub",
                    "Is there something else what I can do for you?",
                    () => EmpirePrestigeNobleStartCondition() && _knowsPlayer, null, 200);
                //---

                obj.AddPlayerLine("prestige_items", "prestige_noble_main_hub", "noble_prestige_items_intro",
                    "Are there any items of interest you might have for me?",
                    null, null, 200);
                
                InitPrestigeItemDialog();

                obj.AddPlayerLine("infrastructure_projects", "prestige_noble_main_hub", "noble_prestige_infrastructure_hub",
                    "I would like to invest in infrastructure, what are my options?",
                    null, null, 200);

                InitInfrastructureProjectsDialog();

                obj.AddPlayerLine("influence_projects", "prestige_noble_main_hub", "noble_prestige_political_power_hub",
                    "I have an interest in the many organisations of the Empire, are there any who I could aid?",
                    null, null, 200);
                
                InitPoliticalPowerProjects();
                
                obj.AddPlayerLine("influence_projects", "prestige_noble_main_hub", "close_window",
                    "Thanks, I will come back to you.",
                    null, null, 200);
                
                
                //prestige items

                void InitPrestigeItemDialog()
                { 
                    campaignGameStarter.AddDialogLine("noble_prestige_items_hub", "noble_prestige_items_intro",
                    "noble_prestige_item_hub",
                    "Hmm.. currently there is only one thing I have on hand, but it is very unique. A beast from the Imperial Menagerie, should you be interested?",
                    null, null, 200);

                obj.AddPlayerLine("noble_prestige_item_selection_mount", "noble_prestige_item_hub",
                    "noble__prestige_item_explain_mount",
                    "What sort of beast?",
                    null, null, 200);

                obj.AddPlayerLine("prestige_item_noble_hub_selection_back", "noble_prestige_item_hub",
                    "noble_hub_intro2",
                    "Maybe something different.(back)",
                    null, null, 200);
                
                InitSelectionMount();
                //selection Mount
                    void InitSelectionMount()
                    {
                        campaignGameStarter.AddDialogLine("noble_prestige_item_explain_mount",
                            "noble__prestige_item_explain_mount", "noble__prestige_item_choice",
                            "Due to some rather unfortunate circumstances, we have a monstrous steed without a rider. A Demigryph, to be precise. While the rider will be missed, the keeper of the Imperial Menagerie doesn't know what to do with it. Luckily, I have contacts within the Order of the fallen Knight and they may be willing to entrust the mount to you... (Cost 1000 Prestige)",
                            null, null, 200);


                        obj.AddPlayerLine("noble__prestige_item_choice_agree", "noble__prestige_item_choice",
                            "noble_hub_intro2",
                            "Price is no issue, such a mighty steed would be worth it. ({PRESTIGE_COST}{PRESTIGE_ICON}) ",
                            () => HasEnoughPrestigeForMount(), SelectDemiGryphen, 200);

                        obj.AddPlayerLine("noble_prestige_item_choice_decline", "noble_prestige_item_choice",
                            "noble_hub_intro2",
                            "Not at this time, perhaps later.", null, null, 200);
                        
                    }
                    bool HasEnoughPrestigeForMount()
                    {
                        var available = Hero.MainHero.GetCustomResourceValue("Prestige");
                        return available >= DemigryphCost;
                    }
                }

                void InitInfrastructureProjectsDialog()
                {
                    campaignGameStarter.AddDialogLine("noble_prestige_infrastructure_hub", "noble_prestige_infrastructure_hub",
                        "noble_prestige_infrastructure_hub_selection",
                        "An interesting choice, there are a number of projects slated for the future...but with the right amount of coin I can ensure you are known as the magnimonous benefactor behind their expedited construction.",
                        null, null, 200);

                    string buildingPrestigeSelection = "noble_prestige_building_selection_";
                    string buildingPrestigeExplain = "noble_prestige_building_explain_";

                    int buildingAmount = 5;
                    
                    string[] buildingsTexts = new[]
                    {
                        "[Dedicate a statue to Sigmar Heldenhammer]",
                        "[Help fund the construction of a Temple of Shallya]",
                        "[Construct a new dry dock]",
                        "[Imperial Training Grounds]",
                        "[Renovate the Heldenhammer]",
                    };
                    
                    string[] buildingExplainTexts = new[]
                    {
                        "The Cult of Sigmar within Altdorf has ambitions to build a rather sizeable statue of our most glorious Sigmar, upon a hill with a prestigous view of his beloved Empire. They are lacking in some funding to ensure it's timely completion, I can ensure your name is written upon such a wondrous memorial for a measly cost. (200k Gold)",
                        "There are many who suffer across the Empire and Shallya's priestesses are needed more than ever, however funding a Temple to the Lady in White is no small endeavour. You can be sure that, for a little coin, your name will be praised for generations for aiding in such a selfless act. (200k)",
                        "The threat we face from Norsca is ever present, the Elector Count of Nordland has called for the construction of a new dry dock to empower the Imperial Navy. It has already been funded by the local lords...however for a little extra I can ensure you are known as the prime contributor. (200k)",
                        "The forests and the hills of the Empire need constant patrols to ensure the safety of it's citizens, one of the local garrisons is in dire need of training equipment and you have an opportunity to provide it for them. (200k)",
                        "It takes a lot of coin to keep the mighty Heldenhammer afloat, the Grand Theogonist and the Cult of Sigmar will look very favourably upon any who donate to ensure it is battle ready at all times. (200k)",
                    };
                    
                    for (int i = 0; i < buildingAmount; i++)
                    {
                        obj.AddPlayerLine(buildingPrestigeSelection+i, "noble_prestige_infrastructure_hub_selection",
                            buildingPrestigeExplain+i,
                            buildingsTexts[i],
                            ()=> !_constructedBuildings.Any(x=>x.Contains("building"+i)), null, 200);
                        
                        campaignGameStarter.AddDialogLine(buildingPrestigeExplain+i, buildingPrestigeExplain+i,
                            $"buildingPrestigeSelection{i}_choice",
                            buildingExplainTexts[i],
                            null, null, 200);
                        
                        campaignGameStarter.AddPlayerLine($"noble_prestige_item_selection_building_{i}_agree", $"buildingPrestigeSelection{i}_choice",
                            "noble_hub_intro2",
                            "That sounds good, I will send you the funding.",
                            null, ()=>_constructedBuildings.Add("building"+i), 200);
                        
                        campaignGameStarter.AddPlayerLine($"noble_prestige_item_selection_building_{i}_decline", $"buildingPrestigeSelection{i}_choice",
                            "noble_hub_intro2",
                            "Not at this time, perhaps later.",
                            null,null  , 200);
                        
                    }
                    
                    obj.AddPlayerLine("noble_prestige_infrastructure_hub_back", "noble_prestige_infrastructure_hub_selection",
                        "noble_hub_intro2",
                        "Maybe something different.(back)",
                        null, null, 200);
                    
                    
                }

                void InitPoliticalPowerProjects()
                {
                    
                    string politicalPowerSelection = "noble_prestige_power_projects_selection_";
                    string politicalPowerExplain = "noble_prestige_explain_selection_";

                    int politicalPowerProjects = 4;
                    
                    string[] politicalPowerProjectTexts = new[]
                    {
                        "[Find support for a diplomatic mission to Ulthuan to establish a new trade route.]",
                        "[Support the Traders Guild envoy on a mission to establish gunpowder trade with Cathay]",
                        "[Support the Engineers Guild technological innovation.]",
                        "[Help fund a Huntsmen Expedition to Lustria]",
                        "[Enlarge your Influence throughout the Empire (Repeatable)]",
                    };
                    
                    string[] explainPowerProjectTexts = new[]
                    {
                        "A diplomatic mission is set to leave for Ulthuan, but they fear they are lacking in gifts elegent enough to appease the knife ears...elves. Perhaps you could help them? (cost here)",
                        "It is with faith, steel and gunpowder that we protect the Empire and we need a lot of gunpowder. Cathay also also makes use of black powder and in a hope to bring our two peoples closer, the Trade Guild has sent an envoy to establish trade ties. They require mercenary protection and are looking for funding. (cost here)",
                        "The wonders of the Engineers Guild are many but they are equally as costly to invent, test and so forth. Funding their experimental endeavours would earn you an ample amount of public opinion. (cost here)",
                        "Beasts are myriad within the jungles of Lustria, the Huntsmen are set to go on a hunt soon but could use help acquiring provisions for such an arduous journey. (cost here)",
                    };
                    
                    campaignGameStarter.AddDialogLine("noble_prestige_infrastructure_hub", "noble_prestige_political_power_hub",
                        "noble_prestige_political_power_hub_selection",
                        "Many parties and organisations throught the empire need your support. Are you willing to provide them the power they need?",
                        null, null, 200);
                    
                    for (int i = 0; i < politicalPowerProjects; i++)
                    {
                        obj.AddPlayerLine(politicalPowerSelection+i, "noble_prestige_political_power_hub_selection",
                            politicalPowerExplain+i,
                            politicalPowerProjectTexts[i],
                            ()=> !_politicalPowerProjects.Any(x=>x.Contains("powerProject"+i)), null, 200);
                        
                        campaignGameStarter.AddDialogLine(politicalPowerExplain+i, politicalPowerExplain+i,
                            $"powerSelection{i}_choice",
                            explainPowerProjectTexts[i],
                            null, null, 200);
                        
                        campaignGameStarter.AddPlayerLine($"powerSelection_choice{i}_agree", $"powerSelection{i}_choice",
                            "noble_hub_intro2",
                            "This sounds good, I will support this.",
                            null, ()=>_constructedBuildings.Add("powerProject"+i), 200);
                        
                        campaignGameStarter.AddPlayerLine($"powerSelection_choice{i}_decline", $"powerSelection{i}_choice",
                            "noble_hub_intro2",
                            "Not at this time, perhaps later.",
                            null,null  , 200);
                        
                    }
                    
                    obj.AddPlayerLine(politicalPowerSelection+4, "noble_prestige_political_power_hub_selection",
                        "noble_prestige_political_power_hub",
                        politicalPowerProjectTexts[4],
                        null, ()=> ExchangeInfluenceForPrestige(), 200);
                    
                    
                    void ExchangeInfluenceForPrestige()
                    {
                        TORCommon.Say("hello");
                    }
                    
                    obj.AddPlayerLine("noble_prestige_politicalpower_hub_back", "noble_prestige_political_power_hub_selection",
                        "noble_hub_intro2",
                        "Maybe something different.(back)",
                        null, null, 200);
                }

                bool EmpirePrestigeNobleStartCondition()
                {
                    var partner = CharacterObject.OneToOneConversationCharacter;
                    if (partner != null) return partner.HeroObject.IsZoologist();

                    return false;
                }

            }
        }

        private bool isPartOfEmpire()
        {
            return Hero.MainHero.Culture.StringId == "empire";
        }

        private bool hasRenown2()
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
                list.Add(new InquiryElement(item, item.Name.ToString(), new ImageIdentifier(item)));

            var inq = new MultiSelectionInquiryData("Choose your demigryph!",
                "The zoologist offers you his service, one of his demi gryphens he kept in the menagerie is available to you.",
                list, false, 1, 1, "OK", null, onGryphRewardClaimed, null);
            MBInformationManager.ShowMultiSelectionInquiry(inq);
        }

        private void onGryphRewardClaimed(List<InquiryElement> obj)
        {
            Hero.MainHero.AddCustomResource("Prestige", -DemigryphCost);
            var item = obj[0].Identifier as ItemObject;
            Hero.MainHero.PartyBelongedTo.Party.ItemRoster.AddToCounts(item, 1);
            _receivedDemiGryphen = true;
        }

        private void OnNewGameStarted(CampaignGameStarter obj)
        {
            foreach (var settlement in Settlement.All)
                if (settlement.StringId == "town_RL1")
                {
                    _altdorf = settlement;
                    CreateNobleOfTheEmpire();
                }

            void CreateNobleOfTheEmpire()
            {
                var template = MBObjectManager.Instance.GetObject<CharacterObject>(_nobleID);
                if (template != null)
                {
                    _empireNoble = HeroCreator.CreateSpecialHero(template, _altdorf, null, null, 50);
                    _empireNoble.SupporterOf = _altdorf.OwnerClan;
                    _empireNoble.SetName(new TextObject(_empireNoble.FirstName + " " + template.Name),
                        _empireNoble.FirstName);
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
            var menagery = _altdorf.LocationComplex.GetLocationWithId("lordshall");
            var currentloc = _altdorf.LocationComplex.GetLocationOfCharacter(locationchar);
            if (locationchar is null || menagery is null || currentloc is null) return;
            if (currentloc != menagery) _altdorf.LocationComplex.ChangeLocation(locationchar, currentloc, menagery);
        }


        public override void SyncData(IDataStore dataStore)
        {
            dataStore.SyncData("_knowsPlayer", ref _knowsPlayer);
            dataStore.SyncData("_receivedDemiGryphen", ref _receivedDemiGryphen);
            dataStore.SyncData("_constructedBuildings", ref _constructedBuildings);
        }
    }
}