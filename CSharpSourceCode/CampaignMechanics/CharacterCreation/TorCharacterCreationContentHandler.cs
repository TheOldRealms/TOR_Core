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
        private string _currentEquipmentRosterId = "player_char_creation_childhood_age_empire_default_m";
        private string _selectedProfessionId = ""; // Track stage 3 selection for stage 4 conditions
        private string _selectedLoreId = null; // Store selected lore for spellcasters (applied at finalization)
        private string _selectedCareerId = null; // Store selected career for vampires/priests (applied at finalization)
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
                "narrative_face_generator_menu",  // Exit narrative stage → TORSpecializationStage handles stage 4
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

            // NOTE: Stage 4 (specialization) is handled by TORSpecializationStage + TORSpecializationStageView
            // No menu needed - the stage view creates its own UI using native CharacterCreationNarrativeStageVM

            characterCreation.AddNewMenu(stage1Menu);
            characterCreation.AddNewMenu(stage2Menu);
            characterCreation.AddNewMenu(stage3Menu);
        }
        
        private List<NarrativeMenuCharacterArgs> GetPlayerMenuCharacterArgs(string characterId, CharacterCreationManager manager)
        {
            List<NarrativeMenuCharacterArgs> list = new List<NarrativeMenuCharacterArgs>();
            list.Add(new NarrativeMenuCharacterArgs(
                characterId,
                25,  // age
                _currentEquipmentRosterId,  // equipment roster ID (updated when option is selected)
                "act_childhood_schooled",    // standard character creation animation
                "spawnpoint_player_1",       // standard spawn point
                isFemale: CharacterObject.PlayerCharacter.IsFemale
            ));
            return list;
        }

        // Helper method: Sets skill and attribute bonuses for an option
        private void GetOptionArgs(NarrativeMenuOptionArgs args, CharacterCreationOption option)
        {
            // Store custom positive effect text if it exists (for display via Harmony patch)
            if (!string.IsNullOrEmpty(option.PositiveEffectText))
            {
                HarmonyPatches.CharacterCreationPatches.CustomPositiveEffects[args] = new TextObject(option.PositiveEffectText);
                TORCommon.Log($"[GetOptionArgs] Stored custom positive effect for {option.Id}: {option.PositiveEffectText}", NLog.LogLevel.Info);
            }
            else
            {
                // No custom text, remove from dictionary to use default behavior
                HarmonyPatches.CharacterCreationPatches.CustomPositiveEffects.Remove(args);
            }

            // Still set skills/attributes so they get applied to the character
            // (They just won't be displayed due to our Harmony patch overriding the text)
            var effectedSkills = new MBList<SkillObject>();
            if (option.SkillsToIncrease != null)
            {
                foreach (var skillId in option.SkillsToIncrease)
                {
                    var skill = Skills.All.FirstOrDefault(x => x.StringId == skillId);
                    if (skill != null)
                    {
                        effectedSkills.Add(skill);
                    }
                }
            }

            CharacterAttribute attribute = Attributes.All.FirstOrDefault(x => x.StringId == option.AttributeToIncrease?.ToLower());

            // Set values so they get applied (via ApplyFinalEffects)
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
        }

        // Helper method: Checks if option should be visible (culture filter)
        private bool OptionCondition(CharacterCreationManager manager, CharacterCreationOption option)
        {
            var stage = manager.CurrentStage;
            // Check if option's culture matches currently selected culture
            CultureObject currentCulture = manager.CharacterCreationContent.SelectedCulture;
            return currentCulture != null && currentCulture.StringId == option.Culture;
        }

        private void OnFinalizeFaceCreation()
        {
            _isFemale = CharacterObject.PlayerCharacter.IsFemale;
            _originalRace = CharacterObject.PlayerCharacter.Race;
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

            if (GameTexts.TryGetText("str_tor_cc_specialization", out var stage4Text, CharacterObject.PlayerCharacter.Culture.StringId))
            {
                GameTexts.SetVariable("TOR_CC_SPECIALIZATION", stage4Text);
            }
            else
            {
                GameTexts.SetVariable("TOR_CC_SPECIALIZATION", "Choose your specialization...");
            }
        }

        private void AddLoreSelectionOptions(NarrativeMenu menu)
        {
            // Add lore selection options for spellcasters
            // Dynamically load from LoreObject.GetAll() like the original popup did
            var allLores = LoreObject.GetAll();

            foreach (var lore in allLores)
            {
                // Skip MinorMagic and vampire-only lores (same filters as original)
                if (lore.ID == "MinorMagic" || lore.IsRestrictedToVampires)
                    continue;

                var option = new NarrativeMenuOption(
                    "lore_" + lore.ID,  // Prefix with "lore_" for unique IDs
                    new TextObject(lore.Name),
                    new TextObject( ""), // Use lore description if available
                    new GetNarrativeMenuOptionArgsDelegate(args => { /* Empty for now */ }),
                    new NarrativeMenuOptionOnConditionDelegate(manager => IsSpellcasterAndLoreAvailable(lore)),
                    new NarrativeMenuOptionOnSelectDelegate(manager => { /* Handle selection */ }),
                    new NarrativeMenuOptionOnConsequenceDelegate(manager => OnLoreSelected(lore.ID))
                );
                menu.AddNarrativeMenuOption(option);
            }
        }

        private bool IsSpellcasterAndLoreAvailable(LoreObject lore)
        {
            bool isSpellcaster = IsSpellcaster();
            TORCommon.Log($"[IsSpellcasterAndLoreAvailable] Lore: {lore.ID}, IsSpellcaster: {isSpellcaster}, _selectedProfessionId: '{_selectedProfessionId}'", NLog.LogLevel.Info);

            if (!isSpellcaster) return false;

            // Filter out lores disabled for current culture (same as original)
            if (lore.DisabledForCultures.Contains(CharacterObject.PlayerCharacter.Culture.StringId))
                return false;

            // Don't show lores already known
            if (Hero.MainHero.GetExtendedInfo().HasKnownLore(lore.ID))
                return false;

            return true;
        }

        private void AddBloodlineSelectionOptions(NarrativeMenu menu)
        {
            // Add bloodline selection options for vampires
            var bloodlines = new[]
            {
                ("bloodline_von_carstein", "Von Carstein Vampire", "The noble bloodline of Sylvania"),
                ("bloodline_blood_knight", "Blood Knight", "An undead warrior of terrible prowess"),
                ("bloodline_necrarch", "Necrarch", "A scholar of the dark arts"),
            };

            foreach (var (id, name, desc) in bloodlines)
            {
                var option = new NarrativeMenuOption(
                    id,
                    new TextObject(name),
                    new TextObject(desc),
                    new GetNarrativeMenuOptionArgsDelegate(args => { /* Empty for now */ }),
                    new NarrativeMenuOptionOnConditionDelegate(manager => IsVampire()),
                    new NarrativeMenuOptionOnSelectDelegate(manager => { /* Handle selection */ }),
                    new NarrativeMenuOptionOnConsequenceDelegate(manager => OnBloodlineSelected(id))
                );
                menu.AddNarrativeMenuOption(option);
            }
        }

        private void AddPriesthoodSelectionOptions(NarrativeMenu menu)
        {
            // Add priesthood selection options for Empire priests
            var gods = new[]
            {
                ("god_sigmar", "Sigmar", "The Warrior God, founder of the Empire"),
                ("god_ulric", "Ulric", "The God of Winter, Wolves, and War"),
            };

            foreach (var (id, name, desc) in gods)
            {
                var option = new NarrativeMenuOption(
                    id,
                    new TextObject(name),
                    new TextObject(desc),
                    new GetNarrativeMenuOptionArgsDelegate(args => { /* Empty for now */ }),
                    new NarrativeMenuOptionOnConditionDelegate(manager => IsPriest()),
                    new NarrativeMenuOptionOnSelectDelegate(manager => { /* Handle selection */ }),
                    new NarrativeMenuOptionOnConsequenceDelegate(manager => OnPriesthoodSelected(id))
                );
                menu.AddNarrativeMenuOption(option);
            }
        }

        public string GetSelectedProfessionId() => _selectedProfessionId;

        public bool IsSpellcaster(string professionId = null)
        {
            string id = professionId ?? _selectedProfessionId;
            return id == "option_3_empire_magister_apprentice" ||
                   id == "option_3_bretonnia_damsel" ||
                   id == "option_3_we_spellsinger" ||
                   id == "option_3_eo_greylord_apprentice";
        }

        public bool IsVampire(string professionId = null)
        {
            string id = professionId ?? _selectedProfessionId;
            return id == "option_3_vc_vampire" ||
                   id == "option_3_mousillon_vampire";
        }

        public bool IsPriest(string professionId = null)
        {
            string id = professionId ?? _selectedProfessionId;
            return id == "option_3_empire_priest_acolyte";
        }

        /// <summary>
        /// Store the selected lore ID to be applied at character creation finalization
        /// </summary>
        public void SetSelectedLore(string loreId)
        {
            _selectedLoreId = loreId;
            TORCommon.Log($"[TorCharacterCreationContentHandler] Stored lore selection: {loreId}", NLog.LogLevel.Info);
        }

        /// <summary>
        /// Store the selected career ID to be applied at character creation finalization
        /// </summary>
        public void SetSelectedCareer(string careerId)
        {
            _selectedCareerId = careerId;
            TORCommon.Log($"[TorCharacterCreationContentHandler] Stored career selection: {careerId}", NLog.LogLevel.Info);
        }

        /// <summary>
        /// Clear any stored specialization selections.
        /// Called when clicking Back to ensure old selections are cleared if user changes profession.
        /// </summary>
        public void ClearStoredSpecializations()
        {
            _selectedLoreId = null;
            _selectedCareerId = null;
            TORCommon.Log("[TorCharacterCreationContentHandler] Cleared stored specialization selections", NLog.LogLevel.Info);
        }

        /// <summary>
        /// Get the currently stored lore ID (null if none selected)
        /// </summary>
        public string GetStoredLoreId() => _selectedLoreId;

        /// <summary>
        /// Get the currently stored career ID (null if none selected)
        /// </summary>
        public string GetStoredCareerId() => _selectedCareerId;

        private void OnLoreSelected(string loreId)
        {
            TORCommon.Log($"[OnLoreSelected] Lore selected: {loreId}", NLog.LogLevel.Info);

            // Remove "lore_" prefix to get actual lore ID
            string actualLoreId = loreId.Replace("lore_", "");

            var info = Hero.MainHero.GetExtendedInfo();
            Hero.MainHero.AddKnownLore(actualLoreId);
            if (info.SpellCastingLevel < SpellCastingLevel.Entry)
            {
                Hero.MainHero.SetSpellCastingLevel(SpellCastingLevel.Entry);
            }

            var lore = LoreObject.GetAll().FirstOrDefault(x => x.ID == actualLoreId);
            if (lore != null)
            {
                MBInformationManager.AddQuickInformation(new TextObject("Successfully learned lore: " + lore.Name), 0, CharacterObject.PlayerCharacter);
            }
        }

        private void OnBloodlineSelected(string bloodlineId)
        {
            TORCommon.Log($"[OnBloodlineSelected] Bloodline selected: {bloodlineId}", NLog.LogLevel.Info);

            if (bloodlineId == "bloodline_von_carstein")
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
            else if (bloodlineId == "bloodline_blood_knight")
            {
                Hero.MainHero.AddCareer(TORCareers.BloodKnight);
            }
            else if (bloodlineId == "bloodline_necrarch")
            {
                Hero.MainHero.AddAttribute("SpellCaster");
                Hero.MainHero.AddAbility("NagashGaze");
                Hero.MainHero.AddKnownLore("MinorMagic");
                Hero.MainHero.AddKnownLore("Necromancy");
                Hero.MainHero.AddCareer(TORCareers.Necrarch);
                var skill = Hero.MainHero.GetSkillValue(TORSkills.SpellCraft);
                Hero.MainHero.HeroDeveloper.SetInitialSkillLevel(TORSkills.SpellCraft, Math.Max(skill, 25));
                Hero.MainHero.HeroDeveloper.AddPerk(TORPerks.SpellCraft.EntrySpells);
                MBInformationManager.AddQuickInformation(new TextObject("Successfully learned Necromancy"), 0, CharacterObject.PlayerCharacter);
            }
        }

        private void OnPriesthoodSelected(string godId)
        {
            TORCommon.Log($"[OnPriesthoodSelected] God selected: {godId}", NLog.LogLevel.Info);

            if (godId == "god_sigmar")
            {
                Hero.MainHero.AddCareer(TORCareers.WarriorPriest);
                Hero.MainHero.AddReligiousInfluence(ReligionObject.All.FirstOrDefault(x => x.StringId == "cult_of_sigmar"), 60);
                Hero.MainHero.AddAttribute("PriestSigmar");
            }
            else if (godId == "god_ulric")
            {
                Hero.MainHero.AddCareer(TORCareers.WarriorPriestUlric);
                Hero.MainHero.AddReligiousInfluence(ReligionObject.All.FirstOrDefault(x => x.StringId == "cult_of_ulric"), 60);
                Hero.MainHero.AddAttribute("PriestUlric");
            }

            var skill = Hero.MainHero.GetSkillValue(TORSkills.Faith);
            Hero.MainHero.HeroDeveloper.SetInitialSkillLevel(TORSkills.Faith, Math.Max(skill, 25));
            Hero.MainHero.HeroDeveloper.AddPerk(TORPerks.Faith.NovicePrayers);
        }

        private void OnOptionSelected(CharacterCreationManager manager, string optionId)
        {
            var selectedOption = _options.Find(x => x.Id == optionId);
            var race = _originalRace;
            var isFemale = _isFemale;

            // Track stage 3 (profession) selection for stage 4 skip logic
            if (selectedOption != null && selectedOption.StageNumber == 3)
            {
                _selectedProfessionId = optionId;
                TORCommon.Log($"[OnOptionSelected] Profession selected: {_selectedProfessionId}", NLog.LogLevel.Info);
            }

            if (optionId == "option_3_vc_vampire" || optionId == "option_3_mousillon_vampire")
            {
                race = FaceGen.GetRaceOrDefault("vampire");
            }
            else if(optionId == "option_3_bretonnia_damsel" && !CharacterObject.PlayerCharacter.IsFemale)
            {
                isFemale = true;
            }
            else if(optionId == "option_3_bretonnia_knight_errant" && CharacterObject.PlayerCharacter.IsFemale)
            {
                isFemale = false;
            }

            UpdateVisuals(race);
            UpdateEquipment(manager, selectedOption, isFemale);
        }

        private void UpdateVisuals(int race)
        {
            // NEW 1.3.1: Just update character data, visual updates handled by GetPlayerMenuCharacterArgs
            CharacterObject.PlayerCharacter.Race = race;
        }

        private void UpdateEquipment(CharacterCreationManager manager, CharacterCreationOption selectedOption, bool isfemale)
        {
            MBEquipmentRoster roster = null;
            try
            { 
                roster = MBObjectManager.Instance.GetObject<MBEquipmentRoster>(selectedOption.EquipmentSetId);
                if (roster == null)
                {
                    TORCommon.Log($"Equipment roster '{selectedOption.EquipmentSetId}' not found, creating placeholder.", NLog.LogLevel.Warn);
                }
                else
                {
                    // Track the equipment roster ID for character display
                    _currentEquipmentRosterId = selectedOption.EquipmentSetId;
                    TORCommon.Log($"Updated equipment roster to: {_currentEquipmentRosterId}", NLog.LogLevel.Info);
                }
            }
            catch (NullReferenceException)
            {
                TORCommon.Log("Attempted to read characterobject " + selectedOption.EquipmentSetId + " in Character Creation, but no such entry exists in XML. Falling back to default.", NLog.LogLevel.Error);
                throw;
            }

            if (roster != null && !roster.DefaultEquipment.IsEmpty())
            {
                var character = manager.CurrentMenu.Characters[0];

                var equipment = roster.DefaultEquipment;
                var bodyProperties = CharacterObject.PlayerCharacter.GetBodyProperties(equipment);
                
                character.SetEquipment(roster);
                CharacterObject.PlayerCharacter.UpdatePlayerCharacterBodyProperties(bodyProperties, CharacterObject.PlayerCharacter.Race, isfemale);
                character.IsFemale = isfemale;
                
                CharacterObject.PlayerCharacter.Equipment.FillFrom(roster.DefaultEquipment);
                CharacterObject.PlayerCharacter.FirstCivilianEquipment.FillFrom(equipment);
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
            
            if (id == "option_3_empire_magister_apprentice" || id == "option_3_bretonnia_damsel")
            {
                Hero.MainHero.AddAttribute("SpellCaster");
                Hero.MainHero.AddAbility("Dart");
                Hero.MainHero.AddKnownLore("MinorMagic");
                var skill = Hero.MainHero.GetSkillValue(TORSkills.SpellCraft);
                Hero.MainHero.HeroDeveloper.SetInitialSkillLevel(TORSkills.SpellCraft, Math.Max(skill, 25));
                Hero.MainHero.HeroDeveloper.AddPerk(TORPerks.SpellCraft.EntrySpells);
            }

            if (id == "option_3_empire_magister_apprentice")
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
            
            if (id == "option_3_empire_witch_hunter")
            {
                Hero.MainHero.AddCareer(TORCareers.WitchHunter);
            }

            if (id == "option_3_bretonnia_knight_errant")
            {
                Hero.MainHero.AddCareer(TORCareers.GrailKnight);
            }

            if (id == "option_3_mousillon_knight_errant")
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
            
            if(id == "option_3_bretonnia_damsel")
            {
                Hero.MainHero.AddAttribute("PriestLady");
                Hero.MainHero.AddCareer(TORCareers.GrailDamsel);
                var skill = Hero.MainHero.GetSkillValue(TORSkills.Faith);
                Hero.MainHero.HeroDeveloper.SetInitialSkillLevel(TORSkills.Faith, Math.Max(skill, 25));
                var knight = MBObjectManager.Instance.GetObject<CharacterObject>("tor_br_realm_knight");
                Hero.MainHero.AddAbility("AuraOfTheLady");
                Hero.MainHero.PartyBelongedTo.Party.AddMember(knight, 1, 0);
            }

            if (id == "option_3_empire_priest_acolyte")
            {
                Hero.MainHero.AddAttribute("Priest");
            }
            else if (id == "option_3_vc_necromancer" || id == "option_3_mousillon_necromancer")
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
            else if (id == "option_3_vc_vampire" || id == "option_3_mousillon_vampire")
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

            // NOTE: Specialization selection now handled by TORSpecializationStageView
            // No need to call NextStage() or do anything here
        }
        
        private void OnCharacterCreationFinalized()
        {
            // Apply stored specialization selections at the very end
            ApplyStoredSpecializations();

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
        }

        /// <summary>
        /// Apply stored specializations (lore/career) at the very end of character creation
        /// This prevents issues where going back and changing profession would keep the old specialization
        /// </summary>
        private void ApplyStoredSpecializations()
        {
            var hero = Hero.MainHero;

            // Apply stored lore for spellcasters
            if (!string.IsNullOrEmpty(_selectedLoreId))
            {
                TORCommon.Log($"[TorCharacterCreationContentHandler] Applying stored lore: {_selectedLoreId}", NLog.LogLevel.Info);
                hero.AddKnownLore(_selectedLoreId);
                var info = hero.GetExtendedInfo();
                if (info.SpellCastingLevel < SpellCastingLevel.Entry)
                {
                    hero.SetSpellCastingLevel(SpellCastingLevel.Entry);
                }
                TORCommon.Log($"[TorCharacterCreationContentHandler] Successfully applied lore {_selectedLoreId}", NLog.LogLevel.Info);
            }

            // Apply stored career for vampires/priests
            if (!string.IsNullOrEmpty(_selectedCareerId))
            {
                TORCommon.Log($"[TorCharacterCreationContentHandler] Applying stored career: {_selectedCareerId}", NLog.LogLevel.Info);
                var career = Game.Current.ObjectManager.GetObject<CharacterDevelopment.CareerSystem.CareerObject>(_selectedCareerId);
                if (career != null)
                {
                    hero.AddCareer(career);
                    TORCommon.Log($"[TorCharacterCreationContentHandler] Successfully applied career {_selectedCareerId}", NLog.LogLevel.Info);

                    // For priest careers, also add devotion to the corresponding god
                    if (_selectedCareerId == "WarriorPriest")
                    {
                        var sigmar = ReligionObject.All.FirstOrDefault(r => r.StringId == "tor_sigmar");
                        if (sigmar != null)
                        {
                            hero.AddReligiousInfluence(sigmar, TORConstants.DEVOTED_TRESHOLD, shouldNotify: false);
                            TORCommon.Log($"[TorCharacterCreationContentHandler] Added devotion to Sigmar", NLog.LogLevel.Info);
                        }
                    }
                    else if (_selectedCareerId == "WarriorPriestUlric")
                    {
                        var ulric = ReligionObject.All.FirstOrDefault(r => r.StringId == "tor_ulric");
                        if (ulric != null)
                        {
                            hero.AddReligiousInfluence(ulric, TORConstants.DEVOTED_TRESHOLD, shouldNotify: false);
                            TORCommon.Log($"[TorCharacterCreationContentHandler] Added devotion to Ulric", NLog.LogLevel.Info);
                        }
                    }
                }
                else
                {
                    TORCommon.Log($"[TorCharacterCreationContentHandler] Career not found: {_selectedCareerId}", NLog.LogLevel.Error);
                }
            }
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
        }

        private CharacterCreationManager _manager; // Store reference for stage management

        public void AfterInitializeContent(CharacterCreationManager manager)
        {
            TORCommon.Log("[TOR CharacterCreation] AfterInitializeContent called", NLog.LogLevel.Info);

            // Store manager reference for later use
            _manager = manager;

            // ALWAYS insert TORSpecializationStage - it will handle skip logic internally
            try
            {
                var stagesField = typeof(CharacterCreationManager).GetField("_stages",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                if (stagesField != null)
                {
                    var stages = stagesField.GetValue(manager) as MBList<CharacterCreationStageBase>;
                    if (stages != null)
                    {
                        // Find CharacterCreationNarrativeStage and insert our stage after it
                        int narrativeIndex = -1;
                        for (int i = 0; i < stages.Count; i++)
                        {
                            if (stages[i].GetType().Name == "CharacterCreationNarrativeStage")
                            {
                                narrativeIndex = i;
                                break;
                            }
                        }

                        if (narrativeIndex >= 0)
                        {
                            stages.Insert(narrativeIndex + 1, new TORSpecializationStage());
                            TORCommon.Log($"[TOR CharacterCreation] Inserted TORSpecializationStage at index {narrativeIndex + 1}", NLog.LogLevel.Info);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                TORCommon.Log($"[TOR CharacterCreation] Failed to insert stage: {ex.Message}", NLog.LogLevel.Error);
            }
        }

        public void OnStageCompleted(CharacterCreationStageBase stage)
        {
            // Called when a character creation stage is completed
            var stages = stage;

            // DEBUG: Log stage type to understand what's completing
            TORCommon.Log($"[OnStageCompleted] Stage completed: {stages.GetType().Name}, _selectedProfessionId: '{_selectedProfessionId}'", NLog.LogLevel.Info);

            if (stages.GetType() == typeof(CharacterCreationFaceGeneratorStage))
            {
                this.OnFinalizeFaceCreation();
            }
            if (stages.GetType() == typeof(CharacterCreationCultureStage))
            {
                OnCultureSelected();
            }
        }

        public bool NeedsSpecialization(string narrativeStep)
        {
            // Check if selected profession requires specialization (lore, bloodline, or priesthood choice)
            return narrativeStep switch
            {
                // Spellcasters need lore selection
                "option_3_empire_magister_apprentice" => true,
                "option_3_bretonnia_damsel" => true,
                "option_3_we_spellsinger" => true,
                "option_3_eo_greylord_apprentice" => true,

                // Vampires need bloodline selection
                "option_3_vc_vampire" => true,
                "option_3_mousillon_vampire" => true,

                // Empire priests need god selection
                "option_3_empire_priest_acolyte" => true,

                _ => false
            };
        }

        private void OnCultureSelected()
        {
            // Set race and default body properties based on selected culture
            string default_elf = "<BodyProperties version='4' age='25.84' weight='0.5000' build='0.5000'  key='000BAC088000100DB976648E6774B835537D86629511323BDCB177278A84F667017776140748B49500000000000000000000000000000000000000003EFC5002'/>";
            string default_empire = "<BodyProperties version='4' age='25.84' weight='0.5000' build='0.5000'  key='000500000000000D797664884754DCBAA35E866295A0967774414A498C8336860F7776F20BA7B7A500000000000000000000000000000000000000003CFC2002'/>";
            string default_bretonnia = "<BodyProperties version='4' age='25.84' weight='0.5000' build='0.5000'  key='001CB80CC000300D7C7664876753888A7577866254C69643C4B647398C95A0370077760307A7497300000000000000000000000000000000000000003AF47002'/>";
            string default_vc = "<BodyProperties version='4' age='25.84' weight='0.5000' build='0.5000'  key='0028C80FC000100DBA756445533377873CD1833B3101B44A21C3C5347CA32C260F7776F20BBC35E8000000000000000000000000000000000000000042F41002'/>";
            string default_dwarf = "<BodyProperties version='4' age='25' weight='0.4182' build='0.1898' key='0005000F00000280F77664884754DCBAFF9E566095F09F1F74414A49893F81FE0F77760307A7B7A536000000000000000000000000000007000000003CFC0002'/>";
            string default_orc = "<BodyProperties version='4' age='25' weight='0.3657' build='0.2978'  key='0005100000CC00005C12429361532471D9656C584FA9A47724B588AAD7B53C5DDACBA6130657877845CCBADBCCBCBABC0000000000000016000000002ECC0000'/>";
            string keyValue;

            var culture = CharacterObject.PlayerCharacter.Culture;

            if (culture.StringId == TORConstants.Cultures.ASRAI || culture.StringId == TORConstants.Cultures.EONIR)
            {
                keyValue = default_elf;
                CharacterObject.PlayerCharacter.Race = FaceGen.GetRaceOrDefault("elf");
            }
            else if (culture.StringId == TORConstants.Cultures.EMPIRE)
            {
                keyValue = default_empire;
                CharacterObject.PlayerCharacter.Race = FaceGen.GetRaceOrDefault("human");
            }
            else if (culture.StringId == TORConstants.Cultures.BRETONNIA || culture.StringId == TORConstants.Cultures.MOUSILLON)
            {
                keyValue = default_bretonnia;
                CharacterObject.PlayerCharacter.Race = FaceGen.GetRaceOrDefault("human");
            }
            else if (culture.StringId == TORConstants.Cultures.SYLVANIA)
            {
                keyValue = default_vc;
                CharacterObject.PlayerCharacter.Race = FaceGen.GetRaceOrDefault("human");
            }
            else if (culture.StringId == TORConstants.Cultures.DAWI)
            {
                keyValue = default_dwarf;
                CharacterObject.PlayerCharacter.Race = FaceGen.GetRaceOrDefault("dwarf");
            }
            else if (culture.StringId == TORConstants.Cultures.GREENSKIN)
            {
                keyValue = default_orc;
                CharacterObject.PlayerCharacter.Race = FaceGen.GetRaceOrDefault("orc");
            }
            else
            {
                keyValue = default_empire;
                CharacterObject.PlayerCharacter.Race = FaceGen.GetRaceOrDefault("human");
            }

            if (BodyProperties.FromString(keyValue, out BodyProperties properties))
            {
                CharacterObject.PlayerCharacter.UpdatePlayerCharacterBodyProperties(properties, CharacterObject.PlayerCharacter.Race, CharacterObject.PlayerCharacter.IsFemale);
            }

            TORCommon.Log($"[OnCultureSelected] Set race for culture {culture.StringId} to {CharacterObject.PlayerCharacter.Race}", NLog.LogLevel.Info);
        }

        public void OnCharacterCreationFinalize(CharacterCreationManager manager)
        {
            OnCharacterCreationFinalized();
        }
    }
}
