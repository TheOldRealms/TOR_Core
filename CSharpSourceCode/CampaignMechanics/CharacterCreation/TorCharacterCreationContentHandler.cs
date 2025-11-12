using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CharacterCreationContent;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;
using TOR_Core.AbilitySystem.Spells;
using TOR_Core.CampaignMechanics.Religion;
using TOR_Core.CampaignMechanics.TORCustomSettlement;
using TOR_Core.CharacterDevelopment;
using TOR_Core.Extensions;
using TOR_Core.Extensions.ExtendedInfoSystem;
using TOR_Core.Utilities;

namespace TOR_Core.CampaignMechanics.CharacterCreation
{
    // TODO: Native 1.3.1 changed from CharacterCreationContentBase (removed) to ICharacterCreationContentHandler
    // This class needs complete refactoring for the new handler pattern
    public class TorCharacterCreationContentHandler : ICharacterCreationContentHandler
    {
        private readonly List<CharacterCreationOption> _options;
        private readonly int _maxStageNumber = 3;
        private bool _isFemale = false;
        private int _originalRace = 0;
        private Equipment PlayerStartEquipment;
        private const int FocusToAdd = 1;
        private const int SkillLevelToAdd = 10;
        private const int AttributeLevelToAdd = 1;

        public TorCharacterCreationContentHandler()
        {
            try
            {
                var path = TORPaths.TORCoreModuleExtendedDataPath + "tor_cc_options.xml";
                XmlSerializer ser = new(typeof(List<CharacterCreationOption>));
                _options = ser.Deserialize(File.OpenRead(path)) as List<CharacterCreationOption>;
            }
            catch (Exception)
            {
                TORCommon.Log("Failed to open tor_cc_options.xml for character creation.", NLog.LogLevel.Error);
                throw;
            }
            ExtendedInfoManager.Instance.ClearInfo(Hero.MainHero);
        }

        private void AddMenus(CharacterCreationManager characterCreation)
        {
            // Initialize menu text variables before creating menus
            SetMenuLabelTexts();

            //stages - Native 1.3.1 uses NarrativeMenu instead of CharacterCreationMenu
            // Menu flow: start -> origin_menu -> growth_menu -> profession_menu -> (continues to face gen, etc.)

            // Create player character for display
            List<NarrativeMenuCharacter> playerCharacterList = new List<NarrativeMenuCharacter>
            {
                new NarrativeMenuCharacter(
                    "player_character",
                    CharacterObject.PlayerCharacter.GetBodyProperties(CharacterObject.PlayerCharacter.Equipment, -1),
                    CharacterObject.PlayerCharacter.Race,
                    CharacterObject.PlayerCharacter.IsFemale)
            };

            // Stage 1: Origin Menu
            NarrativeMenu stage1Menu = new NarrativeMenu(
                "tor_origin_menu",                              // stringId
                "start",                                        // inputMenuId (from culture selection)
                "tor_growth_menu",                              // outputMenuId (to stage 2)
                new TextObject("{=tor_cc_origin_summary_str}Origin"),  // title
                new TextObject("{TOR_CC_ORIGIN}"),              // description
                playerCharacterList,                            // characters to display
                new NarrativeMenu.GetNarrativeMenuCharacterArgsDelegate((culture, occupationType, manager) =>
                    GetPlayerMenuCharacterArgs("player_character", manager))
            );

            // Stage 2: Growth Menu
            NarrativeMenu stage2Menu = new NarrativeMenu(
                "tor_growth_menu",
                "tor_origin_menu",
                "tor_profession_menu",
                new TextObject("{=tor_cc_growth_summary_str}Growth"),
                new TextObject("{TOR_CC_GROWTH}"),
                playerCharacterList,
                new NarrativeMenu.GetNarrativeMenuCharacterArgsDelegate((culture, occupationType, manager) =>
                    GetPlayerMenuCharacterArgs("player_character", manager))
            );

            // Stage 3: Profession Menu
            NarrativeMenu stage3Menu = new NarrativeMenu(
                "tor_profession_menu",
                "tor_growth_menu",
                "narrative_face_generator_menu",  // Continue to face gen stage
                new TextObject("{=tor_cc_profession_summary_str}Profession"),
                new TextObject("{TOR_CC_PROFESSION}"),
                playerCharacterList,
                new NarrativeMenu.GetNarrativeMenuCharacterArgsDelegate((culture, occupationType, manager) =>
                    GetPlayerMenuCharacterArgs("player_character", manager))
            );

            // NEW 1.3.1 Pattern: Create NarrativeMenuOption for each option
            foreach (var option in _options)
            {
                // Determine target menu based on stage number
                NarrativeMenu targetMenu = option.StageNumber switch
                {
                    1 => stage1Menu,
                    2 => stage2Menu,
                    3 => stage3Menu,
                    _ => null
                };

                if (targetMenu == null) continue;

                // Create the option with all delegates
                NarrativeMenuOption narrativeOption = new NarrativeMenuOption(
                    option.Id,                                          // stringId
                    new TextObject(option.OptionText),                  // text
                    new TextObject(option.OptionFlavourText),           // description
                    new GetNarrativeMenuOptionArgsDelegate(args => GetOptionArgs(args, option)),  // argsDelegate
                    new NarrativeMenuOptionOnConditionDelegate(manager => OptionCondition(manager, option)),  // conditionDelegate
                    new NarrativeMenuOptionOnSelectDelegate(manager => OnOptionSelected(manager, option.Id)),  // selectDelegate
                    new NarrativeMenuOptionOnConsequenceDelegate(manager => OnOptionFinalize(manager, option.Id))  // consequenceDelegate
                );

                targetMenu.AddNarrativeMenuOption(narrativeOption);
            }

            characterCreation.AddNewMenu(stage1Menu);
            characterCreation.AddNewMenu(stage2Menu);
            characterCreation.AddNewMenu(stage3Menu);
        }

        // Helper method: Returns character args for displaying player in menus
        private List<NarrativeMenuCharacterArgs> GetPlayerMenuCharacterArgs(string characterId, CharacterCreationManager manager)
        {
            List<NarrativeMenuCharacterArgs> list = new List<NarrativeMenuCharacterArgs>();
            list.Add(new NarrativeMenuCharacterArgs(characterId, 0, "test", "test", "test"));
            return list;
        }

        // Helper method: Sets skill and attribute bonuses for an option
        private void GetOptionArgs(NarrativeMenuOptionArgs args, CharacterCreationOption option)
        {
            // Build affected skills array
            var effectedSkills = new MBList<SkillObject>();
            foreach (var skillId in option.SkillsToIncrease)
            {
                var skill = Skills.All.FirstOrDefault(x => x.StringId == skillId);
                if (skill != null)
                {
                    effectedSkills.Add(skill);
                }
            }

            // Get attribute
            CharacterAttribute attribute = Attributes.All.FirstOrDefault(x => x.StringId == option.AttributeToIncrease.ToLower());

            // Set values on args
            if (effectedSkills.Count > 0)
            {
                args.SetAffectedSkills(effectedSkills.ToArray());
                args.SetFocusToSkills(FocusToAdd);
                args.SetLevelToSkills(SkillLevelToAdd);
            }

            if (attribute != null)
            {
                args.SetLevelToAttribute(attribute, AttributeLevelToAdd);
            }

            // Note: PositiveEffectText is auto-computed from skills/attributes, no need to set manually
        }

        // Helper method: Checks if option should be visible (culture filter)
        private bool OptionCondition(CharacterCreationManager manager, CharacterCreationOption option)
        {
            // Check if option's culture matches currently selected culture
            CultureObject currentCulture = manager.CharacterCreationContent.SelectedCulture;
            return currentCulture != null && currentCulture.StringId == option.Culture;
        }

        private void OnMenuInit(CharacterCreationManager charInfo)
        {
            SetMenuLabelTexts();
            _isFemale = CharacterObject.PlayerCharacter.IsFemale;
            _originalRace = CharacterObject.PlayerCharacter.Race;
            //if(Debugger.IsAttached) _originalRace = CharacterObject.PlayerCharacter.Race; //This is to allow becoming different races by selecting them at character creation for development purposes.
        }
        
        private void SetMenuLabelTexts()
        {
            if (GameTexts.TryGetText("str_tor_cc_origin", out var stage1Text, CharacterObject.PlayerCharacter.Culture.StringId))
            {
                GameTexts.SetVariable("TOR_CC_ORIGIN", stage1Text);
            }
            else
            {
                GameTexts.SetVariable("TOR_CC_ORIGIN", "Choose your family's background...");
            }
            
            if (GameTexts.TryGetText("str_tor_cc_growth", out var stage2Text, CharacterObject.PlayerCharacter.Culture.StringId))
            {
                GameTexts.SetVariable("TOR_CC_GROWTH", stage2Text);
            }
            else
            {
                GameTexts.SetVariable("TOR_CC_GROWTH", "Teenage years...");
            }
            
            if (GameTexts.TryGetText("str_tor_cc_profession", out var stage3Text, CharacterObject.PlayerCharacter.Culture.StringId))
            {
                GameTexts.SetVariable("TOR_CC_PROFESSION", stage3Text);
            }
            else
            {
                GameTexts.SetVariable("TOR_CC_PROFESSION", "Your starting profession...");
            }
        }

        private void OnOptionSelected(CharacterCreationManager manager, string optionId)
        {
            var selectedOption = _options.Find(x => x.Id == optionId);
            var race = _originalRace;
            

            if (optionId == "option_vc_vampire" || optionId == "option_mousillon_vampire")
            {
                race = FaceGen.GetRaceOrDefault("vampire");
            }
            else if(optionId == "option_bretonnia_damsel" && !CharacterObject.PlayerCharacter.IsFemale)
            {
                var equipment = MBObjectManager.Instance.GetObject<MBEquipmentRoster>(selectedOption.EquipmentSetId).DefaultEquipment;
                var bodyProperties = CharacterObject.PlayerCharacter.GetBodyProperties(equipment);
                CharacterObject.PlayerCharacter.UpdatePlayerCharacterBodyProperties(bodyProperties, race,true);
            }
            else if(optionId == "option_bretonnia_knight_errant" && CharacterObject.PlayerCharacter.IsFemale)
            {
                var equipment = MBObjectManager.Instance.GetObject<MBEquipmentRoster>(selectedOption.EquipmentSetId).DefaultEquipment;
                var bodyProperties = CharacterObject.PlayerCharacter.GetBodyProperties(equipment);
                CharacterObject.PlayerCharacter.UpdatePlayerCharacterBodyProperties(bodyProperties, race,false);
            }
            
            UpdateVisuals(race);
            UpdateEquipment(selectedOption);
        }

        private void UpdateVisuals(int race)
        {
            // NEW 1.3.1: Just update character data, visual updates handled by GetPlayerMenuCharacterArgs
            CharacterObject.PlayerCharacter.Race = race;
        }

        private void UpdateEquipment(CharacterCreationOption selectedOption)
        {
            Equipment equipment = null;
            try
            {
                equipment = MBObjectManager.Instance.GetObject<MBEquipmentRoster>(selectedOption.EquipmentSetId).DefaultEquipment;
                if (equipment == null) MBObjectManager.Instance.CreateObject<MBEquipmentRoster>(selectedOption.EquipmentSetId);
            }
            catch (NullReferenceException)
            {
                TORCommon.Log("Attempted to read characterobject " + selectedOption.EquipmentSetId + " in Character Creation, but no such entry exists in XML. Falling back to default.", NLog.LogLevel.Error);
                throw;
            }

            if (equipment != null && !equipment.IsEmpty())
            {
                // NEW 1.3.1: Update player equipment directly, visual updates handled automatically
                PlayerStartEquipment = equipment;
                CharacterObject.PlayerCharacter.Equipment.FillFrom(PlayerStartEquipment);
            }
        }

        private void OnOptionFinalize(CharacterCreationManager manager, string id)
        {
            Hero.MainHero.AddAttribute("AbilityUser");
            Hero.MainHero.AddAttribute("CanPlaceArtillery");

            if (Hero.MainHero.Culture.StringId == TORConstants.Cultures.MOUSILLON)
            {
                Hero.MainHero.AddReligiousInfluence(ReligionObject.All.FirstOrDefault(x => x.StringId == "cult_of_nagash"), 60, false);
            }

            if (id == "option_empire_knight")
            {
                Hero.MainHero.AddCareer(TORCareers.KnightOldWorld);
            }
            
            if (id == "option_empire_magister_apprentice" || id == "option_bretonnia_damsel")
            {
                Hero.MainHero.AddAttribute("SpellCaster");
                Hero.MainHero.AddAbility("Dart");
                Hero.MainHero.AddKnownLore("MinorMagic");
                var skill = Hero.MainHero.GetSkillValue(TORSkills.SpellCraft);
                Hero.MainHero.HeroDeveloper.SetInitialSkillLevel(TORSkills.SpellCraft, Math.Max(skill, 25));
                Hero.MainHero.HeroDeveloper.AddPerk(TORPerks.SpellCraft.EntrySpells);
            }

            if (id == "option_empire_magister_apprentice")
            {
                Hero.MainHero.AddCareer(TORCareers.ImperialMagister);
            }

            if (id == "option_3_we_spellsinger")
            {
                Hero.MainHero.AddCareer(TORCareers.Spellsinger);
                Hero.MainHero.AddAttribute("SpellCaster");
                Hero.MainHero.AddKnownLore("LoreOfLife");
                Hero.MainHero.AddKnownLore("LoreOfBeasts");
                Hero.MainHero.AddAbility("SummerHeat");
                Hero.MainHero.AddAbility("AmberSpear");
                var skill = Hero.MainHero.GetSkillValue(TORSkills.SpellCraft);
                Hero.MainHero.HeroDeveloper.SetInitialSkillLevel(TORSkills.SpellCraft, Math.Max(skill, 25));
                Hero.MainHero.HeroDeveloper.AddPerk(TORPerks.SpellCraft.EntrySpells);
            }

            if (id == "option_3_dw_shield_breaker")
            {
                Hero.MainHero.AddCareer(TORCareers.Ironbreaker);
            }
            
            if (id == "option_3_dw_slayer")
            {
                Hero.MainHero.AddCareer(TORCareers.Slayer);
            }
            
            if (id == "option_empire_witch_hunter")
            {
                Hero.MainHero.AddCareer(TORCareers.WitchHunter);
            }

            if (id == "option_bretonnia_knight_errant")
            {
                Hero.MainHero.AddCareer(TORCareers.GrailKnight);
            }

            if (id == "option_mousillon_knight_errant")
            {
                Hero.MainHero.AddCareer(TORCareers.BlackGrailKnight);
            }

            if (Hero.MainHero.Culture.StringId == TORConstants.Cultures.ASRAI)
            {
                
                var settlementBehavior = Campaign.Current.GetCampaignBehavior<TORCustomSettlementCampaignBehavior>();
                string symbol = null;
                ReligionObject religion = null;
                switch (id)
                {
                    case "option_2_we_kurnous":
                    {
                        symbol = "WEKithbandSymbol"; 
                        religion = ReligionObject.All.FirstOrDefault(x=> x.StringId == "cult_of_kurnous");
                        break;
                    }
                    case "option_2_we_isha":
                    { 
                        symbol = "WETreekinSymbol"; 
                        religion = ReligionObject.All.FirstOrDefault(x=> x.StringId == "cult_of_isha");
                        break;
                    }
                    case "option_2_we_loec":
                        symbol = "WEWardancerSymbol";
                        religion = ReligionObject.All.FirstOrDefault(x=> x.StringId == "cult_of_loec");
                        break;
                    case "option_2_we_vaul":
                        symbol = "WEKithbandSymbol"; 
                        religion = ReligionObject.All.FirstOrDefault(x=> x.StringId == "cult_of_vaul");
                        break;
                    case "option_2_we_khaine":
                        Hero.MainHero.AddAttribute("WEKithbandSymbol");
                        symbol = "WEKithbandSymbol"; 
                        religion = ReligionObject.All.FirstOrDefault(x=> x.StringId == "cult_of_anath_raema");
                        break;
                }

                if (symbol != null && religion!=null)
                {
                    Hero.MainHero.AddAttribute(symbol); // is active
                    settlementBehavior.UnlockOakUpgrade(symbol); // has unlocked it from tree
                    Hero.MainHero.AddReligiousInfluence(religion,40);
                }
            }

            if (Hero.MainHero.Culture.StringId == TORConstants.Cultures.DAWI)
            {
                string grudge = null;
                switch (id)
                {
                    case "option_2_dw_umgi":
                    {
                        grudge = "HumanGrudge"; 
                        break;
                    }
                    case "option_2_dw_elgi":
                    { 
                        grudge = "ElfGrudge"; 
                        break;
                    }
                    case "option_2_dw_urks":
                        grudge = "GreenskinGrudge";
                        break;
                    case "option_2_dw_zanguzaz":
                        grudge = "UndeadGrudge"; 
                        break;
                    case "option_2_dw_thaggoraki":
                        grudge = "SkavenGrudge"; 
                        break;
                }

                if (grudge != null)
                {
                    Hero.MainHero.AddAttribute(grudge); // benefits from battles against people
                }
            }


            if (id == "option_3_we_waywatcher" || id == "option_3_eo_ghost_strider")
            {
                Hero.MainHero.AddCareer(TORCareers.Waywatcher);
            }

            if (id == "option_3_eo_greylord_apprentice")
            {
                Hero.MainHero.AddCareer(TORCareers.GreyLord);
                Hero.MainHero.AddAttribute("SpellCaster");
                Hero.MainHero.AddKnownLore("HighMagic");
                Hero.MainHero.AddKnownLore("LoreOfFire");
                Hero.MainHero.AddAbility("BoltOfAqshy");
                
                var skill = Hero.MainHero.GetSkillValue(TORSkills.SpellCraft);
                Hero.MainHero.HeroDeveloper.SetInitialSkillLevel(TORSkills.SpellCraft, Math.Max(skill, 25));
                Hero.MainHero.HeroDeveloper.AddPerk(TORPerks.SpellCraft.EntrySpells);
            }
            
            if(id == "option_bretonnia_damsel")
            {
                Hero.MainHero.AddAttribute("PriestLady");
                Hero.MainHero.AddCareer(TORCareers.GrailDamsel);
                var skill = Hero.MainHero.GetSkillValue(TORSkills.Faith);
                Hero.MainHero.HeroDeveloper.SetInitialSkillLevel(TORSkills.Faith, Math.Max(skill, 25));
                var knight = MBObjectManager.Instance.GetObject<CharacterObject>("tor_br_realm_knight");
                Hero.MainHero.AddAbility("AuraOfTheLady");
                Hero.MainHero.PartyBelongedTo.Party.AddMember(knight, 1, 0);
            }

            if (id == "option_empire_priest_acolyte")
            {
                Hero.MainHero.AddAttribute("Priest");
            }
            else if (id == "option_vc_necromancer" || id == "option_mousillon_necromancer")
            {
                Hero.MainHero.AddAttribute("SpellCaster");
                Hero.MainHero.AddAttribute("Necromancer");
                Hero.MainHero.AddAbility("SummonSkeleton");
                Hero.MainHero.AddKnownLore("MinorMagic");
                Hero.MainHero.AddKnownLore("Necromancy");
                var skill = Hero.MainHero.GetSkillValue(TORSkills.SpellCraft);
                Hero.MainHero.HeroDeveloper.SetInitialSkillLevel(TORSkills.SpellCraft, Math.Max(skill, 25));
                Hero.MainHero.HeroDeveloper.AddPerk(TORPerks.SpellCraft.EntrySpells);
                Hero.MainHero.AddCareer(TORCareers.Necromancer);
                Hero.MainHero.AddReligiousInfluence(ReligionObject.All.FirstOrDefault(x => x.StringId == "cult_of_nagash"), 25);
            }
            else if (id == "option_vc_vampire" || id == "option_mousillon_vampire")
            {
                Hero.MainHero.AddAttribute("Vampire");
                Hero.MainHero.AddAttribute("Necromancer");
                Hero.MainHero.AddReligiousInfluence(ReligionObject.All.FirstOrDefault(x => x.StringId == "cult_of_nagash"), 60);
            }

            if (id == "option_3_dw_rune_smith")
            {
                Hero.MainHero.AddCareer(TORCareers.Runelord);
                Hero.MainHero.AddAttribute("RuneCraft");
            }

            if (id == "option_3_gs_path_of_boss" || id == "option_3_gs_path_of_bully" ||
                id == "option_3_gs_path_of_boar_boys" || id == "option_3_gs_path_of_savage_boys" ||
                id == "option_3_gs_path_of_shaman")
            {
                Hero.MainHero.AddCareer(TORCareers.OrcBoss);
            }

            if (Hero.MainHero.GetCareer() == null)
            {
                if (Hero.MainHero.Culture.StringId == TORConstants.Cultures.ASRAI)
                {
                    Hero.MainHero.AddCareer(TORCareers.Warden);
                    return;
                }
                
                Hero.MainHero.AddCareer(TORCareers.Mercenary);
            }
        }

        // OLD: Was override, now called from OnCharacterCreationFinalize
        private void OnCharacterCreationFinalized()
        {
            CultureObject culture = CharacterObject.PlayerCharacter.Culture;
            Hero.MainHero.AddCultureSpecificCustomResource(0);
            CampaignVec2 position2D = default;

            position2D = culture.StringId switch
            {
                TORConstants.Cultures.EMPIRE => new CampaignVec2(new Vec2(1281.157f, 1058.522f),true),
                TORConstants.Cultures.SYLVANIA => new CampaignVec2(new Vec2(1617.54f, 969.70f),true),
                TORConstants.Cultures.BRETONNIA => new CampaignVec2(new Vec2(998.96f, 830.02f),true),
                TORConstants.Cultures.MOUSILLON => new CampaignVec2(new Vec2(932.531f, 1049.944f),true),
                TORConstants.Cultures.ASRAI => new CampaignVec2(new Vec2(1153.082f, 846.777f),true),
                TORConstants.Cultures.EONIR => new CampaignVec2(new Vec2(1245.375f, 1292.193f),true),
                _ => new CampaignVec2(new Vec2(1420.97f, 981.37f),true)
            };
            MobileParty.MainParty.Position = position2D;
            MapState mapState;
            if ((mapState = (GameStateManager.Current.ActiveState as MapState)) != null)
            {
                mapState.Handler.ResetCamera(true, true);
                mapState.Handler.TeleportCameraToMainParty();
            }
            SetHeroAge(25);
            if (Hero.MainHero.IsSpellCaster()) PromptChooseLore();
            if (Hero.MainHero.IsVampire()) PromptChooseBloodline();
            if (Hero.MainHero.Culture.StringId == "empire" && Hero.MainHero.IsPriest()) PromptChoosePriesthood();   
        }

        protected void SetHeroAge(float age)
        {
            Hero.MainHero.SetBirthDay(CampaignTime.YearsFromNow(-age));
        }


        private void PromptChooseLore()
        {
            List<InquiryElement> list = [];
            var lores = LoreObject.GetAll();
            foreach (var item in lores)
            {
                if (item.ID != "MinorMagic" && !item.DisabledForCultures.Contains(CharacterObject.PlayerCharacter.Culture.StringId) && !Hero.MainHero.GetExtendedInfo().HasKnownLore(item.ID)&&!item.IsRestrictedToVampires) list.Add(new InquiryElement(item, item.Name, null)) ;
            }

            if (list.IsEmpty()) return;
            
            var inquirydata = new MultiSelectionInquiryData("Choose Lore", "Choose a lore to specialize in.", list, false, 1, 1, "Confirm", "Cancel", OnChooseLore, OnCancel);
            MBInformationManager.ShowMultiSelectionInquiry(inquirydata, true);
        }

        private void OnChooseLore(List<InquiryElement> obj)
        {
            var info = Hero.MainHero.GetExtendedInfo();
            if (obj?[0].Identifier is LoreObject choice)
            {
                Hero.MainHero.AddKnownLore(choice.ID);
                if (info.SpellCastingLevel < SpellCastingLevel.Entry) Hero.MainHero.SetSpellCastingLevel(SpellCastingLevel.Entry);
                MBInformationManager.AddQuickInformation(new TextObject("Successfully learned lore: " + choice.Name), 0, CharacterObject.PlayerCharacter);
            }
            InformationManager.HideInquiry();
        }

        private void OnCancel(List<InquiryElement> obj)
        {
            MBInformationManager.AddQuickInformation(new TextObject("You MUST choose."));
        }

        private void PromptChooseBloodline()
        {
            List<InquiryElement> list =
            [
                new InquiryElement("generic_vampire", "Von Carstein Vampire", null),
                new InquiryElement("blood_knight", "Blood Knight", null),
                new InquiryElement("necrarch", "Necrarch", null),
            ];
            var inquirydata = new MultiSelectionInquiryData("Choose Bloodline", "Choose your vampiric bloodline.", list, false, 1, 1, "Confirm", "Cancel", OnChooseBloodline, OnCancel);
            MBInformationManager.ShowMultiSelectionInquiry(inquirydata, true);
        }
        
        private void PromptChoosePriesthood()
        {
            List<InquiryElement> list =
            [
                new InquiryElement("WarriorPriest", "Sigmar", null),
                new InquiryElement("WarriorPriestUlric", "Ulric", null),
            ];
            var inquirydata = new MultiSelectionInquiryData("Choose God", "You are a priest of the Empire. Choose the God you are devoted to.", list, false, 1, 1, "Confirm", "Cancel", OnChoosePriesthood, OnCancel);
            MBInformationManager.ShowMultiSelectionInquiry(inquirydata, true);
        }

        private void OnChoosePriesthood(List<InquiryElement> obj)
        {
            var choice = obj[0].Identifier as string;
            if(choice == "WarriorPriest")
            {
                Hero.MainHero.AddCareer(TORCareers.WarriorPriest);
                Hero.MainHero.AddReligiousInfluence(ReligionObject.All.FirstOrDefault(x => x.StringId == "cult_of_sigmar"), 60);
                Hero.MainHero.AddAttribute("PriestSigmar");
            }

            if (choice == "WarriorPriestUlric")
            {
                Hero.MainHero.AddCareer(TORCareers.WarriorPriestUlric);
                Hero.MainHero.AddReligiousInfluence(ReligionObject.All.FirstOrDefault(x => x.StringId == "cult_of_ulric"), 60);
                Hero.MainHero.AddAttribute("PriestUlric");
            }
            
            var skill = Hero.MainHero.GetSkillValue(TORSkills.Faith);
            Hero.MainHero.HeroDeveloper.SetInitialSkillLevel(TORSkills.Faith, Math.Max(skill, 25));
            Hero.MainHero.HeroDeveloper.AddPerk(TORPerks.Faith.NovicePrayers);
        }
        private void OnChooseBloodline(List<InquiryElement> obj)
        {
            var choice = obj[0].Identifier as string;
            if(choice == "generic_vampire")
            {
                Hero.MainHero.AddAttribute("SpellCaster");
                Hero.MainHero.AddAbility("NagashGaze");
                Hero.MainHero.AddKnownLore("MinorMagic");
                Hero.MainHero.AddKnownLore("Necromancy");
                var skill = Hero.MainHero.GetSkillValue(TORSkills.SpellCraft);
                Hero.MainHero.HeroDeveloper.SetInitialSkillLevel(TORSkills.SpellCraft, Math.Max(skill, 25));
                Hero.MainHero.HeroDeveloper.AddPerk(TORPerks.SpellCraft.EntrySpells);
                MBInformationManager.AddQuickInformation(new TextObject("Successfully learned Necromancy"), 0, CharacterObject.PlayerCharacter);
                Hero.MainHero.AddCareer(TORCareers.MinorVampire);
            }

            if (choice == "blood_knight")
            {
                Hero.MainHero.AddCareer(TORCareers.BloodKnight);
            }

            if (choice == "necrarch")
            {
                Hero.MainHero.AddAttribute("SpellCaster");
                Hero.MainHero.AddAbility("NagashGaze");
                Hero.MainHero.AddKnownLore("MinorMagic");
                Hero.MainHero.AddKnownLore("Necromancy");
                Hero.MainHero.AddCareer(TORCareers.Necrarch);
                var skill = Hero.MainHero.GetSkillValue(TORSkills.SpellCraft);
                Hero.MainHero.HeroDeveloper.SetInitialSkillLevel(TORSkills.SpellCraft, Math.Max(skill, 25));
                Hero.MainHero.HeroDeveloper.AddPerk(TORPerks.SpellCraft.EntrySpells);
                Hero.MainHero.CharacterObject.Race = FaceGen.GetRaceOrDefault("necrarch");
                
                Hero.MainHero.CharacterObject.Race = FaceGen.GetRaceOrDefault("necrarch");
                var equipment = Hero.MainHero.CharacterObject.Equipment;
                var properties = Hero.MainHero.CharacterObject.GetBodyProperties(equipment);
                Hero.MainHero.CharacterObject.UpdatePlayerCharacterBodyProperties(properties,FaceGen.GetRaceOrDefault("necrarch"),false);
                
                MBInformationManager.AddQuickInformation(new TextObject("Successfully learned Necromancy"), 0, CharacterObject.PlayerCharacter);
            }
        }

        // ======== ICharacterCreationContentHandler Implementation ========
        // Native 1.3.1 requires implementing these 4 methods instead of inheriting from CharacterCreationContentBase

        public void InitializeContent(CharacterCreationManager manager)
        {
            TORCommon.Log("[TOR CharacterCreation] InitializeContent called", NLog.LogLevel.Info);
            
            foreach (var cultureId  in TORConstants.Cultures.All)
            {
                var culture = MBObjectManager.Instance.GetObject<CultureObject>(cultureId);
                if (culture != null)
                {
                    manager.CharacterCreationContent.AddCharacterCreationCulture(culture, 1, 10);
                    TORCommon.Log($"[TOR CharacterCreation] Added culture: {cultureId}", NLog.LogLevel.Info);
                }
            }
            
            AddMenus(manager);

            TORCommon.Log("[TOR CharacterCreation] Added TOR cultures and narrative menus", NLog.LogLevel.Info);
        }

        public void AfterInitializeContent(CharacterCreationManager manager)
        {
            // Called after all handlers' InitializeContent completes
            TORCommon.Log("[TOR CharacterCreation] AfterInitializeContent called", NLog.LogLevel.Info);
        }

        public void OnStageCompleted(CharacterCreationStageBase stage)
        {
            // Called when a character creation stage is completed
            TORCommon.Log($"[TOR CharacterCreation] Stage completed: {stage?.GetType().Name}", NLog.LogLevel.Info);
        }

        public void OnCharacterCreationFinalize(CharacterCreationManager manager)
        {
            // Called when character creation finalizes (before entering campaign)
            // This should call the old OnCharacterCreationFinalized() logic
            TORCommon.Log("[TOR CharacterCreation] OnCharacterCreationFinalize called", NLog.LogLevel.Info);
            OnCharacterCreationFinalized();
        }
    }
}
