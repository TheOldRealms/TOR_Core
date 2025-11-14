using System;
using System.Collections.Generic;
using SandBox.View.CharacterCreation;
using TaleWorlds.CampaignSystem.CharacterCreationContent;
using TaleWorlds.Core.ViewModelCollection;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.Engine.Screens;
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

            // Initialize UI for specialists
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

            // Create GauntletLayer (UI overlay)
            _gauntletLayer = new GauntletLayer(1);
            _gauntletLayer.InputRestrictions.SetInputRestrictions();
            _gauntletLayer.IsFocusLayer = true;
            _gauntletLayer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("GenericPanelGameKeyCategory"));
            ScreenManager.TrySetFocus(_gauntletLayer);

            // Create custom ViewModel
            _dataSource = new TORSpecializationStageVM(
                title,
                description,
                new Action(NextStage),
                _affirmativeActionText,
                new Action(PreviousStage),
                _negativeActionText
            );

            // Load custom Gauntlet movie
            try
            {
                _movie = _gauntletLayer.LoadMovie("TORSpecializationStage", _dataSource);
                TORCommon.Log("[TORSpecializationStageView] UI initialized successfully", NLog.LogLevel.Info);
            }
            catch (Exception ex)
            {
                TORCommon.Log($"[TORSpecializationStageView] Failed to load movie: {ex.Message}", NLog.LogLevel.Error);
                _shouldAutoSkip = true;
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
            _affirmativeAction();
        }

        public override void PreviousStage()
        {
            TORCommon.Log("[TORSpecializationStageView] PreviousStage called", NLog.LogLevel.Info);
            _negativeAction();
        }

        public override IEnumerable<ScreenLayer> GetLayers()
        {
            // MVP: Return only GauntletLayer (UI overlay)
            // SceneLayer (3D character) can be added later if desired
            if (_gauntletLayer != null)
            {
                return new List<ScreenLayer> { _gauntletLayer };
            }

            // Auto-skip case: return empty list
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

            // Clean up GauntletLayer (just set to null, cleanup is automatic)
            _gauntletLayer = null;

            TORCommon.Log("[TORSpecializationStageView] Finalized successfully", NLog.LogLevel.Info);
        }
    }
}
