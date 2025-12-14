using HarmonyLib;
using Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Inventory;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.LinQuick;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;
using TOR_Core.CampaignMechanics.CustomResources;
using TOR_Core.CharacterDevelopment;
using TOR_Core.Extensions;
using TOR_Core.Quests;
using TOR_Core.Utilities;

namespace TOR_Core.CampaignSupport.TownBehaviours
{
    public class MasterEngineerTownBehaviour : CampaignBehaviorBase
    {
        private bool _knowsPlayer;
        private bool _gaveQuestOffer;
        private bool _unlockWeaponsTier1;
        private bool _explained;
        private readonly string _masterEngineerId = "tor_nulnengineernpc_empire";
        private Hero _masterEngineerHero = null;
        private Settlement _nuln;
        private bool _playerIsSkilledEnough;
        private EngineerQuest RunawayPartsQuest;

        private string questDialogId = "str_quest_tor_engineer";

        private string GetRogueEngineerName()
        {
            return new TextObject("{=str_tor_rogue_engineer_name}Goswin").ToString();
        }
        public override void RegisterEvents()
        {
            CampaignEvents.OnNewGameCreatedEvent.AddNonSerializedListener(this, OnNewGameStarted);
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
            CampaignEvents.GameMenuOpened.AddNonSerializedListener(this, OnGameMenuOpened);
            CampaignEvents.BeforeMissionOpenedEvent.AddNonSerializedListener(this, OnBeforeMissionStart);
            CampaignEvents.DailyTickSettlementEvent.AddNonSerializedListener(this, DailyTickSettlement);
        }

        private void DailyTickSettlement(Settlement settlement)
        {
            if (settlement.Culture.StringId != TORConstants.Cultures.BRETONNIA || !settlement.IsTown) return;
            if (settlement.Town.Workshops.Any(x => x.WorkshopType.StringId == "wood_WorkshopType"))
            {
                var trebuchetItem = MBObjectManager.Instance.GetObject<ItemObject>("tor_bretonnia_artillery_fieldtrebuchet_001");
                var trebIndex = settlement.ItemRoster.FindIndexOfItem(trebuchetItem);
                if (trebIndex < 0)
                {
                    if (trebuchetItem != null)
                    {
                        settlement.ItemRoster.Add(new ItemRosterElement(trebuchetItem, 1));
                    }
                }
            }
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
            obj.AddDialogLine("engineer_start1", "start", "rogueengineerquestcomplete", TORTextHelper.GetText("str_quest_tor_engineer", "rogueEngineerFound", "Did you find {ROGUE_ENGINEER_NAME}?"), () => engineerdialogstartcondition() && _knowsPlayer && (rogueengineerquestinprogress() || quest2failed()), null, 200, null);
            obj.AddDialogLine("engineer_start0", "start", "cultistdone", TORTextHelper.GetText("str_quest_tor_engineer", "cultistDone", "Ah, you have returned. What news do you bring?"), () => engineerdialogstartcondition() && _knowsPlayer && (cultistquestinprogress() || quest1failed()), null, 200, null);
            obj.AddDialogLine("engineer_start2", "start", "questcheckrogueengineer", TORTextHelper.GetText("str_quest_tor_engineer", "startRogueEngineerHunt", "Have you changed your mind and want to help hunt down {ROGUE_ENGINEER_NAME}?"), () => engineerdialogstartcondition() && _knowsPlayer && ReturnSucessfullCultistQuest() && !engineerquestcompletecondition(), null, 200, null);
            obj.AddDialogLine("engineer_start3", "start", "close_window", TORTextHelper.GetText("str_quest_tor_engineer", "cultistInProgress", "Come back when you have news."), () => engineerdialogstartcondition() && cultistquestinprogress() && _knowsPlayer, null, 200, null);
            obj.AddDialogLine("engineer_start4", "start", "hub", TORTextHelper.GetText("str_quest_tor_engineer", "hubGreet", "You again, what do you want?"), () => engineerdialogstartcondition() && _knowsPlayer && QuestLineDone(), null, 200, null);
            obj.AddDialogLine("engineer_start5", "start", "playergreet", TORTextHelper.GetText("str_quest_tor_engineer", "rogueEngineerFirstTime", "You have the look of someone who has never seen a speck of black powder nor grease. Are you in the right place?"), engineerdialogstartcondition, knowledgeoverplayer, 200, null);

            //player greet
            obj.AddPlayerLine("engineer_playergreet1", "playergreet", "playerstartquestcheck", TORTextHelper.GetText("str_quest_tor_engineer", "playerReconsider", "I have reconsidered your offer, I would like to help."), () => _gaveQuestOffer && !QuestLineDone() && !QuestIsInProgress(), null, 200, null);
            obj.AddPlayerLine("engineer_playergreet2", "playergreet", "opengunshopcheck", TORTextHelper.GetText("str_quest_tor_engineer", "playerGreet0", "Greetings Master Engineer, I am {PlAYERNAME}. I have come seeking access to the forges of Nuln. Can you help?"), () => !_gaveQuestOffer, null, 200, null);
            obj.AddPlayerLine("engineer_playergreet3", "playergreet", "opengunshopcheck", TORTextHelper.GetText("str_quest_tor_engineer", "playerGreet1", "I am, I have come seeking access to black powder weapons."), () => !_gaveQuestOffer, null, 200, null);

            //skill check
            obj.AddDialogLine("opengunshopcheck", "opengunshopcheck", "skillcheck", TORTextHelper.GetText("str_quest_tor_engineer", "engineerSkillCheck", "Hah! You don't seem like a person with any knowledge of our crafts. What could you possibly need from us?"), null, checkplayerengineerskillrequirements, 200, null);
            obj.AddDialogLine("playerskillcheckfailed", "skillcheck", "close window", TORTextHelper.GetText("str_quest_tor_engineer", "engineerSkillCheckFailed", "I am far too busy for this, leave my sight."), () => !_playerIsSkilledEnough, null, 200);
            obj.AddDialogLine("playerskillchecksuccess", "skillcheck", "playerpassedskillcheck2", TORTextHelper.GetText("str_quest_tor_engineer", "engineerSkillCheckPassed", "These are the mightiest weapons of the Empire. The inventions of our Gunnery School are what held back the tide of darkness time and again. Normally we do not hand out our crafts to any who walks in, you have not earned our trust."), () => _playerIsSkilledEnough && Hero.MainHero.Culture.StringId == TORConstants.Cultures.EMPIRE && !Hero.MainHero.IsVampire(), null, 200);

            //quest start
            obj.AddDialogLine("playerpassskillcheck2", "playerpassedskillcheck2", "playerstartquestcheck", TORTextHelper.GetText("str_quest_tor_engineer", "cultistBriefing0", "We may however be able to come to an agreement, there is an internal matter that needs urgent attention and I am unable to act. If you help us out, as a personal favour, I will see what I can do for you. What say you?"), null, givequestoffer, 200);
            obj.AddPlayerLine("playerstartquestcheck1", "playerstartquestcheck", "explainquest", TORTextHelper.GetText("str_quest_tor_engineer", "cultistBriefingPlayer", "What would you have me do?"), null, null, 200, null);
            obj.AddPlayerLine("playerstartquestcheck2", "playerstartquestcheck", "engineerdeclinequest", TORTextHelper.GetText("str_quest_tor_engineer", "cultistBriefingDecline", "I don't have time for this."), null, null, 200, null);
            obj.AddDialogLine("explainquest", "explainquest", "questcheck", TORTextHelper.GetText("str_quest_tor_engineer", "cultistBriefing1", "Usually we don't resort to outside assistance, but we are shorthanded. We have had some important components stolen from the forges of Nuln, and they must be returned. Immediately. If you can track down these runaways and find these parts then we can talk further."), null, null, 200);
            //alternative quest start
            obj.AddPlayerLine("questcheckrogueengineer1", "questcheckrogueengineer", "startrogueengineerquest", TORTextHelper.GetText("str_quest_tor_engineer", "rogueEngineerBriefingPlayerAccept0", "I can, as long as our bargain remains the same. I will find him for you, and in return, you will allow me access to the forges of Nuln."), null, null, 200, null);
            obj.AddPlayerLine("questcheckrogueengineer2", "questcheckrogueengineer", "startrogueengineerquest", TORTextHelper.GetText("str_quest_tor_engineer", "rogueEngineerBriefingPlayerAccept1", "If this is the only way you will allow me access to the forges, then so be it. I will bring you his head."), null, null, 200, null);
            obj.AddPlayerLine("questcheckrogueengineer3", "questcheckrogueengineer", "close_window", TORTextHelper.GetText("str_quest_tor_engineer", "rogueEngineerBriefingPlayerDecline0", "I'm afraid not, I have other tasks to attend to."), null, null, 200, null);
            obj.AddDialogLine("startrogueengineerquest", "startrogueengineerquest", "close_window", TORTextHelper.GetText("str_quest_tor_engineer", "rogueEngineerBriefingEnd", "We have an agreement then, I believe I may know his whereabouts. I will mark it on your map for you, may Sigmar guide you stranger."), null, QuestBeginRogueEngineer, 200, null);

            //quest start player reaction
            obj.AddPlayerLine("questcheck1", "questcheck", "engineeracceptquest", TORTextHelper.GetText("str_quest_tor_engineer", "cultistPlayerAccept0", "I understand, I will return the moment I have news."), null, QuestBegin, 200, null);
            obj.AddPlayerLine("questcheck2", "questcheck", "engineeracceptquest", TORTextHelper.GetText("str_quest_tor_engineer", "cultistPlayerAccept1", "That is all it will take? Sounds easy enough."), null, QuestBegin, 200, null);
            obj.AddPlayerLine("questcheck3", "questcheck", "engineerdeclinequest", TORTextHelper.GetText("str_quest_tor_engineer", "cultistPlayerDecline", "I do not have time for this."), null, null, 200, null);
            obj.AddDialogLine("engineeracceptquest", "engineeracceptquest", "close_window", TORTextHelper.GetText("str_quest_tor_engineer", "cultistReactionPositive", "Good, I expect positive results and your hasty return."), null, null, 200);
            obj.AddDialogLine("engineerdeclinequest", "engineerdeclinequest", "close_window", TORTextHelper.GetText("str_quest_tor_engineer", "cultistReactionNegative", "A shame, think on it and return if you change your mind."), null, null, 200, null);

            //quests failed -both
            obj.AddPlayerLine("engineer_questcomplete1", "rogueengineerquestcomplete", "engineerquestfailed", TORTextHelper.GetText("str_quest_tor_engineer", "questFail", "I am afraid I have failed to bring what you ask."), () => engineerdialogstartcondition() && (quest1failed() || quest2failed()), null, 200, null);
            obj.AddPlayerLine("engineer_questcomplete1", "cultistdone", "engineerquestfailed", TORTextHelper.GetText("str_quest_tor_engineer", "questFail", "I am afraid I have failed to bring what you ask."), () => engineerdialogstartcondition() && (quest1failed() || quest2failed()), null, 200, null);
            obj.AddDialogLine("engineer_questfailed", "engineerquestfailed", "playerfailedquest", TORTextHelper.GetText("str_quest_tor_engineer", "questFailAnswer", "Tsk, I expected better. There may still be time, you can still track them if you are swift"), () => quest1failed() || quest2failed(), null, 200, null);
            obj.AddPlayerLine("playerfailedquest1", "playerfailedquest", "engineeracceptquest", TORTextHelper.GetText("str_quest_tor_engineer", "questRepeat", "I won't let you down a second time."), quest1failed, ResetQuest, 200, null);
            obj.AddPlayerLine("playerfailedquest2", "playerfailedquest", "engineeracceptquest", TORTextHelper.GetText("str_quest_tor_engineer", "questRepeat", "I won't let you down a second time."), quest2failed, ResetQuest, 200, null);
            obj.AddPlayerLine("playerfailedquest3", "playerfailedquest", "engineerdeclinequest", TORTextHelper.GetText("str_quest_tor_engineer", "questGiveup", "I don't think I can do it at this time."), null, null, 200, null);

            //CULTIST quest
            //done
            obj.AddPlayerLine("engineer_questcomplete3", "cultistdone", "cultistengineerdebrief", TORTextHelper.GetText("str_quest_tor_engineer", "cultistReturn0", "I have returned but without the stolen components, I am afraid to say they are still missing."), () => engineerdialogstartcondition() && ReturnSucessfullCultistQuest(), null, 200, null);
            obj.AddDialogLine("cultistengineerdebrief", "cultistengineerdebrief", "cultistengineerdebrief2", TORTextHelper.GetText("str_quest_tor_engineer", "cultistReturn1", "I see, this is not what I had hoped for. Were there any further clues, did you interrogate these scoundrels?"), null, null, 200, null);
            obj.AddPlayerLine("cultistengineerdebrief2", "cultistengineerdebrief2", "cultistengineerdebrief3", TORTextHelper.GetText("str_quest_tor_engineer", "cultistReturn2", "One of the bandits did mention a name, {ROGUE_ENGINEER_NAME} I think?"), null, null, 200, null);
            obj.AddDialogLine("cultistengineerdebrief3", "cultistengineerdebrief3", "questrogueengineer", TORTextHelper.GetText("str_quest_tor_engineer", "cultistReturn3", "Blast! I should have known. If you are willing, I would ask for your assistance once more. This matter may be more dire than I originally imagined. {ROGUE_ENGINEER_NAME} is an engineer, a good one at that, but his works always seemed… wrong."), null, null, 200, null);
            obj.AddDialogLine("questrogueengineer", "questrogueengineer", "questcheckrogueengineer", TORTextHelper.GetText("str_quest_tor_engineer", "rogueEngineerQuestStart", "If he has stolen these parts, it can only mean some heinous scheme. I must ask that you track him down, and put an end to whatever madness he is trying to concoct. Will you assist us?"), null, null, 200, null);
            // in progress
            obj.AddPlayerLine("engineer_questcomplete3", "cultistdone", "cultistquestinprogress", TORTextHelper.GetText("str_quest_tor_engineer", "cultistInProgressPlayer", "I have yet to track down the runaways."), null, null, 200, null);
            obj.AddDialogLine("cultistquestinprogress", "cultistquestinprogress", "close_window", TORTextHelper.GetText("str_quest_tor_engineer", "cultistInProgressAnswer", "I see, return to me when you have something useful."), null, null, 200, null);

            //GOSWIN quest
            //done
            obj.AddPlayerLine("rogueengineerquestcomplete", "rogueengineerquestcomplete", "engineerquestdebrief", TORTextHelper.GetText("str_quest_tor_engineer", "rogueEngineerHandIn", "{ROGUE_ENGINEER_NAME} will no longer be a problem and I have retrieved what he stole from you. I'm unsure what he was trying to do with them."), () => engineerdialogstartcondition() && engineerquestcompletecondition(), null, 200, null);
            obj.AddDialogLine("engineerquestdebrief", "engineerquestdebrief", "hubaftermission", TORTextHelper.GetText("str_quest_tor_engineer", "rogueEngineerDebrief", "It matters not, it would have been something warped no doubt. I must thank you for your efforts, and your discretion. As agreed upon, you may now access our foundries and place orders as you please."), null, handing_in_rogueengineer_quest, 200, null);
            obj.AddDialogLine("hubaftermission", "hubaftermission", "hub", TORTextHelper.GetText("str_quest_tor_engineer", "hubEntry", "Now, what do you need?"), null, null, 200);
            //in progress
            obj.AddPlayerLine("rogueengineerquestcomplete", "rogueengineerquestcomplete", "engineerquestinprogress", TORTextHelper.GetText("str_quest_tor_engineer", "rogueEngineerInProgressPlayer", "I have yet to track him down"), null, null, 200, null);
            obj.AddDialogLine("engineerquestinprogress", "engineerquestinprogress", "close_window", TORTextHelper.GetText("str_quest_tor_engineer", "rogueEngineerInProgressAnswer", "I see, return to me when you have better news."), null, null, 200, null);

            //hub player
            obj.AddPlayerLine("engineer_hub1", "hub", "opengunshop", TORTextHelper.GetText("str_quest_tor_engineer", "hubPlayerCannons", "I would like to buy some equipment."), null, null, 200, null);
            obj.AddPlayerLine("engineer_hub_upgrade", "hub", "upgradeshop", TORTextHelper.GetText("str_engineer_hub_upgrade", "Your selection leaves something to be desired. Where is your good stuff?"), () => !HasUpgradeGunShopCondition(3), null, 200, null);
            obj.AddPlayerLine("engineer_hub2", "hub", "recruitengineer", TORTextHelper.GetText("str_quest_tor_engineer", "hubPlayerEngineers", "I would like to recruit some engineers."), null, null, 200, null);
            obj.AddPlayerLine("engineer_hub3", "hub", "tutorialcannonbuy", TORTextHelper.GetText("str_quest_tor_engineer", "hubPlayerCannonsLimitInstruction", "How can I buy more cannons?"), null, null, 200, null);
            obj.AddPlayerLine("engineer_hub4", "hub", "tutorialcannonuse", TORTextHelper.GetText("str_quest_tor_engineer", "hubPlayerCannonsInstruction", "How can I use cannons?"), null, null, 200, null);
            obj.AddPlayerLine("engineer_hub5", "hub", "close_window", TORTextHelper.GetText("str_quest_tor_engineer", "hubPlayerLeave", "Nothing at the moment, I must leave."), null, null, 200, null);

            // shop
            obj.AddDialogLine("opengunshop", "opengunshop", "opengunshopandclosedialog", TORTextHelper.GetText("str_quest_tor_engineer", "openShop", "Of course, you'll find only the best from the forges of Nuln!"), null, opengunshopconsequence, 200);
            obj.AddDialogLine("opengunshopandclosedialog", "opengunshopandclosedialog", "hub", TORTextHelper.GetText("str_quest_tor_engineer", "closeShop", "What else can I do for you?"), null, null, 200);
            //recruitment
            obj.AddDialogLine("recruitengineer", "recruitengineer", "recruitmentoptions", TORTextHelper.GetText("str_quest_tor_engineer", "hireEngineers", "A pair of our novice engineers are eagier to join you for the right price ({RECRUITMENT_PRICE})."), UpdateRecruitmentPrices, null, 200);
            obj.AddPlayerLine("recruitengineer_option1", "recruitmentoptions", "opengunshopandclosedialog", TORTextHelper.GetText("str_quest_tor_engineer", "hireEngineersAccept", "Welcome on board."), () => playerhasenoughmoney(), cannoncrewrecruitmentconsequence, 200);
            obj.AddPlayerLine("recruitengineer_option1", "recruitmentoptions", "opengunshopandclosedialog", TORTextHelper.GetText("str_quest_tor_engineer", "hireEngineersNotEnoughMoney", "I don't have the funds for them right now."), () => !playerhasenoughmoney(), null, 200);
            obj.AddPlayerLine("recruitengineer_option1", "recruitmentoptions", "opengunshopandclosedialog", TORTextHelper.GetText("str_quest_tor_engineer", "hireEngineersDecline", "On a second thought, maybe later."), null, null, 200);

            UpgradeGunShopDialog(obj);

            //tutorial buy cannons
            obj.AddDialogLine("tutorialcannonbuy", "tutorialcannonbuy", "tutorialcannonbuy2", TORTextHelper.GetText("str_quest_tor_engineer", "CannonsLimitInstruction0", "To buy cannons you must be an Imperial, that's the law."), null, null, 200);
            obj.AddDialogLine("tutorialcannonbuy2", "tutorialcannonbuy2", "tutorialcannonbuy3", TORTextHelper.GetText("str_quest_tor_engineer", "CannonsLimitInstruction1", "The amount of cannons you can field in your army increases every 50 levels in Engineering skill."), null, null, 200);
            obj.AddDialogLine("tutorialcannonbuy3", "tutorialcannonbuy3", "hub", TORTextHelper.GetText("str_quest_tor_engineer", "CannonsLimitInstruction2", "If you have met these requirements, simply speak to me and I'll show you what we have."), null, null, 200);
            //tutorial use cannons
            obj.AddDialogLine("tutorialcannonuse", "tutorialcannonuse", "tutorialcannonuse2", TORTextHelper.GetText("str_quest_tor_engineer", "CannonsUse0", "Cannons are placed using the spellcasting Mode, but to fire the cannons you will need to hire at least two Cannon Crew"), null, null, 200);
            obj.AddDialogLine("tutorialcannonuse2", "tutorialcannonuse2", "tutorialcannonuse3", TORTextHelper.GetText("str_quest_tor_engineer", "CannonsUse1", "You will also need to ensure that the cannon is in your party inventory"), null, null, 200);
            obj.AddDialogLine("tutorialcannonuse3", "tutorialcannonuse3", "hub", TORTextHelper.GetText("str_quest_tor_engineer", "CannonsUse2", "Engineers and Cannon Crew can both fire cannons."), null, null, 200);
        }

        private void UpgradeGunShopDialog(CampaignGameStarter obj)
        {
            //upgrade shop
            obj.AddDialogLine("upgrade_gunshop_explain_1", "upgradeshop", "upgrade_gunshop_explain_2", TORTextHelper.GetText("str_engineer_upgrade_explain_1", "Not only did Goswin steal valuable parts, but also blueprints. Some of our finest creations were destroyed in his rampage."), () => !HasUpgradeGunShopCondition(1) && !_explained, null, 200);
            obj.AddDialogLine("upgrade_gunshop_explain_2", "upgrade_gunshop_explain_2", "upgradeshop", TORTextHelper.GetText("str_engineer_upgrade_explain_2", "There are copies of the plans, and those parts can be recreated from other engineer schools throughout the empire. But for this I have neither the authority nor the contacts to get them over quickly."), () => !HasUpgradeGunShopCondition(1) && !_explained, () => _explained = true, 200);

            obj.AddDialogLine("upgrade_gunshop_upgrade_1", "upgradeshop", "upgrade_gunshop_upgrade1_response", TORTextHelper.GetText("str_engineer_upgrade_1", "For 500{PRESTIGE_ICON} I can finally stock up our buckshot supplies and continue creating blunderbusses and can get some of those Hochland Long rifles."), () => !HasUpgradeGunShopCondition(1), null, 200);
            obj.AddDialogLine("upgrade_gunshop_upgrade_2", "upgradeshop", "upgrade_gunshop_upgrade2_response", TORTextHelper.GetText("str_engineer_upgrade_2", "For another 500{PRESTIGE_ICON} I can buy parts from which I can create more advanced pistols and rifles. Meinkopt would be proud of me."), () => !HasUpgradeGunShopCondition(2) && HasUpgradeGunShopCondition(1), null, 200);
            obj.AddDialogLine("upgrade_gunshop_upgrade_3", "upgradeshop", "upgrade_gunshop_upgrade3_response", TORTextHelper.GetText("str_engineer_upgrade_3", "For a final 500{PRESTIGE_ICON} I can finally stock up our gunpowder laboratory, which gives me the opportunity to craft grenades and cannons."), () => !HasUpgradeGunShopCondition(3) && HasUpgradeGunShopCondition(2), null, 200);

            obj.AddPlayerLine("upgrade_gunshop_upgrade_1_agree", "upgrade_gunshop_upgrade1_response", "upgrade_gunshop_upgrade_1_response", TORTextHelper.GetText("str_engineer_upgrade_agree", "I hope it is worth it. I will support you on this (Spend 500{PRESTIGE_ICON})"), HasEnoughPrestige, () => UpgradeGunShopCondition(1), 200, null);
            obj.AddPlayerLine("upgrade_gunshop_upgrade_1_decline", "upgrade_gunshop_upgrade1_response", "upgrade_gunshop_upgrade_decline", TORTextHelper.GetText("str_engineer_upgrade_decline", "I can't afford such a venture."), null, null, 200, null);
            obj.AddDialogLine("upgrade_gunshop_upgrade_1_response", "upgrade_gunshop_upgrade_1_response", "hub", TORTextHelper.GetText("str_engineer_upgrade_1_response", "May these guns help you succeed in combat. Just don't shoot yourself in the foot with them."), null, null, 200);

            obj.AddPlayerLine("upgrade_gunshop_upgrade_2_agree", "upgrade_gunshop_upgrade2_response", "upgrade_gunshop_upgrade_2_response", TORTextHelper.GetText("str_engineer_upgrade_agree", "I hope it is worth it. I will support you on this (Spend 500{PRESTIGE_ICON})"), HasEnoughPrestige, () => UpgradeGunShopCondition(2), 200, null);
            obj.AddPlayerLine("upgrade_gunshop_upgrade_2_decline", "upgrade_gunshop_upgrade2_response", "upgrade_gunshop_upgrade_decline", TORTextHelper.GetText("str_engineer_upgrade_decline", "I can't afford such a venture."), null, null, 200, null);
            obj.AddDialogLine("upgrade_gunshop_upgrade_2_response", "upgrade_gunshop_upgrade_2_response", "hub", TORTextHelper.GetText("str_engineer_upgrade_2_response", "It is said Meinkopt checked the clicking sound of every Repeater gun he made."), null, null, 200);

            obj.AddPlayerLine("upgrade_gunshop_upgrade_3_agree", "upgrade_gunshop_upgrade3_response", "upgrade_gunshop_upgrade_3_response", TORTextHelper.GetText("str_engineer_upgrade_agree", "I hope it is worth it. I will support you on this (Spend 500{PRESTIGE_ICON})"), HasEnoughPrestige, () => UpgradeGunShopCondition(3), 200, null);
            obj.AddPlayerLine("upgrade_gunshop_upgrade_3_decline", "upgrade_gunshop_upgrade3_response", "upgrade_gunshop_upgrade_decline", TORTextHelper.GetText("str_engineer_upgrade_decline", "I can't afford such a venture."), null, null, 200, null);
            obj.AddDialogLine("upgrade_gunshop_upgrade_3_response", "upgrade_gunshop_upgrade_3_response", "hub", TORTextHelper.GetText("str_engineer_upgrade_3_response", "I love the smell of burned black powder in the morning."), null, null, 200);

            obj.AddDialogLine("upgrade_gunshop_upgrade_decline", "upgrade_gunshop_upgrade_decline", "hub", TORTextHelper.GetText("str_engineer_upgrade_decline_response", "What a shame. Is there anything else I can do?"), null, null, 200);

            bool HasEnoughPrestige()
            {
                var available = Hero.MainHero.GetCustomResourceValue("Prestige");
                return available >= 500;
            }

            void UpgradeGunShopCondition(int level)
            {
                string engineerupgrade = "EngineerUpgrade" + level;

                if (!Hero.MainHero.HasAttribute(engineerupgrade))
                {
                    Hero.MainHero.AddAttribute(engineerupgrade);
                }
                Hero.MainHero.AddCustomResource("Prestige", -500);
            }
        }


        bool HasUpgradeGunShopCondition(int level)
        {
            string engineerupgrade = "EngineerUpgrade" + level;
            return Hero.MainHero.HasAttribute(engineerupgrade);
        }

        private void AddCultistDialogLines(CampaignGameStarter obj)
        {
            obj.AddDialogLine("engineerquestcultist_start", "start", "cultist_answerplayer", TORTextHelper.GetText("str_quest_tor_engineer", "cultistEncounter", "{ROGUE_ENGINEER_NAME} was right, they sent someone after us! Grab your weapons quickly!"), cultiststartcondition, null, 200);
            obj.AddPlayerLine("cultist_answerplayer", "cultist_answerplayer", "cultist_answer", TORTextHelper.GetText("str_quest_tor_engineer", "cultistEncounterPlayer0", "Woah, hold there, I have merely come for the stolen parts, there is no need for bloodshed. Perhaps an arrangement can be made?"), null, null, 200);
            obj.AddPlayerLine("cultist_answerplayer", "cultist_answerplayer", "cultist_answer", TORTextHelper.GetText("str_quest_tor_engineer", "cultistEncounterPlayer1", "Lay down your weapons and I may spare your lives."), null, null, 200);
            obj.AddPlayerLine("cultist_answerplayer", "cultist_answerplayer", "close_window", TORTextHelper.GetText("str_quest_tor_engineer", "cultistEncounterPlayer2", "Weapons or no, we will slay you all and take back what you stole!"), null, null, 200);
            obj.AddDialogLine("cultist_answer", "cultist_answer", "close_window", TORTextHelper.GetText("str_quest_tor_engineer", "cultistEncounterAnswer", "You will not trick us! They will serve a greater purpose! You will not take them!"), null, null, 200);
        }

        private void AddRogueEngineerDialogLines(CampaignGameStarter obj)
        {
            obj.AddDialogLine("rogueengineer_start", "start", "rogueengineer_answerplayer", TORTextHelper.GetText("str_quest_tor_engineer", "rogueEngineerEncounter", "So the old fool sent you after us? How did he figure out my plans? It matters not, you will not stand in the way of my creations. You will die here!"), rogueengineerdialogstartcondition, null, 200);
            //requires dying dialog of the engineer, here is a player response
            obj.AddPlayerLine("rogueengineer_playerafterbattle", "rogueengineer_playerafterbattle", "close_window", TORTextHelper.GetText("str_quest_tor_engineer", "rogueEngineerEncounterPlayerAfterBattleAnswer", "\tYour schemes end here."), null, null, 200);
        }

        private void OnSessionLaunched(CampaignGameStarter obj)
        {
            MBTextManager.SetTextVariable("PRESTIGE_ICON", CustomResourceManager.GetResourceObject("Prestige").GetCustomResourceIconAsText());
            MBTextManager.SetTextVariable("ROGUE_ENGINEER_NAME", GetRogueEngineerName());
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
            return RunawayPartsQuest.GetCurrentProgress() == (int)EngineerQuestStates.Cultisthunt || RunawayPartsQuest.GetCurrentProgress() == (int)EngineerQuestStates.HandInCultisthunt;
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

        private void handing_in_rogueengineer_quest()
        {
            RunawayPartsQuest.UpdateProgressOnQuest();
            var xp = (float)250f;
            SkillObject skill = TORSkills.GunPowder;
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
            var engineerItems = MBObjectManager.Instance.GetObjectTypeList<ItemObject>().Where(x =>
                x.IsTorItem() && x.Culture != null && x.Culture.StringId == "empire" &&
                (x.StringId.Contains("gun") ||
                 x.StringId.Contains("artillery")));

            var firstLevelShopItems = HasUpgradeGunShopCondition(1);
            var secondLevelShopItems = HasUpgradeGunShopCondition(2);
            var thirdLevelShopItems = HasUpgradeGunShopCondition(3);

            engineerItems = FilterGuns();
            ItemRoster roster = new ItemRoster();
            List<ItemRosterElement> list = new List<ItemRosterElement>();

            foreach (var item in engineerItems)
            {
                list.Add(new ItemRosterElement(item, MBRandom.RandomInt(1, 5)));
            }

            roster.Add(list);

            var oldrifle = MBObjectManager.Instance.GetObject<ItemObject>("tor_neutral_weapon_gun_old_rifle");
            if (oldrifle != null)
            {
                roster.Add(new ItemRosterElement(oldrifle, MBRandom.RandomInt(2, 5)));
                engineerItems.AddItem(oldrifle);
            }

            if (firstLevelShopItems)
            {
                var buckshots = MBObjectManager.Instance.GetObject<ItemObject>("tor_neutral_weapon_ammo_musket_ball_scatter");
                if (buckshots != null) roster.Add(new ItemRosterElement(buckshots, MBRandom.RandomInt(2, 5)));
                engineerItems.AddItem(buckshots);
            }

            if (secondLevelShopItems)
            {
                var mortars = MBObjectManager.Instance.GetObject<ItemObject>("tor_empire_artillery_mortar_001");
                if (mortars != null) roster.Add(new ItemRosterElement(mortars, MBRandom.RandomInt(1, 1)));
                engineerItems.AddItem(mortars);
            }

            if (thirdLevelShopItems)
            {
                var cannons = MBObjectManager.Instance.GetObject<ItemObject>("tor_empire_artillery_cannon_001");
                if (cannons != null) roster.Add(new ItemRosterElement(cannons, MBRandom.RandomInt(1, 1)));
                engineerItems.AddItem(cannons);

                var grenades = MBObjectManager.Instance.GetObject<ItemObject>("tor_empire_weapon_ammo_grenade");
                if (grenades != null) roster.Add(new ItemRosterElement(grenades, MBRandom.RandomInt(2, 5)));
            }

            var ammo = MBObjectManager.Instance.GetObject<ItemObject>("tor_neutral_weapon_ammo_musket_ball");
            if (ammo != null) roster.Add(new ItemRosterElement(ammo, MBRandom.RandomInt(2, 5)));

            InventoryScreenHelper.OpenScreenAsTrade(roster, _nuln.Town);

            List<ItemObject> FilterGuns()
            {
                var final = new List<ItemObject>();
                var items = engineerItems.ToList();
                foreach (var item in items)
                {
                    if (!firstLevelShopItems)
                    {
                        if (item.StringId.Contains("longrifle"))
                            continue;
                        if (item.StringId.Contains("blunderbuss"))
                            continue;
                        if (item.StringId.Contains("big_musket"))
                            continue;
                        if (item.StringId.Contains("special_musket"))
                            continue;
                    }

                    if (!secondLevelShopItems)
                    {
                        if (item.StringId.Contains("tor_empire_weapon_gun_handgun_002"))
                            continue;
                        if (item.StringId.Contains("repeater"))
                            continue;
                        if (item.StringId.Contains("flintlock_pistol_007"))
                            continue;
                    }
                    final.Add(item);
                }
                return final;
            }
        }

        private void cannoncrewrecruitmentconsequence()
        {
            var noviceengineer = MBObjectManager.Instance.GetObject<CharacterObject>("tor_empire_novice_engineer");
            var price = Campaign.Current.Models.PartyWageModel.GetTroopRecruitmentCost(noviceengineer, Hero.MainHero, false).ResultNumber * 2 * 10;
            GiveGoldAction.ApplyBetweenCharacters(Hero.MainHero, null, (int)price);
            MobileParty.MainParty.MemberRoster.AddToCounts(noviceengineer, 2);
        }

        private void ResetQuest()
        {
            RunawayPartsQuest.ResetQuestInCurrentState();
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
            var partner = Hero.OneToOneConversationHero;
            return partner != null && partner.Occupation == Occupation.Lord && partner.Template != null && partner.Template.StringId.Contains(RunawayPartsQuest.GetRogueEngineerTemplateID());
        }

        private void checkplayerengineerskillrequirements()
        {
            if (Hero.MainHero.GetSkillValue(DefaultSkills.Engineering) >= 0) _playerIsSkilledEnough = true;
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
            int price = (int)(Campaign.Current.Models.PartyWageModel.GetTroopRecruitmentCost(noviceengineer, Hero.MainHero, false).ResultNumber * 2 * 10);
            MBTextManager.SetTextVariable("RECRUITMENT_PRICE", price.ToString() + "{GOLD_ICON}");
        }

        private bool playerhasenoughmoney()
        {
            var noviceengineer = MBObjectManager.Instance.GetObject<CharacterObject>("tor_empire_novice_engineer");
            int price = (int)(Campaign.Current.Models.PartyWageModel.GetTroopRecruitmentCost(noviceengineer, Hero.MainHero, false).ResultNumber * 2 * 10);
            return Hero.MainHero.Gold > price;
        }

        private void OnNewGameStarted(CampaignGameStarter obj)
        {
            foreach (var town in Town.AllTowns)
            {
                if (town.StringId == "town_comp_WI1")
                {
                    _nuln = town.Settlement;
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
                var nameObject = template.GetName();
                nameObject.SetTextVariable("FIRSTNAME", _masterEngineerHero.FirstName);
                _masterEngineerHero.SetName(nameObject, _masterEngineerHero.FirstName);
                _masterEngineerHero.CharacterObject.HiddenInEncyclopedia = true;
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