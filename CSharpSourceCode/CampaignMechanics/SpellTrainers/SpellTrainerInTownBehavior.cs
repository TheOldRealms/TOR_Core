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
using TOR_Core.CampaignMechanics.SkillBooks;
using TOR_Core.CharacterDevelopment;
using TOR_Core.CharacterDevelopment.CareerSystem;
using TOR_Core.Extensions;
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
        private string _testResult = "";
        private Dictionary<string, string> _settlementToTrainerMap = new Dictionary<string, string>();
        private readonly float _testSuccessChance = 1f;

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
            else return false;
        }

        private Hero GetTrainerForTown(Settlement settlement)
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
            if (settlement.Culture.StringId == TORConstants.Cultures.BRETONNIA&& settlement.StringId == "town_BA1") template = MBObjectManager.Instance.GetObject<CharacterObject>(_prophetessTrainerId);
            if (settlement.Culture.StringId == TORConstants.Cultures.SYLVANIA || settlement.Culture.StringId == TORConstants.Cultures.MOUSILLON) template = MBObjectManager.Instance.GetObject<CharacterObject>(_vampireTrainerId);
            if(settlement.Culture.StringId == TORConstants.Cultures.EMPIRE) template = MBObjectManager.Instance.GetObject<CharacterObject>(_empireTrainerId);
            if(settlement.Culture.StringId == TORConstants.Cultures.ASRAI) template = MBObjectManager.Instance.GetObject<CharacterObject>(_woodelfTrainerId);


            if (template != null)
            {
                var hero = HeroCreator.CreateSpecialHero(template, settlement, null, null, 50);
                hero.SupporterOf = settlement.OwnerClan;
                hero.SetName(new TextObject(hero.FirstName.ToString() + " " + template.Name.ToString()), hero.FirstName);
                HeroHelper.SpawnHeroForTheFirstTime(hero, settlement);
                _settlementToTrainerMap.Add(settlement.StringId, hero.StringId);
                hero.CharacterObject.HiddenInEncylopedia = true;
                hero.Culture = settlement.Culture;
                
                if (hero.Culture.StringId == TORConstants.Cultures.BRETONNIA)
                {
                   hero.UpdatePlayerGender(true);
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
                .Where(item => TORSkillBookCampaignBehavior.Instance.IsSkillBook(item))
                .ToList()
                .ForEach(item => roster.Add(new ItemRosterElement(item, MBRandom.RandomInt(1, 5))));

            InventoryManager.OpenScreenAsTrade(roster, Settlement.CurrentSettlement.Town);
        }

        public bool IsSpellTrainer(Hero hero)
        {
            return _settlementToTrainerMap.ContainsValue(hero.StringId);
        }

        private void ProphetesseDialogs(CampaignGameStarter obj)
        {
            obj.AddDialogLine("trainer_prophetesse_start", "start", "choices_prophetesse", "{=tor_spelltrainer_prophetesse_start_str}Welcome, child of Bretonnia. The Lady has guided you to my presence. Speak, and let your intentions unfold.", isMorgianaLeFay, null, 200, null);
            obj.AddDialogLine("trainer_prophetesse_start", "hub_prophetesse", "choices_prophetesse", "{=tor_spelltrainer_prophetesse_choices_str}Is there more you seek? Speak your desires.", isMorgianaLeFay, null, 200, null);
            obj.AddPlayerLine("trainer_prophetesse_learnspells", "choices_prophetesse", "openbook_prophetesse", "{=tor_spelltrainer_prophetesse_open_book_str}Revered Fay Enchantress, share with me some of your magic teachings.", () => MobileParty.MainParty.HasSpellCasterMember()&&damselCondition(), null, 200, null);
            obj.AddPlayerLine("trainer_prophetesse_scrollShop", "choices_prophetesse", "hub_prophetesse", "{=tor_spelltrainer_prophetesse_scrolls_str}Gracious Enchantress, I ask you for the tomes and scrolls that hold the keys to the Lady's wisdom.", null, OpenScrollShop, 200, null);
            obj.AddPlayerLine("trainer_prophetesse_scrollShop", "choices_prophetesse", "hub_prophetesse", "{=tor_spelltrainer_prophetesse_damselsecond_lore_str}My Fay Enchantress, I feel that {DAMSELNAME} has reached  a new level of magical potential, is there anything you can teach her?", ()=> MobileParty.MainParty.HasSpellCasterMember()&&damselCondition()&&damselSecondLoreCondition(), damselSecondLoreConsequence, 200, null);
            obj.AddPlayerLine("trainer_prophetesse_playergoodbye", "choices_prophetesse", "saygoodbye", "{=tor_spelltrainer_prophetesse_player_goodbye_str}Until we meet again, my Fay Enchantress.", null, null, 200, null);
            obj.AddDialogLine("trainer_prophetesse_goodbye", "saygoodbye", "close_window", "{=tor_spelltrainer_prophetesse_goodbye_str}Go forth, and may the Lady's grace illuminate your path.", isMorgianaLeFay, null, 200, null);
            obj.AddDialogLine("trainer_prophetesse_afterlearnspells", "openbook_prophetesse", "hub_prophetesse", "{=tor_spelltrainer_prophetesse_close_book_str}You have grasped this weave with prowess. Carry this knowledge, and may it serve you well, as a beacon of the Lady's blessings.", null, openbookconsequence, 200, null);
            
        }

        private void SpellsingerDialogs(CampaignGameStarter obj)
        { 
            obj.AddDialogLine("trainer_spellsinger_start", "start", "choices_spellsinger", "{=tor_spelltrainer_prophetesse_start_str}I welcome you child of Athel Loren.", isSpellsingerTrainer, null, 200, null);
            obj.AddDialogLine("trainer_spellsinger_start", "hub_spellsinger", "choices_spellsinger", "{=tor_spelltrainer_prophetesse_choices_str}Is there more you seek? Speak your desires.", isSpellsingerTrainer, null, 200, null);
            obj.AddPlayerLine("trainer_spellsinger_learnspells", "choices_spellsinger", "openbook_spellsinger", "{=tor_spelltrainer_prophetesse_open_book_str}I seek further knowledge of Athel Loren's Magic.", () => MobileParty.MainParty.HasSpellCasterMember()&&spellsingerCondition(), null, 200, null);
            obj.AddPlayerLine("trainer_spellsinger_spellweaver", "choices_spellsinger", "spellweaver_choice_dialog", "{=tor_spelltrainer_prophetesse_open_book_str}I want to become a spellweaver.", () => MobileParty.MainParty.HasSpellCasterMember()&& spellsingerCondition() && SpellweaverCondition() , null, 200, null);
            obj.AddPlayerLine("trainer_spellsinger_learnlore", "choices_spellsinger", "spellweaver_choice_lores", "{=tor_spelltrainer_prophetesse_open_book_str}Teach me one of Ariels many pathways.", () => MobileParty.MainParty.HasSpellCasterMember()&&spellsingerCondition() && SpellsingerAdditonalLoreCondition(), null, 200, null);

            obj.AddDialogLine("trainer_spellsinger_weaver", "spellweaver_choice_dialog", "spellweaver_choice_player", "{=tor_spelltrainer_prophetesse_goodbye_str}A spellsinger, can pick either the pathway of the Darkweaver or the one of the Highweaver. Choose wisely", isSpellsingerTrainer, null, 200, null);
            obj.AddPlayerLine("spellweaver_choice_player", "spellweaver_choice_player", "choices_spellsinger", "{=tor_spelltrainer_prophetesse_open_book_str}Let me choose.", () => MobileParty.MainParty.HasSpellCasterMember()&&spellsingerCondition(), spellweaverPrompt, 200, null);
            
            obj.AddDialogLine("trainer_spellsinger_lores", "spellweaver_choice_lores", "spellweaver_choice_lores_player", "{=tor_spelltrainer_prophetesse_goodbye_str}You can learn additional aspects of the magic of the forest. Choose wisely (not more than 2 additional lores, 6 in total)", isSpellsingerTrainer, null, 200, null);
            obj.AddPlayerLine("spellweaver_choice_lores_player", "spellweaver_choice_lores_player", "choices_spellsinger", "{=tor_spelltrainer_prophetesse_open_book_str}Let me choose.", () => MobileParty.MainParty.HasSpellCasterMember()&&spellsingerCondition(), additionalLoresPrompt, 200, null);

            
            
            obj.AddDialogLine("trainer_spellsinger_weaver", "spellweaver_choice_dialog", "close_window", "{=tor_spelltrainer_prophetesse_goodbye_str}May Ariel guide you on all your paths through her garden.", isSpellsingerTrainer, null, 200, null);

            
            obj.AddPlayerLine("trainer_spellsinger_scrollShop", "choices_spellsinger", "hub_spellsinger", "{=tor_spelltrainer_prophetesse_damselsecond_lore_str} I feel that {SPELLSINGERNAME} has reached  a new level of magical potential, it is time for a new Spellweaver", ()=> MobileParty.MainParty.HasSpellCasterMember()&&spellsingerCondition()&&damselSecondLoreCondition(), damselSecondLoreConsequence, 200, null);
            obj.AddPlayerLine("trainer_spellsinger_playergoodbye", "choices_spellsinger", "saygoodbye", "{=tor_spelltrainer_prophetesse_player_goodbye_str}Ariel with you. ", null, null, 200, null);
            obj.AddDialogLine("trainer_spellsinger_goodbye", "saygoodbye", "close_window", "{=tor_spelltrainer_prophetesse_goodbye_str}May Ariel guide you on all your paths through her garden.", isSpellsingerTrainer, null, 200, null);
            obj.AddDialogLine("trainer_spellsinger_afterlearnspells", "openbook_spellsinger", "hub_spellsinger", "{=tor_spelltrainer_prophetesse_close_book_str}A new Facette of Ariels infinte knowledge.", null, openbookconsequence, 200, null);

            bool SpellsingerAdditonalLoreCondition()
            {
                if (!Hero.MainHero.HasUnlockedCareerChoiceTier(3))
                    return false;
                
                if (!Hero.MainHero.HasKnownLore("HighMagic") && !Hero.MainHero.HasKnownLore("DarkMagic"))
                {
                    return false;
                }
                var count = Hero.MainHero.GetKnownLoreCount();

                if (count >= 6)
                {
                    return false;
                }
            
                return true;
            }

            void additionalLoresPrompt()
            {
                List<InquiryElement> list = new List<InquiryElement>();
                var lores = LoreObject.GetAll();

                var additionalLores = lores.WhereQ(x => !x.DisabledForCultures.Contains(TORConstants.Cultures.ASRAI)).ToList();
        

                var model = Campaign.Current.Models.GetAbilityModel();
                foreach (var item in additionalLores)
                {
                    
                    if(Hero.MainHero.HasKnownLore(item.ID))
                        continue;
            
                    list.Add(new InquiryElement(item, item.Name, null));
                }
                var inquirydata = new MultiSelectionInquiryData(new TextObject("{=tor_magic_lore_prompt_label_str}Choose Lore").ToString(), new TextObject("{=tor_magic_lore_prompt_description_str}Choose a lore to specialize in.").ToString(), list, true, 1, 1, "Confirm", "Cancel", OnChooseLore, OnCancelLore);
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
            void spellweaverPrompt()
            {
                List<InquiryElement> list = new List<InquiryElement>();
                var lores = LoreObject.GetAll();

                var weaverLores = lores.Where(x => x.ID == "HighMagic" || x.ID == "DarkMagic").ToListQ();
            

                var model = Campaign.Current.Models.GetAbilityModel();
                foreach (var item in weaverLores)
                {
                
                    list.Add(new InquiryElement(item, item.Name, null));
                }
                var inquirydata = new MultiSelectionInquiryData(new TextObject("{=tor_magic_lore_prompt_label_str}Choose Lore").ToString(), new TextObject("{=tor_magic_lore_prompt_description_str}Choose a lore to specialize in.").ToString(), list, true, 1, 1, "Confirm", "Cancel", OnChooseLore, OnCancelLore);
                MBInformationManager.ShowMultiSelectionInquiry(inquirydata, true);
            }
            
        }

        private void AddDialogs(CampaignGameStarter obj)
        {
            ProphetesseDialogs(obj);
            SpellsingerDialogs(obj);

            
            obj.AddDialogLine("trainer_start", "start", "choices", "{=tor_spelltrainer_start_str}Do I know you? What do you need, be quick I am a busy.", spelltrainerstartcondition, null, 200, null);
            obj.AddPlayerLine("trainer_test", "choices", "magictest", "{TEST_QUESTION}", magictestcondition, null, 200, null);
            obj.AddDialogLine("trainer_testoutcome", "magictest", "testoutcome", "{TEST_PROMPT}", filltextfortestprompt, determinetestoutcome, 200, null);
            obj.AddDialogLine("trainer_testresult", "testoutcome", "start", "{TEST_RESULT}", testresultcondition, null, 200, null);
            
            obj.AddPlayerLine("trainer_learnspells", "choices", "openbook", "{=tor_spelltrainer_open_book_str}I have come seeking further knowledge.", () => MobileParty.MainParty.HasSpellCasterMember(), null, 200, null);
            obj.AddPlayerLine("trainer_scrollShop", "choices", "start", "{=tor_spelltrainer_scrolls_str}Do you sell any scrolls?", null, OpenScrollShop, 200, null);
            
            obj.AddDialogLine("trainer_afterlearnspells", "openbook", "start", "{=tor_spelltrainer_close_book_str}Hmm, come then. I will teach you what I can.", null, openbookconsequence, 200, null);
            obj.AddPlayerLine("trainer_specialize", "choices", "specializelore", "{SPECALIZE_QUESTION}", specializelorecondition, null, 200, null);
            obj.AddDialogLine("trainer_chooselore", "specializelore", "start", "{SPECIALIZE_PROMPT}.", fillchooseloretext, chooseloreconsequence, 200, null);

            obj.AddPlayerLine("trainer_vampire_learn_magic", "choices", "specializelore_question", "{=tor_spelltrainer_magictest_vc_vampire_player_specialize_lore_str}I can feel my dark power continuing to grow. Provide me access to more forbidden magic, mortal", VampireCasterReachedNewRankCondition,
                null, 200, null);
            
            obj.AddDialogLine("trainer_vampire_learn_magic2", "specializelore_question", "accept_dark_energy_price", "{=tor_spelltrainer_magictest_vc_vampire_player_specialize_lore_str}Even if you have the everliving-gift, the access to forbidden knowledge is restricted by my master. Provide us a gift and I am not speaking of gold, " +
                                                                                                           "I need you to pay with the essence of Life. Then my masters can give you access... (1500{DARKENERGYICON}) ", null,
                null, 200, null);
            obj.AddPlayerLine("agree_dark_energy_price", "accept_dark_energy_price", "specializelore", "Take my gift. Now give me what I demand! (Pay 2000{DARKENERGYICON})", HasEnoughDarkEnergy,
                chooseloreconsequence, 200, null);
            obj.AddPlayerLine("decline_dark_energy_price", "accept_dark_energy_price", "trainer_vampire_learn_magic3", "I can't provide you this gift",
                null, null, 200);
            
            obj.AddDialogLine("trainer_vampire_learn_magic3", "trainer_vampire_learn_magic3", "choices", "A shame. What else can I help you with? ", null,null, 200, null);
            
            obj.AddPlayerLine("trainer_playergoodbye", "choices", "saygoodbye", "{=tor_spelltrainer_player_goodbye_str}Farewell Magister.", null, null, 200, null);
            obj.AddDialogLine("trainer_goodbye", "saygoodbye", "close_window", "{=tor_spelltrainer_goodbye_str}Hmm, yes. Farewell.", null, null, 200, null);
        }

        private bool HasEnoughDarkEnergy()
        {
            return Hero.MainHero.GetCustomResourceValue("DarkEnergy") >= 1500;
        }

        private bool VampireCasterReachedNewRankCondition()
        {
            if(! Hero.MainHero.HasCareer(TORCareers.Necrarch) && !Hero.MainHero.HasCareer(TORCareers.MinorVampire)) return false;
            MBTextManager.SetTextVariable("DARKENERGYICON", CustomResourceManager.GetResourceObject("DarkEnergy").GetCustomResourceIconAsText());
            
            if (Hero.MainHero.GetCareer()==TORCareers.Necrarch&& Hero.MainHero.HasUnlockedCareerChoiceTier(3))
            {
                return Hero.MainHero.GetExtendedInfo().KnownLores.Count < 5;
            }
            
            if (Hero.MainHero.HasUnlockedCareerChoiceTier(2))
            {
                if (Hero.MainHero.GetCareer()==TORCareers.Necrarch)
                {
                    if (Hero.MainHero.GetExtendedInfo().KnownLores.Count < 4)
                    {
                        return true;
                    }
                }
                else if (Hero.MainHero.GetExtendedInfo().KnownLores.Count < 3)
                {
                    return true;
                }
            }
            
            if (Hero.MainHero.GetCareer()==TORCareers.Necrarch&& Hero.MainHero.HasUnlockedCareerChoiceTier(1))
            {
                if (Hero.MainHero.GetCareer()==TORCareers.Necrarch)
                {
                    if (Hero.MainHero.GetExtendedInfo().KnownLores.Count < 3)
                    {
                        return true;
                    }

                    return false;
                }

                return false;
            }
            

            return false;
        }

        private bool isMorgianaLeFay()
        {
            if(!spelltrainerstartcondition()) return false;
            var partner = CharacterObject.OneToOneConversationCharacter;
            if (partner.HeroObject!=null&& partner.HeroObject.Template.StringId == _prophetessTrainerId)
            {
                return true;
            }

            return false;
        }
        
        private bool isSpellsingerTrainer()
        {
            if(!spelltrainerstartcondition()) return false;
            
            var partner = CharacterObject.OneToOneConversationCharacter;
            if (partner.HeroObject!=null&& partner.HeroObject.Template.StringId == _woodelfTrainerId)
            {
                return true;
            }

            return false;
        }

        
        private bool damselSecondLoreCondition()
        {
            var damselCompanion = Hero.MainHero.PartyBelongedTo.GetMemberHeroes().Where(x => x != Hero.MainHero&& x.Culture.StringId == TORConstants.Cultures.BRETONNIA && x.IsSpellCaster()).FirstOrDefault();
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

            var hero= Hero.MainHero.PartyBelongedTo.GetMemberHeroes().Where(x => x.Name.ToString() == text.ToString() && x.IsSpellCaster()).FirstOrDefault();
            if(hero==null)return;
            hero.AddKnownLore("LoreOfBeasts");
        }
        
        private bool damselCondition()
        {
            if (Hero.MainHero.HasCareer(TORCareers.GrailDamsel)||(Hero.MainHero.PartyBelongedTo!=null&& Hero.MainHero.PartyBelongedTo.GetMemberHeroes().Any(x => x.Culture.StringId == TORConstants.Cultures.BRETONNIA && x.IsSpellCaster()))) return true;
            return false;
        }
        
        private bool spellsingerCondition()
        {
            if (Hero.MainHero.HasCareer(TORCareers.Spellsinger)||(Hero.MainHero.PartyBelongedTo!=null&& Hero.MainHero.PartyBelongedTo.GetMemberHeroes().Any(x => x.Culture.StringId == TORConstants.Cultures.ASRAI && x.IsSpellCaster()))) return true;
            return false;
        }

        private bool spelltrainerstartcondition()
        {
            var partner = CharacterObject.OneToOneConversationCharacter;
            if (partner != null && partner.HeroObject != null && partner.HeroObject.IsSpellTrainer()) return true;
            else return false;
        }

        private bool magictestcondition()
        {
            if (isMorgianaLeFay()) return false;
            if (!CareerHelper.IsMagicCapableCareer(Hero.MainHero.GetCareer())) return false;
            
            var flag = false;
            flag = !Hero.MainHero.IsVampire() && !Hero.MainHero.IsSpellCaster() && !Hero.MainHero.HasAttribute("Priest") && _testResult == "";
            if (flag)
            {
                TextObject text;
                var culture = Hero.OneToOneConversationHero.Culture.StringId;
                switch (culture)
                {
                    case "empire":
                        {
                            text = new TextObject ("{=tor_spelltrainer_magictest_empire_str}I have come seeking knowledge, I wish to learn the arcane arts. Can you help me?");
                            break;
                        }
                    case TORConstants.Cultures.SYLVANIA:
                    case "mousillon":
                        {
                            text = new TextObject("{=tor_spelltrainer_magictest_vc_str}I need the power to escape death, to rule over this world as something more. Can you teach me the ways of your power?");
                            break;
                        }
                    default:
                        text = new TextObject("You shouldn't see this.");
                        break;
                }
                MBTextManager.SetTextVariable("TEST_QUESTION", text);

            }
            return flag;
        }

        private bool filltextfortestprompt()
        {
            TextObject text;
            var culture = Hero.OneToOneConversationHero.Culture.StringId;
            switch (culture)
            {
                case TORConstants.Cultures.EMPIRE:
                    {
                        text = new TextObject("{=tor_spelltrainer_magictest_begin_empire_str}Hmm. To understand the Winds of magic you must have the aethyric senses. Let me perform an experiment on you to determine your potential...");
                        break;
                    }
                case TORConstants.Cultures.SYLVANIA:
                case TORConstants.Cultures.MOUSILLON:
                    {
                        text = new TextObject("{=tor_spelltrainer_magictest_begin_vc_str}I can sense the you might have some grasp on the Winds of magic. Let me subject you to an examination to see your potential...");
                        break;
                    }
                default:
                    text = new TextObject("You shouldn't see this.");
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
            var result = new TextObject("Error.");
            if (_testResult == "success")       //why is this not a simple boolean?
            {
                result = new TextObject("{=tor_spelltrainer_magictest_result_success_str}Hmm...interesting. It would seem you do have an aptitude, perhaps even potential.");
                Hero.MainHero.AddAttribute("AbilityUser");
                Hero.MainHero.AddAttribute("SpellCaster");
                Hero.MainHero.AddKnownLore("MinorMagic");
                Hero.MainHero.SetSpellCastingLevel(SpellCastingLevel.Minor);
                var quest = TORQuestHelper.GetNewSpecializeLoreQuest(true);
                if (quest != null) quest.StartQuest();
            }
            else if (_testResult == "failure")
            {
                result = new TextObject("{=tor_spelltrainer_magictest_result_failure_str}Pah, it is beyond you. Begone before you waste more of my time.");
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
                TextObject text = new TextObject ();
                var culture = Hero.OneToOneConversationHero.Culture.StringId;
                switch (culture)
                {
                    case "empire":
                        {
                            text = new TextObject("{=tor_spelltrainer_magictest_empire_player_specialize_lore_str}I am ready to join the colleges and become a true wizard of the Empire.");
                            break;
                        }
                    case TORConstants.Cultures.SYLVANIA:
                        {
                            text = new TextObject("{=tor_spelltrainer_magictest_vc_player_specialize_lore_str}My Lord, I beseech you to teach me your dark magic. I have learned all that I can on my own and would be a most loyal apprentice to you.");
                            if (Hero.MainHero.IsVampire())
                            {
                                text = new TextObject("{=tor_spelltrainer_magictest_vc_vampire_player_specialize_lore_str}I can feel my dark power continuing to grow, teach me more lest I find myself a new 'tutor'. Hurry on to find your Grimoire, lest I grow thirsty in your absence...");
                            }
                            break;
                        }
                    default:
                        text = new TextObject("You shouldn't see this.");
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
                        text = "{=tor_spelltrainer_magictest_empire_specialize_lore_str}You have proven yourself and have a strong aptitude for the wind of magic, which college will you be joining? You can only dedicate yourself to one, choose wisely.";
                        break;
                    }
                case TORConstants.Cultures.SYLVANIA:
                    {
                        text = "{=tor_spelltrainer_magictest_vc_specialize_lore_str}You have potential Dark One, serve me and I will teach you all that I know in time. Come let me consult my grimoire.";
                        if (Hero.MainHero.IsVampire()) text = "{=tor_spelltrainer_magictest_vc_vampire_specialize_lore_str}Yes sire! Right away my good and merciful lord.";
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
                
                if (item.ID == "MinorMagic"  || Hero.MainHero.GetExtendedInfo().HasKnownLore(item.ID)) continue;

                if(!model.IsValidLoreForCharacter(Hero.MainHero, item)) continue;
                
                list.Add(new InquiryElement(item, item.Name, null));
            }
            var inquirydata = new MultiSelectionInquiryData(new TextObject("{=tor_magic_lore_prompt_label_str}Choose Lore").ToString(), new TextObject("{=tor_magic_lore_prompt_description_str}Choose a lore to specialize in.").ToString(), list, true, 1, 1, "Confirm", "Cancel", OnChooseLore, OnCancelLore);
            MBInformationManager.ShowMultiSelectionInquiry(inquirydata, true);
        }

        private void OnChooseLore(List<InquiryElement> obj)
        {
            var choice = obj[0].Identifier as LoreObject;
            var info = Hero.MainHero.GetExtendedInfo();
            if (choice != null)
            {
                Hero.MainHero.AddKnownLore(choice.ID);
                var choiceText = new TextObject (choice.Name);
                if (info.SpellCastingLevel < SpellCastingLevel.Entry) Hero.MainHero.SetSpellCastingLevel(SpellCastingLevel.Entry);
                MBInformationManager.AddQuickInformation(new TextObject("{=tor_magic_lore_prompt_notification_str}Successfully learned lore: " + choiceText));
                TORQuestHelper.GetCurrentActiveIfExists<SpecializeLoreQuest>()?.CompleteQuestWithSuccess();

                if (Hero.MainHero.IsVampire())
                { 
                    Hero.MainHero.AddCustomResource("DarkEnergy",-2000);
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
