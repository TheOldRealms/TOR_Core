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
        private string _selectedStage2OptionId = ""; // Track stage 2 selection (Wood Elf gods, Dwarf grudges)
        private string _selectedProfessionId = ""; // Track stage 3 selection for stage 4 conditions
        private string _selectedLoreId = null; // Store selected lore for spellcasters (applied at finalization)
        private string _selectedCareerId = null; // Store selected career for vampires/priests (applied at finalization)
        private NarrativeMenuCharacter _narrativePlayerCharacter = null; // Reference to narrative menu character for updating face
        private const int FocusToAdd = 1;
        private const int SkillLevelToAdd = 10;
        private const int AttributeLevelToAdd = 1;

        /// <summary>
        /// Career-specific spawn location overrides. If a career is not in this dictionary,
        /// the culture-based default spawn location will be used instead.
        /// Format: CareerStringId -> CampaignVec2(Vec2(X, Y), true)
        /// </summary>
        private static readonly Dictionary<string, CampaignVec2> CareerSpawnOverrides = new()
        {
            // Empire
            ["WarriorPriest"] = new CampaignVec2(new Vec2(1283.261f, 1067.676f), true), // Altdorf gate - Sigmar's holy city
            ["WarriorPriestUlric"] = new CampaignVec2(new Vec2(1346.493f, 1244.102f), true), // Middenheim gate - Ulric's holy city
            // Knight Orders
            ["KnightBlazingSun"] = new CampaignVec2(new Vec2(1428.939f, 1114.684f), true), // Talabheim - Order of the Blazing Sun (Myrmidia)
            ["KnightPanthers"] = new CampaignVec2(new Vec2(1232.193f, 1105.359f), true), // Carroburg - Knight Panthers (Secular)
            ["KnightWhiteWolf"] = new CampaignVec2(new Vec2(1346.493f, 1244.102f), true), // Middenheim - Knights of the White Wolf (Ulric)
            ["KnightGriphon"] = new CampaignVec2(new Vec2(1283.261f, 1067.676f), true), // Altdorf - Order of the Griphon (Sigmar)
            ["Reiksguard"] = new CampaignVec2(new Vec2(1306.128f, 1044.178f), true), // Castle Reiksguard - Reiksguard (Secular)
            ["KnightOldWorld"] = new CampaignVec2(new Vec2(1434.869f, 917.6942f), true),
            ["ImperialMagister"] = new CampaignVec2(new Vec2(1278.084f, 1056.505f), true),
            ["WitchHunter"] = new CampaignVec2(new Vec2(1560.23f, 974.5349f), true),

            // Vampire Counts
            ["Necromancer"] = new CampaignVec2(new Vec2(1666.918f, 1019.001f), true),
            ["BloodKnight"] = new CampaignVec2(new Vec2(1277.776f, 942.5178f), true),
            ["Necrarch"] = new CampaignVec2(new Vec2(1565.885f, 1095.13f), true),
            ["MinorVampire"] = new CampaignVec2(new Vec2(1594.974f, 988.7784f), true),

            // Wood Elves
            ["Spellsinger"] = new CampaignVec2(new Vec2(1233.78f, 781.862f), true),
            ["Waywatcher"] = new CampaignVec2(new Vec2(1243.44f, 910.1643f), true),
            ["Warden"] = new CampaignVec2(new Vec2(1187.113f, 864.41f), true),

            // Eonir
            ["GreyLord"] = new CampaignVec2(new Vec2(1216.198f, 1345.101f), true),

            // Dwarfs
            ["Slayer"] = new CampaignVec2(new Vec2(1787.716f, 1021.437f), true),
            ["Ironbreaker"] = new CampaignVec2(new Vec2(1306.575f, 838.3152f), true),
            ["Runelord"] = new CampaignVec2(new Vec2(1222.444f, 692.9744f), true),

            // Bretonnia
            ["GrailDamsel"] = new CampaignVec2(new Vec2(941.8889f, 1249.213f), true),
            ["GrailKnight"] = new CampaignVec2(new Vec2(941.8889f, 1249.213f), true),

            // Mousillon
            ["BlackGrailKnight"] = new CampaignVec2(new Vec2(958.4354f, 1044.788f), true),
        };

        /// <summary>
        /// Culture-specific fallback spawn locations for Mercenary and other careers without specific overrides.
        /// These are used when a career doesn't have a specific spawn override.
        /// </summary>
        private static readonly Dictionary<string, CampaignVec2> CultureMercenarySpawns = new()
        {
            [TORConstants.Cultures.EMPIRE] = new CampaignVec2(new Vec2(1135.848f, 1176.32f), true),
            [TORConstants.Cultures.SYLVANIA] = new CampaignVec2(new Vec2(1655.133f, 1059.423f), true),
            [TORConstants.Cultures.MOUSILLON] = new CampaignVec2(new Vec2(918.8679f, 1025.561f), true),
            [TORConstants.Cultures.ASRAI] = new CampaignVec2(new Vec2(1164.307f, 822.5884f), true),
            [TORConstants.Cultures.EONIR] = new CampaignVec2(new Vec2(1295.974f, 1336.11f), true),
            [TORConstants.Cultures.DAWI] = new CampaignVec2(new Vec2(1485.11f, 809.4648f), true),
            [TORConstants.Cultures.BRETONNIA] = new CampaignVec2(new Vec2(1070.923f, 1116.021f), true),
        };

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
            _narrativePlayerCharacter = new NarrativeMenuCharacter(
                "player_character",
                CharacterObject.PlayerCharacter.GetBodyProperties(CharacterObject.PlayerCharacter.Equipment, -1),
                CharacterObject.PlayerCharacter.Race,
                CharacterObject.PlayerCharacter.IsFemale);

            List<NarrativeMenuCharacter> playerCharacterList = new List<NarrativeMenuCharacter>
            {
                _narrativePlayerCharacter
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

            // Get the customized body properties from face editor
            var bodyProps = CharacterObject.PlayerCharacter.GetBodyProperties(CharacterObject.PlayerCharacter.Equipment, -1);

            // CRITICAL: Update the NarrativeMenuCharacter so Origin/Growth/Profession stages show the customized face
            // This also automatically updates CharacterObject.PlayerCharacter which the specialization stage uses
            if (_narrativePlayerCharacter != null)
            {
                _narrativePlayerCharacter.UpdateBodyProperties(bodyProps, CharacterObject.PlayerCharacter.Race, CharacterObject.PlayerCharacter.IsFemale);
                TORCommon.Log($"[OnFinalizeFaceCreation] Updated narrative menu character with customized face", NLog.LogLevel.Info);
            }

            TORCommon.Log($"[OnFinalizeFaceCreation] Finalized face customization: Race={_originalRace}, IsFemale={_isFemale}", NLog.LogLevel.Info);
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

        public bool IsKnight(string professionId = null)
        {
            string id = professionId ?? _selectedProfessionId;
            return id == "option_3_empire_knight";
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


        /// <summary>
        /// Clear profession-specific bonuses that were applied during character creation.
        /// Called when user goes back from specialization stage to change profession.
        /// </summary>
        public void ClearProfessionBonuses()
        {
            var hero = Hero.MainHero;
            var info = hero.GetExtendedInfo();

            TORCommon.Log("[TorCharacterCreationContentHandler] Clearing profession-specific bonuses", NLog.LogLevel.Info);

            // Remove all lores - they'll be re-added if the profession still needs them
            var allLores = LoreObject.GetAll();
            foreach (var lore in allLores)
            {
                if (info.HasKnownLore(lore.ID))
                {
                    info.RemoveKnownLore(lore.ID);
                    TORCommon.Log($"[TorCharacterCreationContentHandler] Removed lore: {lore.ID}", NLog.LogLevel.Info);
                }
            }

            // Remove careers - they'll be re-added when profession is finalized again
            var currentCareer = hero.GetCareer();
            if (currentCareer != null)
            {
                info.CareerID = null;
                TORCommon.Log($"[TorCharacterCreationContentHandler] Removed career: {currentCareer.StringId}", NLog.LogLevel.Info);
            }

            // Remove profession-specific attributes
            string[] professionAttributes =
            {
                "SpellCaster", "Necromancer", "Vampire", "Priest",
                "PriestSigmar", "PriestUlric", "PriestLady", "RuneCraft"
            };
            foreach (var attribute in professionAttributes)
            {
                hero.RemoveAttribute(attribute);
                TORCommon.Log($"[TorCharacterCreationContentHandler] Removed attribute: {attribute}", NLog.LogLevel.Info);
            }

            // Remove profession-specific abilities
            string[] professionAbilities =
            {
                "Dart", "NagashGaze", "SummerHeat", "AmberSpear",
                "BoltOfAqshy", "AuraOfTheLady", "SummonSkeleton"
            };
            foreach (var ability in professionAbilities)
            {
                info.RemoveAbility(ability);
                TORCommon.Log($"[TorCharacterCreationContentHandler] Removed ability: {ability}", NLog.LogLevel.Info);
            }

            // Reset spell casting level if it was set
            if (info.SpellCastingLevel != SpellCastingLevel.None)
            {
                hero.SetSpellCastingLevel(SpellCastingLevel.None);
                TORCommon.Log("[TorCharacterCreationContentHandler] Reset spell casting level to None", NLog.LogLevel.Info);
            }

            TORCommon.Log("[TorCharacterCreationContentHandler] Profession bonuses cleared", NLog.LogLevel.Info);
        }

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

            // Track stage 2 selection for deferred application
            if (selectedOption != null && selectedOption.StageNumber == 2)
            {
                _selectedStage2OptionId = optionId;
                TORCommon.Log($"[OnOptionSelected] Stage 2 option selected: {_selectedStage2OptionId}", NLog.LogLevel.Info);
            }

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

        private void 
            OnOptionFinalize(CharacterCreationManager manager, string id)
        {
            // NOTE: Specialization selection now handled by TORSpecializationStageView
            // All profession bonuses are now applied at character creation finalization via ApplyProfessionBonuses()
        }
        
        private void OnCharacterCreationFinalized()
        {
            // Apply bonuses in order: Stage 2 (gods/grudges) -> Stage 3 (professions) -> Stage 4 (specializations)
            ApplyStage2Bonuses();
            ApplyProfessionBonuses();
            ApplyStoredSpecializations();

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
        /// Get the spawn location for the player character based on their career and culture.
        /// Priority: Career-specific override > Culture-specific mercenary spawn > Culture default
        /// </summary>
        private CampaignVec2 GetSpawnLocation(CultureObject culture)
        {
            var career = Hero.MainHero.GetCareer();

            // Priority 1: Check for career-specific spawn override
            if (career != null && CareerSpawnOverrides.TryGetValue(career.StringId, out var careerSpawn))
            {
                TORCommon.Log($"[GetSpawnLocation] Using career-specific spawn for {career.StringId}: X={careerSpawn.X}, Y={careerSpawn.Y}", NLog.LogLevel.Info);
                return careerSpawn;
            }

            // Priority 2: Check for culture-specific mercenary/default spawn
            if (CultureMercenarySpawns.TryGetValue(culture.StringId, out var mercenarySpawn))
            {
                TORCommon.Log($"[GetSpawnLocation] Using culture mercenary spawn for {culture.StringId}: X={mercenarySpawn.X}, Y={mercenarySpawn.Y}", NLog.LogLevel.Info);
                return mercenarySpawn;
            }

            // Priority 3: Read spawn position from culture XML (start_point_position_x/y attributes)
            var cultureSpawn = culture.StartingPoint;
            TORCommon.Log($"[GetSpawnLocation] Using culture XML spawn for {culture.StringId}: X={cultureSpawn.X}, Y={cultureSpawn.Y}", NLog.LogLevel.Info);
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
                TORCommon.Log("[TorCharacterCreationContentHandler] No Stage 2 option selected, skipping Stage 2 bonuses", NLog.LogLevel.Info);
                return;
            }

            TORCommon.Log($"[TorCharacterCreationContentHandler] Applying Stage 2 bonuses for: {stage2OptionId}", NLog.LogLevel.Info);

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
                    TORCommon.Log($"[TorCharacterCreationContentHandler] Added Wood Elf symbol {symbol} and religion {religion.StringId}", NLog.LogLevel.Info);
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
                    TORCommon.Log($"[TorCharacterCreationContentHandler] Added Dwarf grudge: {grudge}", NLog.LogLevel.Info);
                }
            }

            TORCommon.Log($"[TorCharacterCreationContentHandler] Stage 2 bonuses applied for: {stage2OptionId}", NLog.LogLevel.Info);
        }

        /// <summary>
        /// Apply profession-specific bonuses based on the selected profession at character creation finalization.
        /// This is called BEFORE applying specializations, so base profession bonuses are set first.
        /// </summary>
        private void ApplyProfessionBonuses()
        {
            var hero = Hero.MainHero;
            string professionId = _selectedProfessionId;

            if (string.IsNullOrEmpty(professionId))
            {
                TORCommon.Log("[TorCharacterCreationContentHandler] No profession selected, skipping profession bonuses", NLog.LogLevel.Warn);
                return;
            }

            TORCommon.Log($"[TorCharacterCreationContentHandler] Applying profession bonuses for: {professionId}", NLog.LogLevel.Info);

            // Everyone gets these base attributes
            hero.AddAttribute("AbilityUser");
            hero.AddAttribute("CanPlaceArtillery");

            // Apply profession-specific bonuses based on selected profession
            switch (professionId)
            {
                case "option_3_empire_magister_apprentice":
                    hero.AddAttribute("SpellCaster");
                    hero.AddAbility("Dart");
                    hero.AddKnownLore("MinorMagic");
                    hero.SetSpellCastingLevel(SpellCastingLevel.Entry);
                    hero.HeroDeveloper.SetInitialSkillLevel(TORSkills.SpellCraft, 25);
                    hero.HeroDeveloper.AddPerk(TORPerks.SpellCraft.EntrySpells);
                    hero.AddCareer(TORCareers.ImperialMagister);
                    break;

                case "option_3_bretonnia_damsel":
                    hero.AddAttribute("SpellCaster");
                    hero.AddAttribute("PriestLady");
                    hero.AddAbility("Dart");
                    hero.AddAbility("AuraOfTheLady");
                    hero.AddKnownLore("MinorMagic");
                    hero.SetSpellCastingLevel(SpellCastingLevel.Entry);
                    hero.HeroDeveloper.SetInitialSkillLevel(TORSkills.SpellCraft, 25);
                    hero.HeroDeveloper.SetInitialSkillLevel(TORSkills.Faith, 25);
                    hero.HeroDeveloper.AddPerk(TORPerks.SpellCraft.EntrySpells);
                    hero.AddCareer(TORCareers.GrailDamsel);
                    // Add Realm Knight companion
                    var knight = MBObjectManager.Instance.GetObject<CharacterObject>("tor_br_realm_knight");
                    if (knight != null) hero.PartyBelongedTo.Party.AddMember(knight, 1, 0);
                    break;

                case "option_3_we_spellsinger":
                    hero.AddAttribute("SpellCaster");
                    hero.AddKnownLore("MinorMagic");
                    hero.AddKnownLore("LoreOfLife");
                    hero.AddKnownLore("LoreOfBeasts");
                    hero.AddAbility("SummerHeat");
                    hero.AddAbility("AmberSpear");
                    hero.SetSpellCastingLevel(SpellCastingLevel.Entry);
                    hero.HeroDeveloper.SetInitialSkillLevel(TORSkills.SpellCraft, 25);
                    hero.HeroDeveloper.AddPerk(TORPerks.SpellCraft.EntrySpells);
                    hero.AddCareer(TORCareers.Spellsinger);
                    break;

                case "option_3_eo_greylord_apprentice":
                    hero.AddAttribute("SpellCaster");
                    hero.AddKnownLore("HighMagic");
                    hero.AddKnownLore("LoreOfFire");
                    hero.AddAbility("BoltOfAqshy");
                    hero.SetSpellCastingLevel(SpellCastingLevel.Entry);
                    hero.HeroDeveloper.SetInitialSkillLevel(TORSkills.SpellCraft, 25);
                    hero.HeroDeveloper.AddPerk(TORPerks.SpellCraft.EntrySpells);
                    hero.AddCareer(TORCareers.GreyLord);
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
                    hero.HeroDeveloper.SetInitialSkillLevel(TORSkills.SpellCraft, 25);
                    hero.HeroDeveloper.AddPerk(TORPerks.SpellCraft.EntrySpells);
                    hero.AddCareer(TORCareers.Necromancer);
                    hero.AddReligiousInfluence(ReligionObject.All.FirstOrDefault(x => x.StringId == "cult_of_nagash"), 25);
                    break;

                case "option_3_dw_shield_breaker":
                    hero.AddCareer(TORCareers.Ironbreaker);
                    break;

                case "option_3_dw_slayer":
                    hero.AddCareer(TORCareers.Slayer);
                    break;

                case "option_3_dw_rune_smith":
                    hero.AddCareer(TORCareers.Runelord);
                    hero.AddAttribute("RuneCraft");
                    break;

                case "option_3_empire_witch_hunter":
                    hero.AddCareer(TORCareers.WitchHunter);
                    break;

                case "option_3_bretonnia_knight_errant":
                    hero.AddCareer(TORCareers.GrailKnight);
                    break;

                case "option_3_mousillon_knight_errant":
                    hero.AddCareer(TORCareers.BlackGrailKnight);
                    break;

                case "option_3_we_waywatcher":
                case "option_3_eo_ghost_strider":
                    hero.AddCareer(TORCareers.Waywatcher);
                    break;

                case "option_3_gs_path_of_boss":
                case "option_3_gs_path_of_bully":
                case "option_3_gs_path_of_boar_boys":
                case "option_3_gs_path_of_savage_boys":
                case "option_3_gs_path_of_shaman":
                    hero.AddCareer(TORCareers.OrcBoss);
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

            TORCommon.Log($"[TorCharacterCreationContentHandler] Profession bonuses applied for: {professionId}", NLog.LogLevel.Info);
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

                    // Apply career-specific equipment
                    ApplyCareerEquipment(_selectedCareerId);
                }
                else
                {
                    TORCommon.Log($"[TorCharacterCreationContentHandler] Career not found: {_selectedCareerId}", NLog.LogLevel.Error);
                }
            }

            // Apply lore-specific equipment for spellcasters
            if (!string.IsNullOrEmpty(_selectedLoreId))
            {
                ApplyLoreEquipment(_selectedLoreId);
            }
        }

        /// <summary>
        /// Apply equipment based on the selected career
        /// </summary>
        private void ApplyCareerEquipment(string careerId)
        {
            if (string.IsNullOrEmpty(careerId)) return;

            string equipmentRosterId = careerId switch
            {
                "MinorVampire" => "tor_vampire_noble_equipment",
                "BloodKnight" => "tor_blood_dragon_equipment",
                "Necrarch" => "tor_necrarch_equipment",
                "WarriorPriest" => "tor_sigmar_priest_equipment",
                "WarriorPriestUlric" => "tor_ulric_priest_equipment",
                // Knight orders
                "KnightBlazingSun" => "tor_empire_knight_equipment",
                "KnightPanthers" => "tor_empire_knight_equipment",
                "KnightWhiteWolf" => "tor_empire_knight_equipment",
                "KnightGriphon" => "tor_empire_knight_equipment",
                "Reiksguard" => "tor_empire_knight_equipment",
                _ => null
            };

            if (!string.IsNullOrEmpty(equipmentRosterId))
            {
                ApplyEquipmentFromRoster(equipmentRosterId, "career");
            }
        }

        /// <summary>
        /// Apply equipment based on the selected lore
        /// </summary>
        private void ApplyLoreEquipment(string loreId)
        {
            if (string.IsNullOrEmpty(loreId)) return;

            // All magisters use the same equipment for now
            ApplyEquipmentFromRoster("tor_magister_equipment", "lore");
        }

        /// <summary>
        /// Load equipment from a roster and apply it to the player character
        /// </summary>
        private void ApplyEquipmentFromRoster(string rosterId, string equipmentType)
        {
            try
            {
                var roster = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(rosterId);
                if (roster != null && roster.AllEquipments.Count > 0)
                {
                    var sourceEquipment = roster.AllEquipments[0];
                    var playerEquipment = CharacterObject.PlayerCharacter.Equipment;

                    // Copy only equipment items slot by slot, preserving character customization
                    for (EquipmentIndex i = EquipmentIndex.WeaponItemBeginSlot; i < EquipmentIndex.NumEquipmentSetSlots; i++)
                    {
                        var equipmentElement = sourceEquipment.GetEquipmentFromSlot(i);
                        if (!equipmentElement.IsEmpty)
                        {
                            playerEquipment.AddEquipmentToSlotWithoutAgent(i, equipmentElement);
                        }
                    }

                    TORCommon.Log($"[TorCharacterCreationContentHandler] Applied {equipmentType} equipment from roster '{rosterId}' (face/body preserved)", NLog.LogLevel.Info);
                }
                else
                {
                    TORCommon.Log($"[TorCharacterCreationContentHandler] Equipment roster '{rosterId}' not found or empty", NLog.LogLevel.Warn);
                }
            }
            catch (Exception ex)
            {
                TORCommon.Log($"[TorCharacterCreationContentHandler] Error loading equipment roster '{rosterId}': {ex.Message}", NLog.LogLevel.Error);
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
