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
        private TORCharacterCreationContentHandler _cachedHandler;

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
            

            // Check if specialization is needed
            var handler = GetHandler();
            if (handler == null)
            {
                _shouldAutoSkip = true;
                return;
            }

            // NEW: Check if there are any specialization options available for this profession in XML
            string professionId = handler.GetSelectedProfessionId();
            bool hasOptions = handler.HasSpecializationOptions(professionId);


            if (!hasOptions)
            {
                // Auto-skip if no specialization options available for this profession
                _shouldAutoSkip = true;
                return;
            }

            // NEVER auto-skip if we're just showing the UI - let the user interact
            // Auto-skip is only for professions without options
            _shouldAutoSkip = false;

            // Initialize UI for professions with specialization options
            InitializeUI(handler);
        }

        private void InitializeUI(TORCharacterCreationContentHandler handler)
        {

            string professionId = handler.GetSelectedProfessionId();
            string title = new TextObject("{=str_tor_cc_specialization_title_generic}Specialization").ToString();
            string description = new TextObject("{=str_tor_cc_specialization_desc_generic}Choose your specialization").ToString();

            // Set description based on profession type
            if (IsSpellcaster(professionId))
            {
                title = new TextObject("{=str_tor_cc_specialization_title_lore}Choose Your Lore").ToString();
                description = new TextObject("{=str_tor_cc_specialization_desc_lore}As a spellcaster, you must choose a lore of magic to specialize in. This will determine which spells you can learn.").ToString();
            }
            else if (professionId == "option_3_vc_vampire" || professionId == "option_3_mousillon_vampire")
            {
                title = new TextObject("{=str_tor_cc_specialization_title_bloodline}Choose Your Bloodline").ToString();
                description = new TextObject("{=str_tor_cc_specialization_desc_bloodline}As a vampire, you must choose your bloodline. This will determine your abilities and strengths.").ToString();
            }
            else if (professionId == "option_3_empire_priest_acolyte")
            {
                title = new TextObject("{=str_tor_cc_specialization_title_god}Choose Your God").ToString();
                description = new TextObject("{=str_tor_cc_specialization_desc_god}As a priest, you must choose which god you serve. This will determine your divine powers.").ToString();
            }
            else if (professionId == "option_3_empire_knight")
            {
                title = new TextObject("{=str_tor_cc_specialization_title_order}Choose Your Order").ToString();
                description = new TextObject("{=str_tor_cc_specialization_desc_order}As a knight, you must choose which knightly order you belong to. This will determine your martial traditions and bonuses.").ToString();
            }
            
            _gauntletLayer = new GauntletLayer(1, "GauntletLayer", true);
            _gauntletLayer.InputRestrictions.SetInputRestrictions(true, InputUsageMask.All);
            _gauntletLayer.IsFocusLayer = true;
            _gauntletLayer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("GenericPanelGameKeyCategory"));
            ScreenManager.TrySetFocus(_gauntletLayer);


            // Create custom ViewModel with equipment preview callback
            _dataSource = new TORSpecializationStageVM(title, description, new Action(NextStage), _affirmativeActionText, new Action(PreviousStage),
                _negativeActionText, (selectedOption) =>
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
                });

            // Populate options based on profession type
            PopulateOptions(handler, professionId);
            
            _movie = _gauntletLayer.LoadMovie("TORSpecializationStage", _dataSource);
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
                PreviousStage();
            }
            else if (_gauntletLayer.Input.IsHotKeyReleased("Confirm") && _dataSource.CanAdvance)
            {
                NextStage();
            }
        }

        public override void NextStage()
        {

            // Store the selected specialization (will be applied at the very end of character creation)
            StoreSpecialization();

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
                return;
            }

            var selectedData = _dataSource.GetSelectedData();
            if (selectedData == null)
            {
                return;
            }

            var handler = GetHandler();
            if (handler == null)
            {
                return;
            }
            
            if (selectedData is SpecializationOption option)
            {
                handler.SetSelectedSpecializationOptionId(option.Id);

                // Apply equipment immediately so it shows in banner editor
                if (!string.IsNullOrEmpty(option.EquipmentSetId))
                {
                    ApplyEquipmentFromRoster(option.EquipmentSetId);
                }
            }
        }

        public override void PreviousStage()
        {

            // Clear stored selections when going back (user might change profession)
            // With deferred application, we don't need to clear bonuses since nothing is applied yet
            var handler = GetHandler();
            if (handler != null)
            {
                handler.ClearStoredSpecializations();
            }

            // Reset the visited flag so next time we come forward it's treated as a fresh visit
            _wasVisited = false;

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


            // Clean up ViewModel
            _dataSource?.OnFinalize();
            _dataSource = null;

            // Clean up GauntletLayer
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
        /// Check if profession is a spellcaster (multiple professions share this trait)
        /// </summary>
        private bool IsSpellcaster(string professionId)
        {
            return professionId == "option_3_empire_magister_apprentice" ||
                   professionId == "option_3_bretonnia_damsel" ||
                   professionId == "option_3_we_spellsinger" ||
                   professionId == "option_3_eo_greylord_apprentice";
        }
    }
}