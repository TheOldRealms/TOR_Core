using System;
using System.Collections.Generic;
using System.Linq;
using SandBox.View.CharacterCreation;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CharacterCreationContent;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ScreenSystem;
using TOR_Core.Utilities;

namespace TOR_Core.CampaignMechanics.CharacterCreation
{
    /// <summary>
    /// MVP: Custom character creation stage view for optional specialization selection
    /// Shows description text like narrative stages, auto-skips if player doesn't need specialization.
    /// </summary>
    [CharacterCreationStageView(typeof(TORSpecializationStage))]
    public class TORSpecializationStageView : CharacterCreationStageViewBase
    {
        private readonly CharacterCreationManager _characterCreationManager;
        private readonly TextObject _affirmativeActionText;
        private readonly TextObject _negativeActionText;

        private GauntletLayer _gauntletLayer;
        private TORSpecializationStageVM _dataSource;
        private GauntletMovieIdentifier _movie;

        private bool _shouldAutoSkip;
        private TORCharacterCreationContentHandler _cachedHandler;

        // Instance flag to track if we've visited this stage before (within this character creation session)
        // Changed from static to prevent cross-session contamination when creating multiple characters
        private bool _wasVisited = false;

        // Track currently applied preview bonuses to properly clear them when switching options
        private SpecializationOption _currentPreviewOption = null;

        public TORSpecializationStageView(CharacterCreationManager characterCreationManager, ControlCharacterCreationStage affirmativeAction,
            TextObject affirmativeActionText, ControlCharacterCreationStage negativeAction, TextObject negativeActionText,
            ControlCharacterCreationStage onRefresh, ControlCharacterCreationStageReturnInt getCurrentStageIndexAction,
            ControlCharacterCreationStageReturnInt getTotalStageCountAction, ControlCharacterCreationStageReturnInt getFurthestIndexAction,
            ControlCharacterCreationStageWithInt goToIndexAction) : base(affirmativeAction, negativeAction, onRefresh, getTotalStageCountAction,
            getCurrentStageIndexAction, getFurthestIndexAction, goToIndexAction)
        {
            _characterCreationManager = characterCreationManager;
            _affirmativeActionText = affirmativeActionText;
            _negativeActionText = negativeActionText;

            // Check if specialization is needed
            var handler = GetHandler();

            // Update last stage index when entering this stage
            /*if (handler != null)
            {
                handler.LastStageIndex = getCurrentStageIndexAction();
            }*/
            if (handler == null)
            {
                _shouldAutoSkip = true;
                return;
            }

            // Check if there are any specialization options available for this profession in XML
            string professionId = handler.GetSelectedProfessionId();
            bool hasOptions = handler.HasSpecializationOptions(professionId);

            if (!hasOptions)
            {
                // Auto-skip if no specialization options available for this profession
                _shouldAutoSkip = true;
                return;
            }

            _shouldAutoSkip = false;

            // Mark that we're now past the narrative stages (Stage 4+)
            // This helps the Harmony patch know to show all bonuses as "previous" on final review stage
            TORCharacterCreationContentHandler.IsPastNarrativeStages = true;

            // Clear any previously applied preview bonuses when entering this stage
            // This ensures clean state if user went back and is returning
            handler.ClearSpecializationBonuses();

            // Initialize UI for professions with specialization options
            InitializeUI(handler);
        }

        private void InitializeUI(TORCharacterCreationContentHandler handler)
        {

            string professionId = handler.GetSelectedProfessionId();
            string title = GameTexts.FindText("str_tor_cc_specialization_title_generic")?.ToString() ?? "Specialization";
            string description = GameTexts.FindText("str_tor_cc_specialization_desc_generic")?.ToString() ?? "Choose your specialization";

            // Set description based on profession type
            if (IsSpellcaster(professionId))
            {
                title = GameTexts.FindText("str_tor_cc_specialization_title_lore")?.ToString() ?? "Choose Your Lore";
                description = GameTexts.FindText("str_tor_cc_specialization_desc_lore")?.ToString() ?? "As a spellcaster, you must choose a lore of magic to specialize in. This will determine which spells you can learn.";
            }
            else if (professionId == "option_3_vc_vampire" || professionId == "option_3_mousillon_vampire")
            {
                title = GameTexts.FindText("str_tor_cc_specialization_title_bloodline")?.ToString() ?? "Choose Your Bloodline";
                description = GameTexts.FindText("str_tor_cc_specialization_desc_bloodline")?.ToString() ?? "As a vampire, you must choose your bloodline. This will determine your abilities and strengths.";
            }
            else if (professionId == "option_3_empire_priest_acolyte")
            {
                title = GameTexts.FindText("str_tor_cc_specialization_title_god")?.ToString() ?? "Choose Your God";
                description = GameTexts.FindText("str_tor_cc_specialization_desc_god")?.ToString() ?? "As a priest, you must choose which god you serve. This will determine your divine powers.";
            }
            else if (professionId == "option_3_empire_knight")
            {
                title = GameTexts.FindText("str_tor_cc_specialization_title_order")?.ToString() ?? "Choose Your Order";
                description = GameTexts.FindText("str_tor_cc_specialization_desc_order")?.ToString() ?? "As a knight, you must choose which knightly order you belong to. This will determine your martial traditions and bonuses.";
            }
            
            _gauntletLayer = new GauntletLayer("GauntletLayer",1, true);
            _gauntletLayer.InputRestrictions.SetInputRestrictions(true, InputUsageMask.All);
            _gauntletLayer.IsFocusLayer = true;
            _gauntletLayer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("GenericPanelGameKeyCategory"));
            ScreenManager.TrySetFocus(_gauntletLayer);


            // Create custom ViewModel with equipment preview callback
            _dataSource = new TORSpecializationStageVM(_characterCreationManager, title, description,
                new Action(NextStage), _affirmativeActionText,
                new Action(PreviousStage), _negativeActionText,
                OnSpecializationOptionSelected);

            // Populate options based on profession type
            PopulateOptions(handler, professionId);
            
            _movie = _gauntletLayer.LoadMovie("TORSpecializationStage", _dataSource);
        }

        private void OnSpecializationOptionSelected(SpecializationOptionVM selectedOption)
        {
            if (selectedOption?.Data == null)
            {
                return;
            }

            Equipment equipment = GetEquipmentForOption(selectedOption.Data);
            _dataSource.UpdateCharacterEquipment(equipment);

            // Apply race change immediately if this option has a race
            if (selectedOption.Data is SpecializationOption option && !string.IsNullOrEmpty(option.RaceId))
            {
                ApplyRaceChangeImmediate(option.RaceId);
            }

            // Apply preview bonuses so they show in the gained properties panel
            ApplyPreviewBonuses(selectedOption.Data as SpecializationOption);
        }

        private void PopulateOptions(TORCharacterCreationContentHandler handler, string professionId)
        {

            // Get specialization options from XML
            var specializationOptions = handler.GetSpecializationOptions(professionId);

            if (specializationOptions == null || specializationOptions.Count == 0)
            {
                return;
            }

            foreach (var option in specializationOptions)
            {
                // Get the display name (handles translation keys)
                string displayName = new TextObject(option.Name).ToString();
                string description = new TextObject(option.Description).ToString();
                string positiveEffect = new TextObject(option.PositiveEffect).ToString();
                string negativeEffect = new TextObject(option.NegativeEffect).ToString();
                string iconSprite = string.IsNullOrEmpty(option.IconSprite) ? "traits_magic_icon" : option.IconSprite;

                // Pass icon sprite along with other data
                _dataSource.AddOption(displayName, description, option, iconSprite, positiveEffect, negativeEffect);
            }

            // Restore previously selected option if returning to this stage
            RestorePreviousSelection(handler);
        }
        
        private void RestorePreviousSelection(TORCharacterCreationContentHandler handler)
        {
            string storedId = handler.GetSelectedSpecializationOptionId();
            if (string.IsNullOrEmpty(storedId))
            {
                return;
            }

            // Find and select the matching option by ID
            foreach (var option in _dataSource.Options)
            {
                if (option.Data is SpecializationOption specOption && specOption.Id == storedId)
                {
                    option.ExecuteSelect();
                    break;
                }
            }
        }

        private Equipment GetEquipmentForOption(object optionData)
        {
            if (optionData is not SpecializationOption)
            {
                throw new TORCCInvalidOptionTypeException(typeof(SpecializationOption), optionData?.GetType());
            }

            SpecializationOption option = (SpecializationOption)optionData;
            // optionData is now a SpecializationOption - get equipment from its EquipmentSetId
            if ( string.IsNullOrEmpty(option.EquipmentSetId))
            {
                return CharacterObject.PlayerCharacter.Equipment.Clone();
            }

            MBEquipmentRoster roster = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(option.EquipmentSetId);
            if (roster != null && roster.AllEquipments.Count > 0)
            {
                return roster.AllEquipments[0].Clone();
            }
            
            return CharacterObject.PlayerCharacter.Equipment.Clone();
        }

        private TORCharacterCreationContentHandler GetHandler()
        {
            // Return cached handler if already retrieved
            if (_cachedHandler != null)
            {
                return _cachedHandler;
            }

            // Access handler from manager using reflection (only done once)
            try
            {
                var handlersField = typeof(CharacterCreationManager).GetField("_handlers",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                if (handlersField != null)
                {
                    if (handlersField.GetValue(_characterCreationManager) is SortedList<int, ICharacterCreationContentHandler> handlers)
                    {
                        foreach (var handler in handlers.Values)
                        {
                            if (handler is TORCharacterCreationContentHandler torHandler)
                            {
                                _cachedHandler = torHandler;
                                return _cachedHandler;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new TORCCReflectionException("_handlers", ex);
            }

            return null;
        }

        public override void Tick(float dt)
        {
            base.Tick(dt);

            // auto skip back to previous menu
            if (_shouldAutoSkip)
            {
                _shouldAutoSkip = false;

                // Determine direction based on last stage index
                var handler = GetHandler();
                int currentIndex = 3;
                int lastIndex = handler?.LastStageIndex ?? -1;
                bool comingFromLaterStage = lastIndex>= currentIndex;

                TORCommon.Log($"[TORCC] Auto-skip detected: currentIndex={currentIndex}, lastIndex={lastIndex}, comingFromLaterStage={comingFromLaterStage}", NLog.LogLevel.Info);

                if (comingFromLaterStage)
                {
                    // Coming back from banner editor - set flag to jump to Stage 3
                    HarmonyPatches.CharacterCreationPatches.ShouldJumpToProfessionStage = true;
                    TORCommon.Log($"[TORCC] Auto-skip backward: Setting flag to jump to Stage 3 (Profession)", NLog.LogLevel.Info);
                    PreviousStage();
                }
                else
                {
                    // Auto-skip forward - just continue to next stage
                    TORCommon.Log($"[TORCC] Auto-skip forward: Skipping specialization stage", NLog.LogLevel.Info);
                    NextStage();
                }

                return;
            }

            // Handle hotkey input
            HandleLayerInput();
        }

        private void HandleLayerInput()
        {
            if (_gauntletLayer == null || _dataSource == null) return;

            if (_gauntletLayer.Input.IsHotKeyReleased("Exit"))
            {
                PreviousStage();
            }
            else if (_gauntletLayer.Input.IsHotKeyReleased("Confirm") && _dataSource.CanAdvance)
            {
                NextStage();
            }
        }

        public override void NextStage()
        {
            // Clear preview bonuses before applying final bonuses
            ClearCurrentPreviewBonuses();

            // Store the selected specialization
            StoreSpecialization();

            // Apply bonuses immediately when clicking Next
            // This will apply final bonuses with proper tracking for removal later
            var handler = GetHandler();
            if (handler != null)
            {
                TORCommon.Log($"[TORCC] TORSpecializationStageView.NextStage: Calling ApplySpecializationBonuses", NLog.LogLevel.Info);
                handler.ApplySpecializationBonuses();
            }

            // Reset preview tracking since we've applied final bonuses
            _currentPreviewOption = null;

            // Mark this stage as visited
            _wasVisited = true;

            _affirmativeAction();
        }

        /// <summary>
        /// Store the selected specialization in the handler to be applied at character creation finalization.
        /// This prevents issues where going back and changing profession would keep the old specialization.
        /// </summary>
        private void StoreSpecialization()
        {
            if (_dataSource == null)
            {
                TORCommon.Log($"[TORCC] StoreSpecialization: _dataSource is null", NLog.LogLevel.Warn);
                return;
            }

            var selectedData = _dataSource.GetSelectedData();
            if (selectedData == null)
            {
                TORCommon.Log($"[TORCC] StoreSpecialization: No data selected", NLog.LogLevel.Warn);
                return;
            }

            var handler = GetHandler();
            if (handler == null)
            {
                TORCommon.Log($"[TORCC] StoreSpecialization: Handler is null", NLog.LogLevel.Warn);
                return;
            }

            if (selectedData is SpecializationOption option)
            {
                handler.SetSelectedSpecializationOptionId(option.Id);

                // NOTE: Skill/attribute bonuses are applied in NextStage() when user clicks Next
                // Equipment is applied immediately so it shows in banner editor

                // Apply equipment immediately so it shows in banner editor
                if (!string.IsNullOrEmpty(option.EquipmentSetId))
                {
                    ApplyEquipmentFromRoster(option.EquipmentSetId);
                }
            }
        }

        public override void PreviousStage()
        {
            // Reset the "past narrative stages" flag when going back
            // This allows stages 1-3 to show light green highlighting normally
            TORCharacterCreationContentHandler.IsPastNarrativeStages = false;

            // Clear preview bonuses before leaving
            ClearCurrentPreviewBonuses();

            // Clear stored selections and remove applied bonuses when going back (user might change profession)
            // ClearStoredSpecializations() will call ClearSpecializationBonuses() to revert stat changes
            var handler = GetHandler();
            if (handler != null)
            {
                handler.ClearStoredSpecializations();
            }

            // Reset preview tracking since we're clearing everything
            _currentPreviewOption = null;

            // Reset the visited flag so next time we come forward it's treated as a fresh visit
            _wasVisited = false;

            // Set flag to jump directly to Stage 3 (Profession) instead of Stage 1
            HarmonyPatches.CharacterCreationPatches.ShouldJumpToProfessionStage = true;
            TORCommon.Log($"[TORCC] PreviousStage: Setting flag to jump to Stage 3 (Profession)", NLog.LogLevel.Info);

            _negativeAction();
        }

        public override IEnumerable<ScreenLayer> GetLayers()
        {
            // Return only GauntletLayer (CharacterTableauWidget handles character rendering)
            if (_gauntletLayer != null)
            {
                return new List<ScreenLayer> { _gauntletLayer };
            }

            return new List<ScreenLayer>();
        }

        public override int GetVirtualStageCount() => 1;

        public override void LoadEscapeMenuMovie()
        {
        }

        public override void ReleaseEscapeMenuMovie()
        {
            
        }

        protected override void OnFinalize()
        {
            base.OnFinalize();
            
            _dataSource?.OnFinalize();
            _dataSource = null;

            // Clean up GauntletLayer
            // Note: CharacterCreationManager handles layer lifecycle via GetLayers() pattern
            _gauntletLayer = null;
            
        }

        /// <summary>
        /// Load equipment from roster and apply to player character
        /// Only copies equipment items, preserves character's face and body customization
        /// </summary>
        private void ApplyEquipmentFromRoster(string rosterId)
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
            }
        }

        /// <summary>
        /// Refresh the character preview in the ViewModel to reflect race/equipment changes
        /// </summary>
        private void RefreshCharacterPreview()
        {
            if (_dataSource?.CurrentCharacter != null)
            {
                var playerCharacter = CharacterObject.PlayerCharacter;
                _dataSource.CurrentCharacter.FillFrom(playerCharacter);

                // Force update of body properties to reflect race change
                var bodyProperties = playerCharacter.GetBodyProperties(playerCharacter.Equipment, -1);
                _dataSource.CurrentCharacter.OnPropertyChangedWithValue(bodyProperties.ToString(), "BodyProperties");

            }
        }

        /// <summary>
        /// Apply race change immediately (for specializations like Necrarch that change appearance)
        /// </summary>
        private void ApplyRaceChangeImmediate(string raceIdString)
        {
            var handler = GetHandler();
            if (handler == null)
            {
                return;
            }

            // Get the race int from the string ID
            var newRace = FaceGen.GetRaceOrDefault(raceIdString);
            var playerCharacter = CharacterObject.PlayerCharacter;

            playerCharacter.Race = newRace;
            var equipment = playerCharacter.Equipment;
            var properties = playerCharacter.GetBodyProperties(equipment);
            playerCharacter.UpdatePlayerCharacterBodyProperties(properties, newRace, false);

            // Refresh the character preview to show the race change
            RefreshCharacterPreview();
        }

        /// <summary>
        /// Clear currently applied preview bonuses without applying new ones
        /// </summary>
        private void ClearCurrentPreviewBonuses()
        {
            if (_currentPreviewOption == null) return;

            var hero = Hero.MainHero;

            // Remove skill focus points
            if (_currentPreviewOption.SkillsToIncrease != null)
            {
                foreach (var skillIdRaw in _currentPreviewOption.SkillsToIncrease)
                {
                    bool isDecrease = skillIdRaw.StartsWith("-");
                    string skillId = isDecrease ? skillIdRaw.Substring(1) : skillIdRaw;
                    int amount = isDecrease ? 1 : -1; // Reverse the operation

                    var skill = Game.Current.ObjectManager.GetObject<SkillObject>(skillId);
                    if (skill != null)
                    {
                        hero.HeroDeveloper.AddFocus(skill, amount, false);
                    }
                }
            }

            // Remove attribute points (can have multiple)
            if (_currentPreviewOption.AttributesToIncrease != null && _currentPreviewOption.AttributesToIncrease.Length > 0)
            {
                foreach (var attributeRaw in _currentPreviewOption.AttributesToIncrease)
                {
                    bool isDecrease = attributeRaw.StartsWith("-");
                    string attributeId = isDecrease ? attributeRaw.Substring(1) : attributeRaw;
                    int amount = isDecrease ? 1 : -1; // Reverse the operation

                    var attribute = Game.Current.ObjectManager.GetObject<CharacterAttribute>(attributeId);
                    if (attribute != null)
                    {
                        hero.HeroDeveloper.AddAttribute(attribute, amount, false);
                    }
                }
            }

            _currentPreviewOption = null;

            // Clear the display (show empty list)
            _dataSource.GainedPropertiesController?.UpdateFromOption(null);
        }

        /// <summary>
        /// Apply preview bonuses from specialization option to the hero so they show in gained properties panel
        /// This is a temporary preview - actual application happens in NextStage()
        /// </summary>
        private void ApplyPreviewBonuses(SpecializationOption option)
        {
            if (option == null) return;

            // Clear previous preview bonuses if any
            ClearCurrentPreviewBonuses();

            var hero = Hero.MainHero;

            // Apply skill focus points
            if (option.SkillsToIncrease != null)
            {
                foreach (var skillIdRaw in option.SkillsToIncrease)
                {
                    bool isDecrease = skillIdRaw.StartsWith("-");
                    string skillId = isDecrease ? skillIdRaw.Substring(1) : skillIdRaw;
                    int amount = isDecrease ? -1 : 1;

                    var skill = Game.Current.ObjectManager.GetObject<SkillObject>(skillId);
                    if (skill != null)
                    {
                        hero.HeroDeveloper.AddFocus(skill, amount, false);
                    }
                }
            }

            // Apply attribute points (can have multiple)
            if (option.AttributesToIncrease != null && option.AttributesToIncrease.Length > 0)
            {
                foreach (var attributeRaw in option.AttributesToIncrease)
                {
                    bool isDecrease = attributeRaw.StartsWith("-");
                    string attributeId = isDecrease ? attributeRaw.Substring(1) : attributeRaw;
                    int amount = isDecrease ? -1 : 1;

                    var attribute = Game.Current.ObjectManager.GetObject<CharacterAttribute>(attributeId);
                    if (attribute != null)
                    {
                        hero.HeroDeveloper.AddAttribute(attribute, amount, false);
                    }
                }
            }

            // Store current option as the active preview
            _currentPreviewOption = option;

            // Update the custom gained properties display (happens automatically through ViewModel)
            // The display is already updated by OnOptionSelected in the ViewModel
        }

        /// <summary>
        /// Check if profession is a spellcaster (multiple professions share this trait)
        /// </summary>
        private bool IsSpellcaster(string professionId)
        {
            return professionId == "option_3_empire_magister_apprentice";
        }

    }
}