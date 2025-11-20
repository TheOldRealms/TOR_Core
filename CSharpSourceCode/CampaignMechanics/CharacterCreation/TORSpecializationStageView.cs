using System;
using System.Collections.Generic;
using System.Linq;
using SandBox.View.CharacterCreation;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CharacterCreationContent;
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

        // Static flag to track if we've visited this stage before (persists across reconstructions)
        private static bool _wasVisited = false;

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

            TORCommon.Log("[TORSpecializationStageView] Constructed", NLog.LogLevel.Info);

            // Detect direction: if we've visited before, we're coming back from banner editor
            if (_wasVisited)
            {
                TORCommon.Log("[TORSpecializationStageView] Direction: BACKWARD (coming from banner editor)", NLog.LogLevel.Info);
            }
            else
            {
                TORCommon.Log("[TORSpecializationStageView] Direction: FORWARD (coming from narrative stage)", NLog.LogLevel.Info);
            }

            // Check if specialization is needed
            var handler = GetHandler();
            if (handler == null)
            {
                TORCommon.Log("[TORSpecializationStageView] Handler not found, will auto-skip", NLog.LogLevel.Warn);
                _shouldAutoSkip = true;
                return;
            }

            // NEW: Check if there are any specialization options available for this profession in XML
            string professionId = handler.GetSelectedProfessionId();
            bool hasOptions = handler.HasSpecializationOptions(professionId);

            TORCommon.Log($"[TORSpecializationStageView] ProfessionId: '{professionId}', HasOptions: {hasOptions}", NLog.LogLevel.Info);

            if (!hasOptions)
            {
                // Auto-skip if no specialization options available for this profession
                TORCommon.Log("[TORSpecializationStageView] Will auto-skip - no specialization options for this profession", NLog.LogLevel.Info);
                _shouldAutoSkip = true;
                return;
            }

            // NEVER auto-skip if we're just showing the UI - let the user interact
            // Auto-skip is only for professions without options
            _shouldAutoSkip = false;

            // Initialize UI for professions with specialization options
            InitializeUI(handler);
        }

        private void InitializeUI(TorCharacterCreationContentHandler handler)
        {
            TORCommon.Log("[TORSpecializationStageView] Initializing UI", NLog.LogLevel.Info);

            string professionId = handler.GetSelectedProfessionId();
            string title = new TextObject("{=str_tor_cc_specialization_title_generic}Specialization").ToString();
            string description = new TextObject("{=str_tor_cc_specialization_desc_generic}Choose your specialization").ToString();

            // Set description based on profession type
            if (handler.IsSpellcaster(professionId))
            {
                title = new TextObject("{=str_tor_cc_specialization_title_lore}Choose Your Lore").ToString();
                description = new TextObject("{=str_tor_cc_specialization_desc_lore}As a spellcaster, you must choose a lore of magic to specialize in. This will determine which spells you can learn.").ToString();
            }
            else if (handler.IsVampire(professionId))
            {
                title = new TextObject("{=str_tor_cc_specialization_title_bloodline}Choose Your Bloodline").ToString();
                description = new TextObject("{=str_tor_cc_specialization_desc_bloodline}As a vampire, you must choose your bloodline. This will determine your abilities and strengths.").ToString();
            }
            else if (handler.IsPriest(professionId))
            {
                title = new TextObject("{=str_tor_cc_specialization_title_god}Choose Your God").ToString();
                description = new TextObject("{=str_tor_cc_specialization_desc_god}As a priest, you must choose which god you serve. This will determine your divine powers.").ToString();
            }
            else if (handler.IsKnight(professionId))
            {
                title = new TextObject("{=str_tor_cc_specialization_title_order}Choose Your Order").ToString();
                description = new TextObject("{=str_tor_cc_specialization_desc_order}As a knight, you must choose which knightly order you belong to. This will determine your martial traditions and bonuses.").ToString();
            }

            // Create GauntletLayer (UI overlay) - use layer order 1 like native
            _gauntletLayer = new GauntletLayer(1, "GauntletLayer", true);
            _gauntletLayer.InputRestrictions.SetInputRestrictions(true, InputUsageMask.All);
            _gauntletLayer.IsFocusLayer = true;
            _gauntletLayer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("GenericPanelGameKeyCategory"));
            ScreenManager.TrySetFocus(_gauntletLayer);

            TORCommon.Log($"[TORSpecializationStageView] GauntletLayer created, IsActive={_gauntletLayer.IsActive}", NLog.LogLevel.Info);

            // Create custom ViewModel with equipment preview callback
            _dataSource = new TORSpecializationStageVM(title, description, new Action(NextStage), _affirmativeActionText, new Action(PreviousStage),
                _negativeActionText, (selectedOption) =>
                {
                    // Update character equipment and race when option is selected
                    TORCommon.Log($"[TORSpecializationStageView] Equipment preview callback triggered", NLog.LogLevel.Info);
                    if (selectedOption?.Data != null)
                    {
                        TaleWorlds.Core.Equipment equipment = GetEquipmentForOption(selectedOption.Data);
                        _dataSource.UpdateCharacterEquipment(equipment);

                        // Apply race change immediately if this option has a race
                        if (selectedOption.Data is SpecializationOption option && !string.IsNullOrEmpty(option.RaceId))
                        {
                            ApplyRaceChangeImmediate(option.RaceId);
                        }
                    }
                });

            // Populate options based on profession type
            PopulateOptions(handler, professionId);

            // TEST: Try loading a native movie that we know works to verify GauntletLayer can render
            try
            {
                // Try loading our custom movie first
                _movie = _gauntletLayer.LoadMovie("TORSpecializationStage", _dataSource);
                TORCommon.Log($"[TORSpecializationStageView] LoadMovie returned: {(_movie != null ? "success" : "null")}", NLog.LogLevel.Info);

                // Log layer status
                TORCommon.Log($"[TORSpecializationStageView] GauntletLayer IsActive: {_gauntletLayer.IsActive}", NLog.LogLevel.Info);
            }
            catch (Exception ex)
            {
                TORCommon.Log($"[TORSpecializationStageView] Failed to load movie: {ex.Message}\nStack: {ex.StackTrace}", NLog.LogLevel.Error);
                _shouldAutoSkip = true;
            }
        }

        private void PopulateOptions(TorCharacterCreationContentHandler handler, string professionId)
        {
            TORCommon.Log($"[TORSpecializationStageView] Populating options for profession: {professionId}", NLog.LogLevel.Info);

            // Get specialization options from XML
            var specializationOptions = handler.GetSpecializationOptions(professionId);

            if (specializationOptions == null || specializationOptions.Count == 0)
            {
                TORCommon.Log($"[TORSpecializationStageView] No specialization options found for profession: {professionId}", NLog.LogLevel.Warn);
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
                TORCommon.Log($"[TORSpecializationStageView] Added option: {displayName} (ID: {option.Id}, Icon: {iconSprite})", NLog.LogLevel.Info);
            }

            TORCommon.Log($"[TORSpecializationStageView] Added {_dataSource.Options.Count} total options", NLog.LogLevel.Info);

            // Pre-select stored option if it exists
            PreSelectStoredOption(handler, professionId);
        }

        /// <summary>
        /// Pre-select the previously chosen option if one is stored in the handler
        /// </summary>
        private void PreSelectStoredOption(TorCharacterCreationContentHandler handler, string professionId)
        {
            string storedId = null;


            // Find and select the matching option
            foreach (var option in _dataSource.Options)
            {
                if (handler.IsSpellcaster(professionId) && option.Data is TOR_Core.AbilitySystem.Spells.LoreObject lore)
                {
                    if (lore.ID == storedId)
                    {
                        option.ExecuteSelect(); // This will mark it as selected and enable Continue
                        TORCommon.Log($"[TORSpecializationStageView] Pre-selected lore: {lore.Name} ({lore.ID})", NLog.LogLevel.Info);
                        break;
                    }
                }
                else if ((handler.IsVampire(professionId) || handler.IsPriest(professionId)) &&
                         option.Data is TOR_Core.CharacterDevelopment.CareerSystem.CareerObject career)
                {
                    if (career.StringId == storedId)
                    {
                        option.ExecuteSelect(); // This will mark it as selected and enable Continue
                        TORCommon.Log($"[TORSpecializationStageView] Pre-selected career: {career.Name} ({career.StringId})", NLog.LogLevel.Info);
                        break;
                    }
                }
            }
        }

        private Equipment GetEquipmentForOption(object optionData)
        {
            // optionData is now a SpecializationOption - get equipment from its EquipmentSetId
            if (optionData is SpecializationOption option && !string.IsNullOrEmpty(option.EquipmentSetId))
            {
                try
                {
                    var roster = TaleWorlds.Core.Game.Current.ObjectManager.GetObject<TaleWorlds.Core.MBEquipmentRoster>(option.EquipmentSetId);
                    if (roster != null && roster.AllEquipments.Count > 0)
                    {
                        TORCommon.Log($"[TORSpecializationStageView] Loaded equipment from roster '{option.EquipmentSetId}'", NLog.LogLevel.Info);
                        return roster.AllEquipments[0].Clone();
                    }
                    else
                    {
                        TORCommon.Log($"[TORSpecializationStageView] Equipment roster '{option.EquipmentSetId}' not found or empty",
                            NLog.LogLevel.Warn);
                    }
                }
                catch (Exception ex)
                {
                    TORCommon.Log($"[TORSpecializationStageView] Error loading equipment roster '{option.EquipmentSetId}': {ex.Message}",
                        NLog.LogLevel.Error);
                }
            }
            else
            {
                TORCommon.Log($"[TORSpecializationStageView] Invalid option data or missing EquipmentSetId", NLog.LogLevel.Warn);
            }

            // Fallback: Use player's current equipment
            return CharacterObject.PlayerCharacter.Equipment.Clone();
        }

        private TorCharacterCreationContentHandler GetHandler()
        {
            // Access handler from manager using reflection
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
                            if (handler is TorCharacterCreationContentHandler torHandler)
                            {
                                return torHandler;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                TORCommon.Log($"[TORSpecializationStageView] Failed to get handler: {ex.Message}", NLog.LogLevel.Error);
            }

            return null;
        }

        public override void Tick(float dt)
        {
            base.Tick(dt);

            // auto skip back to previous menu
            if (_shouldAutoSkip)
            {
                TORCommon.Log("[TORSpecializationStageView] Auto-skipping stage", NLog.LogLevel.Info);
                _shouldAutoSkip = false;
                if (_wasVisited)
                {
                    PreviousStage();
                }
                else
                {
                    NextStage();
                }

                return;
            }

            // Reset the banner editor flag after first tick

            // Handle hotkey input
            HandleLayerInput();
        }

        private void HandleLayerInput()
        {
            if (_gauntletLayer == null || _dataSource == null) return;

            if (_gauntletLayer.Input.IsHotKeyReleased("Exit"))
            {
                TORCommon.Log("[TORSpecializationStageView] Exit hotkey pressed", NLog.LogLevel.Info);
                PreviousStage();
            }
            else if (_gauntletLayer.Input.IsHotKeyReleased("Confirm") && _dataSource.CanAdvance)
            {
                TORCommon.Log("[TORSpecializationStageView] Confirm hotkey pressed", NLog.LogLevel.Info);
                NextStage();
            }
        }

        public override void NextStage()
        {
            TORCommon.Log("[TORSpecializationStageView] NextStage called", NLog.LogLevel.Info);

            // Store the selected specialization (will be applied at the very end of character creation)
            StoreSpecialization();

            // Mark this stage as visited
            _wasVisited = true;
            TORCommon.Log("[TORSpecializationStageView] Set _wasVisited = true", NLog.LogLevel.Info);

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
                TORCommon.Log("[TORSpecializationStageView] Cannot store specialization - no data source", NLog.LogLevel.Warn);
                return;
            }

            var selectedData = _dataSource.GetSelectedData();
            if (selectedData == null)
            {
                TORCommon.Log("[TORSpecializationStageView] No specialization selected", NLog.LogLevel.Warn);
                return;
            }

            var handler = GetHandler();
            if (handler == null)
            {
                TORCommon.Log("[TORSpecializationStageView] Cannot store specialization - handler not found", NLog.LogLevel.Error);
                return;
            }

            // selectedData is now a SpecializationOption - just store the option ID
            if (selectedData is SpecializationOption option)
            {
                TORCommon.Log($"[TORSpecializationStageView] Storing specialization selection: {option.Name} (ID: {option.Id})", NLog.LogLevel.Info);
                handler.SetSelectedSpecializationOptionId(option.Id);

                // Apply equipment immediately so it shows in banner editor
                if (!string.IsNullOrEmpty(option.EquipmentSetId))
                {
                    ApplyEquipmentFromRoster(option.EquipmentSetId, option.Id);
                }

                // NOTE: Race is applied in the equipment preview callback when option is selected
            }
            else
            {
                TORCommon.Log($"[TORSpecializationStageView] Unknown specialization type: {selectedData.GetType().Name}", NLog.LogLevel.Error);
            }
        }

        public override void PreviousStage()
        {
            TORCommon.Log("[TORSpecializationStageView] PreviousStage (Back button) called", NLog.LogLevel.Info);

            // Clear stored selections when going back (user might change profession)
            // With deferred application, we don't need to clear bonuses since nothing is applied yet
            var handler = GetHandler();
            if (handler != null)
            {
                handler.ClearStoredSpecializations();
                TORCommon.Log("[TORSpecializationStageView] Cleared stored specialization selections (user clicked Back)", NLog.LogLevel.Info);
            }

            // Reset the visited flag so next time we come forward it's treated as a fresh visit
            _wasVisited = false;
            TORCommon.Log("[TORSpecializationStageView] Set _wasVisited = false", NLog.LogLevel.Info);

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
            // Not needed for MVP
        }

        public override void ReleaseEscapeMenuMovie()
        {
            // Not needed for MVP
        }

        protected override void OnFinalize()
        {
            base.OnFinalize();

            TORCommon.Log("[TORSpecializationStageView] OnFinalize called", NLog.LogLevel.Info);

            // Clean up ViewModel
            _dataSource?.OnFinalize();
            _dataSource = null;

            // Clean up GauntletLayer
            _gauntletLayer = null;

            TORCommon.Log("[TORSpecializationStageView] Finalized successfully", NLog.LogLevel.Info);
        }

        /// <summary>
        /// Apply equipment for selected lore immediately (shows in banner editor)
        /// </summary>
        private void ApplyEquipmentForLore(string loreId)
        {
            if (string.IsNullOrEmpty(loreId)) return;

            // All magisters use the same equipment for now
            ApplyEquipmentFromRoster("tor_magister_equipment", loreId);
        }

        /// <summary>
        /// Load equipment from roster and apply to player character
        /// Only copies equipment items, preserves character's face and body customization
        /// </summary>
        private void ApplyEquipmentFromRoster(string rosterId, string specializationId)
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

                    TORCommon.Log(
                        $"[TORSpecializationStageView] Applied equipment items from '{rosterId}' for specialization '{specializationId}' (face/body preserved)",
                        NLog.LogLevel.Info);
                }
                else
                {
                    TORCommon.Log($"[TORSpecializationStageView] Equipment roster '{rosterId}' not found or empty", NLog.LogLevel.Warn);
                }
            }
            catch (Exception ex)
            {
                TORCommon.Log($"[TORSpecializationStageView] Error loading equipment roster '{rosterId}': {ex.Message}", NLog.LogLevel.Error);
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

                TORCommon.Log("[TORSpecializationStageView] Refreshed character preview", NLog.LogLevel.Info);
            }
        }

        /// <summary>
        /// Apply race change immediately (for specializations like Necrarch that change appearance)
        /// Stores the default race in the handler for restoration when going back
        /// </summary>
        private void ApplyRaceChangeImmediate(string raceIdString)
        {
            try
            {
                var handler = GetHandler();
                if (handler == null)
                {
                    TORCommon.Log("[TORSpecializationStageView] Cannot apply race - handler not found", NLog.LogLevel.Error);
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

                TORCommon.Log($"[TORSpecializationStageView] Applied race change: {raceIdString} (race: {newRace})", NLog.LogLevel.Info);
            }
            catch (Exception ex)
            {
                TORCommon.Log($"[TORSpecializationStageView] Error applying race '{raceIdString}': {ex.Message}", NLog.LogLevel.Error);
            }
        }
    }
}