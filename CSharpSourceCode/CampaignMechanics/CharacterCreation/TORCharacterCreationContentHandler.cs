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
    public class TORCharacterCreationContentHandler : ICharacterCreationContentHandler
    {
        // Static instance for access from Harmony patches
        public static TORCharacterCreationContentHandler Instance { get; private set; }

        // Static flag to indicate we're past narrative stages (Stage 4 or later)
        public static bool IsPastNarrativeStages { get; set; } = false;

        // Track last stage index to determine navigation direction
        public int LastStageIndex { get; set; } = -1;

        private readonly List<CharacterCreationOption> _options;
        private readonly List<SpecializationOption> _specializationOptions;
        private bool _isFemale = false;
        private int _originalRace = 0;
        private string _currentEquipmentRosterId = "player_char_creation_childhood_age_empire_default_m";
        private string _selectedStage2OptionId = ""; // Track stage 2 selection (Wood Elf gods, Dwarf grudges)
        private string _selectedProfessionId = ""; // Track stage 3 selection for stage 4 conditions
        private string _selectedSpecializationOptionId = null; // Store selected specialization option ID from XML (applied at finalization)
        private NarrativeMenuCharacter _narrativePlayerCharacter = null; // Reference to narrative menu character for updating face
        private CampaignVec2? _storedSpawnPosition = null; // Store spawn position from specialization option (if specified)

        private const int FocusToAdd = 1;
        private const int SkillLevelToAdd = 10;
        private const int AttributeLevelToAdd = 1;

        // Store original skill levels before applying bonuses (ChangeSkillLevel with negative values doesn't work)
        private Dictionary<string, int> _originalSkillLevels = new Dictionary<string, int>();
        private Dictionary<string, int> _originalSkillFocus = new Dictionary<string, int>();


        public TORCharacterCreationContentHandler()
        {
            // Set static instance for Harmony patch access
            Instance = this;

            // Reset flag for fresh character creation
            IsPastNarrativeStages = false;

            try
            {
                var path = TORPaths.TORCoreModuleExtendedDataPath + "tor_cc_options.xml";
                XmlSerializer ser = new(typeof(List<CharacterCreationOption>));
                _options = ser.Deserialize(File.OpenRead(path)) as List<CharacterCreationOption>;

                if (_options == null)
                {
                    throw new TORCCXmlLoadException(path,
                        new InvalidOperationException("XML deserialization returned null - file may be empty or invalid"));
                }
            }
            catch (Exception ex) when (ex is not TORCCXmlLoadException)
            {
                var path = TORPaths.TORCoreModuleExtendedDataPath + "tor_cc_options.xml";
                throw new TORCCXmlLoadException(path, ex);
            }

            try
            {
                var specPath = TORPaths.TORCoreModuleExtendedDataPath + "tor_specialization_options.xml";
                XmlSerializer specSer = new(typeof(List<SpecializationOption>));
                _specializationOptions = specSer.Deserialize(File.OpenRead(specPath)) as List<SpecializationOption>;

                if (_specializationOptions == null)
                {
                    throw new TORCCXmlLoadException(specPath,
                        new InvalidOperationException("XML deserialization returned null - file may be empty or invalid"));
                }
            }
            catch (Exception ex) when (ex is not TORCCXmlLoadException)
            {
                var specPath = TORPaths.TORCoreModuleExtendedDataPath + "tor_specialization_options.xml";
                throw new TORCCXmlLoadException(specPath, ex);
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
            _narrativePlayerCharacter = new NarrativeMenuCharacter("player_character",
                CharacterObject.PlayerCharacter.GetBodyProperties(CharacterObject.PlayerCharacter.Equipment, -1),
                CharacterObject.PlayerCharacter.Race, CharacterObject.PlayerCharacter.IsFemale);

            List<NarrativeMenuCharacter> playerCharacterList = new List<NarrativeMenuCharacter> { _narrativePlayerCharacter };

            // Stage 1: Origin Menu
            NarrativeMenu stage1Menu = new NarrativeMenu("tor_origin_menu", // stringId
                "start", // inputMenuId (from culture selection)
                "tor_growth_menu", // outputMenuId (to stage 2)
                new TextObject("{=tor_cc_origin_summary_str}Origin"), // title
                new TextObject("{TOR_CC_ORIGIN}"), // description
                playerCharacterList, // characters to display
                new NarrativeMenu.GetNarrativeMenuCharacterArgsDelegate((culture, occupationType, manager) =>
                    GetPlayerMenuCharacterArgs("player_character", manager)));

            // Stage 2: Growth Menu
            NarrativeMenu stage2Menu = new NarrativeMenu("tor_growth_menu", "tor_origin_menu", "tor_profession_menu",
                new TextObject("{=tor_cc_growth_summary_str}Growth"), new TextObject("{TOR_CC_GROWTH}"), playerCharacterList,
                new NarrativeMenu.GetNarrativeMenuCharacterArgsDelegate((culture, occupationType, manager) =>
                    GetPlayerMenuCharacterArgs("player_character", manager)));

            // Stage 3: Profession Menu
            NarrativeMenu stage3Menu = new NarrativeMenu("tor_profession_menu", "tor_growth_menu",
                "narrative_face_generator_menu", // Exit narrative stage ? TORSpecializationStage handles stage 4
                new TextObject("{=tor_cc_profession_summary_str}Profession"), new TextObject("{TOR_CC_PROFESSION}"), playerCharacterList,
                new NarrativeMenu.GetNarrativeMenuCharacterArgsDelegate((culture, occupationType, manager) =>
                    GetPlayerMenuCharacterArgs("player_character", manager)));
            
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
                NarrativeMenuOption narrativeOption = new(option.Id, // stringId
                    new TextObject(option.OptionText), // text
                    new TextObject(option.OptionFlavourText), // description
                    args => GetOptionArgs(args, option), // argsDelegate
                    manager => OptionCondition(manager, option), // conditionDelegate
                    manager => OnOptionSelected(manager, option.Id), // selectDelegate
                    null // consequenceDelegate
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
            List<NarrativeMenuCharacterArgs> list =
            [
                new(characterId, 25, // age
                    _currentEquipmentRosterId, // equipment roster ID (updated when option is selected)
                    "act_childhood_schooled", // standard character creation animation
                    "spawnpoint_player_1", // standard spawn point
                    isFemale: CharacterObject.PlayerCharacter.IsFemale)

            ];
            return list;
        }

        // Helper method: Sets skill and attribute bonuses for an option
        private void GetOptionArgs(NarrativeMenuOptionArgs args, CharacterCreationOption option)
        {
            // Store custom positive effect text if it exists (for display via Harmony patch)
            if (!string.IsNullOrEmpty(option.PositiveEffectText))
            {
                HarmonyPatches.CharacterCreationPatches.CustomPositiveEffects[args] = new TextObject(option.PositiveEffectText);
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

            // Set values so they get applied (via ApplyFinalEffects) AND displayed in UI
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
            // Check if option's culture matches currently selected culture
            CultureObject currentCulture = manager.CharacterCreationContent.SelectedCulture;
            return currentCulture != null && currentCulture.StringId == option.Culture;
        }

        private void OnFinalizeFaceCreation()
        {
            _isFemale = CharacterObject.PlayerCharacter.IsFemale;
            _originalRace = CharacterObject.PlayerCharacter.Race;

            // Get the customized body properties from face editor
            var bodyProps = CharacterObject.PlayerCharacter.GetBodyProperties(CharacterObject.PlayerCharacter.Equipment);

            // CRITICAL: Update the NarrativeMenuCharacter so Origin/Growth/Profession stages show the customized face
            // This also automatically updates CharacterObject.PlayerCharacter which the specialization stage uses
            if (_narrativePlayerCharacter != null)
            {
                _narrativePlayerCharacter.UpdateBodyProperties(bodyProps, CharacterObject.PlayerCharacter.Race,
                    CharacterObject.PlayerCharacter.IsFemale);
            }

            // Reload culture-specific menu label texts after face creation
            SetMenuLabelTexts();
        }

        private void SetMenuLabelTexts()
        {
            TextObject originText;
            if (GameTexts.TryGetText("tor_cc_origin", out var stage1Text, CharacterObject.PlayerCharacter.Culture.StringId))
            {
                originText = stage1Text;
            }
            else
            {
                originText = new TextObject("Choose your family's background...");
            }
            MBTextManager.SetTextVariable("TOR_CC_ORIGIN", originText, false);

            TextObject growthText;
            if (GameTexts.TryGetText("tor_cc_growth", out var stage2Text, CharacterObject.PlayerCharacter.Culture.StringId))
            {
                growthText = stage2Text;
            }
            else
            {
                growthText = new TextObject("Teenage years...");
            }
            MBTextManager.SetTextVariable("TOR_CC_GROWTH", growthText, false);

            TextObject professionText;
            if (GameTexts.TryGetText("tor_cc_profession", out var stage3Text, CharacterObject.PlayerCharacter.Culture.StringId))
            {
                professionText = stage3Text;
            }
            else
            {
                professionText = new TextObject("Your starting profession...");
            }
            MBTextManager.SetTextVariable("TOR_CC_PROFESSION", professionText, false);
        }

        public string GetSelectedProfessionId() => _selectedProfessionId;

        /// <summary>
        /// Clear any stored specialization selections.
        /// Called when clicking Back to ensure old selections are cleared if user changes profession.
        /// NOTE: Race restoration is handled by the narrative menu when going back.
        /// </summary>
        public void ClearStoredSpecializations()
        {
            // Remove previously applied bonuses BEFORE clearing the ID
            ClearSpecializationBonuses();

            // Clear stored data
            _selectedSpecializationOptionId = null;
            _storedSpawnPosition = null;
        }


        /// <summary>
        /// Clear skill and attribute bonuses from specialization
        /// Called when entering specialization stage or going back
        /// </summary>
        public void ClearSpecializationBonuses()
        {
            if (string.IsNullOrEmpty(_selectedSpecializationOptionId))
            {
                return;
            }

            var hero = Hero.MainHero;
            var option = _specializationOptions?.FirstOrDefault(opt => opt.Id == _selectedSpecializationOptionId);

            if (option == null)
            {
                TORCommon.Log($"[TORCC] ClearSpecializationBonuses: Option not found for ID '{_selectedSpecializationOptionId}'", NLog.LogLevel.Warn);
                return;
            }

            // Restore skills to original values
            // We iterate through stored originals (unique skills only), not the full array with duplicates
            foreach (var kvp in _originalSkillLevels)
            {
                string skillId = kvp.Key;
                int originalLevel = kvp.Value;
                int originalFocus = _originalSkillFocus.ContainsKey(skillId) ? _originalSkillFocus[skillId] : 0;

                var skill = Skills.All.FirstOrDefault(x => x.StringId == skillId);
                if (skill != null)
                {
                    int beforeFocus = hero.HeroDeveloper.GetFocus(skill);

                    // Restore focus by calculating the difference
                    int focusDiff = originalFocus - beforeFocus;
                    if (focusDiff != 0)
                    {
                        hero.HeroDeveloper.AddFocus(skill, focusDiff, false);
                    }

                    // Restore skill level to original value
                    hero.HeroDeveloper.SetInitialSkillLevel(skill, originalLevel);
                }
            }

            // Clear stored originals after restoring
            _originalSkillLevels.Clear();
            _originalSkillFocus.Clear();

            // Remove attribute bonuses (can have multiple)
            if (option.AttributesToIncrease != null && option.AttributesToIncrease.Length > 0)
            {
                foreach (var attributeRaw in option.AttributesToIncrease)
                {
                    bool isDecrease = attributeRaw.StartsWith("-");
                    string attributeName = isDecrease ? attributeRaw.Substring(1) : attributeRaw;

                    var attribute = Attributes.All.FirstOrDefault(x => x.StringId == attributeName.ToLower());
                    if (attribute != null)
                    {
                        // Reverse the change: if it was +1, now -1; if it was -1, now +1
                        int changeAmount = isDecrease ? AttributeLevelToAdd : -AttributeLevelToAdd;
                        hero.HeroDeveloper.AddAttribute(attribute, changeAmount, false);
                    }
                }
            }
        }

        /// <summary>
        /// Calculate final skill/attribute bonuses by combining profession + specialization.
        /// Specialization can modify (add/remove from) profession bonuses.
        /// Returns net skill changes and attribute change.
        /// </summary>
        private (Dictionary<string, int> skillChanges, string attributeChange) CalculateFinalBonuses()
        {
            var netSkillChanges = new Dictionary<string, int>();
            string netAttributeChange = null;

            // Start with profession bonuses
            var professionOption = _options?.FirstOrDefault(opt => opt.Id == _selectedProfessionId);
            if (professionOption != null)
            {
                // Add profession skills
                if (professionOption.SkillsToIncrease != null)
                {
                    foreach (var skillId in professionOption.SkillsToIncrease)
                    {
                        if (!netSkillChanges.ContainsKey(skillId))
                        {
                            netSkillChanges[skillId] = 0;
                        }
                        netSkillChanges[skillId] += 1; // Each entry is +1
                    }
                }

                // Set profession attribute
                netAttributeChange = professionOption.AttributeToIncrease;
            }

            // Apply specialization modifications (if any)
            if (!string.IsNullOrEmpty(_selectedSpecializationOptionId))
            {
                var specializationOption = _specializationOptions?.FirstOrDefault(opt => opt.Id == _selectedSpecializationOptionId);
                if (specializationOption != null)
                {
                    // Modify skills (can add or remove)
                    if (specializationOption.SkillsToIncrease != null)
                    {
                        foreach (var skillIdRaw in specializationOption.SkillsToIncrease)
                        {
                            bool isDecrease = skillIdRaw.StartsWith("-");
                            string skillId = isDecrease ? skillIdRaw.Substring(1) : skillIdRaw;

                            if (!netSkillChanges.ContainsKey(skillId))
                            {
                                netSkillChanges[skillId] = 0;
                            }
                            netSkillChanges[skillId] += isDecrease ? -1 : 1;
                        }
                    }

                    // TODO: Update for multiple attributes - this method is currently unused
                    // Override attribute if specialization specifies one
                    if (specializationOption.AttributesToIncrease != null && specializationOption.AttributesToIncrease.Length > 0)
                    {
                        // For now, just take the first attribute (this method isn't called anywhere)
                        netAttributeChange = specializationOption.AttributesToIncrease[0];
                    }
                }
            }

            return (netSkillChanges, netAttributeChange);
        }

        /// <summary>
        /// Check if the given profession has any specialization options available
        /// </summary>
        /// <param name="professionId">The profession ID to check (e.g., "option_3_empire_knight")</param>
        /// <returns>True if at least one specialization option exists for this profession</returns>
        public bool HasSpecializationOptions(string professionId)
        {
            if (string.IsNullOrEmpty(professionId) || _specializationOptions == null)
            {
                return false;
            }

            return _specializationOptions.Any(opt => opt.ProfessionRequirement == professionId);
        }

        /// <summary>
        /// Get all specialization options for the given profession
        /// </summary>
        /// <param name="professionId">The profession ID to filter by (e.g., "option_3_empire_knight")</param>
        /// <returns>List of specialization options matching the profession requirement</returns>
        public List<SpecializationOption> GetSpecializationOptions(string professionId)
        {
            if (string.IsNullOrEmpty(professionId))
            {
                return new List<SpecializationOption>();
            }

            return _specializationOptions?.Where(opt => opt.ProfessionRequirement == professionId).ToList() ?? new List<SpecializationOption>();
        }

        /// <summary>
        /// Store the selected specialization option ID to be processed at character creation finalization.
        /// This is the new XML-driven approach - the option ID determines which career/lore to apply.
        /// </summary>
        public void SetSelectedSpecializationOptionId(string optionId)
        {
            _selectedSpecializationOptionId = optionId;
        }

        public string GetSelectedSpecializationOptionId() => _selectedSpecializationOptionId;

        /// <summary>
        /// Get the currently selected specialization option object (for Harmony patch access to bonuses)
        /// </summary>
        public SpecializationOption GetSelectedSpecializationOption()
        {
            if (string.IsNullOrEmpty(_selectedSpecializationOptionId))
                return null;

            return _specializationOptions?.FirstOrDefault(opt => opt.Id == _selectedSpecializationOptionId);
        }

        private void OnOptionSelected(CharacterCreationManager manager, string optionId)
        {
            var selectedOption = _options.Find(x => x.Id == optionId);
            var race = _originalRace;
            var isFemale = _isFemale;

            // Track stage 2 selection for deferred application
            if (selectedOption != null && selectedOption.StageNumber == 2)
            {
                _selectedStage2OptionId = optionId;
            }

            // Track stage 3 (profession) selection for stage 4 skip logic
            if (selectedOption != null && selectedOption.StageNumber == 3)
            {
                _selectedProfessionId = optionId;
            }

            if (optionId == "option_3_vc_vampire" || optionId == "option_3_mousillon_vampire")
            {
                race = FaceGen.GetRaceOrDefault("vampire");
            }
            else if (optionId == "option_3_bretonnia_damsel" && !CharacterObject.PlayerCharacter.IsFemale)
            {
                isFemale = true;
            }
            else if (optionId == "option_3_bretonnia_knight_errant" && CharacterObject.PlayerCharacter.IsFemale)
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

        private void UpdateEquipment(CharacterCreationManager manager, CharacterCreationOption selectedOption, bool isFemale)
        {
            MBEquipmentRoster roster = null;
            try
            {
                roster = MBObjectManager.Instance.GetObject<MBEquipmentRoster>(selectedOption.EquipmentSetId);
                if (roster == null)
                {
                }
                else
                {
                    // Track the equipment roster ID for character display
                    _currentEquipmentRosterId = selectedOption.EquipmentSetId;
                }
            }
            catch (NullReferenceException ex)
            {
                throw new TORCCEquipmentUpdateException(selectedOption?.EquipmentSetId ?? "unknown", ex);
            }

            if (roster != null && !roster.DefaultEquipment.IsEmpty())
            {
                var character = manager.CurrentMenu.Characters[0];

                var equipment = roster.DefaultEquipment;
                var bodyProperties = CharacterObject.PlayerCharacter.GetBodyProperties(equipment);

                character.SetEquipment(roster);
                CharacterObject.PlayerCharacter.UpdatePlayerCharacterBodyProperties(bodyProperties, CharacterObject.PlayerCharacter.Race, isFemale);
                character.IsFemale = isFemale;

                CharacterObject.PlayerCharacter.Equipment.FillFrom(roster.DefaultEquipment);
                CharacterObject.PlayerCharacter.FirstCivilianEquipment.FillFrom(equipment);
            }
        }
        
        public void OnCharacterCreationFinalize(CharacterCreationManager manager)
        {
            // Apply bonuses in order: Stage 2 (gods/grudges) -> Stage 3 (professions) -> Stage 4 (specializations)
            ApplyStage2Bonuses();
            // All profession bonuses are now applied at character creation finalization via ApplyProfessionBonuses()
            ApplyProfessionBonuses();
            ApplyStoredSpecializations();

            // Everyone gets these base attributes
            Hero.MainHero.AddAttribute("AbilityUser");
            Hero.MainHero.AddAttribute("CanPlaceArtillery");
            
            CultureObject culture = CharacterObject.PlayerCharacter.Culture;
            Hero.MainHero.AddCultureSpecificCustomResource(0);

            // Determine spawn location with priority: Career override > Culture-specific mercenary > Culture default
            CampaignVec2 position2D = GetSpawnLocation(culture);

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
        /// Get the spawn location for the player character based on their specialization and culture.
        /// Priority: Stored spawn position from specialization > Culture default
        /// </summary>
        private CampaignVec2 GetSpawnLocation(CultureObject culture)
        {
            // Priority 1: Check for stored spawn position from specialization option
            if (_storedSpawnPosition.HasValue)
            {
                return _storedSpawnPosition.Value;
            }

            // Priority 2: Read spawn position from culture XML (start_point_position_x/y attributes)
            var cultureSpawn = culture.StartingPoint;
            return cultureSpawn;
        }

        /// <summary>
        /// Apply Stage 2 bonuses (Wood Elf gods/symbols, Dwarf grudges) at character creation finalization.
        /// This is called BEFORE applying profession bonuses to ensure proper application order.
        /// </summary>
        private void ApplyStage2Bonuses()
        {
            var hero = Hero.MainHero;
            string stage2OptionId = _selectedStage2OptionId;

            if (string.IsNullOrEmpty(stage2OptionId))
            {
                return;
            }


            // Wood Elf god/symbol selection
            if (hero.Culture.StringId == TORConstants.Cultures.ASRAI)
            {
                var settlementBehavior = Campaign.Current.GetCampaignBehavior<TORCustomSettlementCampaignBehavior>();
                string symbol = null;
                ReligionObject religion = null;

                switch (stage2OptionId)
                {
                    case "option_2_we_kurnous":
                        symbol = "WEKithbandSymbol";
                        religion = ReligionObject.All.FirstOrDefault(x => x.StringId == "cult_of_kurnous");
                        break;
                    case "option_2_we_isha":
                        symbol = "WETreekinSymbol";
                        religion = ReligionObject.All.FirstOrDefault(x => x.StringId == "cult_of_isha");
                        break;
                    case "option_2_we_loec":
                        symbol = "WEWardancerSymbol";
                        religion = ReligionObject.All.FirstOrDefault(x => x.StringId == "cult_of_loec");
                        break;
                    case "option_2_we_vaul":
                        symbol = "WEKithbandSymbol";
                        religion = ReligionObject.All.FirstOrDefault(x => x.StringId == "cult_of_vaul");
                        break;
                    case "option_2_we_khaine":
                        symbol = "WEKithbandSymbol";
                        religion = ReligionObject.All.FirstOrDefault(x => x.StringId == "cult_of_anath_raema");
                        break;
                }

                if (symbol != null && religion != null)
                {
                    hero.AddAttribute(symbol);
                    if (settlementBehavior != null)
                    {
                        settlementBehavior.UnlockOakUpgrade(symbol);
                    }

                    hero.AddReligiousInfluence(religion, 40);
                }
            }

            // Dwarf grudge selection
            if (hero.Culture.StringId == TORConstants.Cultures.DAWI)
            {
                string grudge = null;

                switch (stage2OptionId)
                {
                    case "option_2_dw_umgi":
                        grudge = "HumanGrudge";
                        break;
                    case "option_2_dw_elgi":
                        grudge = "ElfGrudge";
                        break;
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
                    hero.AddAttribute(grudge);
                }
            }

        }

        /// <summary>
        /// Apply profession-specific bonuses based on the selected profession at character creation finalization.
        /// This is called BEFORE applying specializations, so base profession bonuses are set first.
        /// </summary>
        private void ApplyProfessionBonuses()
        {
            var hero = Hero.MainHero;
            string professionId = _selectedProfessionId;
            
            // Apply profession-specific bonuses based on selected profession
            switch (professionId)
            {
                case "option_3_empire_magister_apprentice":
                    hero.AddAttribute("SpellCaster");
                    hero.AddAbility("Dart");
                    hero.AddKnownLore("MinorMagic");
                    hero.SetSpellCastingLevel(SpellCastingLevel.Entry);
                    hero.HeroDeveloper.SetInitialSkillLevel(TORSkills.Spellcraft, 25);
                    hero.HeroDeveloper.AddPerk(TORPerks.Spellcraft.EntrySpells);
                    hero.AddCareer(TORCareers.ImperialMagister);
                    _storedSpawnPosition = new CampaignVec2(new Vec2(1278.084f, 1056.505f), true); // Imperial College of Magic
                    break;
                case "option_3_empire_knight":
                    hero.AddCareer(TORCareers.KnightOldWorld);
                    _storedSpawnPosition = new CampaignVec2(new Vec2(1434.869f, 917.6942f), true); // Knight of the Old World
                    break;

                case "option_3_bretonnia_damsel":
                    hero.AddAttribute("SpellCaster");
                    hero.AddAttribute("PriestLady");
                    hero.AddAbility("Dart");
                    hero.AddAbility("AuraOfTheLady");
                    hero.AddKnownLore("MinorMagic");
                    hero.SetSpellCastingLevel(SpellCastingLevel.Entry);
                    hero.HeroDeveloper.SetInitialSkillLevel(TORSkills.Spellcraft, 25);
                    hero.HeroDeveloper.SetInitialSkillLevel(TORSkills.Faith, 25);
                    hero.HeroDeveloper.AddPerk(TORPerks.Spellcraft.EntrySpells);
                    hero.AddCareer(TORCareers.GrailDamsel);
                    _storedSpawnPosition = new CampaignVec2(new Vec2(941.8889f, 1249.213f), true); // Grail Chapel
                    // Add Realm Knight companion
                    var knight = MBObjectManager.Instance.GetObject<CharacterObject>("tor_br_realm_knight");
                    if (knight != null) hero.PartyBelongedTo.Party.AddMember(knight, 1);
                    break;

                case "option_3_we_spellsinger":
                    hero.AddAttribute("SpellCaster");
                    hero.AddKnownLore("LoreOfLife");
                    hero.AddKnownLore("LoreOfBeasts");
                    hero.AddAbility("SummerHeat");
                    hero.AddAbility("AmberSpear");
                    hero.SetSpellCastingLevel(SpellCastingLevel.Entry);
                    hero.HeroDeveloper.SetInitialSkillLevel(TORSkills.Spellcraft, 25);
                    hero.HeroDeveloper.AddPerk(TORPerks.Spellcraft.EntrySpells);
                    hero.AddCareer(TORCareers.Spellsinger);
                    _storedSpawnPosition = new CampaignVec2(new Vec2(1233.78f, 781.862f), true); // Spellsinger location
                    break;

                case "option_3_eo_greylord_apprentice":
                    hero.AddAttribute("SpellCaster");
                    hero.AddKnownLore("LoreOfFire");
                    hero.AddAbility("BoltOfAqshy");
                    hero.SetSpellCastingLevel(SpellCastingLevel.Entry);
                    hero.HeroDeveloper.SetInitialSkillLevel(TORSkills.Spellcraft, 25);
                    hero.HeroDeveloper.AddPerk(TORPerks.Spellcraft.EntrySpells);
                    hero.AddCareer(TORCareers.GreyLord);
                    _storedSpawnPosition = new CampaignVec2(new Vec2(1216.198f, 1345.101f), true); // Grey Lord location
                    break;

                case "option_3_empire_priest_acolyte":
                    hero.AddAttribute("Priest");
                    // Specialization (Sigmar/Ulric) will be applied by ApplyStoredSpecializations
                    break;

                case "option_3_vc_vampire":
                case "option_3_mousillon_vampire":
                    hero.AddAttribute("Vampire");
                    hero.AddAttribute("Necromancer");
                    hero.AddReligiousInfluence(ReligionObject.All.FirstOrDefault(x => x.StringId == "cult_of_nagash"), 60);
                    // Bloodline career will be applied by ApplyStoredSpecializations
                    break;

                case "option_3_vc_necromancer":
                case "option_3_mousillon_necromancer":
                    hero.AddAttribute("SpellCaster");
                    hero.AddAttribute("Necromancer");
                    hero.AddAbility("SummonSkeleton");
                    hero.AddKnownLore("MinorMagic");
                    hero.AddKnownLore("Necromancy");
                    hero.SetSpellCastingLevel(SpellCastingLevel.Entry);
                    hero.HeroDeveloper.SetInitialSkillLevel(TORSkills.Spellcraft, 25);
                    hero.HeroDeveloper.AddPerk(TORPerks.Spellcraft.EntrySpells);
                    hero.AddCareer(TORCareers.Necromancer);
                    hero.AddReligiousInfluence(ReligionObject.All.FirstOrDefault(x => x.StringId == "cult_of_nagash"), 25);
                    _storedSpawnPosition = new CampaignVec2(new Vec2(1666.918f, 1019.001f), true); // Necromancer location
                    break;

                case "option_3_dw_shield_breaker":
                    hero.AddCareer(TORCareers.Ironbreaker);
                    _storedSpawnPosition = new CampaignVec2(new Vec2(1306.575f, 838.3152f), true); // Ironbreaker hold
                    break;

                case "option_3_dw_slayer":
                    hero.AddCareer(TORCareers.Slayer);
                    _storedSpawnPosition = new CampaignVec2(new Vec2(1787.716f, 1021.437f), true); // Slayer shrine
                    break;

                case "option_3_dw_rune_smith":
                    hero.AddCareer(TORCareers.Runelord);
                    hero.AddAttribute("RuneCraft");
                    _storedSpawnPosition = new CampaignVec2(new Vec2(1222.444f, 692.9744f), true); // Runelord forge
                    break;

                case "option_3_empire_witch_hunter":
                    hero.AddCareer(TORCareers.WitchHunter);
                    _storedSpawnPosition = new CampaignVec2(new Vec2(1560.23f, 974.5349f), true); // Witch Hunter location
                    break;

                case "option_3_bretonnia_knight_errant":
                    hero.AddCareer(TORCareers.GrailKnight);
                    _storedSpawnPosition = new CampaignVec2(new Vec2(941.8889f, 1249.213f), true); // Grail Knight location
                    break;

                case "option_3_mousillon_knight_errant":
                    hero.AddCareer(TORCareers.BlackGrailKnight);
                    _storedSpawnPosition = new CampaignVec2(new Vec2(958.4354f, 1044.788f), true); // Mousillon - Black Grail Knight
                    break;

                case "option_3_we_waywatcher":
                case "option_3_eo_ghost_strider":
                    hero.AddCareer(TORCareers.Waywatcher);
                    _storedSpawnPosition = new CampaignVec2(new Vec2(1243.44f, 910.1643f), true); // Waywatcher location
                    break;

                case "option_3_gs_path_of_boss":
                case "option_3_gs_path_of_bully":
                case "option_3_gs_path_of_boar_boys":
                case "option_3_gs_path_of_savage_boys":
                    hero.AddCareer(TORCareers.OrcBoss);
                    break;

                case "option_3_gs_path_of_shaman":
                    hero.AddAttribute("SpellCaster");
                    hero.AddAbility("GazeOfMork");
                    hero.AddKnownLore("BigWaaagh");
                    hero.SetSpellCastingLevel(SpellCastingLevel.Entry);
                    hero.HeroDeveloper.SetInitialSkillLevel(TORSkills.Spellcraft, 25);
                    hero.HeroDeveloper.AddPerk(TORPerks.Spellcraft.EntrySpells);
                    hero.AddCareer(TORCareers.OrcShaman);
                    break;

                // Empire knight from stage 2 (not stage 3, but included for completeness)
                case "option_empire_knight":
                    hero.AddCareer(TORCareers.KnightOldWorld);
                    break;
            }

            // Default career if none was set
            if (hero.GetCareer() == null)
            {
                if (hero.Culture.StringId == TORConstants.Cultures.ASRAI)
                {
                    hero.AddCareer(TORCareers.Warden);
                }
                else
                {
                    hero.AddCareer(TORCareers.Mercenary);
                }
            }
        }

        /// <summary>
        /// Apply stored specializations (lore/career) at the very end of character creation
        /// </summary>
        private void ApplyStoredSpecializations()
        {
            var hero = Hero.MainHero;

            // NEW: Apply specialization based on option ID from XML
            if (string.IsNullOrEmpty(_selectedSpecializationOptionId))
            {
                return;
            }

            // NOTE: Skill/Attribute bonuses are applied when user clicks Next on specialization stage
            // via TORSpecializationStageView.NextStage() -> ApplySpecializationBonuses()
            // This makes them visible in the character creation final screen.
            // We only apply careers, lores, and other non-stat bonuses here at finalization.

            // NOTE: Race is applied immediately in TORSpecializationStageView when option is selected
            // This ensures the race change is visible in the banner editor stage

            switch (_selectedSpecializationOptionId)
            {
                // Hard-coded logic to map option IDs to careers/lores
                // PRIEST OPTIONS
                case "priest_sigmar":
                    {
                        hero.AddCareer(TORCareers.WarriorPriest);
                        hero.AddReligiousInfluence(ReligionObject.All.FirstOrDefault(x => x.StringId == "cult_of_sigmar"), 60);
                        hero.AddAttribute("PriestSigmar");
                        var skill = hero.GetSkillValue(TORSkills.Faith);
                        hero.HeroDeveloper.SetInitialSkillLevel(TORSkills.Faith, Math.Max(skill, 25));
                        hero.HeroDeveloper.AddPerk(TORPerks.Faith.NovicePrayers);
                        _storedSpawnPosition = new CampaignVec2(new Vec2(1283.261f, 1067.676f), true); // Altdorf gate - Sigmar's holy city
                        break;
                    }
                case "priest_ulric":
                    {
                        hero.AddCareer(TORCareers.WarriorPriestUlric);
                        hero.AddReligiousInfluence(ReligionObject.All.FirstOrDefault(x => x.StringId == "cult_of_ulric"), 60);
                        hero.AddAttribute("PriestUlric");
                        var skill = hero.GetSkillValue(TORSkills.Faith);
                        hero.HeroDeveloper.SetInitialSkillLevel(TORSkills.Faith, Math.Max(skill, 25));
                        hero.HeroDeveloper.AddPerk(TORPerks.Faith.NovicePrayers);
                        _storedSpawnPosition = new CampaignVec2(new Vec2(1346.493f, 1244.102f), true); // Middenheim gate - Ulric's holy city
                        break;
                    }
                // VAMPIRE BLOODLINE OPTIONS
                case "bloodline_von_carstein":
                    {
                        hero.AddAttribute("SpellCaster");
                        hero.AddAbility("NagashGaze");
                        hero.AddKnownLore("MinorMagic");
                        hero.AddKnownLore("Necromancy");
                        var skill = hero.GetSkillValue(TORSkills.Spellcraft);
                        hero.HeroDeveloper.SetInitialSkillLevel(TORSkills.Spellcraft, Math.Max(skill, 25));
                        hero.HeroDeveloper.AddPerk(TORPerks.Spellcraft.EntrySpells);
                        hero.AddCareer(TORCareers.MinorVampire);
                        _storedSpawnPosition = new CampaignVec2(new Vec2(1594.974f, 988.7784f), true); // Von Carstein territory
                        MBInformationManager.AddQuickInformation(new TextObject("Successfully learned Necromancy"), 0, CharacterObject.PlayerCharacter);
                        break;
                    }
                case "bloodline_blood_dragon":
                    hero.AddCareer(TORCareers.BloodKnight);
                    _storedSpawnPosition = new CampaignVec2(new Vec2(1277.776f, 942.5178f), true); // Blood Dragon location
                    break;
                case "bloodline_necrarch":
                    {
                        hero.AddAttribute("SpellCaster");
                        hero.AddAbility("NagashGaze");
                        hero.AddKnownLore("MinorMagic");
                        hero.AddKnownLore("Necromancy");
                        hero.AddCareer(TORCareers.Necrarch);
                        var skill = hero.GetSkillValue(TORSkills.Spellcraft);
                        hero.HeroDeveloper.SetInitialSkillLevel(TORSkills.Spellcraft, Math.Max(skill, 25));
                        hero.HeroDeveloper.AddPerk(TORPerks.Spellcraft.EntrySpells);
                        _storedSpawnPosition = new CampaignVec2(new Vec2(1565.885f, 1095.13f), true); // Necrarch location
                        MBInformationManager.AddQuickInformation(new TextObject("Successfully learned Necromancy"), 0, CharacterObject.PlayerCharacter);
                        break;
                    }
                case "knight_blazing_sun":
                    hero.AddReligiousInfluence(ReligionObject.All.FirstOrDefault(x => x.StringId == "cult_of_myrmidia"), 30);
                    _storedSpawnPosition = new CampaignVec2(new Vec2(1428.939f, 1114.684f), true); // Talabheim - Order of the Blazing Sun
                    break;
                case "knight_panthers":
                    _storedSpawnPosition = new CampaignVec2(new Vec2(1232.193f, 1105.359f), true); // Carroburg - Knight Panthers
                    break;
                case "knight_white_wolf":
                    hero.AddReligiousInfluence(ReligionObject.All.FirstOrDefault(x => x.StringId == "cult_of_ulric"), 30);
                    _storedSpawnPosition = new CampaignVec2(new Vec2(1346.493f, 1244.102f), true); // Middenheim - Knights of the White Wolf
                    break;
                case "knight_gryphon":
                    hero.AddReligiousInfluence(ReligionObject.All.FirstOrDefault(x => x.StringId == "cult_of_sigmar"), 30);
                    _storedSpawnPosition = new CampaignVec2(new Vec2(1283.261f, 1067.676f), true); // Altdorf - Order of the Gryphon
                    break;
                case "knight_reiksguard":
                    _storedSpawnPosition = new CampaignVec2(new Vec2(1306.128f, 1044.178f), true); // Castle Reiksguard - Reiksguard
                    break;
                // SPELLCASTER LORE OPTIONS
                case "lore_of_fire":
                    hero.AddKnownLore("LoreOfFire");
                    hero.AddAbility("CinderBlast");
                    break;
                case "lore_of_light":
                    hero.AddKnownLore("LoreOfLight");
                    hero.AddAbility("ShemsBurningGaze");
                    break;
                case "lore_of_metal":
                    hero.AddKnownLore("LoreOfMetal");
                    hero.AddAbility("GleamingArrow");
                    break;
                case "lore_of_death":
                    hero.AddKnownLore("LoreOfDeath");
                    hero.AddAbility("AshesAndDust");
                    break;
                case "lore_of_beasts":
                    hero.AddKnownLore("LoreOfBeasts");
                    hero.AddAbility("AmberSpear");
                    break;
                case "lore_of_heavens":
                    hero.AddKnownLore("LoreOfHeavens");
                    hero.AddAbility("LightningBolt");
                    break;
                case "lore_of_life":
                    hero.AddKnownLore("LoreOfLife");
                    hero.AddAbility("DrainLife");
                    break;
            }
        }

        private void SetHeroAge(float age)
        {
            Hero.MainHero.SetBirthDay(CampaignTime.YearsFromNow(-age));
        }

        public void InitializeContent(CharacterCreationManager manager)
        {
            foreach (var cultureId in TORConstants.Cultures.All)
            {
                var culture = MBObjectManager.Instance.GetObject<CultureObject>(cultureId);
                if (culture != null)
                {
                    manager.CharacterCreationContent.AddCharacterCreationCulture(culture, 1, 10);
                }
            }

            AddMenus(manager);
        }
        
        public void AfterInitializeContent(CharacterCreationManager manager)
        {
            //Intialize Specialization stage. No easy way to access stage field (if not current) therefore we use reflection
            try
            {
                var stagesField = typeof(CharacterCreationManager).GetField("_stages", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                if (stagesField != null)
                {
                    if (stagesField.GetValue(manager) is not MBList<CharacterCreationStageBase> stages)
                    {
                        return;
                    }

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
                    }
                }
            }
            catch (Exception ex)
            {
                throw new TORCCSpecializationStageLoadException("Failed to inject specialization stage into character creation flow", ex);
            }
        }

        public void OnStageCompleted(CharacterCreationStageBase stage)
        {
            // Called when a character creation stage is completed
            var stages = stage;

            if (stages.GetType() == typeof(CharacterCreationFaceGeneratorStage))
            {
                this.OnFinalizeFaceCreation();
            }

            if (stages.GetType() == typeof(CharacterCreationCultureStage))
            {
                OnCultureSelected();
            }

            if (stages.GetType() == typeof(CharacterCreationNarrativeStage))
            {
                OnNarrativeStageFinalized();
            }
            
            if (stages.GetType() == typeof(TORSpecializationStage))
            {
                OnSpecializationFinalized();
            }
            
            if (stages.GetType() == typeof(BannerEditorState))
            {
                LastStageIndex = 4;
            }
        }

        private void OnNarrativeStageFinalized()
        {
            LastStageIndex = 2;
        }
        
        private void OnSpecializationFinalized()
        {
            LastStageIndex = 3;
        }

        /// <summary>
        /// Apply ONLY specialization modifications (profession bonuses will be applied by engine)
        /// Called when user clicks Next on specialization stage
        /// Stores originals so we can revert if user goes back
        /// </summary>
        public void ApplySpecializationBonuses()
        {
            if (string.IsNullOrEmpty(_selectedSpecializationOptionId))
            {
                return;
            }

            var hero = Hero.MainHero;
            var specializationOption = _specializationOptions?.FirstOrDefault(opt => opt.Id == _selectedSpecializationOptionId);
            if (specializationOption == null)
            {
                TORCommon.Log($"[TORCC] ApplySpecializationBonuses: Option not found", NLog.LogLevel.Warn);
                return;
            }

            // Clear any stored original values from previous applications
            _originalSkillLevels.Clear();
            _originalSkillFocus.Clear();

            // Calculate ONLY specialization's modifications
            var skillChanges = new Dictionary<string, int>();
            if (specializationOption.SkillsToIncrease != null)
            {
                foreach (var skillIdRaw in specializationOption.SkillsToIncrease)
                {
                    bool isDecrease = skillIdRaw.StartsWith("-");
                    string skillId = isDecrease ? skillIdRaw.Substring(1) : skillIdRaw;

                    if (!skillChanges.ContainsKey(skillId))
                    {
                        skillChanges[skillId] = 0;
                    }
                    skillChanges[skillId] += isDecrease ? -1 : 1;
                }
            }

            // Store originals for ALL skills that will be modified
            foreach (var kvp in skillChanges)
            {
                string skillId = kvp.Key;
                var skill = Skills.All.FirstOrDefault(x => x.StringId == skillId);
                if (skill != null)
                {
                    _originalSkillLevels[skillId] = hero.GetSkillValue(skill);
                    _originalSkillFocus[skillId] = hero.HeroDeveloper.GetFocus(skill);
                }
            }

            // Apply combined skill changes as preview
            foreach (var kvp in skillChanges)
            {
                string skillId = kvp.Key;
                int netChange = kvp.Value;

                if (netChange == 0)
                {
                    continue; // No net change for this skill
                }

                var skill = Skills.All.FirstOrDefault(x => x.StringId == skillId);
                if (skill != null)
                {
                    int beforeSkill = hero.GetSkillValue(skill);

                    // Apply focus change
                    int focusChange = netChange * FocusToAdd;
                    hero.HeroDeveloper.AddFocus(skill, focusChange, false);

                    // Apply skill level change
                    int skillChange = netChange * SkillLevelToAdd;
                    if (netChange > 0)
                    {
                        hero.HeroDeveloper.ChangeSkillLevel(skill, skillChange, false);
                    }
                    else
                    {
                        int newLevel = Math.Max(0, beforeSkill + skillChange);
                        hero.HeroDeveloper.SetInitialSkillLevel(skill, newLevel);
                    }
                }
            }

            // Apply attributes (specialization can have multiple)
            if (specializationOption.AttributesToIncrease != null && specializationOption.AttributesToIncrease.Length > 0)
            {
                CharacterCreationOption professionOption = _options?.FirstOrDefault(opt => opt.Id == _selectedProfessionId);
                string professionAttribute = professionOption?.AttributeToIncrease;

                foreach (var attributeRaw in specializationOption.AttributesToIncrease)
                {
                    bool isDecrease = attributeRaw.StartsWith("-");
                    string attributeName = isDecrease ? attributeRaw.Substring(1) : attributeRaw;

                    // Check if this attribute matches the profession attribute
                    bool matchesProfessionAttribute = !string.IsNullOrEmpty(professionAttribute) &&
                                                      attributeName.Equals(professionAttribute, StringComparison.OrdinalIgnoreCase);

                    // If it matches profession attribute, check if it's already been applied by native system
                    if (matchesProfessionAttribute)
                    {
                        var attribute = Attributes.All.FirstOrDefault(x => x.StringId == attributeName.ToLower());
                        if (attribute != null)
                        {
                            int currentValue = hero.GetAttributeValue(attribute);
                            const int BaseAttributeValue = 2; // Default starting value for attributes

                            // If attribute is already above base value, profession bonus was already applied
                            bool professionBonusAlreadyApplied = currentValue > BaseAttributeValue;

                            if (professionBonusAlreadyApplied && !isDecrease)
                            {
                                continue;
                            }
                        }
                    }

                    // Apply the attribute change
                    var attributeToModify = Attributes.All.FirstOrDefault(x => x.StringId == attributeName.ToLower());
                    if (attributeToModify != null)
                    {
                        int changeAmount = isDecrease ? -AttributeLevelToAdd : AttributeLevelToAdd;
                        hero.HeroDeveloper.AddAttribute(attributeToModify, changeAmount, false);
                    }
                    else
                    {
                        TORCommon.Log($"[TORCC]     ERROR: Could not find attribute '{attributeName}'", NLog.LogLevel.Error);
                    }
                }
            }
        }


        private void OnCultureSelected()
        {
            // Set race and default body properties based on selected culture
            string default_elf =
                "<BodyProperties version='4' age='25.84' weight='0.5000' build='0.5000'  key='000BAC088000100DB976648E6774B835537D86629511323BDCB177278A84F667017776140748B49500000000000000000000000000000000000000003EFC5002'/>";
            string default_empire =
                "<BodyProperties version='4' age='25.84' weight='0.5000' build='0.5000'  key='000500000000000D797664884754DCBAA35E866295A0967774414A498C8336860F7776F20BA7B7A500000000000000000000000000000000000000003CFC2002'/>";
            string default_bretonnia =
                "<BodyProperties version='4' age='25.84' weight='0.5000' build='0.5000'  key='001CB80CC000300D7C7664876753888A7577866254C69643C4B647398C95A0370077760307A7497300000000000000000000000000000000000000003AF47002'/>";
            string default_vc =
                "<BodyProperties version='4' age='25.84' weight='0.5000' build='0.5000'  key='0028C80FC000100DBA756445533377873CD1833B3101B44A21C3C5347CA32C260F7776F20BBC35E8000000000000000000000000000000000000000042F41002'/>";
            string default_dwarf =
                "<BodyProperties version='4' age='25' weight='0.4182' build='0.1898' key='0005000F00000280F77664884754DCBAFF9E566095F09F1F74414A49893F81FE0F77760307A7B7A536000000000000000000000000000007000000003CFC0002'/>";
            string default_orc =
                "<BodyProperties version='4' age='25' weight='0.3657' build='0.2978'  key='0005100000CC00005C12429361532471D9656C584FA9A47724B588AAD7B53C5DDACBA6130657877845CCBADBCCBCBABC0000000000000016000000002ECC0000'/>";
            string keyValue;

            var culture = CharacterObject.PlayerCharacter.Culture;

            switch (culture.StringId)
            {
                case TORConstants.Cultures.ASRAI:
                case TORConstants.Cultures.EONIR:
                    keyValue = default_elf;
                    CharacterObject.PlayerCharacter.Race = FaceGen.GetRaceOrDefault("elf");
                    break;
                case TORConstants.Cultures.EMPIRE:
                    keyValue = default_empire;
                    CharacterObject.PlayerCharacter.Race = FaceGen.GetRaceOrDefault("human");
                    break;
                case TORConstants.Cultures.BRETONNIA:
                case TORConstants.Cultures.MOUSILLON:
                    keyValue = default_bretonnia;
                    CharacterObject.PlayerCharacter.Race = FaceGen.GetRaceOrDefault("human");
                    break;
                case TORConstants.Cultures.SYLVANIA:
                    keyValue = default_vc;
                    CharacterObject.PlayerCharacter.Race = FaceGen.GetRaceOrDefault("human");
                    break;
                case TORConstants.Cultures.DAWI:
                    keyValue = default_dwarf;
                    CharacterObject.PlayerCharacter.Race = FaceGen.GetRaceOrDefault("dwarf");
                    break;
                case TORConstants.Cultures.GREENSKIN:
                    keyValue = default_orc;
                    CharacterObject.PlayerCharacter.Race = FaceGen.GetRaceOrDefault("orc");
                    break;
                default:
                    keyValue = default_empire;
                    CharacterObject.PlayerCharacter.Race = FaceGen.GetRaceOrDefault("human");
                    break;
            }

            if (BodyProperties.FromString(keyValue, out BodyProperties properties))
            {
                CharacterObject.PlayerCharacter.UpdatePlayerCharacterBodyProperties(properties, CharacterObject.PlayerCharacter.Race,
                    CharacterObject.PlayerCharacter.IsFemale);
            }

        }
        
    }
}