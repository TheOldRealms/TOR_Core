using Helpers;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Inventory;
using TaleWorlds.CampaignSystem.LogEntries;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;
using TOR_Core.Extensions;
using TOR_Core.Quests;

namespace TOR_Core.CampaignSupport.TownBehaviours
{
    public class MasterEngineerTownBehaviour : CampaignBehaviorBase
    {
        private bool _knowsPlayer;
        private bool _gaveQuestOffer;
        private readonly string _masterEngineerId = "tor_nulnengineernpc_empire";
        private readonly string _rogueEngineerName = "Goswin";
        private Hero _masterEngineerHero = null;
        private Settlement _nuln;
        private bool _playerIsSkilledEnough;
        private EngineerQuest RunawayPartsQuest;


        private string questDialogId = "str_quest_tor_engineer";

        public override void RegisterEvents()
        {
            CampaignEvents.OnNewGameCreatedEvent.AddNonSerializedListener(this, OnNewGameStarted);
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
            CampaignEvents.GameMenuOpened.AddNonSerializedListener(this, OnGameMenuOpened);
            CampaignEvents.BeforeMissionOpenedEvent.AddNonSerializedListener(this, OnBeforeMissionStart);
        }

        private void OnGameMenuOpened(MenuCallbackArgs obj) => EnforceEngineerLocation();
        private void OnBeforeMissionStart() => EnforceEngineerLocation();

        private void EnforceEngineerLocation()
        {
            if (Settlement.CurrentSettlement == null || Settlement.CurrentSettlement != _nuln) return;
            var locationchar = _nuln.LocationComplex.GetLocationCharacterOfHero(_masterEngineerHero);
            var office = _nuln.LocationComplex.GetLocationWithId("house_2");
            var currentloc = _nuln.LocationComplex.GetLocationOfCharacter(locationchar);
            if (locationchar is null || office is null || currentloc is null) return;
            if (currentloc != office) _nuln.LocationComplex.ChangeLocation(locationchar, currentloc, office);
        }

        private void AddEngineerDialogLines(CampaignGameStarter obj)
        
        {
            //conversation start
            obj.AddDialogLine("engineer_start1", "start", "rogueengineerquestcomplete",GameTexts.FindText(questDialogId,"rogueEngineerFound").ToString(), () => engineerdialogstartcondition() && _knowsPlayer && (rogueengineerquestinprogress() || quest2failed()), null, 200, null);
            obj.AddDialogLine("engineer_start0", "start", "cultistdone",GameTexts.FindText(questDialogId,"cultistDone").ToString() , () => engineerdialogstartcondition() && _knowsPlayer && (cultistquestinprogress() || quest1failed()), null, 200, null);
            obj.AddDialogLine("engineer_start2", "start", "questcheckrogueengineer", GameTexts.FindText(questDialogId,"startRogueEngineerHunt").ToString(), () => engineerdialogstartcondition() && _knowsPlayer && ReturnSucessfullCultistQuest() && !engineerquestcompletecondition(), null, 200, null);
            obj.AddDialogLine("engineer_start3", "start", "close_window",GameTexts.FindText(questDialogId,"cultistInProgress").ToString() , () => engineerdialogstartcondition() && cultistquestinprogress() && _knowsPlayer, null, 200, null);
            obj.AddDialogLine("engineer_start4", "start", "hub", GameTexts.FindText(questDialogId,"hubGreet").ToString(), () => engineerdialogstartcondition() && _knowsPlayer && QuestLineDone(), null, 200, null);
            obj.AddDialogLine("engineer_start5", "start", "playergreet", GameTexts.FindText(questDialogId,"rogueEngineerFirstTime").ToString(), engineerdialogstartcondition, knowledgeoverplayer, 200, null);

            //player greet
            obj.AddPlayerLine("engineer_playergreet1", "playergreet", "playerstartquestcheck", GameTexts.FindText(questDialogId,"playerReconsider").ToString(), () => _gaveQuestOffer && !QuestLineDone()&&!QuestIsInProgress(), null, 200, null);
            obj.AddPlayerLine("engineer_playergreet2", "playergreet", "opengunshopcheck", GameTexts.FindText(questDialogId,"playerGreet0").ToString(),() => !_gaveQuestOffer, null, 200, null);
            obj.AddPlayerLine("engineer_playergreet3", "playergreet", "opengunshopcheck", GameTexts.FindText(questDialogId,"playerGreet1").ToString(),() => !_gaveQuestOffer, null, 200, null);

            //skill check
            obj.AddDialogLine("opengunshopcheck", "opengunshopcheck", "skillcheck", GameTexts.FindText(questDialogId,"engineerSkillCheck").ToString(), null, checkplayerengineerskillrequirements, 200, null);
            obj.AddDialogLine("playerskillcheckfailed", "skillcheck", "close window", GameTexts.FindText(questDialogId,"engineerSkillCheckFailed").ToString(), () => !_playerIsSkilledEnough, null, 200);
            obj.AddDialogLine("playerskillchecksuccess", "skillcheck", "playerpassedskillcheck2",GameTexts.FindText(questDialogId,"engineerSkillCheckPassed").ToString(), () => _playerIsSkilledEnough, null, 200);

            //quest start
            obj.AddDialogLine("playerpassskillcheck2", "playerpassedskillcheck2", "playerstartquestcheck",GameTexts.FindText(questDialogId,"cultistBriefing0").ToString(), null, givequestoffer, 200);
            obj.AddPlayerLine("playerstartquestcheck1", "playerstartquestcheck", "explainquest", GameTexts.FindText(questDialogId,"cultistBriefingPlayer").ToString(), null, null, 200, null);
            obj.AddPlayerLine("playerstartquestcheck2", "playerstartquestcheck", "engineerdeclinequest",GameTexts.FindText(questDialogId,"cultistBriefingDecline").ToString() , null, null, 200, null);
            obj.AddDialogLine("explainquest", "explainquest", "questcheck",GameTexts.FindText(questDialogId,"cultistBriefing1").ToString()  , null, null, 200);
            //alternative quest start
            obj.AddPlayerLine("questcheckrogueengineer1", "questcheckrogueengineer", "startrogueengineerquest", GameTexts.FindText(questDialogId,"rogueEngineerBriefingPlayerAccept0").ToString(), null, null, 200, null);
            obj.AddPlayerLine("questcheckrogueengineer2", "questcheckrogueengineer", "startrogueengineerquest", GameTexts.FindText(questDialogId,"rogueEngineerBriefingPlayerAccept1").ToString(), null, null, 200, null);
            obj.AddPlayerLine("questcheckrogueengineer3", "questcheckrogueengineer", "close_window", GameTexts.FindText(questDialogId,"rogueEngineerBriefingPlayerDecline0").ToString(), null, null, 200, null);
            obj.AddDialogLine("startrogueengineerquest", "startrogueengineerquest", "close_window", GameTexts.FindText(questDialogId,"rogueEngineerBriefingEnd").ToString(), null, QuestBeginRogueEngineer, 200, null);

            //quest start player reaction
            obj.AddPlayerLine("questcheck1", "questcheck", "engineeracceptquest",GameTexts.FindText(questDialogId,"rogueEngineerBriefingPlayerAccept0").ToString() , null, QuestBegin, 200, null);
            obj.AddPlayerLine("questcheck2", "questcheck", "engineeracceptquest", GameTexts.FindText(questDialogId,"rogueEngineerBriefingPlayerAccept1").ToString(), null, QuestBegin, 200, null);
            obj.AddPlayerLine("questcheck3", "questcheck", "engineerdeclinequest", GameTexts.FindText(questDialogId,"cultistPlayerDecline").ToString(), null, null, 200, null);
            obj.AddDialogLine("engineeracceptquest", "engineeracceptquest", "close_window", GameTexts.FindText(questDialogId,"cultistReactionPositive").ToString(), null, null, 200);
            obj.AddDialogLine("engineerdeclinequest", "engineerdeclinequest", "close_window", GameTexts.FindText(questDialogId,"cultistReactionNegative").ToString(), null, null, 200, null);

            //quests failed -both
            obj.AddPlayerLine("engineer_questcomplete1", "rogueengineerquestcomplete", "engineerquestfailed", GameTexts.FindText(questDialogId,"questFail").ToString(),  () => engineerdialogstartcondition() && (quest1failed() || quest2failed()), null, 200, null);
            obj.AddPlayerLine("engineer_questcomplete1", "cultistdone", "engineerquestfailed", GameTexts.FindText(questDialogId,"questFail").ToString(), () => engineerdialogstartcondition() && (quest1failed() || quest2failed()), null, 200, null);
            obj.AddDialogLine("engineer_questfailed", "engineerquestfailed", "playerfailedquest", GameTexts.FindText(questDialogId,"questFailAnswer").ToString(), () => quest1failed() || quest2failed(), null, 200, null);
            obj.AddPlayerLine("playerfailedquest1", "playerfailedquest", "engineeracceptquest", GameTexts.FindText(questDialogId,"questRepeat").ToString(), quest1failed, ResetQuest, 200, null);
            obj.AddPlayerLine("playerfailedquest2", "playerfailedquest", "engineeracceptquest", GameTexts.FindText(questDialogId,"questRepeat").ToString(), quest2failed, ResetQuest, 200, null);
            obj.AddPlayerLine("playerfailedquest3", "playerfailedquest", "engineerdeclinequest", GameTexts.FindText(questDialogId,"questGiveup").ToString(), null, null, 200, null);

            //CULTIST quest
            //done
            obj.AddPlayerLine("engineer_questcomplete3", "cultistdone", "cultistengineerdebrief", GameTexts.FindText(questDialogId,"cultistReturn0").ToString(), () => engineerdialogstartcondition() && ReturnSucessfullCultistQuest(), null, 200, null);
            obj.AddDialogLine("cultistengineerdebrief", "cultistengineerdebrief", "cultistengineerdebrief2", GameTexts.FindText(questDialogId,"cultistReturn1").ToString(), null, null, 200, null);
            obj.AddPlayerLine("cultistengineerdebrief2", "cultistengineerdebrief2", "cultistengineerdebrief3", GameTexts.FindText(questDialogId,"cultistReturn2").ToString(), null, null, 200, null);
            obj.AddDialogLine("cultistengineerdebrief3", "cultistengineerdebrief3", "questrogueengineer",GameTexts.FindText(questDialogId,"cultistReturn3").ToString() , null, null, 200, null);
            obj.AddDialogLine("questrogueengineer", "questrogueengineer", "questcheckrogueengineer",  GameTexts.FindText(questDialogId,"rogueEngineerQuestStart").ToString(), null, null, 200, null);
            // in progress
            obj.AddPlayerLine("engineer_questcomplete3", "cultistdone", "cultistquestinprogress", GameTexts.FindText(questDialogId,"cultistInProgressPlayer").ToString(), null, null, 200, null);
            obj.AddDialogLine("cultistquestinprogress", "cultistquestinprogress", "close_window", GameTexts.FindText(questDialogId,"cultistInProgressAnswer").ToString(), null, null, 200, null);

            //GOSWIN quest
            //done
            obj.AddPlayerLine("rogueengineerquestcomplete", "rogueengineerquestcomplete", "engineerquestdebrief", GameTexts.FindText(questDialogId,"rogueEngineerHandIn").ToString(), () => engineerdialogstartcondition() && engineerquestcompletecondition(), null, 200, null);
            obj.AddDialogLine("engineerquestdebrief", "engineerquestdebrief", "hubaftermission",GameTexts.FindText(questDialogId,"rogueEngineerDebrief").ToString() , null, handing_in_rogueengineer_quest, 200, null);
            obj.AddDialogLine("hubaftermission", "hubaftermission", "hub", GameTexts.FindText(questDialogId,"hubEntry").ToString(), null, null, 200);
            //in progress
            obj.AddPlayerLine("rogueengineerquestcomplete", "rogueengineerquestcomplete", "engineerquestinprogress", GameTexts.FindText(questDialogId,"rogueEngineerInProgressPlayer").ToString(), null, null, 200, null);
            obj.AddDialogLine("engineerquestinprogress", "engineerquestinprogress", "close_window", GameTexts.FindText(questDialogId,"rogueEngineerInProgressAnswer").ToString(), null, null, 200, null);

            //hub player
            obj.AddPlayerLine("engineer_hub1", "hub", "opengunshop", GameTexts.FindText(questDialogId,"hubPlayerCannons").ToString(), null, null, 200, null);
            obj.AddPlayerLine("engineer_hub2", "hub", "recruitengineer", GameTexts.FindText(questDialogId,"hubPlayerEngineers").ToString(), null, null, 200, null);
            obj.AddPlayerLine("engineer_hub3", "hub", "tutorialcannonbuy", GameTexts.FindText(questDialogId,"hubPlayerCannonsLimitInstruction").ToString(), null, null, 200, null);
            obj.AddPlayerLine("engineer_hub4", "hub", "tutorialcannonuse", GameTexts.FindText(questDialogId,"hubPlayerCannonsInstruction").ToString(), null, null, 200, null);
            obj.AddPlayerLine("engineer_hub5", "hub", "close_window", GameTexts.FindText(questDialogId,"hubPlayerLeave").ToString(), null, null, 200, null);
            
            

            // shop
            obj.AddDialogLine("opengunshop", "opengunshop", "opengunshopandclosedialog", GameTexts.FindText(questDialogId,"openShop").ToString(), null, opengunshopconsequence, 200);
            obj.AddDialogLine("opengunshopandclosedialog", "opengunshopandclosedialog", "hub", GameTexts.FindText(questDialogId,"closeShop").ToString(), null, null, 200);
            //recruitment
            obj.AddDialogLine("recruitengineer", "recruitengineer", "recruitmentoptions", TORCommon.GetCompleteStringValue(GameTexts.FindText(questDialogId,"hireEngineers")), UpdateRecruitmentPrices, null, 200);
            obj.AddPlayerLine("recruitengineer_option1", "recruitmentoptions", "opengunshopandclosedialog", GameTexts.FindText(questDialogId,"hireEngineersAccept").ToString(), () => playerhasenoughmoney(), cannoncrewrecruitmentconsequence, 200);
            obj.AddPlayerLine("recruitengineer_option1", "recruitmentoptions", "opengunshopandclosedialog", GameTexts.FindText(questDialogId,"hireEngineersNotEnoughMoney").ToString(), () => !playerhasenoughmoney(), null, 200);
            obj.AddPlayerLine("recruitengineer_option1", "recruitmentoptions", "opengunshopandclosedialog", GameTexts.FindText(questDialogId,"hireEngineersDecline").ToString(), null, null, 200);

            //tutorial buy cannons
            obj.AddDialogLine("tutorialcannonbuy", "tutorialcannonbuy", "tutorialcannonbuy2", GameTexts.FindText(questDialogId,"CannonsLimitInstruction0").ToString(), null, null, 200);
            obj.AddDialogLine("tutorialcannonbuy2", "tutorialcannonbuy2", "tutorialcannonbuy3", GameTexts.FindText(questDialogId,"CannonsLimitInstruction1").ToString(), null, null, 200);
            obj.AddDialogLine("tutorialcannonbuy3", "tutorialcannonbuy3", "hub", GameTexts.FindText(questDialogId,"CannonsLimitInstruction2").ToString(), null, null, 200);
            //tutorial use cannons
            obj.AddDialogLine("tutorialcannonuse", "tutorialcannonuse", "tutorialcannonuse2", GameTexts.FindText(questDialogId,"CannonsUse0").ToString(), null, null, 200);
            obj.AddDialogLine("tutorialcannonuse2", "tutorialcannonuse2", "tutorialcannonuse3", GameTexts.FindText(questDialogId,"CannonsUse1").ToString(), null, null, 200);
            obj.AddDialogLine("tutorialcannonuse3", "tutorialcannonuse3", "hub", GameTexts.FindText(questDialogId,"CannonsUse2").ToString(), null, null, 200);
        }

        private void AddCultistDialogLines(CampaignGameStarter obj)
        {
            obj.AddDialogLine("engineerquestcultist_start", "start", "cultist_answerplayer", GameTexts.FindText(questDialogId,"cultistEncounter").ToString(), cultiststartcondition, null, 200);
            obj.AddPlayerLine("cultist_answerplayer", "cultist_answerplayer", "cultist_answer", GameTexts.FindText(questDialogId,"cultistEncounterPlayer0").ToString(), null, null, 200);
            obj.AddPlayerLine("cultist_answerplayer", "cultist_answerplayer", "cultist_answer", GameTexts.FindText(questDialogId,"cultistEncounterPlayer1").ToString(), null, null, 200);
            obj.AddPlayerLine("cultist_answerplayer", "cultist_answerplayer", "close_window", GameTexts.FindText(questDialogId,"cultistEncounterPlayer2").ToString(), null, null, 200);
            obj.AddDialogLine("cultist_answer", "cultist_answer", "close_window", GameTexts.FindText(questDialogId,"cultistEncounterAnswer").ToString(), null, null, 200);
        }

        private void AddRogueEngineerDialogLines(CampaignGameStarter obj)
        {
            obj.AddDialogLine("rogueengineer_start", "start", "rogueengineer_answerplayer", GameTexts.FindText(questDialogId,"rogueEngineerEncounter").ToString(), rogueengineerdialogstartcondition, null, 200);

            //requires dying dialog of the engineer, here is a player response
            obj.AddPlayerLine("rogueengineer_playerafterbattle", "rogueengineer_playerafterbattle", "close_window", GameTexts.FindText(questDialogId,"rogueEngineerEncounterPlayerAfterBattleAnswer").ToString(), null, null, 200);
        }
        
        

        private void OnSessionLaunched(CampaignGameStarter obj)
        {
            MBTextManager.SetTextVariable("ROGUE_ENGINEER_NAME", _rogueEngineerName);
            MBTextManager.SetTextVariable("PLAYERNAME", Hero.MainHero.Name);
            _nuln = Settlement.All.FirstOrDefault(x => x.StringId == "town_WI1");
            AddEngineerDialogLines(obj);
            AddCultistDialogLines(obj);
            AddRogueEngineerDialogLines(obj);
        }

        private bool rogueengineerquestinprogress()
        {
            if (RunawayPartsQuest == null) return false;
            if (RunawayPartsQuest.IsFinalized) return false;
            var currentprogress = (EngineerQuestStates)RunawayPartsQuest.GetCurrentProgress();
            return currentprogress == EngineerQuestStates.RogueEngineerhunt || currentprogress == EngineerQuestStates.HandInRogueEngineerHunt;
        }

        private bool cultistquestinprogress()
        {
            if (RunawayPartsQuest == null) return false;
            if (RunawayPartsQuest.IsFinalized) return false;
            return RunawayPartsQuest.GetCurrentProgress() == (int)EngineerQuestStates.Cultisthunt||RunawayPartsQuest.GetCurrentProgress() == (int)EngineerQuestStates.HandInCultisthunt;
        }

        private bool quest1failed()
        {
            if (RunawayPartsQuest == null) return false;
            if (RunawayPartsQuest.GetCurrentProgress() == (int)EngineerQuestStates.Cultisthunt) return RunawayPartsQuest.FailState;
            return false;
        }

        private bool quest2failed()
        {
            if (RunawayPartsQuest == null) return false;
            if (RunawayPartsQuest.GetCurrentProgress() == (int)EngineerQuestStates.RogueEngineerhunt)
            {
                return RunawayPartsQuest.FailState;
            }

            return false;
        }

        private bool engineerquestcompletecondition()
        {
            if (RunawayPartsQuest == null) return false;
            if (RunawayPartsQuest.IsFinalized) return false;
            var progress = (EngineerQuestStates)RunawayPartsQuest.GetCurrentProgress();
            if (progress != EngineerQuestStates.HandInRogueEngineerHunt) return false;
            return true;
        }

        private bool cultisthuntcompletecondition()
        {
            if (RunawayPartsQuest == null) return false;
            if (RunawayPartsQuest.JournalEntries[0].HasBeenCompleted())
            {
                return true;
            }

            return false;
        }

        private void handing_in_rogueengineer_quest()
        {
            RunawayPartsQuest.UpdateProgressOnQuest();
            var xp = (float)250f;
            SkillObject skill = DefaultSkills.Engineering;
            Hero.MainHero.AddSkillXp(skill, xp);
            if (!Hero.MainHero.HasAttribute("AbilityUser")) Hero.MainHero.AddAttribute("AbilityUser");
            if (!Hero.MainHero.HasAttribute("CanPlaceArtillery")) Hero.MainHero.AddAttribute("CanPlaceArtillery");
        }

        private bool ReturnSucessfullCultistQuest()
        {
            if (RunawayPartsQuest == null) return false;
            if (RunawayPartsQuest.GetCurrentProgress() != 1) return false;
            return RunawayPartsQuest.JournalEntries[1].CurrentProgress == 0;
        }

        public bool QuestIsInProgress() => RunawayPartsQuest != null && RunawayPartsQuest.GetCurrentProgress() < (int)EngineerQuestStates.HandInRogueEngineerHunt;
        private bool QuestLineDone() => RunawayPartsQuest != null && RunawayPartsQuest.IsFinalized;
        private void givequestoffer() => _gaveQuestOffer = true;

        private void opengunshopconsequence()
        {
            var engineerItems = MBObjectManager.Instance.GetObjectTypeList<ItemObject>().Where(x => x.IsTorItem() && (x.StringId.Contains("gun") || x.StringId.Contains("artillery")));
            var ammo = MBObjectManager.Instance.GetObject<ItemObject>("tor_neutral_weapon_ammo_musket_ball");
            List<ItemRosterElement> list = new List<ItemRosterElement>();
            foreach (var item in engineerItems)
            {
                list.Add(new ItemRosterElement(item, MBRandom.RandomInt(1, 5)));
            }

            ItemRoster roster = new ItemRoster();
            roster.Add(list);
            if (ammo != null) roster.Add(new ItemRosterElement(ammo, MBRandom.RandomInt(2, 5)));
            InventoryManager.OpenScreenAsTrade(roster, _nuln.Town);
        }

        private void cannoncrewrecruitmentconsequence()
        {
            var noviceengineer = MBObjectManager.Instance.GetObject<CharacterObject>("tor_empire_novice_engineer");
            var price = Campaign.Current.Models.PartyWageModel.GetTroopRecruitmentCost(noviceengineer, Hero.MainHero, false) * 2 * 10;
            GiveGoldAction.ApplyBetweenCharacters(Hero.MainHero, null, price);
            MobileParty.MainParty.MemberRoster.AddToCounts(noviceengineer, 2);
        }

        private void ResetQuest()
        {
            RunawayPartsQuest.ResetQuestinCurrentState();
        }

        private void QuestBegin()
        {
            if (RunawayPartsQuest != null)
            {
                return;
            }

            RunawayPartsQuest = TORQuestHelper.GetNewEngineerQuest(true);
            RunawayPartsQuest.StartQuest();
        }

        private void QuestBeginRogueEngineer()
        {
            RunawayPartsQuest.UpdateProgressOnQuest();
        }

        private void knowledgeoverplayer() => _knowsPlayer = true;

        private bool engineerdialogstartcondition()
        {
            
            var partner = CharacterObject.OneToOneConversationCharacter;



            if (partner != null)
            {
                return partner.HeroObject.IsMasterEngineer();
            } 
                
            return false;
        }

        private bool cultiststartcondition()
        {
            if (RunawayPartsQuest == null) return false;
            if (!RunawayPartsQuest.IsOngoing) return false;
            if (Campaign.Current.CurrentConversationContext != ConversationContext.PartyEncounter) return false;
            if (RunawayPartsQuest.CultistQuestIsActive()) return Campaign.Current.ConversationManager.ConversationParty == RunawayPartsQuest.TargetParty;
            return false;
        }

        private bool rogueengineerdialogstartcondition()
        {
            if (RunawayPartsQuest == null) return false;
            if (!RunawayPartsQuest.IsOngoing) return false;
            if (Campaign.Current.CurrentConversationContext != ConversationContext.PartyEncounter) return false;
            if (!RunawayPartsQuest.RogueEngineerQuestPartIsActive()) return false;
            var partner = CharacterObject.OneToOneConversationCharacter;
            return partner != null && partner.Occupation == Occupation.Lord && partner.HeroObject.Name.Contains(_rogueEngineerName);
        }

        private void checkplayerengineerskillrequirements()
        {
            if (Hero.MainHero.GetSkillValue(DefaultSkills.Engineering) >= 50) _playerIsSkilledEnough = true;
            else
            {
                _playerIsSkilledEnough = false;
            }
        }

        private bool UpdateRecruitmentPrices()
        {
            setrecruitmentprice();
            return true;
        }
        
        private void setrecruitmentprice()
        {
            var noviceengineer = MBObjectManager.Instance.GetObject<CharacterObject>("tor_empire_novice_engineer");
            var price = Campaign.Current.Models.PartyWageModel.GetTroopRecruitmentCost(noviceengineer, Hero.MainHero, false) * 2 * 10;
            MBTextManager.SetTextVariable("RECRUITMENT_PRICE", price.ToString() + "{GOLD_ICON}");


        }

        private bool playerhasenoughmoney()
        {
            var noviceengineer = MBObjectManager.Instance.GetObject<CharacterObject>("tor_empire_novice_engineer");
            var price = Campaign.Current.Models.PartyWageModel.GetTroopRecruitmentCost(noviceengineer, Hero.MainHero, false) * 2 * 10;
            return Hero.MainHero.Gold > price;
        }

        private void OnNewGameStarted(CampaignGameStarter obj)
        {
            foreach (var settlement in Settlement.All)
            {
                if (settlement.StringId == "town_WI1")
                {
                    _nuln = settlement;
                    CreateEngineer();
                }
            }
        }

        private void CreateEngineer()
        {
            CharacterObject template = MBObjectManager.Instance.GetObject<CharacterObject>(_masterEngineerId);
            if (template != null)
            {
                _masterEngineerHero = HeroCreator.CreateSpecialHero(template, _nuln, null, null, 50);
                _masterEngineerHero.SupporterOf = _nuln.OwnerClan;
                _masterEngineerHero.SetName(new TextObject(_masterEngineerHero.FirstName.ToString() + " " + template.Name.ToString()), _masterEngineerHero.FirstName);
                HeroHelper.SpawnHeroForTheFirstTime(_masterEngineerHero, _nuln);
            }
        }

        public override void SyncData(IDataStore dataStore)
        {
            dataStore.SyncData<bool>("_gaveQuestOffer", ref _gaveQuestOffer);
            dataStore.SyncData<bool>("_knowsPlayer", ref _knowsPlayer);
            dataStore.SyncData<Hero>("_masterEngineerHero", ref _masterEngineerHero);
            dataStore.SyncData<EngineerQuest>("Engineerquest", ref RunawayPartsQuest);
        }
    }
}