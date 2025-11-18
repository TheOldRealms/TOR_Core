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

        public TORSpecializationStageView(
            CharacterCreationManager characterCreationManager,
            ControlCharacterCreationStage affirmativeAction,
            TextObject affirmativeActionText,
            ControlCharacterCreationStage negativeAction,
            TextObject negativeActionText,
            ControlCharacterCreationStage onRefresh,
            ControlCharacterCreationStageReturnInt getCurrentStageIndexAction,
            ControlCharacterCreationStageReturnInt getTotalStageCountAction,
            ControlCharacterCreationStageReturnInt getFurthestIndexAction,
            ControlCharacterCreationStageWithInt goToIndexAction)
            : base(affirmativeAction, negativeAction, onRefresh, getTotalStageCountAction, getCurrentStageIndexAction, getFurthestIndexAction, goToIndexAction)
        {
            _characterCreationManager = characterCreationManager;
            _affirmativeActionText = affirmativeActionText;
            _negativeActionText = negativeActionText;

            TORCommon.Log("[TORSpecializationStageView] Constructed", NLog.LogLevel.Info);

            // Check if specialization is needed
            var handler = GetHandler();
            if (handler == null)
            {
                TORCommon.Log("[TORSpecializationStageView] Handler not found, will auto-skip", NLog.LogLevel.Warn);
                _shouldAutoSkip = true;
                return;
            }

            bool needsSpec = handler.NeedsSpecialization(handler.GetSelectedProfessionId());
            TORCommon.Log($"[TORSpecializationStageView] NeedsSpecialization: {needsSpec}, ProfessionId: '{handler.GetSelectedProfessionId()}'", NLog.LogLevel.Info);

            if (!needsSpec)
            {
                // Auto-skip for non-specialists
                TORCommon.Log("[TORSpecializationStageView] Will auto-skip for non-specialist", NLog.LogLevel.Info);
                _shouldAutoSkip = true;
                return;
            }

            // Initialize UI for specialists (will pre-select if there's a stored choice)
            InitializeUI(handler);
        }

        private void InitializeUI(TorCharacterCreationContentHandler handler)
        {
            TORCommon.Log("[TORSpecializationStageView] Initializing UI", NLog.LogLevel.Info);

            string professionId = handler.GetSelectedProfessionId();
            string title = "Specialization";
            string description = "Choose your specialization";

            // Set description based on profession type
            if (handler.IsSpellcaster(professionId))
            {
                title = "Choose Your Lore";
                description = "As a spellcaster, you must choose a lore of magic to specialize in. This will determine which spells you can learn.";
            }
            else if (handler.IsVampire(professionId))
            {
                title = "Choose Your Bloodline";
                description = "As a vampire, you must choose your bloodline. This will determine your abilities and strengths.";
            }
            else if (handler.IsPriest(professionId))
            {
                title = "Choose Your God";
                description = "As a priest, you must choose which god you serve. This will determine your divine powers.";
            }

            // Create GauntletLayer (UI overlay) - use layer order 1 like native
            _gauntletLayer = new GauntletLayer(1, "GauntletLayer", true);
            _gauntletLayer.InputRestrictions.SetInputRestrictions(true, InputUsageMask.All);
            _gauntletLayer.IsFocusLayer = true;
            _gauntletLayer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("GenericPanelGameKeyCategory"));
            ScreenManager.TrySetFocus(_gauntletLayer);

            TORCommon.Log($"[TORSpecializationStageView] GauntletLayer created, IsActive={_gauntletLayer.IsActive}", NLog.LogLevel.Info);

            // Create custom ViewModel with equipment preview callback
            _dataSource = new TORSpecializationStageVM(
                title,
                description,
                new Action(NextStage),
                _affirmativeActionText,
                new Action(PreviousStage),
                _negativeActionText,
                (selectedOption) =>
                {
                    // Update character equipment when option is selected
                    TORCommon.Log($"[TORSpecializationStageView] Equipment preview callback triggered", NLog.LogLevel.Info);
                    if (selectedOption?.Data != null)
                    {
                        TaleWorlds.Core.Equipment equipment = GetEquipmentForOption(selectedOption.Data);
                        _dataSource.UpdateCharacterEquipment(equipment);
                    }
                }
            );

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

            if (handler.IsSpellcaster(professionId))
            {
                // Add only the 8 Winds of Magic lore options
                var allLores = TOR_Core.AbilitySystem.Spells.LoreObject.GetAll();
                var playerCulture = TaleWorlds.CampaignSystem.Hero.MainHero.Culture.StringId;

                // Only include the 8 base winds of magic
                var allowedLoreIds = new HashSet<string>
                {
                    "LoreOfLife", "LoreOfFire", "LoreOfDeath", "LoreOfLight",
                    "LoreOfHeavens", "LoreOfBeasts", "LoreOfMetal", "LoreOfShadow"
                };

                TORCommon.Log($"[TORSpecializationStageView] Found {allLores.Count} total lores, player culture: {playerCulture}", NLog.LogLevel.Info);

                foreach (var lore in allLores)
                {
                    // Only include the 8 winds of magic
                    if (!allowedLoreIds.Contains(lore.ID))
                    {
                        continue;
                    }

                    // Skip lores that are disabled for player's culture
                    if (lore.DisabledForCultures.Contains(playerCulture))
                    {
                        TORCommon.Log($"[TORSpecializationStageView] Skipping {lore.Name} (disabled for {playerCulture})", NLog.LogLevel.Info);
                        continue;
                    }

                    string description = $"Master the {lore.Name}.";
                    _dataSource.AddOption(lore.Name, description, lore);
                    TORCommon.Log($"[TORSpecializationStageView] Added lore option: {lore.Name} ({lore.ID})", NLog.LogLevel.Info);
                }
            }
            else if (handler.IsVampire(professionId))
            {
                // Add only the 3 vampire bloodline/career options
                AddCareerOption("MinorVampire", "Von Carstein", "The most powerful vampire bloodline, rulers of Sylvania.");
                AddCareerOption("BloodKnight", "Blood Dragon", "Honorable warriors seeking worthy opponents in battle.");
                AddCareerOption("Necrarch", "Necrarch", "Obsessed with necromantic knowledge and dark sorcery.");
            }
            else if (handler.IsPriest(professionId))
            {
                // Add only the 2 Warrior Priest careers
                AddCareerOption("WarriorPriest", "Warrior Priest of Sigmar", "A warrior-priest devoted to Sigmar, protector of mankind.");
                AddCareerOption("WarriorPriestUlric", "Warrior Priest of Ulric", "A warrior-priest of Ulric, god of winter, wolves, and war.");
            }
            else if (handler.IsKnight(professionId))
            {
                // Add the 5 Knight Order careers
                AddCareerOption("KnightBlazingSun", "Order of the Blazing Sun", "Knights of Myrmidia, masters of strategy and warfare from Talabheim.");
                AddCareerOption("KnightPanthers", "Knight Panthers", "Elite secular knights from Carroburg, known for their ferocity.");
                AddCareerOption("KnightWhiteWolf", "Knights of the White Wolf", "Devoted followers of Ulric from Middenheim, fierce and relentless.");
                AddCareerOption("KnightGriphon", "Order of the Griphon", "Noble knights of Sigmar from Altdorf, defenders of the faithful.");
                AddCareerOption("Reiksguard", "Reiksguard", "The Emperor's personal guard, elite secular knights from Castle Reiksguard.");
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

            if (handler.IsSpellcaster(professionId))
            {
                storedId = handler.GetStoredLoreId();
            }
            else if (handler.IsVampire(professionId) || handler.IsPriest(professionId))
            {
                storedId = handler.GetStoredCareerId();
            }

            if (string.IsNullOrEmpty(storedId))
            {
                TORCommon.Log("[TORSpecializationStageView] No stored selection to pre-select", NLog.LogLevel.Info);
                return;
            }

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
                else if ((handler.IsVampire(professionId) || handler.IsPriest(professionId)) && option.Data is TOR_Core.CharacterDevelopment.CareerSystem.CareerObject career)
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

        private void AddCareerOption(string careerId, string displayName, string description)
        {
            try
            {
                var career = TaleWorlds.Core.Game.Current.ObjectManager.GetObject<TOR_Core.CharacterDevelopment.CareerSystem.CareerObject>(careerId);
                if (career != null)
                {
                    _dataSource.AddOption(displayName, description, career);
                    TORCommon.Log($"[TORSpecializationStageView] Added career option: {displayName} ({careerId})", NLog.LogLevel.Info);
                }
                else
                {
                    TORCommon.Log($"[TORSpecializationStageView] Career not found: {careerId}", NLog.LogLevel.Warn);
                }
            }
            catch (Exception ex)
            {
                TORCommon.Log($"[TORSpecializationStageView] Error adding career {careerId}: {ex.Message}", NLog.LogLevel.Error);
            }
        }

        private TaleWorlds.Core.Equipment GetEquipmentForOption(object optionData)
        {
            string rosterId = null;

            if (optionData is TOR_Core.AbilitySystem.Spells.LoreObject)
            {
                rosterId = "tor_magister_equipment"; // All magisters same for now
            }
            else if (optionData is TOR_Core.CharacterDevelopment.CareerSystem.CareerObject career)
            {
                rosterId = career.StringId switch
                {
                    "MinorVampire" => "tor_vampire_noble_equipment",
                    "BloodKnight" => "tor_blood_dragon_equipment",
                    "Necrarch" => "tor_necrarch_equipment",
                    "WarriorPriest" => "tor_sigmar_priest_equipment",
                    "WarriorPriestUlric" => "tor_ulric_priest_equipment",
                    // Knight orders - all use same equipment
                    "KnightBlazingSun" => "tor_empire_knight_equipment",
                    "KnightPanthers" => "tor_empire_knight_equipment",
                    "KnightWhiteWolf" => "tor_empire_knight_equipment",
                    "KnightGriphon" => "tor_empire_knight_equipment",
                    "Reiksguard" => "tor_empire_knight_equipment",
                    _ => null
                };
            }

            if (string.IsNullOrEmpty(rosterId))
            {
                TORCommon.Log($"[TORSpecializationStageView] No equipment roster ID for option type {optionData?.GetType().Name}, using player equipment", NLog.LogLevel.Warn);
                // Fallback: Use player's current equipment for testing
                return TaleWorlds.CampaignSystem.CharacterObject.PlayerCharacter.Equipment.Clone();
            }

            // Try to find the equipment roster
            try
            {
                var roster = TaleWorlds.Core.Game.Current.ObjectManager.GetObject<TaleWorlds.Core.MBEquipmentRoster>(rosterId);
                if (roster != null && roster.AllEquipments.Count > 0)
                {
                    // Get first equipment from roster
                    TORCommon.Log($"[TORSpecializationStageView] Loaded equipment from roster '{rosterId}'", NLog.LogLevel.Info);
                    return roster.AllEquipments[0].Clone();
                }
                else
                {
                    TORCommon.Log($"[TORSpecializationStageView] Equipment roster '{rosterId}' not found or empty, using player equipment", NLog.LogLevel.Warn);
                    // Fallback: Use player's current equipment for testing
                    return TaleWorlds.CampaignSystem.CharacterObject.PlayerCharacter.Equipment.Clone();
                }
            }
            catch (Exception ex)
            {
                TORCommon.Log($"[TORSpecializationStageView] Error loading equipment roster '{rosterId}': {ex.Message}, using player equipment", NLog.LogLevel.Error);
                // Fallback: Use player's current equipment for testing
                return TaleWorlds.CampaignSystem.CharacterObject.PlayerCharacter.Equipment.Clone();
            }
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
                    var handlers = handlersField.GetValue(_characterCreationManager) as System.Collections.Generic.SortedList<int, ICharacterCreationContentHandler>;
                    if (handlers != null)
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

            // Handle auto-skip on first tick
            if (_shouldAutoSkip)
            {
                TORCommon.Log("[TORSpecializationStageView] Auto-skipping stage", NLog.LogLevel.Info);
                _shouldAutoSkip = false;
                NextStage();
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

            // Store based on type
            if (selectedData is TOR_Core.AbilitySystem.Spells.LoreObject lore)
            {
                TORCommon.Log($"[TORSpecializationStageView] Storing lore selection: {lore.Name} ({lore.ID})", NLog.LogLevel.Info);
                handler.SetSelectedLore(lore.ID);

                // Apply equipment immediately so it shows in banner editor
                ApplyEquipmentForLore(lore.ID);
            }
            else if (selectedData is TOR_Core.CharacterDevelopment.CareerSystem.CareerObject career)
            {
                TORCommon.Log($"[TORSpecializationStageView] Storing career selection: {career.Name} ({career.StringId})", NLog.LogLevel.Info);
                handler.SetSelectedCareer(career.StringId);

                // Apply equipment immediately so it shows in banner editor
                ApplyEquipmentForCareer(career.StringId);
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
        /// Apply equipment for selected career immediately (shows in banner editor)
        /// </summary>
        private void ApplyEquipmentForCareer(string careerId)
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
                ApplyEquipmentFromRoster(equipmentRosterId, careerId);
            }
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

                    TORCommon.Log($"[TORSpecializationStageView] Applied equipment items from '{rosterId}' for specialization '{specializationId}' (face/body preserved)", NLog.LogLevel.Info);
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
    }
}
