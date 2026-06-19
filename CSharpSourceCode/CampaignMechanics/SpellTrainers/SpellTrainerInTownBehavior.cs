using HarmonyLib;
using Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.AgentOrigins;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Inventory;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.LinQuick;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;
using TOR_Core.AbilitySystem.SpellBook;
using TOR_Core.AbilitySystem.Spells;
using TOR_Core.CampaignMechanics.CustomResources;
using TOR_Core.CharacterDevelopment;
using TOR_Core.CharacterDevelopment.CareerSystem;
using TOR_Core.CharacterDevelopment.CareerSystem.Choices;
using TOR_Core.Extensions;
using TOR_Core.Items;
using TOR_Core.Quests;
using TOR_Core.Utilities;

namespace TOR_Core.CampaignMechanics.SpellTrainers
{
    public class SpellTrainerInTownBehavior : CampaignBehaviorBase
    {
        private readonly string _empireTrainerId = "tor_spelltrainer_empire_0";
        private readonly string _vampireTrainerId = "tor_spelltrainer_vc_0";
        private readonly string _prophetessTrainerId = "tor_spelltrainer_bretonnia_0";
        private readonly string _woodelfTrainerId = "tor_spelltrainer_woodelves_0";
        private readonly string _greenskinTrainerId = "tor_spelltrainer_greenskins_0";
        private string _testResult = "";
        private Dictionary<string, string> _settlementToTrainerMap = new Dictionary<string, string>();

        private readonly float _testSuccessChance = 1f;
        private const int DarkEnergyLoreCost = 2000;

        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
            CampaignEvents.OnNewGameCreatedEvent.AddNonSerializedListener(this, OnNewGameCreated);
            CampaignEvents.GameMenuOpened.AddNonSerializedListener(this, OnGameMenuOpened);
            CampaignEvents.BeforeMissionOpenedEvent.AddNonSerializedListener(this, OnBeforeMissionStart);
        }

        private void OnBeforeMissionStart() => SpawnTrainerIfNeeded();
        private void OnGameMenuOpened(MenuCallbackArgs obj) => SpawnTrainerIfNeeded();
        private void SpawnTrainerIfNeeded()
        {
            if (Settlement.CurrentSettlement != null && Settlement.CurrentSettlement.IsTown && !IsTrainerInCollege(Settlement.CurrentSettlement))
            {
                SpawnTrainerInCollege(Settlement.CurrentSettlement, true);
            }
        }

        private bool IsTrainerInCollege(Settlement settlement)
        {
            var location = settlement.LocationComplex.GetLocationWithId("house_1");
            var trainer = GetTrainerForTown(settlement);
            if (trainer != null)
            {
                return location.GetLocationCharacter(trainer) != null;
            }

            return false;
        }

        public Hero GetTrainerForTown(Settlement settlement)
        {
            Hero hero = null;
            var heroId = "";
            if (_settlementToTrainerMap.TryGetValue(settlement.StringId, out heroId))
            {
                hero = Hero.FindFirst(x => x.StringId == heroId);
            }
            return hero;
        }

        private void SpawnTrainerInCollege(Settlement settlement, bool forceSpawn)
        {
            var trainer = GetTrainerForTown(settlement);
            var collegeloc = settlement.LocationComplex.GetLocationWithId("house_1");
            if (trainer != null && collegeloc != null)
            {
                // Skip spellsinger envoy - they should stay in the lordshall, not the magic college
                if (trainer.HasAttribute("SpellsingerEnvoy"))
                {
                    return;
                }

                if (forceSpawn)
                {
                    LocationCharacter locationCharacter = new LocationCharacter(new AgentData(new SimpleAgentOrigin(trainer.CharacterObject, -1, null, default(UniqueTroopDescriptor))).Monster(FaceGen.GetBaseMonsterFromRace(trainer.CharacterObject.Race)), new LocationCharacter.AddBehaviorsDelegate(SandBoxManager.Instance.AgentBehaviorManager.AddWandererBehaviors), "npc_common", true, LocationCharacter.CharacterRelations.Neutral, null, true, false, null, false, false, true);
                    collegeloc.AddCharacter(locationCharacter);
                }
                else
                {
                    var locChar = settlement.LocationComplex.GetLocationCharacterOfHero(trainer);
                    var currentloc = settlement.LocationComplex.GetLocationOfCharacter(trainer);
                    if (currentloc != collegeloc) settlement.LocationComplex.ChangeLocation(locChar, currentloc, collegeloc);
                }
            }
        }

        private void OnNewGameCreated(CampaignGameStarter obj)
        {
            foreach (var settlement in Settlement.All)
            {
                if (settlement.IsTown)
                {
                    CreateTrainer(settlement);
                }
            }
        }

        private void CreateTrainer(Settlement settlement)
        {
            CharacterObject template = null;
            if (settlement.Culture.StringId == TORConstants.Cultures.BRETONNIA && settlement.StringId == "town_BA1") template = MBObjectManager.Instance.GetObject<CharacterObject>(_prophetessTrainerId);
            if (settlement.Culture.StringId == TORConstants.Cultures.SYLVANIA || settlement.Culture.StringId == TORConstants.Cultures.MOUSILLON) template = MBObjectManager.Instance.GetObject<CharacterObject>(_vampireTrainerId);
            if (settlement.Culture.StringId == TORConstants.Cultures.EMPIRE) template = MBObjectManager.Instance.GetObject<CharacterObject>(_empireTrainerId);
            if (settlement.Culture.StringId == TORConstants.Cultures.ASRAI) template = MBObjectManager.Instance.GetObject<CharacterObject>(_woodelfTrainerId);
            if (settlement.Culture.StringId == TORConstants.Cultures.GREENSKIN) template = MBObjectManager.Instance.GetObject<CharacterObject>(_greenskinTrainerId);

            if (template != null)
            {
                Hero hero = null;
                if (settlement.HeroesWithoutParty.Any(x => x.Template == template))
                {
                    hero = settlement.HeroesWithoutParty.First(x => x.Template == template);
                }
                else
                {
                    hero = HeroCreator.CreateSpecialHero(template, settlement, null, null, 50);
                }

                hero.SupporterOf = settlement.OwnerClan;
                hero.Culture = settlement.Culture;//for templates shared across cultures
                var nameObject = template.GetName();
                nameObject.SetTextVariable("FIRSTNAME", hero.FirstName);
                hero.SetName(nameObject, hero.FirstName);
                HeroHelper.SpawnHeroForTheFirstTime(hero, settlement);
                _settlementToTrainerMap.Add(settlement.StringId, hero.StringId);
                hero.CharacterObject.HiddenInEncyclopedia = true;

                if (hero.Culture.StringId == TORConstants.Cultures.BRETONNIA)
                {
                    hero.SetName(template.Name, template.Name);
                }
            }
        }

        private void OnSessionLaunched(CampaignGameStarter obj)
        {
            AddDialogs(obj);
            ForceLevelsForAllHeroes();
        }

        private void ForceLevelsForAllHeroes()
        {
            foreach (var hero in Hero.AllAliveHeroes)
            {
                if (hero.IsSpellCaster() && hero != Hero.MainHero && hero.Occupation == Occupation.Lord)
                {
                    var info = hero.GetExtendedInfo();
                    if (info == null || info.SpellCastingLevel == SpellCastingLevel.Master) continue;
                    hero.SetSpellCastingLevel(SpellCastingLevel.Master);
                }
            }
        }

        private void OpenScrollShop()
        {
            ItemRoster roster = new ItemRoster();

            MBObjectManager.Instance.GetObjectTypeList<ItemObject>()
                .Where(item => item.IsInventoryUsable() && item.GetTraits().Any(trait => trait.OnInventoryUseScript.InventoryScriptName.Contains("SkillBookScript")))
                .ToList()
                .ForEach(item => roster.Add(new ItemRosterElement(item, MBRandom.RandomInt(1, 5))));

            InventoryScreenHelper.OpenScreenAsTrade(roster, Settlement.CurrentSettlement.Town);
        }

        public bool IsSpellTrainer(Hero hero)
        {
            return _settlementToTrainerMap.ContainsValue(hero.StringId);
        }

        private void ProphetesseDialogs(CampaignGameStarter obj)
        {
            obj.AddDialogLine("trainer_prophetesse_start", "start", "choices_prophetesse", TORTextHelper.GetText("tor_spelltrainer_prophetesse_start", "Welcome, child of Bretonnia. The Lady has guided you to my presence. Speak, and let your intentions unfold."), isMorgianaLeFay, null, 200, null);
            obj.AddDialogLine("trainer_prophetesse_start", "hub_prophetesse", "choices_prophetesse", TORTextHelper.GetText("tor_spelltrainer_prophetesse_choices", "Is there more you seek? Speak your desires."), isMorgianaLeFay, null, 200, null);
            obj.AddPlayerLine("trainer_prophetesse_learnspells", "choices_prophetesse", "openbook_prophetesse", TORTextHelper.GetText("tor_spelltrainer_prophetesse_open_book", "Revered Fay Enchantress, share with me some of your magic teachings."), () => MobileParty.MainParty.HasSpellCasterMember() && damselCondition(), null, 200, null);
            obj.AddPlayerLine("trainer_prophetesse_scrollShop", "choices_prophetesse", "hub_prophetesse", TORTextHelper.GetText("tor_spelltrainer_prophetesse_scrolls", "Gracious Enchantress, I ask you for the tomes and scrolls that hold the keys to the Lady's wisdom."), null, OpenScrollShop, 200, null);
            obj.AddPlayerLine("trainer_prophetesse_scrollShop", "choices_prophetesse", "hub_prophetesse", TORTextHelper.GetText("tor_spelltrainer_prophetesse_damselsecond_lore", "My Fay Enchantress, I feel that {DAMSELNAME} has reached  a new level of magical potential, is there anything you can teach her?"), () => MobileParty.MainParty.HasSpellCasterMember() && damselCondition() && damselSecondLoreCondition(), damselSecondLoreConsequence, 200, null);
            obj.AddPlayerLine("trainer_prophetesse_playergoodbye", "choices_prophetesse", "saygoodbye", TORTextHelper.GetText("tor_spelltrainer_prophetesse_player_goodbye", "Until we meet again, my Fay Enchantress."), null, null, 200, null);
            obj.AddDialogLine("trainer_prophetesse_goodbye", "saygoodbye", "close_window", TORTextHelper.GetText("tor_spelltrainer_prophetesse_goodbye", "Go forth, and may the Lady's grace illuminate your path."), isMorgianaLeFay, null, 200, null);
            obj.AddDialogLine("trainer_prophetesse_afterlearnspells", "openbook_prophetesse", "hub_prophetesse", TORTextHelper.GetText("tor_spelltrainer_prophetesse_close_book", "You have grasped this weave with prowess. Carry this knowledge, and may it serve you well, as a beacon of the Lady's blessings."), null, openbookconsequence, 200, null);


            bool isMorgianaLeFay()
            {
                if (!spelltrainerstartcondition()) return false;
                var partner = CharacterObject.OneToOneConversationCharacter;
                if (partner.HeroObject != null && partner.HeroObject.Template.StringId == _prophetessTrainerId)
                {
                    return true;
                }

                return false;
            }
        }

        private void SpellsingerDialogs(CampaignGameStarter obj)
        {
            List<LoreObject> SpellweaverLores = LoreObject.GetAll().Where(x => x.ID == "HighMagic" || x.ID == "DarkMagic").ToList();

            obj.AddDialogLine("trainer_spellsinger_start", "start", "choices_spellsinger",TORTextHelper.GetText("tor_spelltrainer_spellsinger_start","I welcome you, child of Athel Loren."), isSpellsingerTrainer, null, 200, null);
            obj.AddDialogLine("trainer_spellsinger_start", "hub_spellsinger", "choices_spellsinger", TORTextHelper.GetText("tor_spelltrainer_spellsinger_reintro","Is there more you seek? Speak your desires."), isSpellsingerTrainer, null, 200, null);

            obj.AddPlayerLine("trainer_spellsinger_learnmagic", "choices_spellsinger", "trainer_spellsinger_learnmagic_answer", TORTextHelper.GetText("tor_spelltrainer_spellsinger_learnmagic","Greetings wise Spellsinger. I am seeking the capabilities of performing magic."), () => Hero.MainHero.HasCareer(TORCareers.Warden) && !Hero.MainHero.IsSpellCaster(), null, 200, null);
            obj.AddPlayerLine("trainer_spellsinger_learnmagic_second_lore", "choices_spellsinger", "trainer_spellsinger_learnmagic_answer_secondlore", TORTextHelper.GetText("tor_spelltrainer_spellsinger_learn_magic_warden_second_lore", "I want to enhance my magic capabilities."), () => Hero.MainHero.HasCareer(TORCareers.Warden) &&
                Hero.MainHero.HasUnlockedCareerChoiceTier(3) && Hero.MainHero.GetSkillValue(TORSkills.Spellcraft) > 200, null, 200, null);
            obj.AddDialogLine("trainer_spellsinger_learnmagic_answer_secondlore", "trainer_spellsinger_learnmagic_answer_secondlore", "trainer_spellsinger_learnmagic_secondlore__answer_player", TORTextHelper.GetText("tor_spelltrainer_spellsinger_learn_magic_warden_second_lore_answer", "You truly choose the path of Ariel. But the forest demands another tribute. Pay it and I will teach you more"), isSpellsingerTrainer, null, 200, null);

            obj.AddPlayerLine("trainer_spellsinger_learnmagic_answer_player_secondlore_agree", "trainer_spellsinger_learnmagic_secondlore__answer_player", "hub_spellsinger", TORTextHelper.GetText("tor_spelltrainer_spellsinger_learn_magic_warden_agree", "Yes I am."), null, learnMagicWardenSecondLore, 200, null);
            obj.AddPlayerLine("trainer_spellsinger_learnmagic_answer_player_secondlore_decline", "trainer_spellsinger_learnmagic_secondlore__answer_player", "hub_spellsinger", TORTextHelper.GetText("tor_spelltrainer_spellsinger_learn_magic_warden_decline", "I have to think about this."), null, null, 200, null);



            obj.AddDialogLine("trainer_spellsinger_learnmagic_answer", "trainer_spellsinger_learnmagic_answer", "trainer_spellsinger_learnmagic_answer_player", TORTextHelper.GetText("tor_spelltrainer_spellsinger_learn_magic_warden_answer", "Hm. You are not a spellsinger, yet I see your potential. If you are willing to show me how much you are bound to the forest. I am willing to teach you the first steps of the path of Ariel"), isSpellsingerTrainer, null, 200, null);

            obj.AddPlayerLine("trainer_spellsinger_learnmagic_answer_player_agree", "trainer_spellsinger_learnmagic_answer_player", "hub_spellsinger", TORTextHelper.GetText("tor_spelltrainer_spellsinger_learn_magic_warden_agree", "Yes I am."), null, learnMagicWarden, 200, null);
            obj.AddPlayerLine("trainer_spellsinger_learnmagic_answer_player_decline", "trainer_spellsinger_learnmagic_answer_player", "hub_spellsinger", TORTextHelper.GetText("tor_spelltrainer_spellsinger_learn_magic_warden_decline", "I have to think about this."), null, null, 200, null);

            obj.AddPlayerLine("trainer_spellsinger_learnspells", "choices_spellsinger", "openbook_spellsinger", TORTextHelper.GetText("tor_spelltrainer_spellsinger_open_book", "I seek further knowledge of Athel Loren's Magic."), () => MobileParty.MainParty.HasSpellCasterMember() && spellsingerCondition(), null, 200, null);
            obj.AddPlayerLine("trainer_spellsinger_spellweaver", "choices_spellsinger", "spellweaver_choice_dialog", TORTextHelper.GetText("tor_spelltrainer_spellweaver", "I want to become a spellweaver."), () => Hero.MainHero.HasCareer(TORCareers.Spellsinger) && spellsingerCondition() && SpellweaverCondition(), null, 200, null);
            obj.AddPlayerLine("trainer_spellsinger_spellweaver", "choices_spellsinger", "spellweaver_companion_choice_dialog",
                TORTextHelper.GetText("tor_spelltrainer_spellweaver_companion", "My companion is ready to become a spellweaver."),
                () => MobileParty.MainParty.HasSpellCasterMember() && spellsingerCondition() && MobileParty.MainParty.GetMemberHeroes()
                          .AnyQ(x => x.IsSpellCaster() && x.CharacterObject.IsElf() && x != Hero.MainHero && !(x.HasKnownLore("DarkMagic") || x.HasKnownLore("HighMagic"))) &&
                Hero.MainHero.HasUnlockedCareerChoiceTier(3), null, 200, null);

            obj.AddPlayerLine("trainer_spellsinger_learnlore", "choices_spellsinger", "choices_spellsinger", TORTextHelper.GetText("tor_spelltrainer_spellsinger_learnlore", "Teach me one of Ariel's many pathways."), () => MobileParty.MainParty.HasSpellCasterMember() && spellsingerCondition() && SpellsingerAdditonalLoreCondition(), AdditionalLoresPrompt, 200, null);

            obj.AddDialogLine("trainer_spellsinger_weaver", "spellweaver_choice_dialog", "spellweaver_choice_player", TORTextHelper.GetText("tor_spelltrainer_spellsinger_weaver_choice", "A spellsinger can pick either the pathway of the Darkweaver or the one of the Highweaver. Choose wisely."), isSpellsingerTrainer, null, 200, null);
            obj.AddPlayerLine("spellweaver_choice_player", "spellweaver_choice_player", "choices_spellsinger", TORTextHelper.GetText("tor_spelltrainer_spellweaver_choice", "Let me choose."), () => MobileParty.MainParty.HasSpellCasterMember() && spellsingerCondition(), spellweaverPrompt, 200, null);

            obj.AddDialogLine("spellweaver_companion_choice_dialog", "spellweaver_companion_choice_dialog", "spellweaver_choice_lores_companion", TORTextHelper.GetText("tor_spelltrainer_spellsinger_companion_choice", "A Highweaver, or Darkweaver... a tough choice"), isSpellsingerTrainer, null, 200, null);
            obj.AddPlayerLine("spellweaver_choice_lores_companion", "spellweaver_choice_lores_companion", "choices_spellsinger", TORTextHelper.GetText("tor_spelltrainer_spellsinger_companion_choose", "Let me choose."), () => MobileParty.MainParty.HasSpellCasterMember() && spellsingerCondition(), spellweaverCompanionPrompt, 200, null);



            obj.AddDialogLine("trainer_spellsinger_weaver", "spellweaver_choice_dialog", "close_window", TORTextHelper.GetText("tor_spelltrainer_spellsinger_goodbye", "May She guide you on all your paths through her garden."), isSpellsingerTrainer, null, 200, null);


            obj.AddPlayerLine("trainer_spellsinger_playergoodbye", "choices_spellsinger", "saygoodbye", TORTextHelper.GetText("tor_spelltrainer_spellsinger_player_goodbye", "Ariel be with you."), null, null, 200, null);
            obj.AddDialogLine("trainer_spellsinger_goodbye", "saygoodbye", "close_window", TORTextHelper.GetText("tor_spelltrainer_spellsinger_goodbye", "May She guide you on all your paths through her garden."), isSpellsingerTrainer, null, 200, null);
            obj.AddDialogLine("trainer_spellsinger_afterlearnspells", "openbook_spellsinger", "hub_spellsinger", TORTextHelper.GetText("tor_spelltrainer_spellsinger_close_book", "A new facet of Ariel's infinite knowledge."), null, openbookconsequence, 200, null);


            void learnMagicWarden()
            {
                Hero.MainHero.AddKnownLore("LoreOfLife");
                Hero.MainHero.AddCultureSpecificCustomResource(-2500);
                Hero.MainHero.AddAttribute("SpellCaster");
                Hero.MainHero.SetSpellCastingLevel(SpellCastingLevel.Entry);
                if (Hero.MainHero.GetSkillValue(TORSkills.Spellcraft) < 25)
                {
                    Hero.MainHero.SetSkillValue(TORSkills.Spellcraft, 25);
                }
            }

            void learnMagicWardenSecondLore()
            {
                Hero.MainHero.AddKnownLore("LoreOfBeasts");
                Hero.MainHero.AddCultureSpecificCustomResource(-2500);
            }

            bool SpellsingerAdditonalLoreCondition()
            {
                if (!Hero.MainHero.HasCareer(TORCareers.Spellsinger))
                    return false;

                // Count additional base lores only (exclude High/Dark magic and starting lores)
                var additionalLoreCount = Hero.MainHero.GetKnownLoreCount();
                if (Hero.MainHero.HasKnownLore("HighMagic")) additionalLoreCount--;
                if (Hero.MainHero.HasKnownLore("DarkMagic")) additionalLoreCount--;
                if (Hero.MainHero.HasKnownLore("LoreOfLife")) additionalLoreCount--;
                if (Hero.MainHero.HasKnownLore("LoreOfBeasts")) additionalLoreCount--;

                if (Hero.MainHero.HasUnlockedCareerChoiceTier(3))
                {
                    return true; // Can learn all remaining base lores
                }

                if (Hero.MainHero.HasUnlockedCareerChoiceTier(2))
                {
                    return additionalLoreCount < 3;
                }

                return additionalLoreCount < 2;
            }

            void AdditionalLoresPrompt()
            {
                List<InquiryElement> list = new List<InquiryElement>();
                var lores = LoreObject.GetAll();

                var additionalLores = lores.WhereQ(x => !x.DisabledForCultures.Contains(TORConstants.Cultures.ASRAI)).ToList();

                additionalLores = additionalLores.WhereQ(x => x.ID != "DarkMagic" && x.ID != "HighMagic").ToList();

                var model = Campaign.Current.Models.GetAbilityModel();
                foreach (var item in additionalLores)
                {

                    if (Hero.MainHero.HasKnownLore(item.ID))
                        continue;

                    list.Add(new InquiryElement(item, item.Name, null));
                }

                var inquirydata = new MultiSelectionInquiryData(TORTextHelper.GetText("tor_magic_lore_prompt_label", "Choose Lore"),
                    TORTextHelper.GetText("tor_magic_lore_prompt_description", "Choose a lore to specialize in."), list, true, 1, 1, TORTextHelper.GetText("tor_inquiry_confirm_text", "Confirm"),
                    TORTextHelper.GetText("tor_inquiry_cancel_text", "Cancel"), data =>
                    {
                        OnChooseLore(data);
                        Hero.MainHero.AddCultureSpecificCustomResource(-1000);
                    }, null);

                MBInformationManager.ShowMultiSelectionInquiry(inquirydata, true);
            }

            bool SpellweaverCondition()
            {
                if (Hero.MainHero.HasKnownLore("HighMagic") || Hero.MainHero.HasKnownLore("DarkMagic"))
                {
                    return false;
                }

                if (Hero.MainHero.HasUnlockedCareerChoiceTier(2))
                {
                    return true;
                }

                return false;
            }

            void spellweaverCompanionPrompt()
            {
                List<InquiryElement> list = new List<InquiryElement>();
                var heroes = (from hero in Hero.MainHero.PartyBelongedTo.GetMemberHeroes() where hero != Hero.MainHero where hero.IsSpellCaster() where hero.CharacterObject.IsElf() where !(hero.HasKnownLore("DarkMagic") || hero.HasKnownLore("HighMagic")) select hero).ToList();

                foreach (var hero in heroes)
                {
                    list.Add(new InquiryElement(hero, hero.FirstName.ToString(), null));
                }

                var inquirydata = new MultiSelectionInquiryData(TORTextHelper.GetText("tor_magic_lore_prompt_choose_companion", "Choose Companion"), TORTextHelper.GetText("tor_magic_lore_prompt_companion_desc", "Which companion should become a Spellcaster?."), list, true, 1, 1, TORTextHelper.GetText("tor_inquiry_confirm_text", "Confirm"), TORTextHelper.GetText("tor_inquiry_cancel_text", "Cancel"), OnChooseCompanion, null);
                MBInformationManager.ShowMultiSelectionInquiry(inquirydata, true);
            }


            void OnChooseCompanion(List<InquiryElement> inquiryElements)
            {
                spellweaverCompanionChooseLore((Hero)inquiryElements[0].Identifier);
            }

            void spellweaverCompanionChooseLore(Hero hero)
            {
                List<InquiryElement> list = new List<InquiryElement>();
                var weaverLores = SpellweaverLores;

                foreach (var item in weaverLores)
                {
                    list.Add(new InquiryElement(item, item.Name, null));
                }

                var inquirydata = new MultiSelectionInquiryData(TORTextHelper.GetText("tor_magic_lore_prompt_companion_lore", "Choose Lore for your companion"), TORTextHelper.GetText("tor_magic_lore_prompt_description", "Choose a lore to specialize in."), list, true, 1, 1, TORTextHelper.GetText("tor_inquiry_confirm_text", "Confirm"), TORTextHelper.GetText("tor_inquiry_cancel_text", "Cancel"), args => OnChooseCompanionLore(hero, args), OnCancelLore);
                MBInformationManager.ShowMultiSelectionInquiry(inquirydata, true);
            }

            void OnChooseCompanionLore(Hero hero, List<InquiryElement> obj)
            {
                var lore = (LoreObject)obj[0].Identifier;
                hero.AddKnownLore(lore.ID);
                InformationManager.HideInquiry();
            }

            void spellweaverPrompt()
            {
                List<InquiryElement> list = new List<InquiryElement>();

                var weaverLores = SpellweaverLores;

                foreach (var item in weaverLores)
                {

                    list.Add(new InquiryElement(item, item.Name, null));
                }
                var inquirydata = new MultiSelectionInquiryData(TORTextHelper.GetText("tor_magic_lore_prompt_label", "Choose Lore"), TORTextHelper.GetText("tor_magic_lore_prompt_description", "Choose a lore to specialize in."), list, true, 1, 1, TORTextHelper.GetText("tor_inquiry_confirm_text", "Confirm"), TORTextHelper.GetText("tor_inquiry_cancel_text", "Cancel"), OnChooseLore, OnCancelLore);
                MBInformationManager.ShowMultiSelectionInquiry(inquirydata, true);
            }
        }

        private void VampireDialogs(CampaignGameStarter obj)
        {
            // Start and hub dialogs
            obj.AddDialogLine("trainer_vampire_start", "start", "vampire_choices",
                TORTextHelper.GetText("tor_spelltrainer_vampire_start", "Ah, a creature of the night graces my presence. What do you seek?"),
                isVampireTrainer, null, 200, null);
            obj.AddDialogLine("trainer_vampire_hub", "hub_vampire", "vampire_choices",
                TORTextHelper.GetText("tor_spelltrainer_vampire_hub", "What else do you require?"),
                isVampireTrainer, null, 200, null);

            // Common options
            obj.AddPlayerLine("trainer_vampire_learnspells", "vampire_choices", "openbook_vampire",
                TORTextHelper.GetText("tor_spelltrainer_vampire_open_book", "I wish to study the dark arts."),
                () => MobileParty.MainParty.HasSpellCasterMember() && vampireCondition(), null, 200, null);
            obj.AddPlayerLine("trainer_vampire_scrollShop", "vampire_choices", "hub_vampire",
                TORTextHelper.GetText("tor_spelltrainer_vampire_scrolls", "Show me your forbidden tomes."),
                null, OpenScrollShop, 200, null);
            obj.AddDialogLine("trainer_vampire_afterlearnspells", "openbook_vampire", "hub_vampire",
                TORTextHelper.GetText("tor_spelltrainer_vampire_close_book", "The shadows reveal their secrets to you."),
                null, openbookconsequence, 200, null);

            // Vampire lore learning
            obj.AddPlayerLine("trainer_vampire_learn_lore", "vampire_choices", "vampire_dark_energy_request",
                TORTextHelper.GetText("tor_spelltrainer_magictest_vc_vampire_player_specialize_lore",
                    "I can feel my dark power continuing to grow, teach me more lest I find myself a new 'tutor'. Hurry on to find your Grimoire, lest I grow thirsty in your absence..."),
                VampireCasterReachedNewRankCondition, null, 200);

            obj.AddDialogLine("trainer_vampire_tier_limit", "vampire_dark_energy_request", "hub_vampire",
                TORTextHelper.GetText("tor_spelltrainer_vampire_tier_limit",
                    "You have learned all that your current mastery allows. Return when you have advanced further in your dark arts."),
                HasReachedNecrarchTierLimit, null, 210);
            obj.AddDialogLine("trainer_vampire_dark_energy_request", "vampire_dark_energy_request", "vampire_dark_energy_response",
                TORTextHelper.GetText("tor_spelltrainer_vampire_dark_energy_request",
                    "Even if you have the everliving-gift, the access to forbidden knowledge is restricted by my master. Provide us a gift and I am not speaking of gold, " +
                    "I need you to pay with the essence of Life. Then my masters can give you access... ({DARK_ENERGY_COST}{DARKENERGYICON})"),
                SetDarkEnergyCostVariable, null, 200);
            obj.AddPlayerLine("vampire_dark_energy_accept", "vampire_dark_energy_response", "vampire_lore_prompt",
                "{PAY_DARK_ENERGY_FOR_LORE}", HasEnoughDarkEnergy, null, 200);
            obj.AddPlayerLine("vampire_dark_energy_decline", "vampire_dark_energy_response", "vampire_dark_energy_declined",
                TORTextHelper.GetText("tor_spelltrainer_vampire_decline_price", "I can't provide you this gift"), null,
                null, 200);

            obj.AddDialogLine("trainer_vampire_lore_prompt", "vampire_lore_prompt", "vampire_lore_chosen",
                TORTextHelper.GetText("tor_dialog_ellipsis", "…"),
                null, chooseVampireLoreConsequence, 200);
            obj.AddDialogLine("trainer_vampire_lore_chosen", "vampire_lore_chosen", "hub_vampire",
                TORTextHelper.GetText("tor_spelltrainer_vampire_lore_learned", "The dark knowledge is now yours. Use it wisely... or not."),
                null, null, 200);
            obj.AddDialogLine("trainer_vampire_dark_energy_declined", "vampire_dark_energy_declined", "hub_vampire",
                TORTextHelper.GetText("tor_spelltrainer_vampire_shame", "A shame. What else can I help you with?"),
                null, null, 200);
            
            
            //good bye
            obj.AddPlayerLine("trainer_vampire_playergoodbye", "vampire_choices", "saygoodbye_vampire",
                TORTextHelper.GetText("tor_spelltrainer_vampire_player_goodbye", "I shall take my leave."),
                null, null, 100, null);
            obj.AddDialogLine("trainer_vampire_goodbye", "saygoodbye_vampire", "close_window",
                TORTextHelper.GetText("tor_spelltrainer_vampire_goodbye", "Until the next moonrise…"),
                isVampireTrainer, null, 200, null);

            bool HasReachedNecrarchTierLimit()
            {
                if (!Hero.MainHero.HasCareer(TORCareers.Necrarch)) return false;
                int loreCount = Hero.MainHero.GetExtendedInfo().KnownLores.Count;
                int maxLores = GetNecrarchMaxLores();
                return loreCount >= maxLores;
            }

            bool SetDarkEnergyCostVariable()
            {
                MBTextManager.SetTextVariable("DARK_ENERGY_COST", DarkEnergyLoreCost);
                MBTextManager.SetTextVariable("DARKENERGYICON",
                    CustomResourceManager.GetResourceObject("DarkEnergy").GetCustomResourceIconAsText());
                return true;
            }

            bool HasEnoughDarkEnergy()
            {
                MBTextManager.SetTextVariable("DARK_ENERGY_COST", DarkEnergyLoreCost);
                MBTextManager.SetTextVariable("DARKENERGYICON",
                    CustomResourceManager.GetResourceObject("DarkEnergy").GetCustomResourceIconAsText());

                string text = TORTextHelper.GetText("tor_spelltrainer_vampire_agree_price",
                    "Take my gift. Now give me what I demand! (Pay {DARK_ENERGY_COST}{DARKENERGYICON})");
                MBTextManager.SetTextVariable("PAY_DARK_ENERGY_FOR_LORE", text);
                return Hero.MainHero.GetCustomResourceValue("DarkEnergy") >= DarkEnergyLoreCost;
            }

            bool VampireCasterReachedNewRankCondition()
            {
                if (!Hero.MainHero.HasCareer(TORCareers.Necrarch) && !Hero.MainHero.HasCareer(TORCareers.MinorVampire))
                {
                    return false;
                }

                MBTextManager.SetTextVariable("DARKENERGYICON",
                    CustomResourceManager.GetResourceObject("DarkEnergy").GetCustomResourceIconAsText());

                int loreCount = Hero.MainHero.GetExtendedInfo().KnownLores.Count;

                if (Hero.MainHero.HasCareer(TORCareers.Necrarch))
                {
                    // Necrarch: always show option unless at absolute max (8 lores)
                    // Tier restrictions are handled in the lore selection dialog
                    return loreCount < 8;
                }

                if (Hero.MainHero.HasCareer(TORCareers.MinorVampire))
                {
                    // MinorVampire: show option at Tier 2 unless at absolute max (3 lores)
                    if (Hero.MainHero.HasUnlockedCareerChoiceTier(2))
                    {
                        return loreCount < 3;
                    }
                }

                return false;
            } 
            
            
            void chooseVampireLoreConsequence()
            {
                if (Hero.MainHero.HasCareer(TORCareers.MinorVampire))
                {
                    showMinorVampireLoreChoice();
                }
                else if (Hero.MainHero.HasCareer(TORCareers.Necrarch))
                {
                    showNecrarchLoreChoice();
                }
            }

            void showMinorVampireLoreChoice()
            {
                List<InquiryElement> list = new List<InquiryElement>();
                var lores = LoreObject.GetAll();

                foreach (var item in lores)
                {
                    if (item.ID != "DarkMagic" && item.ID != "LoreOfDeath") continue;
                    if (Hero.MainHero.GetExtendedInfo().HasKnownLore(item.ID)) continue;

                    list.Add(new InquiryElement(item, item.Name, null));
                }

                var inquirydata = new MultiSelectionInquiryData(
                    TORTextHelper.GetText("tor_vampire_lore_choice_label", "Choose Your Dark Path"),
                    TORTextHelper.GetText("tor_vampire_lore_choice_description", "This choice is final. You may only master one of these dark arts."),
                    list, true, 1, 1,
                    TORTextHelper.GetText("tor_inquiry_confirm_text", "Confirm"),
                    TORTextHelper.GetText("tor_inquiry_cancel_text", "Cancel"),
                    OnChooseLore, OnCancelLore);
                MBInformationManager.ShowMultiSelectionInquiry(inquirydata, true);
            }

            void showNecrarchLoreChoice()
            {
                List<InquiryElement> list = new List<InquiryElement>();
                var lores = LoreObject.GetAll();
                var model = Campaign.Current.Models.GetAbilityModel();

                foreach (var item in lores)
                {
                    if (item.ID == "MinorMagic" || Hero.MainHero.GetExtendedInfo().HasKnownLore(item.ID)) continue;
                    if (!model.IsValidLoreForCharacter(Hero.MainHero, item)) continue;

                    list.Add(new InquiryElement(item, item.Name, null));
                }

                int maxLores = GetNecrarchMaxLores();
                int currentLores = Hero.MainHero.GetExtendedInfo().KnownLores.Count;
                int remainingLores = maxLores - currentLores;

                MBTextManager.SetTextVariable("REMAINING_LORES", remainingLores);
                var description = TORTextHelper.GetText("tor_necrarch_lore_choice_description", "You may learn {REMAINING_LORES} more lores at your current level of mastery.");

                var inquirydata = new MultiSelectionInquiryData(
                    TORTextHelper.GetText("tor_magic_lore_prompt_label", "Choose Lore"),
                    description,
                    list, true, 1, 1,
                    TORTextHelper.GetText("tor_inquiry_confirm_text", "Confirm"),
                    TORTextHelper.GetText("tor_inquiry_cancel_text", "Cancel"),
                    OnChooseLore, OnCancelLore);
                MBInformationManager.ShowMultiSelectionInquiry(inquirydata, true);
            }

            int GetNecrarchMaxLores()
            {
                if (Hero.MainHero.HasUnlockedCareerChoiceTier(3))
                    return 8;
                if (Hero.MainHero.HasUnlockedCareerChoiceTier(2))
                    return 5;
                return 3;
            }

            bool isVampireTrainer()
            {
                if (!spelltrainerstartcondition()) return false;
                var partner = CharacterObject.OneToOneConversationCharacter;
                if (partner.HeroObject != null && partner.HeroObject.Template.StringId == _vampireTrainerId)
                {
                    return true;
                }
                return false;
            }

            bool vampireCondition()
            {
                if (Hero.MainHero.IsVampire()) return true;
                if (Hero.MainHero.PartyBelongedTo != null &&
                    Hero.MainHero.PartyBelongedTo.GetMemberHeroes().Any(x =>
                        (x.Culture.StringId == TORConstants.Cultures.SYLVANIA || x.Culture.StringId == TORConstants.Cultures.MOUSILLON) && x.IsSpellCaster()))
                    return true;
                return false;
            }
        }

        private void GreenskinDialogs(CampaignGameStarter obj)
        {
            // Greenskin Shaman greeting and main hub
            obj.AddDialogLine("trainer_greenskin_start", "start", "choices_greenskin",
                TORTextHelper.GetText("tor_spelltrainer_greenskin_start", "Whoo'z dere? Wot you want?"),
                isGreenskinTrainer, null, 200, null);
            obj.AddDialogLine("trainer_greenskin_hub", "hub_greenskin", "choices_greenskin",
                TORTextHelper.GetText("tor_spelltrainer_greenskin_hub", "Wot else ya need? Speak up!"),
                isGreenskinTrainer, null, 200, null);

            // Learn spells (open spell book)
            obj.AddPlayerLine("trainer_greenskin_learnspells", "choices_greenskin", "openbook_greenskin",
                TORTextHelper.GetText("tor_spelltrainer_greenskin_open_book", "I'z here fer sum ju-ju spooks."),
                () => MobileParty.MainParty.HasSpellCasterMember() && greenskinCondition(), null, 200, null);
            obj.AddDialogLine("trainer_greenskin_afterlearnspells", "openbook_greenskin", "hub_greenskin",
                TORTextHelper.GetText("tor_spelltrainer_greenskin_close_book", "Eat dis shroom an' dance wiv me."),
                null, openbookconsequence, 200, null);


            // Goodbye
            obj.AddPlayerLine("trainer_greenskin_playergoodbye", "choices_greenskin", "saygoodbye_greenskin",
                TORTextHelper.GetText("tor_spelltrainer_greenskin_player_goodbye", "I'z outta 'ere."),
                null, null, 200, null);
            obj.AddDialogLine("trainer_greenskin_goodbye", "saygoodbye_greenskin", "close_window",
                TORTextHelper.GetText("tor_spelltrainer_greenskin_goodbye", "I'z can feel da Gods talkin'. Get outta 'ere!"),
                isGreenskinTrainer, null, 200, null);

            // Helper condition: is this a greenskin trainer?
            bool isGreenskinTrainer()
            {
                if (!spelltrainerstartcondition()) return false;
                var partner = CharacterObject.OneToOneConversationCharacter;
                if (partner.HeroObject != null && partner.HeroObject.Template.StringId == _greenskinTrainerId)
                {
                    return true;
                }
                return false;
            }

            // Helper condition: is player/party eligible for greenskin spells?
            bool greenskinCondition()
            {
                if (Hero.MainHero.HasCareer(TORCareers.OrcShaman)) return true;
                if (Hero.MainHero.PartyBelongedTo != null &&
                    Hero.MainHero.PartyBelongedTo.GetMemberHeroes().Any(x =>
                        x.Culture.StringId == TORConstants.Cultures.GREENSKIN && x.IsSpellCaster()))
                    return true;
                return false;
            }
        }

        private void AddDialogs(CampaignGameStarter obj)
        {
            ProphetesseDialogs(obj);
            SpellsingerDialogs(obj);
            GreenskinDialogs(obj);
            VampireDialogs(obj);

            obj.AddDialogLine("trainer_start", "start", "choices", TORTextHelper.GetText("tor_spelltrainer_start", "Do I know you? What do you need, be quick I am a busy."), spelltrainerstartcondition, null, 200, null);
            obj.AddDialogLine("trainer_start_rejected", "start", "close_window", TORTextHelper.GetText("tor_spelltrainer_start_rejected", "I have nothing to teach you. Begone."), IsBlockedSpellTrainerConversation, null, 250, null);
            obj.AddPlayerLine("trainer_test", "choices", "magictest", "{TEST_QUESTION}", magictestcondition, null, 200, null);
            obj.AddDialogLine("trainer_testoutcome", "magictest", "testoutcome", "{TEST_PROMPT}", filltextfortestprompt, determinetestoutcome, 200,
                null);
            obj.AddDialogLine("trainer_testresult", "testoutcome", "start", "{TEST_RESULT}", testresultcondition, null, 200, null);

            obj.AddPlayerLine("trainer_learnspells", "choices", "openbook", TORTextHelper.GetText("tor_spelltrainer_open_book", "I have come seeking further knowledge."),
                () => !MobileParty.MainParty.GetSpellCasterMemberHeroes().IsEmpty() && MobileParty.MainParty.GetSpellCasterMemberHeroes().Any(x => x.Culture.StringId != TORConstants.Cultures.DAWI), null, 200, null);
            obj.AddPlayerLine("trainer_scrollShop", "choices", "start", TORTextHelper.GetText("tor_spelltrainer_scrolls", "Do you sell any scrolls?"), null,
                OpenScrollShop, 200, null);

            obj.AddDialogLine("trainer_afterlearnspells", "openbook", "start",
                TORTextHelper.GetText("tor_spelltrainer_close_book", "Hmm, come then. I will teach you what I can."), null, openbookconsequence, 200, null);
            obj.AddPlayerLine("trainer_specialize", "choices", "specializelore", "{SPECALIZE_QUESTION}", specializelorecondition, null, 200, null);
            obj.AddDialogLine("trainer_chooselore", "specializelore", "start", "{SPECIALIZE_PROMPT}.", fillchooseloretext, chooseloreconsequence, 200,
                null);

            obj.AddPlayerLine("trainer_playergoodbye", "choices", "saygoodbye", TORTextHelper.GetText("tor_spelltrainer_player_goodbye", "Farewell Magister."), null,
                null, 200, null);
            obj.AddDialogLine("trainer_goodbye", "saygoodbye", "close_window", TORTextHelper.GetText("tor_spelltrainer_goodbye", "Hmm, yes. Farewell."), null, null, 200,
                null);
        }






        private bool isMorgianaLeFay()
        {
            if (!spelltrainerstartcondition()) return false;
            var partner = CharacterObject.OneToOneConversationCharacter;
            if (partner.HeroObject != null && partner.HeroObject.Template.StringId == _prophetessTrainerId)
            {
                return true;
            }

            return false;
        }

        private bool isSpellsingerTrainer()
        {
            if (!spelltrainerstartcondition()) return false;

            var partner = CharacterObject.OneToOneConversationCharacter;
            if (partner.HeroObject != null && partner.HeroObject.Template.StringId == _woodelfTrainerId)
            {
                return true;
            }
            return false;
        }

        private bool damselSecondLoreCondition()
        {
            var damselCompanion = Hero.MainHero.PartyBelongedTo.GetMemberHeroes().Where(x => x != Hero.MainHero && x.Culture.StringId == TORConstants.Cultures.BRETONNIA && x.IsSpellCaster()).FirstOrDefault();
            if (damselCompanion == null) return false;

            if (Hero.MainHero.HasCareer(TORCareers.GrailDamsel) && !damselCompanion.HasKnownLore("LoreOfBeasts"))
            {
                GameTexts.SetVariable("DAMSELNAME", damselCompanion.Name);
                return Hero.MainHero.HasAttribute("SecondLoreForDamselCompanions");
            }
            return false;
        }

        private void damselSecondLoreConsequence()
        {
            TextObject text = new TextObject("{DAMSELNAME}");

            var hero = Hero.MainHero.PartyBelongedTo.GetMemberHeroes().Where(x => x.Name.ToString() == text.ToString() && x.IsSpellCaster()).FirstOrDefault();
            if (hero == null) return;
            hero.AddKnownLore("LoreOfBeasts");
        }

        private bool damselCondition()
        {
            if (Hero.MainHero.HasCareer(TORCareers.GrailDamsel) || (Hero.MainHero.PartyBelongedTo != null && Hero.MainHero.PartyBelongedTo.GetMemberHeroes().Any(x => x.Culture.StringId == TORConstants.Cultures.BRETONNIA && x.IsSpellCaster()))) return true;
            return false;
        }

        private bool spellsingerCondition()
        {
            if (Hero.MainHero.HasCareer(TORCareers.Spellsinger))
            {
                return true;
            }

            if (Hero.MainHero.Culture.StringId == TORConstants.Cultures.ASRAI &&
                Hero.MainHero.IsSpellCaster() &&
                Hero.MainHero.GetKnownLoreCount() > 0)
            {
                return true;
            }

            if (Hero.MainHero.PartyBelongedTo != null &&
                Hero.MainHero.PartyBelongedTo.GetMemberHeroes().Any(x => x.Culture.StringId == TORConstants.Cultures.ASRAI && x.IsSpellCaster()))
            {
                return true;
            }

            return false;
        }

        private bool IsVcTrainerCulture(string cultureId)
        {
            return cultureId == TORConstants.Cultures.SYLVANIA || cultureId == TORConstants.Cultures.MOUSILLON;
        }

        private bool HasMatchingSpellTrainerCompanion(Func<Hero, bool> predicate)
        {
            if (Hero.MainHero.PartyBelongedTo == null)
            {
                return false;
            }

            return Hero.MainHero.PartyBelongedTo.GetMemberHeroes().Any(x => x != Hero.MainHero && predicate(x));
        }

        private bool HasEmpireTrainerAccess()
        {
            if (Hero.MainHero.Culture.StringId == TORConstants.Cultures.EMPIRE)
            {
                return true;
            }

            return HasMatchingSpellTrainerCompanion(x => x.Culture.StringId == TORConstants.Cultures.EMPIRE && x.IsSpellCaster());
        }

        private bool HasVampireTrainerAccess()
        {
            if (IsVcTrainerCulture(Hero.MainHero.Culture.StringId))
            {
                return true;
            }

            return HasMatchingSpellTrainerCompanion(x => IsVcTrainerCulture(x.Culture.StringId) && x.IsSpellCaster());
        }

        private bool HasBretonnianTrainerAccess()
        {
            if (Hero.MainHero.Culture.StringId == TORConstants.Cultures.BRETONNIA)
            {
                return true;
            }

            return HasMatchingSpellTrainerCompanion(x => x.Culture.StringId == TORConstants.Cultures.BRETONNIA && x.IsSpellCaster());
        }

        private bool HasSpellsingerTrainerAccess()
        {
            if (Hero.MainHero.HasCareer(TORCareers.Spellsinger))
            {
                return true;
            }

            if (Hero.MainHero.HasCareer(TORCareers.Warden))
            {
                return true;
            }

            if (Hero.MainHero.Culture.StringId == TORConstants.Cultures.ASRAI &&
                Hero.MainHero.IsSpellCaster() &&
                Hero.MainHero.GetKnownLoreCount() > 0)
            {
                return true;
            }

            return HasMatchingSpellTrainerCompanion(x => x.Culture.StringId == TORConstants.Cultures.ASRAI && x.IsSpellCaster());
        }

        private bool HasGreenskinTrainerAccess()
        {
            if (Hero.MainHero.Culture.StringId == TORConstants.Cultures.GREENSKIN)
            {
                return true;
            }

            return HasMatchingSpellTrainerCompanion(x => x.Culture.StringId == TORConstants.Cultures.GREENSKIN && x.IsSpellCaster());
        }

        private bool CanAccessSpellTrainer(Hero trainer)
        {
            var settlementCulture = Settlement.CurrentSettlement?.Culture?.StringId;
            var templateId = trainer.Template?.StringId;

            if (settlementCulture == null || templateId == null)
            {
                return false;
            }

            if (templateId == _empireTrainerId)
            {
                return settlementCulture == TORConstants.Cultures.EMPIRE && HasEmpireTrainerAccess();
            }

            if (templateId == _vampireTrainerId)
            {
                return IsVcTrainerCulture(settlementCulture) && HasVampireTrainerAccess();
            }

            if (templateId == _prophetessTrainerId)
            {
                return settlementCulture == TORConstants.Cultures.BRETONNIA && HasBretonnianTrainerAccess();
            }

            if (templateId == _woodelfTrainerId)
            {
                return settlementCulture == TORConstants.Cultures.ASRAI && HasSpellsingerTrainerAccess();
            }

            if (templateId == _greenskinTrainerId)
            {
                return settlementCulture == TORConstants.Cultures.GREENSKIN && HasGreenskinTrainerAccess();
            }

            return false;
        }
        private bool IsBlockedSpellTrainerConversation()
        {
            var partner = CharacterObject.OneToOneConversationCharacter;
            return partner?.HeroObject != null && partner.HeroObject.IsSpellTrainer() && !CanAccessSpellTrainer(partner.HeroObject);
        }
        private bool spelltrainerstartcondition()
        {
            var partner = CharacterObject.OneToOneConversationCharacter;
            if (partner?.HeroObject == null || !partner.HeroObject.IsSpellTrainer())
            {
                return false;
            }

            return CanAccessSpellTrainer(partner.HeroObject);
        }

        private bool magictestcondition()
        {
            var partner = CharacterObject.OneToOneConversationCharacter;
            var culture = partner.Culture.StringId;
            if (partner.Culture.StringId is TORConstants.Cultures.ASRAI or TORConstants.Cultures.BRETONNIA) return false;
            if (!CareerHelper.IsMagicCapableCareer(Hero.MainHero.GetCareer())) return false;

            var flag = false;
            flag = !Hero.MainHero.IsVampire() && !Hero.MainHero.IsSpellCaster() && !Hero.MainHero.HasAttribute("Priest") && _testResult == ""; //checking IsPriest would prevent damsels from learning spells - "Priest" is specific to warrior priests
            if (flag)
            {
                string text;

                switch (culture)
                {
                    case "empire":
                        {
                            text = TORTextHelper.GetText("tor_spelltrainer_magictest_empire", "I have come seeking knowledge, I wish to learn the arcane arts. Can you help me?");
                            break;
                        }
                    case TORConstants.Cultures.SYLVANIA:
                    case "mousillon":
                        {
                            text = TORTextHelper.GetText("tor_spelltrainer_magictest_vc", "I need the power to escape death, to rule over this world as something more. Can you teach me the ways of your power?");
                            break;
                        }
                    default:
                        text = "You shouldn't see this.";
                        break;
                }
                MBTextManager.SetTextVariable("TEST_QUESTION", text);
            }
            return flag;
        }

        private bool filltextfortestprompt()
        {
            string text;
            var culture = Hero.OneToOneConversationHero.Culture.StringId;
            switch (culture)
            {
                case TORConstants.Cultures.EMPIRE:
                    {
                        text = TORTextHelper.GetText("tor_spelltrainer_magictest_begin_empire", "Hmm. To understand the Winds of magic you must have the aethyric senses. Let me perform an experiment on you to determine your potential...");
                        break;
                    }
                case TORConstants.Cultures.SYLVANIA:
                case TORConstants.Cultures.MOUSILLON:
                    {
                        text = TORTextHelper.GetText("tor_spelltrainer_magictest_begin_vc", "I can sense the you might have some grasp on the Winds of magic. Let me subject you to an examination to see your potential...");
                        break;
                    }
                default:
                    text = "You shouldn't see this.";
                    break;
            }
            MBTextManager.SetTextVariable("TEST_PROMPT", text);
            return true;
        }

        private void determinetestoutcome()
        {
            var rng = MBRandom.RandomFloatRanged(0, 1);
            if (rng <= _testSuccessChance) _testResult = "success";
            else _testResult = "failure";
        }

        private bool testresultcondition()
        {
            string result = "Error.";
            if (_testResult == "success")       //why is this not a simple boolean?
            {
                result = TORTextHelper.GetText("tor_spelltrainer_magictest_result_success", "Hmm...interesting. It would seem you do have an aptitude, perhaps even potential.");
                Hero.MainHero.AddAttribute("AbilityUser");
                Hero.MainHero.AddAttribute("SpellCaster");
                Hero.MainHero.AddKnownLore("MinorMagic");
                Hero.MainHero.SetSpellCastingLevel(SpellCastingLevel.Minor);
                var quest = TORQuestHelper.GetNewSpecializeLoreQuest(true);
                if (quest != null) quest.StartQuest();
            }
            else if (_testResult == "failure")
            {
                result = TORTextHelper.GetText("tor_spelltrainer_magictest_result_failure", "Pah, it is beyond you. Begone before you waste more of my time.");
            }
            MBTextManager.SetTextVariable("TEST_RESULT", result);
            return true;
        }

        private void openbookconsequence()
        {
            var state = Game.Current.GameStateManager.CreateState<SpellBookState>();
            state.IsTrainerMode = true;
            state.TrainerCulture = CharacterObject.OneToOneConversationCharacter.Culture.StringId;
            Game.Current.GameStateManager.PushState(state);
        }

        private bool specializelorecondition()
        {
            var quest = TORQuestHelper.GetCurrentActiveIfExists<SpecializeLoreQuest>();
            var partnerCulture = CharacterObject.OneToOneConversationCharacter.Culture.StringId;
            if (Hero.MainHero.IsVampire() && partnerCulture == "empire") return false;
            var info = Hero.MainHero.GetExtendedInfo();
            var possibleLores = new List<LoreObject>();
            foreach (var item in LoreObject.GetAll())
            {
                if (item.ID != "MinorMagic" &&
                    !item.DisabledForCultures.Contains(partnerCulture) &&
                    !info.HasKnownLore(item.ID) &&
                    !(item.IsRestrictedToVampires && !Hero.MainHero.IsVampire())) possibleLores.Add(item);
            }
            bool flag = false;
            if (quest != null && possibleLores.Count > 0)
            {
                flag = info.KnownLores.Count == 1 && info.KnownLores[0].ID == "MinorMagic" && quest.Task1Complete;
                if (!flag && Hero.MainHero.IsVampire() && quest.Task1Complete) flag = true;
            }
            else if (Hero.MainHero.IsVampire() && possibleLores.Count > 0 && info.SpellCastingLevel == SpellCastingLevel.Master) flag = true;
            if (flag)
            {
                string text = "";
                var culture = Hero.OneToOneConversationHero.Culture.StringId;
                switch (culture)
                {
                    case "empire":
                        {
                            text = TORTextHelper.GetText("tor_spelltrainer_magictest_empire_player_specialize_lore", "I am ready to join the colleges and become a true wizard of the Empire.");
                            break;
                        }
                    case TORConstants.Cultures.SYLVANIA:
                        {
                            text = TORTextHelper.GetText("tor_spelltrainer_magictest_vc_player_specialize_lore", "My Lord, I beseech you to teach me your dark magic. I have learned all that I can on my own and would be a most loyal apprentice to you.");
                            if (Hero.MainHero.IsVampire())
                            {
                                text = TORTextHelper.GetText("tor_spelltrainer_magictest_vc_vampire_player_specialize_lore", "I can feel my dark power continuing to grow, teach me more lest I find myself a new 'tutor'. Hurry on to find your Grimoire, lest I grow thirsty in your absence...");
                            }
                            break;
                        }
                    default:
                        text = "You shouldn't see this.";
                        break;
                }
                MBTextManager.SetTextVariable("SPECALIZE_QUESTION", text);
            }
            return flag;
        }

        private bool fillchooseloretext()
        {
            string text = "";
            var culture = Hero.OneToOneConversationHero.Culture.StringId;
            switch (culture)
            {
                case "empire":
                    {
                        text = TORTextHelper.GetText("tor_spelltrainer_magictest_empire_specialize_lore", "You have proven yourself and have a strong aptitude for the wind of magic, which college will you be joining? You can only dedicate yourself to one, choose wisely.");
                        break;
                    }
                case TORConstants.Cultures.SYLVANIA:
                    {
                        text = TORTextHelper.GetText("tor_spelltrainer_magictest_vc_specialize_lore", "You have potential Dark One, serve me and I will teach you all that I know in time. Come let me consult my grimoire.");
                        if (Hero.MainHero.IsVampire()) text = TORTextHelper.GetText("tor_spelltrainer_magictest_vc_vampire_specialize_lore", "Yes sire! Right away my good and merciful lord.");
                        break;
                    }
                default:
                    text = "You shouldn't see this.";
                    break;
            }

            MBTextManager.SetTextVariable("SPECIALIZE_PROMPT", text);
            return true;
        }

        private void chooseloreconsequence()
        {
            List<InquiryElement> list = new List<InquiryElement>();
            var lores = LoreObject.GetAll();

            var model = Campaign.Current.Models.GetAbilityModel();
            foreach (var item in lores)
            {
                if (item.ID == "MinorMagic" || Hero.MainHero.GetExtendedInfo().HasKnownLore(item.ID)) continue;

                if (!model.IsValidLoreForCharacter(Hero.MainHero, item)) continue;

                list.Add(new InquiryElement(item, item.Name, null));
            }
            var inquirydata = new MultiSelectionInquiryData(TORTextHelper.GetText("tor_magic_lore_prompt_label", "Choose Lore"), TORTextHelper.GetText("tor_magic_lore_prompt_description", "Choose a lore to specialize in."), list, true, 1, 1, TORTextHelper.GetText("tor_inquiry_confirm_text", "Confirm"), TORTextHelper.GetText("tor_inquiry_cancel_text", "Cancel"), OnChooseLore, OnCancelLore);
            MBInformationManager.ShowMultiSelectionInquiry(inquirydata, true);
        }

        private void OnChooseLore(List<InquiryElement> obj)
        {
            var choice = obj[0].Identifier as LoreObject;
            var info = Hero.MainHero.GetExtendedInfo();
            if (choice != null)
            {
                Hero.MainHero.AddKnownLore(choice.ID);
                var choiceText = new TextObject(choice.Name);
                if (info.SpellCastingLevel < SpellCastingLevel.Entry) Hero.MainHero.SetSpellCastingLevel(SpellCastingLevel.Entry);
                MBInformationManager.AddQuickInformation(new TextObject(TORTextHelper.GetText("tor_magic_lore_prompt_notification", "Successfully learned lore: ") + choiceText));
                TORQuestHelper.GetCurrentActiveIfExists<SpecializeLoreQuest>()?.CompleteQuestWithSuccess();

                if (Hero.MainHero.IsVampire())
                {
                    Hero.MainHero.AddCustomResource("DarkEnergy", -DarkEnergyLoreCost);
                }
            }
            InformationManager.HideInquiry();
        }

        private void OnCancelLore(List<InquiryElement> obj)
        {
            InformationManager.HideInquiry();
        }

        public override void SyncData(IDataStore dataStore)
        {
            dataStore.SyncData("_trainerToSettlementMap", ref _settlementToTrainerMap);

        }
    }
}