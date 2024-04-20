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

namespace TOR_Core.CampaignMechanics.Menagery
{
    public class MenageryTownBehavior : CampaignBehaviorBase
    {
        private Settlement _altdorf;
        private Hero _zoologist;
        private bool _knowsPlayer;
        private bool _receivedDemiGryphen;
        private const string _zoologistID = "tor_zoologist_empire";
        private const int DemigryphCost = 1000;

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
                campaignGameStarter.AddDialogLine("zoologist_done", "start", "close_window",
                    "Thank you for your service! This week I only lost 3 of my assistants... It's very promising!",
                    () => _receivedDemiGryphen && GryphenZoologistStartCondition(), null, 200);

                campaignGameStarter.AddDialogLine("zoologist_foreign", "start", "close_window",
                    "I neither have the time nor the will to talk to strangers.",
                    () => _receivedDemiGryphen && GryphenZoologistStartCondition(), null, 200);
                
                campaignGameStarter.AddDialogLine("zoologist_introduction", "start", "zoologist_task",
                    "Ah hello again, have you thought about my offer?",
                    () => GryphenZoologistStartCondition() && _knowsPlayer, null, 200);
                campaignGameStarter.AddDialogLine("zoologist_introduction_1", "start", "zoologist_introduction_2",
                    "Hello noble of the empire, May I introduce myself? ",
                    () => !_knowsPlayer && GryphenZoologistStartCondition(), null, 200);
                campaignGameStarter.AddDialogLine("zoologist_introduction_2", "zoologist_introduction_2",
                    "zoologist_introduction_3",
                    "My name is {ZOOLOGIST_NAME}, I am operating the imperial menagerie of the empire here in Altdorf. In the name of our Emporer I conduct experiments to breed demigrphyens on large scale.",
                    () => !_knowsPlayer, null, 200);
                campaignGameStarter.AddDialogLine("zoologist_introduction_3", "zoologist_introduction_3",
                    "zoologist_introduction_4",
                    "It seems such a promising venture, imagining all of our mighty knights are riding into the battles on these majestic creatures.",
                    () => !_knowsPlayer, null, 200);
                campaignGameStarter.AddDialogLine("zoologist_introduction_4", "zoologist_introduction_4",
                    "zoologist_introduction_5",
                    "Don't let me start on all the problems, I could better feed them directly our wardens. They are such impetuous beasts!",
                    () => !_knowsPlayer, null, 200);
                campaignGameStarter.AddDialogLine("zoologist_introduction_5", "zoologist_introduction_5",
                    "zoologist_introduction_6",
                    "As a man of science, I want to study these creatures, and tame them and provide them to our empire. Yet my studies are doomed to fail, since nobody sees value in my experiments and they are too costly... ",
                    () => !_knowsPlayer, null, 200);
                campaignGameStarter.AddDialogLine("zoologist_introduction_6", "zoologist_introduction_6",
                    "zoologist_task",
                    "I propose you a deal, since you seem to be someone interested in adventures, I am sure you can make your voice count through out the empire, letting me continue my experiments... In return I grand you one of my creations!",
                    () => !_knowsPlayer, () => _knowsPlayer = true, 200);

                campaignGameStarter.AddDialogLine("zoologist_task", "zoologist_task", "zoologist_offer",
                    "It will not be easy to convince the Elector Counts for letting me continue my experiments (1000{PRESTIGE_ICON})... But as I say, I am willing to give you one of the exemplars... Free of charge",
                    null, null, 200);

                obj.AddPlayerLine("zoologist_task_agree", "zoologist_offer", "zoologist_success",
                    "So let it be, I will make the authorities agree with your experiments... ({PRESTIGE_COST}{PRESTIGE_ICON}) ",
                    HasEnoughPrestige, SelectDemiGryphen, 200, null);
                obj.AddPlayerLine("zoologist_task_agree", "zoologist_offer", "zoologist_decline",
                    "I can't effort this ({PRESTIGE_COST}{PRESTIGE_ICON}) ", () => !HasEnoughPrestige(), null, 200,
                    null);
                obj.AddPlayerLine("zoologist_task_decline", "zoologist_offer", "zoologist_decline",
                    "I need to think about this.", null, null, 200, null);

                campaignGameStarter.AddDialogLine("zoologist_success", "zoologist_success", "close_window",
                    "Thank you noble! Sigmar will rejoice seeing his majestic riders", null, null, 200);
                campaignGameStarter.AddDialogLine("zoologist_decline", "zoologist_decline", "close_window",
                    "A pitty... Maybe another time.", null, null, 200);

                bool HasEnoughPrestige()
                {
                    var available = Hero.MainHero.GetCustomResourceValue("Prestige");
                    return available >= DemigryphCost;
                }


                bool GryphenZoologistStartCondition()
                {
                    var partner = CharacterObject.OneToOneConversationCharacter;
                    if (partner != null)
                    {
                        return partner.HeroObject.IsZoologist();
                    }

                    return false;
                }
            }
        }
        

        private void SelectDemiGryphen()
        {
            var demigryphens = MBObjectManager.Instance.GetObjectTypeList<ItemObject>().Where(x =>
                x.IsMountable && x.StringId.Contains("tor_empire_mount_demigryph")
            );

            List<InquiryElement> list = new List<InquiryElement>();

            foreach (var item in demigryphens)
            {
                list.Add(new InquiryElement(item, item.Name.ToString(), new ImageIdentifier(item)));
            }

            var inq = new MultiSelectionInquiryData("Choose your demigryph!",
                "The zoologist offers you his service, one of his demi gryphens he kept in the menagerie is available to you.",
                list, false, 1, 1, "OK", null, onRewardClaimed, null);
            MBInformationManager.ShowMultiSelectionInquiry(inq);
        }

        private void onRewardClaimed(List<InquiryElement> obj)
        {
            Hero.MainHero.AddCustomResource("Prestige", -DemigryphCost);
            var item = obj[0].Identifier as ItemObject;
            Hero.MainHero.PartyBelongedTo.Party.ItemRoster.AddToCounts(item, 1);
            _receivedDemiGryphen = true;
        }

        private void OnNewGameStarted(CampaignGameStarter obj)
        {
            foreach (var settlement in Settlement.All)
            {
                if (settlement.StringId == "town_RL1")
                {
                    _altdorf = settlement;
                    CreateZoologistOfTheEmpire();
                }
            }

            void CreateZoologistOfTheEmpire()
            {
                CharacterObject template = MBObjectManager.Instance.GetObject<CharacterObject>(_zoologistID);
                if (template != null)
                {
                    _zoologist = HeroCreator.CreateSpecialHero(template, _altdorf, null, null, 50);
                    _zoologist.SupporterOf = _altdorf.OwnerClan;
                    _zoologist.SetName(new TextObject(_zoologist.FirstName.ToString() + " " + template.Name.ToString()),
                        _zoologist.FirstName);
                    _altdorf.MapFaction.Heroes.Add(_zoologist);
                    HeroHelper.SpawnHeroForTheFirstTime(_zoologist, _altdorf);
                }
            }
        }

        private void OnBeforeMissionStart() => EnforceZoologistLocation();
        private void OnGameMenuOpened(MenuCallbackArgs obj) => EnforceZoologistLocation();

        void EnforceZoologistLocation()
        {
            if (Settlement.CurrentSettlement == null || Settlement.CurrentSettlement != _altdorf) return;
            var locationchar = _altdorf.LocationComplex.GetLocationCharacterOfHero(_zoologist);
            var menagery = _altdorf.LocationComplex.GetLocationWithId("lordshall");
            var currentloc = _altdorf.LocationComplex.GetLocationOfCharacter(locationchar);
            if (locationchar is null || menagery is null || currentloc is null) return;
            if (currentloc != menagery) _altdorf.LocationComplex.ChangeLocation(locationchar, currentloc, menagery);
        }


        public override void SyncData(IDataStore dataStore)
        {
            dataStore.SyncData<bool>("_knowsPlayer", ref _knowsPlayer);
            dataStore.SyncData<bool>("_receivedDemiGryphen", ref _receivedDemiGryphen);
        }
    }
}