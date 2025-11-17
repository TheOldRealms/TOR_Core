using System;
using System.Collections.Generic;
using System.Linq;
using SandBox.View.CharacterCreation;
using System.Numerics;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CharacterCreationContent;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection;
using TaleWorlds.Engine;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.Engine.Screens;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.GauntletUI.BodyGenerator;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.ScreenSystem;
using TOR_Core.Extensions;
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
        private Vec3 _cameraPosition = new Vec3(0.65f, 1.55f, 1.27f, -1f);

        // Character model display
        private List<AgentVisuals> _characterVisuals;
        private bool _isCharacterVisualsReady;

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

            // Create GauntletLayer (UI overlay) - use high layer order to ensure it's on top
            _gauntletLayer = new GauntletLayer(100, "GauntletLayer", true);
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
                    // Update character visual when option is selected
                    TORCommon.Log($"[TORSpecializationStageView] Equipment preview callback triggered", NLog.LogLevel.Info);
                    if (selectedOption?.Data != null)
                    {
                        TaleWorlds.Core.Equipment equipment = GetEquipmentForOption(selectedOption.Data);
                        CreateCharacterVisual(equipment);
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

        private void CreateCharacterVisual(Equipment equipment)
        {
            if (_characterScene == null)
            {
                TORCommon.Log("[TORSpecializationStageView] Cannot create character visual - scene is null", NLog.LogLevel.Error);
                return;
            }

            TORCommon.Log($"[TORSpecializationStageView] CreateCharacterVisual called with equipment: {equipment != null}", NLog.LogLevel.Info);

            ClearCharacterVisuals();

            MatrixFrame frame = MatrixFrame.Identity;

            // Try multiple spawn point tags that might exist in the scene
            string[] spawnTags = { "spawnpoint_player", "sp_player", "strategycamera_1", "character_spawn", "player_spawn" };
            GameEntity spawnPoint = null;

            foreach (var tag in spawnTags)
            {
                spawnPoint = _characterScene.FindEntityWithTag(tag);
                if (spawnPoint != null)
                {
                    frame = spawnPoint.GetGlobalFrame();
                    TORCommon.Log($"[TORSpecializationStageView] Found spawn point with tag '{tag}', position: {frame.origin}", NLog.LogLevel.Info);
                    break;
                }
            }

            if (spawnPoint == null)
            {
                // Position character in front of camera at proper height
                // Camera is at (0.65, 1.55, 1.27), so place character centered in view
                frame.origin = new Vec3(0f, 1.5f, 0f, -1f);
                frame.rotation.RotateAboutUp(MathF.PI); // Face the camera
                TORCommon.Log($"[TORSpecializationStageView] No spawn point found in scene, positioning at: {frame.origin}", NLog.LogLevel.Warn);
            }

            ActionIndexCache actionCode = ActionIndexCache.Create("act_childhood_schooled");
            Monster baseMonster = TaleWorlds.Core.FaceGen.GetBaseMonsterFromRace(TaleWorlds.CampaignSystem.CharacterObject.PlayerCharacter.Race);
            TORCommon.Log($"[TORSpecializationStageView] Using monster: {baseMonster.StringId}, action: {actionCode.Index}", NLog.LogLevel.Info);

            AgentVisualsData visualData = new AgentVisualsData()
                .UseMorphAnims(true)
                .Equipment(equipment)
                .BodyProperties(TaleWorlds.CampaignSystem.CharacterObject.PlayerCharacter.GetBodyProperties(TaleWorlds.CampaignSystem.CharacterObject.PlayerCharacter.Equipment, -1))
                .Frame(frame)
                .ActionSet(MBGlobals.GetActionSetWithSuffix(baseMonster, TaleWorlds.CampaignSystem.CharacterObject.PlayerCharacter.IsFemale, "_facegen"))
                .ActionCode(actionCode)
                .Scene(_characterScene)
                .Monster(baseMonster)
                .UseTranslucency(false)  // CHANGED: Try without translucency
                .UseTesselation(true)
                .Race(TaleWorlds.CampaignSystem.CharacterObject.PlayerCharacter.Race)
                .SkeletonType(TaleWorlds.CampaignSystem.CharacterObject.PlayerCharacter.IsFemale ? SkeletonType.Female : SkeletonType.Male);

            TORCommon.Log($"[TORSpecializationStageView] Created AgentVisualsData with frame position: {frame.origin}, rotation: {frame.rotation}", NLog.LogLevel.Info);

            AgentVisuals agentVisuals = AgentVisuals.Create(visualData, "specialization_character", false, false, false);
            if (agentVisuals == null)
            {
                TORCommon.Log("[TORSpecializationStageView] Failed to create AgentVisuals - returned null", NLog.LogLevel.Error);
                return;
            }

            TORCommon.Log($"[TORSpecializationStageView] AgentVisuals created successfully, entity: {agentVisuals.GetEntity() != null}", NLog.LogLevel.Info);

            // CRITICAL: Force initial animation update (from native code line 149)
            agentVisuals.GetVisuals().GetSkeleton().TickAnimationsAndForceUpdate(MBRandom.RandomFloat, frame, true);
            TORCommon.Log("[TORSpecializationStageView] Initial animation update completed", NLog.LogLevel.Info);

            // CHANGED: Keep visible immediately - don't wait for resources
            agentVisuals.SetVisible(true);
            agentVisuals.SetAgentLodZeroOrMax(true);
            agentVisuals.GetEntity().SetEnforcedMaximumLodLevel(0);
            agentVisuals.GetEntity().CheckResources(true, true);

            // CRITICAL: Set focused shadowmap on CHARACTER position (native code line 161)
            CharacterLayer.SetFocusedShadowmap(true, ref frame.origin, 0.59999996f);
            TORCommon.Log($"[TORSpecializationStageView] SetFocusedShadowmap on character position: {frame.origin}", NLog.LogLevel.Info);

            // Point camera at character
            if (_camera != null)
            {
                TaleWorlds.Library.Vec3 lookAtTarget = new TaleWorlds.Library.Vec3(frame.origin.x, frame.origin.y + 1.0f, frame.origin.z); // Look at character's head height
                TaleWorlds.Library.Vec3 cameraPos = new TaleWorlds.Library.Vec3(_cameraPosition.x, _cameraPosition.y, _cameraPosition.z);
                _camera.LookAt(cameraPos, lookAtTarget, TaleWorlds.Library.Vec3.Up);
                TORCommon.Log($"[TORSpecializationStageView] Camera looking at: {lookAtTarget} from {cameraPos}", NLog.LogLevel.Info);
            }

            _characterVisuals.Add(agentVisuals);

            // Force scene to update
             if (_characterScene != null)
            {
                _characterScene.ForceLoadResources();
                TORCommon.Log("[TORSpecializationStageView] Forced scene resource load", NLog.LogLevel.Info);
            }

            TORCommon.Log($"[TORSpecializationStageView] Created character visual (total visuals: {_characterVisuals.Count})", NLog.LogLevel.Info);
        }

        private void ClearCharacterVisuals()
        {
            if (_characterVisuals != null)
            {
                foreach (var visual in _characterVisuals)
                {
                    visual.Reset();
                }
                _characterVisuals.Clear();
            }
        }

        public override void SetGenericScene(Scene scene)
        {
            TORCommon.Log("[TORSpecializationStageView] SetGenericScene called", NLog.LogLevel.Info);
            _characterScene = scene;
            _characterScene.SetShadow(true);
            _characterScene.SetDynamicShadowmapCascadesRadiusMultiplier(0.1f);
            _characterScene.SetDoNotWaitForLoadingStatesToRender(true);
            _characterScene.DisableStaticShadows(true);
            // Add lighting to the scene
            uint sunLightColor = 0xFFFFFFFF; // White light
            Vec3 sunDirection = new Vec3(-0.5f, -1f, -0.5f); // Direction pointing down and towards character
            _characterScene.SetDefaultLighting();
            TORCommon.Log($"[TORSpecializationStageView] Set sun light with direction: {sunDirection}", NLog.LogLevel.Info);

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

            // CRITICAL: Explicitly enable the scene view for rendering
            CharacterLayer.SceneView.SetEnable(true);

            // CRITICAL: Set focused shadowmap for proper character lighting/rendering
            MatrixFrame cameraFrame = MatrixFrame.Identity;
            cameraFrame.origin = _cameraPosition;
            CharacterLayer.SetFocusedShadowmap(true, ref cameraFrame.origin, 0.59999996f);

            // Ensure GauntletLayer input is registered after scene is set up
            if (_gauntletLayer != null)
            {
                CharacterLayer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("GenericPanelGameKeyCategory"));
            }

            // Initialize character visuals list
            _characterVisuals = new List<AgentVisuals>();

            // Create initial character visual with default equipment
            CreateCharacterVisual(Hero.MainHero.CharacterObject.Equipment.Clone());

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

            // CRITICAL: Tick visuals every frame to update their rendering state
            if (_characterVisuals != null && _characterVisuals.Count > 0)
            {
                foreach (var visual in _characterVisuals)
                {
                    visual.TickVisuals();
                }

                // Debug: Log entity state every 2 seconds
                if (Time.ApplicationTime % 2.0f < dt)
                {
                    var visual = _characterVisuals[0];
                    var entity = visual.GetEntity();
                    TORCommon.Log($"[DEBUG] Character - Visible: {visual.GetVisuals().GetVisible()}, Entity exists: {entity != null}, Entity visible: {entity?.IsVisibleIncludeParents()}, Position: {entity?.GetGlobalFrame().origin}", NLog.LogLevel.Info);
                }
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
            }
            else if (selectedData is TOR_Core.CharacterDevelopment.CareerSystem.CareerObject career)
            {
                TORCommon.Log($"[TORSpecializationStageView] Storing career selection: {career.Name} ({career.StringId})", NLog.LogLevel.Info);
                handler.SetSelectedCareer(career.StringId);
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

            // Clean up character visuals
            ClearCharacterVisuals();
            _characterVisuals = null;

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
