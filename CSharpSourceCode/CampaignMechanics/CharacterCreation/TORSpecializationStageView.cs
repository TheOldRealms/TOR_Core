using System;
using System.Collections.Generic;
using SandBox.View.CharacterCreation;
using TaleWorlds.CampaignSystem.CharacterCreationContent;
using TaleWorlds.Core.ViewModelCollection;
using TaleWorlds.Engine;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.Engine.Screens;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade.GauntletUI.BodyGenerator;
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

        private Scene _characterScene;
        private Camera _camera;

        public SceneLayer CharacterLayer { get; private set; }

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

            // Create GauntletLayer (UI overlay) - use high layer order to ensure it's on top
            _gauntletLayer = new GauntletLayer(100, "GauntletLayer", true);
            _gauntletLayer.InputRestrictions.SetInputRestrictions(true, InputUsageMask.All);
            _gauntletLayer.IsFocusLayer = true;
            _gauntletLayer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("GenericPanelGameKeyCategory"));
            ScreenManager.TrySetFocus(_gauntletLayer);

            TORCommon.Log($"[TORSpecializationStageView] GauntletLayer created, IsActive={_gauntletLayer.IsActive}", NLog.LogLevel.Info);

            // Create custom ViewModel
            _dataSource = new TORSpecializationStageVM(
                title,
                description,
                new Action(NextStage),
                _affirmativeActionText,
                new Action(PreviousStage),
                _negativeActionText
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
                // Add lore options - TODO: Get actual lores from handler
                _dataSource.AddOption("Lore of Fire", "Master the destructive power of flames and infernos.", "lore_fire");
                _dataSource.AddOption("Lore of Death", "Command the dark arts and raise the dead to serve you.", "lore_death");
                _dataSource.AddOption("Lore of Life", "Harness nature's healing energies to mend wounds and cure disease.", "lore_life");
                _dataSource.AddOption("Lore of Light", "Wield holy magic to smite the undead and protect the innocent.", "lore_light");
                _dataSource.AddOption("Lore of Shadow", "Manipulate darkness and illusion to confuse and defeat your enemies.", "lore_shadow");
            }
            else if (handler.IsVampire(professionId))
            {
                // Add bloodline options - TODO: Get actual bloodlines from handler
                _dataSource.AddOption("Von Carstein", "The most powerful vampire bloodline, rulers of Sylvania.", "bloodline_von_carstein");
                _dataSource.AddOption("Lahmian", "Masters of seduction and political intrigue.", "bloodline_lahmian");
                _dataSource.AddOption("Blood Dragon", "Honorable warriors seeking worthy opponents in battle.", "bloodline_blood_dragon");
                _dataSource.AddOption("Necrarch", "Obsessed with necromantic knowledge and dark sorcery.", "bloodline_necrarch");
                _dataSource.AddOption("Strigoi", "Feral and bestial vampires, shunned by other bloodlines.", "bloodline_strigoi");
            }
            else if (handler.IsPriest(professionId))
            {
                // Add god options - TODO: Get actual gods/priesthoods from handler
                _dataSource.AddOption("Sigmar", "The patron god of the Empire, protector of mankind.", "god_sigmar");
                _dataSource.AddOption("Ulric", "God of winter, wolves, and war.", "god_ulric");
                _dataSource.AddOption("Taal", "God of nature and wild places.", "god_taal");
                _dataSource.AddOption("Morr", "God of death and dreams.", "god_morr");
                _dataSource.AddOption("Shallya", "Goddess of healing and mercy.", "god_shallya");
            }

            TORCommon.Log($"[TORSpecializationStageView] Added {_dataSource.Options.Count} options", NLog.LogLevel.Info);
        }

        public override void SetGenericScene(Scene scene)
        {
            TORCommon.Log("[TORSpecializationStageView] SetGenericScene called", NLog.LogLevel.Info);
            _characterScene = scene;
            _characterScene.SetShadow(true);
            _characterScene.SetDynamicShadowmapCascadesRadiusMultiplier(0.1f);
            _characterScene.SetDoNotWaitForLoadingStatesToRender(true);
            _characterScene.DisableStaticShadows(true);

            _camera = Camera.CreateCamera();
            BodyGeneratorView.InitCamera(_camera, _cameraPosition);

            CharacterLayer = new SceneLayer(clearSceneOnFinalize: false);
            CharacterLayer.SetScene(_characterScene);
            CharacterLayer.SetCamera(_camera);
            CharacterLayer.SetSceneUsesShadows(true);
            CharacterLayer.SetRenderWithPostfx(true);
            CharacterLayer.SetPostfxFromConfig();
            CharacterLayer.SceneView.SetResolutionScaling(true);
            CharacterLayer.SetPostfxConfigParams(-1 & -5);

            // Ensure GauntletLayer input is registered after scene is set up
            if (_gauntletLayer != null)
            {
                CharacterLayer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("GenericPanelGameKeyCategory"));
            }

            TORCommon.Log("[TORSpecializationStageView] Scene initialized successfully", NLog.LogLevel.Info);
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

            // Tick the scene if it exists
            if (_characterScene != null)
            {
                _characterScene.Tick(dt);
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
            // Return both layers like native stages - order matters: SceneLayer first, GauntletLayer on top
            var layers = new List<ScreenLayer>();

            if (CharacterLayer != null)
            {
                layers.Add(CharacterLayer);
                TORCommon.Log($"[TORSpecializationStageView] GetLayers: CharacterLayer added", NLog.LogLevel.Debug);
            }

            if (_gauntletLayer != null)
            {
                layers.Add(_gauntletLayer);
                TORCommon.Log($"[TORSpecializationStageView] GetLayers: GauntletLayer added, IsActive={_gauntletLayer.IsActive}", NLog.LogLevel.Debug);
            }

            TORCommon.Log($"[TORSpecializationStageView] GetLayers: Returning {layers.Count} layers", NLog.LogLevel.Debug);
            return layers;
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

            // Clean up SceneLayer
            if (CharacterLayer != null)
            {
                CharacterLayer.SceneView.SetEnable(false);
                CharacterLayer.SceneView.ClearAll(false, false);
                CharacterLayer = null;
            }

            // Clean up Scene and Camera
            _characterScene = null;
            _camera = null;

            TORCommon.Log("[TORSpecializationStageView] Finalized successfully", NLog.LogLevel.Info);
        }
    }
}
