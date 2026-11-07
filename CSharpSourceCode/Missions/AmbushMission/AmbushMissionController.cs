using NLog;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.TroopSuppliers;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TOR_Core.Extensions;
using TOR_Core.Models;
using TOR_Core.Utilities;

namespace TOR_Core.Missions.AmbushMission
{
    /// <summary>
    /// Battle setup for the ambush mission. Clones the useful parts of vanilla
    /// SallyOutMissionController (reinforcement timer, attacker-activation
    /// deployment delay, troop-ratio balancing) and skips the castle-gate code
    /// that doesn't apply to a field ambush.
    ///
    /// All tune-ables pulled from <see cref="TORMissionAmbushModel"/>.
    /// </summary>
    public class AmbushMissionController : MissionLogic
    {
        // Grace window before we decide the scene has no authored retreat zone
        // and set a procedural fallback. ScriptComponentBehavior.OnInit on
        // AmbushRetreatZone isn't guaranteed to run before OnDeploymentFinished,
        // so checking immediately races with the engine and can turn a scene
        // that DOES have an authored zone into "fallback at player position"
        // (= instant extraction victory as soon as the player stands still).
        private const float FallbackGraceSeconds = 2f;

        private DefaultBattleMissionAgentSpawnLogic _spawnLogic;
        // Guard flag read by OnAgentCreated so our own custom spawn
        // pipeline doesn't re-teleport the agents we've just placed at
        // the correct grid position via AgentBuildData.InitialPosition.
        private bool _customSpawnInProgress;

        // Cached reflection handles for bumping each side's
        // MissionBattleSideSpawnContext._numSpawnedTroops as we do our
        // custom spawn. Vanilla's DefaultBattleMissionAgentSpawnLogic
        // tracks a per-side "troops the context placed" counter that
        // reads NumberOfActiveTroops = _numSpawnedTroops - supplier
        // .NumRemovedTroops. The supplier side of the math still works
        // for us (PartyGroupAgentOrigin.SetKilled forwards to supplier
        // .OnTroopKilled as our custom-spawned agents die). But the
        // _numSpawnedTroops side stays at 0 because our spawn path
        // bypasses the vanilla SpawnTroops loop that bumps it. Mid-
        // battle that math goes negative; IsSideDepleted never fires
        // true; vanilla's post-battle state machine stalls; the
        // stealth-menu restore / loot-flow chain never completes.
        // Bumping _numSpawnedTroops per custom-spawned agent matches
        // vanilla line 386 of MissionBattleSideSpawnContext.SpawnTroops.
        private System.Reflection.FieldInfo _numSpawnedTroopsField;
        private System.Array _battleSideSpawnContextsArray;
        private AmbushReinforcementSpawnTimer _reinforcementTimer;
        private BasicMissionTimer _fallbackGraceTimer;
        private bool _fallbackResolved;
        private TORMissionAmbushModel _model;
        // The two parties involved in the ambush. The controller works out
        // which battle side each is on at AfterStart time (reads
        // PartyBase.MapEventSide.MissionSide), so Phase D (player ambushes
        // AI) and Phase F (AI ambushes player) share this class with no
        // per-phase branching.
        private readonly PartyBase _ambusherParty;
        private readonly PartyBase _victimParty;
        private BattleSideEnum _ambusherSide;
        private BattleSideEnum _victimSide;

        // Per-class flat position + yaw lists built from the AmbushFormationLayout
        // result: for each AmbushFormation chunk with N troops bound to a spot
        // at (pos, yaw), the lists hold N copies of pos and yaw. OnAgentCreated
        // round-robins through the matching-class list, so agents distribute
        // across spots in proportion to each chunk's troop count.
        private readonly Dictionary<FormationClass, List<Vec3>> _attackerSpawnPositions
            = new Dictionary<FormationClass, List<Vec3>>();
        private readonly Dictionary<FormationClass, List<float>> _attackerSpawnYaws
            = new Dictionary<FormationClass, List<float>>();
        private readonly Dictionary<FormationClass, int> _attackerSpawnIndex
            = new Dictionary<FormationClass, int>();
        // Legacy fallback for scenes authored before the role-tag scheme —
        // uses the flat `ambush_attacker` tag. Only populated if the role-
        // specific lists come up empty.
        private readonly List<Vec3> _legacyAttackerSpawnPositions = new List<Vec3>();
        private int _legacyAttackerSpawnIndex;

        /// <summary>
        /// Auto-chunked formation list plus default spot assignments from
        /// <see cref="AmbushFormationLayout.Compute"/>. Computed at AfterStart
        /// time. Null if the scene uses the legacy flat <c>ambush_attacker</c>
        /// tag (no spot buckets) or has no attacker spawns at all. Readers:
        /// the deployment UI binds sidebar panels / spot markers to this.
        /// </summary>
        public AmbushLayoutResult Layout { get; private set; }

        /// <summary>
        /// Every attacker spot the scener placed, in scene-order. Readers:
        /// the deployment UI's spot-picker filters this to spots in the
        /// clicked formation's bucket. Public list so VMs can read directly.
        /// </summary>
        public List<AmbushSpot> SceneSpots { get; private set; } = new List<AmbushSpot>();

        /// <summary>
        /// Stable 1-based number per spot, keyed by its scene position. Used
        /// across the sidebar chips, the spot-picker rows, and the (deferred)
        /// scene markers so the player sees the same "Spot 3" everywhere.
        /// </summary>
        public Dictionary<Vec3, int> SpotNumbers { get; private set; }
            = new Dictionary<Vec3, int>();

        // Snapshot of defender agents present at OnDeploymentFinished — the
        // "initial patrol". Reinforcement waves are NOT in this set. Phase 1
        // → Phase 2 transition fires when every member is inactive.
        private readonly List<Agent> _initialPatrol = new List<Agent>();
        private bool _initialPatrolDefeated;

        /// <summary>True once every member of the initial defender spawn is
        /// inactive. Readers: <see cref="AmbushEndLogic"/> gates Phase 2
        /// behaviours on this; <see cref="AmbushReinforcementSpawnTimer"/>
        /// applies the one-shot delay bonus when it flips.</summary>
        public bool IsInitialPatrolDefeated => _initialPatrolDefeated;

        /// <summary>Fires once, the frame <see cref="IsInitialPatrolDefeated"/>
        /// flips from false to true.</summary>
        public event System.Action InitialPatrolDefeated;

        /// <summary>True when the local player's party is the ambusher side
        /// (the player launched the ambush). False when the player is the
        /// victim (an AI party ambushed the player). Drives the deployment-UI
        /// gate — only the ambusher gets a placement screen; the victim is
        /// dropped straight into combat.</summary>
        public bool IsPlayerAmbusher
            => _ambusherParty != null
               && _ambusherParty.MobileParty == MobileParty.MainParty;

        /// <summary>
        /// Optional shared reference to the attacker-side troop supplier
        /// vanilla's DefaultBattleMissionAgentSpawnLogic also holds. When set,
        /// AfterStart drains it and spawns the ambusher roster manually at
        /// AmbushFormation chunk positions, so vanilla's SetupTeams runs
        /// against an empty supplier and places no agents at (0, 0, 0).
        /// Null for player-victim missions (we don't take over the spawn
        /// pipeline there).
        /// </summary>
        private readonly IMissionTroopSupplier _ambusherSupplier;
        private readonly IMissionTroopSupplier _victimSupplier;

        public AmbushMissionController(PartyBase ambusherParty, PartyBase victimParty,
                                        IMissionTroopSupplier ambusherSupplier = null,
                                        IMissionTroopSupplier victimSupplier = null)
        {
            _ambusherParty = ambusherParty;
            _victimParty = victimParty;
            _ambusherSupplier = ambusherSupplier;
            _victimSupplier = victimSupplier;
        }

        public override void OnBehaviorInitialize()
        {
            base.OnBehaviorInitialize();
            // DO NOT call AmbushRetreatZone.ResetRegistry() here. Scene scripts
            // (including AmbushRetreatZone.OnInit) fire during scene load, which
            // happens before MissionLogic.OnBehaviorInitialize. Clearing the
            // registry at this point wipes the zone that just registered —
            // every subsequent IsAgentInsideAnyZone check falls through to the
            // procedural fallback. Reset is done by the campaign behavior
            // immediately before MissionState.OpenNew so stale zones from any
            // prior scene are cleared BEFORE this scene's scripts fire.
            _spawnLogic = Mission.GetMissionBehavior<DefaultBattleMissionAgentSpawnLogic>();
            _model = Campaign.Current?.Models?.GetMissionAmbushModel() ?? new TORMissionAmbushModel();
            AmbushRetreatZone.ProceduralFallbackRadius = _model.ProceduralFallbackRadius;

            // If the player is the victim (AI ambushed the player), there's no
            // placement choice — the mission kicks straight into combat. Keep
            // the vanilla deployment controller (it owns agent spawn kickoff
            // + player-agent handoff — reimplementing that broke the mission
            // in interesting ways: loading-screen hang when fully bypassed,
            // spectator view when half-done). Subscribe to its OnAfterSetupTeams
            // event instead: it fires on the first mission tick after the
            // scene is ready, which is exactly when vanilla has done the
            // per-side SetSpawnTroops + SetupAgentAIStates work and is just
            // waiting for the player to click Confirm. Call FinishDeployment
            // from there and the mission transitions as if Confirm had been
            // pressed.
            // If the player is the ambusher, vanilla's auto-spawn drops every
            // attacker at (0, 0, 0) because the new scene uses sp_ambush_attacker_point
            // entities instead of vanilla's sp_attacker_<class> names. Rather than
            // chase the scene-side fix across every ambush scene we end up
            // shipping, we intercept the deployment-controller's OnAfterSetupTeams
            // (fires once per mission, after both sides' setup, before the first
            // spawn tick), disable vanilla's attacker spawn there, and spawn our
            // own agents at the AmbushFormation chunk positions. Decoupling us
            // from vanilla's spawn pipeline is also what lets the pending
            // "player ambushes with a subset of the party" feature land cleanly
            // later: we already own the roster iteration, we can filter it.
            // Ambusher custom spawn is driven by a Harmony prefix on
            // BattleDeploymentMissionController.OnSetupTeamsOfSide, not by an
            // event subscription here. See AmbusherCustomSpawnPatch. We
            // tried OnAfterSetupTeams first; it fires one tick after the
            // bad (0, 0, 0) spawn, too late to help.

            if (!IsPlayerAmbusher)
            {
                var deploymentController =
                    Mission.GetMissionBehavior<BattleDeploymentMissionController>();
                if (deploymentController != null)
                {
                    deploymentController.OnAfterSetupTeams += AutoFinishDeploymentForVictim;
                }
            }
        }

        /// <summary>
        /// Disables vanilla's attacker auto-spawn (which places agents at
        /// (0, 0, 0) because the scene lacks vanilla sp_attacker_<class>
        /// entities) and spawns the ambusher roster ourselves at each
        /// AmbushFormation chunk's AssignedSpot. Called once per mission
        /// via BattleDeploymentMissionController.OnAfterSetupTeams, which
        /// fires after per-side team setup but before the first spawn
        /// tick, giving us a clean window to take over.
        /// </summary>
        private void DoCustomAmbusherSpawn()
        {
            TORCommon.Log(LogLevel.Info, "[AmbushMission] DoCustomAmbusherSpawn entry");
            if (Layout == null || SceneSpots == null || SceneSpots.Count == 0)
            {
                TORCommon.Log(LogLevel.Warn,
                    "[AmbushMission] DoCustomAmbusherSpawn skipped: no layout/spots");
                return;
            }
            if (_ambusherSupplier == null)
            {
                throw new AmbushSupplierMissingException(
                    "_ambusherSupplier is null in DoCustomAmbusherSpawn. "
                    + "Mission opener must pass the shared PartyGroupTroopSupplier "
                    + "instance to the controller.");
            }

            // Drain the same supplier instance vanilla's spawn logic holds, so
            // vanilla's later SetSpawnTroops(..., enforceSpawning: true) has
            // nothing to allocate and places no agents at (0, 0, 0).
            var origins = _ambusherSupplier.SupplyTroops(int.MaxValue).ToList();
            bool isPlayerSide = _ambusherSide == BattleSideEnum.Attacker
                                && IsPlayerAmbusher;
            _customSpawnInProgress = true;

            // Group by FormationClass so a class's agents spawn as one
            // line (or grid when the spot has a MaxWidth budget) instead
            // of all piling onto spot.Position.
            var agentsByClass = new Dictionary<FormationClass, List<IAgentOriginBase>>();
            foreach (var origin in origins)
            {
                var formationClass = origin.Troop.GetFormationClass();
                if (!agentsByClass.TryGetValue(formationClass, out var list))
                {
                    list = new List<IAgentOriginBase>();
                    agentsByClass[formationClass] = list;
                }
                list.Add(origin);
            }

            int spawned = 0;
            try
            {
                foreach (var classGroup in agentsByClass)
                {
                    var formationClass = classGroup.Key;
                    var agents = classGroup.Value;
                    var spot = ChooseSpotForClass(formationClass);
                    if (!spot.HasValue) continue;
                    var facing = FacingFromYaw(spot.Value.Yaw);
                    var perp = new Vec2(-facing.Y, facing.X);
                    int cols = ColumnsForLayout(agents.Count, spot.Value.MaxWidth);
                    Formation chunkFormation = null;

                    for (int i = 0; i < agents.Count; i++)
                    {
                        var origin = agents[i];
                        var pos = GridPositionFor(spot.Value.Position, facing, perp,
                                                  i, agents.Count, cols);

                        var buildData = Mission.GetAgentBuildDataToSpawnTroop(
                            origin,
                            isPlayerSide: isPlayerSide,
                            hasFormation: true,
                            spawnWithHorse: true,
                            isReinforcement: false,
                            formationTroopCount: 1,
                            formationTroopIndex: 0,
                            initialPosition: pos,
                            initialDirection: facing,
                            formationIndex: formationClass);

                        bool isPlayerChar = origin.Troop == CharacterObject.PlayerCharacter;
                        if (origin.Troop.IsHero)
                        {
                            buildData.FixedEquipment(true);
                            if (isPlayerChar)
                                buildData.Controller(AgentControllerType.Player);
                        }

                        var agent = Mission.SpawnAgent(buildData, false);
                        BumpSpawnedCount(_ambusherSide);
                        if (chunkFormation == null) chunkFormation = agent.Formation;
                        if (isPlayerChar)
                        {
                            TORCommon.Log(LogLevel.Info,
                                "[AmbushMission] player spawned ctrl=" + agent.Controller
                                + " initialPlayerAgent set=" + (Mission.InitialPlayerAgent != null));
                        }
                        spawned++;
                    }

                    TryAnchorRangedFormation(chunkFormation, formationClass, spot.Value);
                }
            }
            finally
            {
                _customSpawnInProgress = false;
            }
            TORCommon.Log(LogLevel.Info,
                "[AmbushMission] DoCustomAmbusherSpawn: spawned=" + spawned
                + "/" + origins.Count);
        }

        // Shared helpers so attacker + defender spawns use the same
        // line-/grid-layout math. Agents form a single row along the
        // perpendicular to the spot's facing; when the spot carries a
        // MaxWidth budget we wrap into additional rows that stack
        // backward along the facing (behind the front row).
        private const float AmbushSpawnUnitSpacing = 1.5f;

        private static Vec2 FacingFromYaw(float yaw)
        {
            var f = new Vec2((float)System.Math.Cos(yaw),
                             (float)System.Math.Sin(yaw));
            return f.LengthSquared > 0.0001f ? f : new Vec2(0f, 1f);
        }

        private static int ColumnsForLayout(int groupCount, float maxWidth)
        {
            if (groupCount <= 0) return 1;
            if (maxWidth <= 0.001f) return groupCount;
            int cols = (int)(maxWidth / AmbushSpawnUnitSpacing);
            return System.Math.Max(1, System.Math.Min(groupCount, cols));
        }

        private static Vec3 GridPositionFor(Vec3 center, Vec2 facing, Vec2 perp,
                                            int index, int groupCount, int cols)
        {
            // Temporarily disabled: per-agent lateral offsets occasionally
            // land off the scene's navmesh (cliffs, water, etc). The engine
            // tolerates that at spawn time but hard-crashes during the
            // deployment -> battle mode switch when it tries to initialize
            // pathfinding from the invalid frame. Return the raw spot
            // center so all troops stack at one point (the pre-refactor
            // behaviour); re-enable once we clamp lateral/depth to the
            // navmesh via Mission.DeploymentPlan.ProjectPositionToDeploymentBoundaries.
            _ = facing;
            _ = perp;
            _ = index;
            _ = groupCount;
            _ = cols;
            return center;
        }

        /// <summary>
        /// Attaches a tactical-point anchor to a ranged formation so the
        /// captain AI pins it at the spawn-point (parent) and the
        /// individual archers scripted-position onto the tactical-child
        /// entities authored under that spawn-point. Does nothing for
        /// non-ranged classes or when the spot carries no tactical
        /// children. Player override is handled by
        /// <see cref="AmbushTacticalAnchorLogic"/>.
        /// </summary>
        private void TryAnchorRangedFormation(Formation formation,
                                               FormationClass cls,
                                               AmbushSpot spot)
        {
            int tac = spot.TacticalChildren?.Count ?? 0;
            bool isRanged = cls == FormationClass.Ranged
                            || cls == FormationClass.Skirmisher
                            || cls == FormationClass.HorseArcher;
            if (formation == null || !isRanged || tac == 0)
            {
                TORCommon.Log(LogLevel.Info,
                    "[AmbushMission] anchor skipped class=" + cls
                    + " formationNull=" + (formation == null)
                    + " isRanged=" + isRanged
                    + " tacticalChildren=" + tac
                    + " spot=" + spot.GroupTag);
                return;
            }
            var anchorLogic = Mission.GetMissionBehavior<AmbushTacticalAnchorLogic>();
            if (anchorLogic == null)
            {
                TORCommon.Log(LogLevel.Warn,
                    "[AmbushMission] anchor skipped: AmbushTacticalAnchorLogic missing from mission");
                return;
            }
            var scene = Mission.Scene;
            if (scene == null) return;
            var parent = new WorldPosition(scene, spot.Position);
            var children = spot.TacticalChildren
                .Select(c => new WorldPosition(scene, c))
                .ToList();
            var behavior = new AmbushBehaviorDefendTacticalPoint(
                formation, parent, children,
                spot.ActivationRadius, spot.ActivationIgnoreHeight);
            anchorLogic.RegisterAnchor(formation, behavior);
            TORCommon.Log(LogLevel.Info,
                "[AmbushMission] anchored " + cls + " to " + children.Count
                + " tactical points at spot " + spot.GroupTag);
        }

        /// <summary>
        /// Called by the sidebar spot-picker after a formation is
        /// reassigned to a new spot. Rebinds the formation's tactical
        /// anchor to the new spot's children, or unregisters the anchor
        /// entirely if the new spot has none. Keeps the pinned state in
        /// sync with the user's UI choice.
        /// </summary>
        public void OnFormationSpotReassigned(Formation formation,
                                               FormationClass cls,
                                               AmbushSpot newSpot)
        {
            if (formation == null) return;
            var anchorLogic = Mission.GetMissionBehavior<AmbushTacticalAnchorLogic>();
            if (anchorLogic == null) return;
            bool isRanged = cls == FormationClass.Ranged
                            || cls == FormationClass.Skirmisher
                            || cls == FormationClass.HorseArcher;
            int tac = newSpot.TacticalChildren?.Count ?? 0;
            if (!isRanged || tac == 0)
            {
                anchorLogic.UnregisterAnchor(formation);
                TORCommon.Log(LogLevel.Info,
                    "[AmbushMission] anchor cleared for " + cls
                    + " on reassignment to spot " + newSpot.GroupTag);
                return;
            }
            var scene = Mission.Scene;
            if (scene == null) return;
            var parent = new WorldPosition(scene, newSpot.Position);
            var children = newSpot.TacticalChildren
                .Select(c => new WorldPosition(scene, c))
                .ToList();
            var behavior = new AmbushBehaviorDefendTacticalPoint(
                formation, parent, children,
                newSpot.ActivationRadius, newSpot.ActivationIgnoreHeight);
            anchorLogic.RebindAnchor(formation, behavior);
            TORCommon.Log(LogLevel.Info,
                "[AmbushMission] anchor rebound " + cls + " to " + children.Count
                + " tactical points at spot " + newSpot.GroupTag);
        }

        /// <summary>
        /// Resets every chunked formation back to its initial layout spot
        /// (the <see cref="AmbushLayoutResult.DefaultAssignments"/> dict
        /// built once at <c>AfterStart</c>), then repositions every spot
        /// that gained or lost occupants so the troop plates follow the
        /// reassignment. Wired to the vanilla "Reset Deployment" button
        /// via <see cref="HarmonyPatches.AmbushResetDeploymentPatch"/>.
        /// Safe to call during the deployment phase only; after
        /// <c>OnDeploymentFinished</c> the formations are already in
        /// combat and the player has no business resetting.
        /// </summary>
        public void ResetAssignments()
        {
            if (Layout == null || Layout.Formations == null) return;
            if (Mission?.Mode != TaleWorlds.Core.MissionMode.Deployment)
            {
                TORCommon.Log(LogLevel.Info,
                    "[AmbushMission] ResetAssignments ignored outside Deployment mode");
                return;
            }
            var touched = new HashSet<Vec3>();
            foreach (var f in Layout.Formations)
            {
                if (f.AssignedSpot.HasValue) touched.Add(f.AssignedSpot.Value.Position);
                if (Layout.DefaultAssignments != null
                    && Layout.DefaultAssignments.TryGetValue(f, out var def))
                {
                    f.AssignedSpot = def;
                    touched.Add(def.Position);
                }
            }
            // Deduplicate by position; reposition once per spot so the
            // share-spot side-by-side logic runs with the final occupants.
            foreach (var pos in touched)
            {
                AmbushSpot? spot = null;
                foreach (var s in SceneSpots)
                {
                    if (s.Position.NearlyEquals(pos, 0.01f)) { spot = s; break; }
                }
                if (spot.HasValue)
                    UI.AmbushFormationPanelExtension.RepositionSpotGroup(spot.Value, this);
            }
            UI.AmbushFormationPanelExtension.BumpMoveGeneration();
            TORCommon.Log(LogLevel.Info,
                "[AmbushMission] ResetAssignments restored " + Layout.Formations.Count
                + " formations across " + touched.Count + " spots");
        }

        /// <summary>
        /// First AmbushFormation chunk whose Class matches the requested
        /// class; falls back to SceneSpots[0] if the layout has nothing
        /// bound to the class so a troop never lands at (0, 0, 0).
        /// </summary>
        private AmbushSpot? ChooseSpotForClass(FormationClass cls)
        {
            if (Layout?.Formations != null)
            {
                foreach (var f in Layout.Formations)
                {
                    if (f.Class == cls && f.AssignedSpot.HasValue)
                        return f.AssignedSpot.Value;
                }
            }
            return SceneSpots.Count > 0 ? SceneSpots[0] : (AmbushSpot?)null;
        }

        private void AutoFinishDeploymentForVictim()
        {
            var deploymentController =
                Mission.GetMissionBehavior<BattleDeploymentMissionController>();
            if (deploymentController == null) return;
            deploymentController.OnAfterSetupTeams -= AutoFinishDeploymentForVictim;
            deploymentController.FinishDeployment();
        }

        public override void AfterStart()
        {
            base.AfterStart();
            // Resolve each party's battle side from its MapEventSide. Both
            // parties are expected to be on the PlayerMapEvent at this point
            // (set up by the opener via PlayerEncounter.RestartPlayerEncounter).
            _ambusherSide = _ambusherParty?.MapEventSide?.MissionSide ?? BattleSideEnum.Attacker;
            _victimSide = _victimParty?.MapEventSide?.MissionSide ?? BattleSideEnum.Defender;
            SetupInitialSpawn();
            CollectAttackerSpawnPositions();
            // Custom spawn: drain the shared suppliers and place both sides
            // ourselves before vanilla's first OnMissionTick fires its
            // SetupTeams -> OnSetupTeamsOfSide -> SetSpawnTroops(..
            // enforceSpawning: true), which synchronously spawns from the
            // same supplier. By draining here, vanilla finds nothing left
            // and never drops agents at (0, 0, 0). The walkable-area check
            // hangs the loading screen on that point; this is the "(0,0,0)
            // hang" referenced in feedback_bannerlord_initial_frame_hang.
            //
            // Runs for both Phase D (player ambusher) and Phase F (AI
            // ambusher). Phase F has no deployment UI either way;
            // AutoFinishDeploymentForVictim fires FinishDeployment on
            // OnAfterSetupTeams so the battle drops straight into combat
            // with agents already placed at the ambush spots.
            //
            // No null-guards around the suppliers. The mission opener is
            // responsible for passing shared instances. A missing supplier
            // is a bug we want surfaced, not silently skipped (silent skip
            // is how Phase F's (0, 0, 0) loading-screen hang went unnoticed
            // until the AI-ambush playtest). The catch is narrow: only our
            // own AmbushSupplierMissingException lands here, with a context
            // log then rethrow. Any other exception propagates with its
            // own stack trace untouched.
            try
            {
                DoCustomAmbusherSpawn();
                DoCustomVictimSpawn();
            }
            catch (AmbushSupplierMissingException e)
            {
                TORCommon.Log(LogLevel.Error,
                    "[AmbushMission] supplier contract violated: isPlayerAmbusher="
                    + IsPlayerAmbusher
                    + " ambusherSupplier=" + (_ambusherSupplier == null ? "null" : "set")
                    + " victimSupplier=" + (_victimSupplier == null ? "null" : "set")
                    + ". " + e.Message);
                throw;
            }

            // Register the ambush tactic on the ambusher team's TeamAI.
            // Ambusher = Attacker in both phases. Does not short-circuit
            // player control: if the player is the ambusher, the tactic
            // is on the player's team but FormationAI only ticks on
            // formations the player has flipped to captain control
            // (F6-delegated). Direct-controlled player formations
            // ignore the tactic's behavior entirely.
            var attacker = Mission.AttackerTeam;
            if (attacker != null)
            {
                attacker.AddTacticOption(new AmbushTacticHoldPositions(attacker));
                TORCommon.Log(LogLevel.Info,
                    "[AmbushMission] registered AmbushTacticHoldPositions on AttackerTeam");
            }
        }

        /// <summary>
        /// Cache the reflection handles once in AfterStart so BumpSpawnedCount
        /// doesn't do dictionary lookups per agent. Both handles may legitimately
        /// resolve to null (null-tolerant game versions) — BumpSpawnedCount
        /// no-ops in that case and the mission runs as before (same stall risk
        /// but no new failure mode).
        /// </summary>
        private void InitSpawnContextReflection()
        {
            if (_spawnLogic == null) return;
            var contextsField = typeof(DefaultBattleMissionAgentSpawnLogic).GetField(
                "_battleSideSpawnContexts",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            _battleSideSpawnContextsArray = contextsField?.GetValue(_spawnLogic) as System.Array;
            if (_battleSideSpawnContextsArray == null || _battleSideSpawnContextsArray.Length == 0) return;
            var sampleContext = _battleSideSpawnContextsArray.GetValue(0);
            if (sampleContext == null) return;
            _numSpawnedTroopsField = sampleContext.GetType().GetField(
                "_numSpawnedTroops",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        }

        /// <summary>
        /// Match what vanilla does at MissionBattleSideSpawnContext.SpawnTroops
        /// line 386: <c>_numSpawnedTroops++</c>. Called per agent in DoCustom
        /// *Spawn so the context's NumberOfActiveTroops reflects reality.
        /// </summary>
        private void BumpSpawnedCount(BattleSideEnum side)
        {
            if (_battleSideSpawnContextsArray == null || _numSpawnedTroopsField == null) return;
            int idx = (int)side;
            if (idx < 0 || idx >= _battleSideSpawnContextsArray.Length) return;
            var ctx = _battleSideSpawnContextsArray.GetValue(idx);
            if (ctx == null) return;
            int current = (int)_numSpawnedTroopsField.GetValue(ctx);
            _numSpawnedTroopsField.SetValue(ctx, current + 1);
        }

        /// <summary>
        /// Vanilla's defender spawn occasionally drops specific troops (seen
        /// with Ungor Raider in the test scene) at (0, 0, 0), which hangs
        /// the walkable-area check on the deployment screen. The scene has
        /// valid sp_defender_* entities, so we place the entire victim
        /// roster at their world positions ourselves, same strategy as the
        /// ambusher spawn. If the supplier isn't shared or the scene lacks
        /// sp_defender_&lt;class&gt; entities we log and skip, letting
        /// vanilla try.
        /// </summary>
        private void DoCustomVictimSpawn()
        {
            TORCommon.Log(LogLevel.Info, "[AmbushMission] DoCustomVictimSpawn entry");
            if (_victimSupplier == null)
            {
                throw new AmbushSupplierMissingException(
                    "_victimSupplier is null in DoCustomVictimSpawn. "
                    + "Mission opener must pass the shared PartyGroupTroopSupplier "
                    + "instance to the controller.");
            }
            var spots = CollectDefenderSceneSpots();
            if (spots.Count == 0)
            {
                TORCommon.Log(LogLevel.Warn,
                    "[AmbushMission] DoCustomVictimSpawn skipped: no sp_defender_* entities in scene");
                return;
            }
            var origins = _victimSupplier.SupplyTroops(int.MaxValue).ToList();
            // In Phase F (AI ambusher) the victim IS the player. Their
            // agents go on the player side and the player character gets
            // AgentControllerType.Player. In Phase D the victim is AI, so
            // isPlayerSide is false and no player character sits here.
            bool isPlayerSide = !IsPlayerAmbusher;
            // Scener-authored camera anchor. Only used in Phase F to
            // override the player hero's spawn position so the vanilla
            // camera init lands on a sensible overlook of the engagement
            // instead of snapping across the scene.
            Vec3 anchorPos = Vec3.Zero;
            Vec2 anchorFacing = new Vec2(0f, 1f);
            bool hasAnchor = !IsPlayerAmbusher
                             && TryGetCameraAnchor(out anchorPos, out anchorFacing);
            _customSpawnInProgress = true;

            // Same grid layout as the ambusher side, grouped per
            // FormationClass. Position + Yaw read off each defender
            // scene entity so the line faces the direction the scener
            // oriented the marker (not a hard-coded +X).
            var agentsByClass = new Dictionary<FormationClass, List<IAgentOriginBase>>();
            foreach (var origin in origins)
            {
                var formationClass = origin.Troop.GetFormationClass();
                if (!agentsByClass.TryGetValue(formationClass, out var list))
                {
                    list = new List<IAgentOriginBase>();
                    agentsByClass[formationClass] = list;
                }
                list.Add(origin);
            }

            int spawned = 0;
            try
            {
                foreach (var classGroup in agentsByClass)
                {
                    var formationClass = classGroup.Key;
                    var agents = classGroup.Value;
                    var slot = spots.TryGetValue(formationClass, out var s) ? s
                             : spots.Values.First();
                    var center = slot.Position;
                    var facing = FacingFromYaw(slot.Yaw);
                    var perp = new Vec2(-facing.Y, facing.X);
                    // Victim entities carry no MaxWidth script yet; keep
                    // them in a single line (cols = groupCount).
                    int cols = ColumnsForLayout(agents.Count, 0f);

                    for (int i = 0; i < agents.Count; i++)
                    {
                        var origin = agents[i];
                        bool isPlayerChar = origin.Troop == CharacterObject.PlayerCharacter;
                        Vec3 spawnPos;
                        Vec2 spawnDir;
                        if (isPlayerChar && hasAnchor)
                        {
                            spawnPos = anchorPos;
                            spawnDir = anchorFacing;
                        }
                        else
                        {
                            spawnPos = GridPositionFor(center, facing, perp,
                                                      i, agents.Count, cols);
                            spawnDir = facing;
                        }
                        var buildData = Mission.GetAgentBuildDataToSpawnTroop(
                            origin,
                            isPlayerSide: isPlayerSide,
                            hasFormation: true,
                            spawnWithHorse: true,
                            isReinforcement: false,
                            formationTroopCount: 1,
                            formationTroopIndex: 0,
                            initialPosition: spawnPos,
                            initialDirection: spawnDir,
                            formationIndex: formationClass);
                        if (origin.Troop.IsHero)
                        {
                            buildData.FixedEquipment(true);
                            if (isPlayerChar)
                                buildData.Controller(AgentControllerType.Player);
                        }
                        var agent = Mission.SpawnAgent(buildData, false);
                        BumpSpawnedCount(_victimSide);
                        if (isPlayerChar)
                        {
                            TORCommon.Log(LogLevel.Info,
                                "[AmbushMission] player spawned as victim ctrl=" + agent.Controller
                                + " initialPlayerAgent set=" + (Mission.InitialPlayerAgent != null));
                        }
                        spawned++;
                    }
                }
            }
            finally
            {
                _customSpawnInProgress = false;
            }
            TORCommon.Log(LogLevel.Info,
                "[AmbushMission] DoCustomVictimSpawn: spawned=" + spawned
                + "/" + origins.Count);
        }

        /// <summary>
        /// Looks up the scener-authored <c>ambush_camera_anchor</c>
        /// entity and returns its world position + forward direction.
        /// Returns false when the scene has no such entity (older
        /// ambush scenes without the convention still work; they just
        /// miss the camera-anchor polish).
        ///
        /// Dual use:
        /// Phase F spawns the player hero at this frame so the vanilla
        /// camera init lands on the engagement instead of panning
        /// across the scene from (0, 0, 0) to the victim column.
        /// Phase D's <c>AmbushDeploymentMissionView</c> re-aims the
        /// deployment camera here so the ambusher starts looking at
        /// the kill zone instead of a scene corner.
        /// </summary>
        public bool TryGetCameraAnchor(out Vec3 position, out Vec2 facing)
        {
            position = Vec3.Zero;
            facing = new Vec2(0f, 1f);
            if (Mission?.Scene == null) return false;
            foreach (var e in Mission.Scene.FindEntitiesWithTag("ambush_camera_anchor"))
            {
                if (e == null) continue;
                var frame = e.GetGlobalFrame();
                position = frame.origin;
                facing = frame.rotation.f.AsVec2.Normalized();
                return true;
            }
            return false;
        }

        private Dictionary<FormationClass, (Vec3 Position, float Yaw)> CollectDefenderSceneSpots()
        {
            // Vanilla defender entities carry tags without the sp_ prefix
            // (the sp_ is only in the entity name). Archer entity carries
            // both `defender_archer` and `defender_ranged` so FormationClass.Ranged
            // can be found via either. Yaw is read off the entity's global
            // frame rotation so the defender troops line up facing the
            // direction the scener placed the marker, instead of a hard-
            // coded +X default.
            var dict = new Dictionary<FormationClass, (Vec3, float)>();
            TryAdd(dict, FormationClass.Infantry,    "defender_infantry");
            TryAdd(dict, FormationClass.Ranged,      "defender_ranged");
            if (!dict.ContainsKey(FormationClass.Ranged))
                TryAdd(dict, FormationClass.Ranged,  "defender_archer");
            TryAdd(dict, FormationClass.Cavalry,     "defender_cavalry");
            TryAdd(dict, FormationClass.HorseArcher, "defender_horsearcher");
            return dict;
        }

        private void TryAdd(Dictionary<FormationClass, (Vec3, float)> dict,
                            FormationClass cls, string tag)
        {
            foreach (var e in Mission.Scene.FindEntitiesWithTag(tag))
            {
                var frame = e.GetGlobalFrame();
                dict[cls] = (frame.origin, frame.rotation.f.AsVec2.RotationInRadians);
                return;
            }
        }

        /// <summary>
        /// Collects attacker spot entities from the scene (ambush_attacker_*
        /// tags), runs <see cref="AmbushFormationLayout.Compute"/> against
        /// the ambusher roster, and builds per-class flat teleport lists that
        /// <see cref="OnAgentCreated"/> consumes. Scener convention (short
        /// tags): attacker_infantry / attacker_archer / attacker_cavalry /
        /// attacker_horsearcher. If none exist, falls back to the legacy
        /// flat <c>ambush_attacker</c> tag with no layout computation.
        /// </summary>
        private void CollectAttackerSpawnPositions()
        {
            _attackerSpawnPositions.Clear();
            _attackerSpawnYaws.Clear();
            _attackerSpawnIndex.Clear();
            _legacyAttackerSpawnPositions.Clear();
            _legacyAttackerSpawnIndex = 0;
            Layout = null;
            SceneSpots.Clear();
            SpotNumbers.Clear();

            var spots = CollectAmbushSpots();
            SceneSpots.AddRange(spots);
            int number = 1;
            foreach (var s in spots)
            {
                if (!SpotNumbers.ContainsKey(s.Position))
                {
                    SpotNumbers[s.Position] = number;
                    number++;
                }
            }
            if (spots.Count == 0)
            {
                _legacyAttackerSpawnPositions.AddRange(CollectPositionsForTag("ambush_attacker"));
                Debug.Print(
                    "[AmbushMission] no typed attacker spots, legacy="
                    + _legacyAttackerSpawnPositions.Count);
                return;
            }

            var roster = _ambusherParty?.MemberRoster;
            if (roster == null || roster.Count == 0)
            {
                Debug.Print("[AmbushMission] no ambusher roster, skipping layout");
                return;
            }

            var mode = IsPlayerAmbusher ? LayoutMode.Player : LayoutMode.AI;
            Layout = AmbushFormationLayout.Compute(roster, spots, mode, _model);

            foreach (var chunk in Layout.Formations)
            {
                if (!chunk.AssignedSpot.HasValue) continue;
                var cls = chunk.Class;
                if (!_attackerSpawnPositions.ContainsKey(cls))
                {
                    _attackerSpawnPositions[cls] = new List<Vec3>();
                    _attackerSpawnYaws[cls] = new List<float>();
                    _attackerSpawnIndex[cls] = 0;
                }
                var spot = chunk.AssignedSpot.Value;
                for (int i = 0; i < chunk.Count; i++)
                {
                    _attackerSpawnPositions[cls].Add(spot.Position);
                    _attackerSpawnYaws[cls].Add(spot.Yaw);
                }
            }

            Debug.Print(
                "[AmbushMission] layout: " + Layout.Formations.Count + " formations, "
                + "spots found=" + spots.Count + " mode=" + mode);
            for (int i = 0; i < SceneSpots.Count; i++)
            {
                var s = SceneSpots[i];
                int tac = s.TacticalChildren?.Count ?? 0;
                TORCommon.Log(LogLevel.Info,
                    "[AmbushMission] SceneSpots[" + i + "] group=" + s.GroupTag
                    + " pos=" + s.Position
                    + " preferredClass=" + s.PreferredClass
                    + " tacticalChildren=" + tac);
            }
            foreach (var kv in _attackerSpawnPositions)
                TORCommon.Log(LogLevel.Info,
                    "[AmbushMission] _attackerSpawnPositions[" + kv.Key + "] count=" + kv.Value.Count
                    + " first=" + (kv.Value.Count > 0 ? kv.Value[0].ToString() : "n/a"));
        }

        /// <summary>
        /// Reads every entity tagged <c>attacker_ambush_spawnpoint</c>; each
        /// is paired with one group tag (e.g. <c>attacker_ambush_hill</c>)
        /// that drives the picker grouping and the chip's localized label.
        /// The group tag is whichever remaining tag on the entity carries
        /// the <c>attacker_ambush_</c> prefix. Entities missing a group tag
        /// are logged and dropped so the picker stays usable.
        /// </summary>
        private List<AmbushSpot> CollectAmbushSpots()
        {
            const string MarkerTag = "attacker_ambush_spawnpoint";
            const string GroupPrefix = "attacker_ambush_";
            var list = new List<AmbushSpot>();
            foreach (var e in Mission.Scene.FindEntitiesWithTag(MarkerTag))
            {
                string groupTag = null;
                foreach (var t in e.Tags)
                {
                    if (string.IsNullOrEmpty(t) || t == MarkerTag) continue;
                    if (t.StartsWith(GroupPrefix))
                    {
                        groupTag = t;
                        break;
                    }
                }
                if (groupTag == null) continue;
                var frame = e.GetGlobalFrame();
                var script = e.GetFirstScriptOfType<AmbushSpawnPointScript>();
                var tacticalChildren = new List<Vec3>();
                int totalChildren = 0;
                foreach (var child in e.GetChildren())
                {
                    if (child == null) continue;
                    totalChildren++;
                    // Accept either a vanilla TacticalPosition or StrategicArea
                    // script. The test scene uses StrategicArea prefabs
                    // (strategic_archer_point); either is valid authoring.
                    bool hasTactical = child.HasScriptOfType<TaleWorlds.MountAndBlade.TacticalPosition>();
                    bool hasStrategic = child.HasScriptOfType<TaleWorlds.MountAndBlade.StrategicArea>();
                    bool isTacticalChild = hasTactical || hasStrategic;
                    TORCommon.Log(LogLevel.Info,
                        "[AmbushMission] spot " + groupTag + " child name=" + child.Name
                        + " tactical=" + hasTactical
                        + " strategic=" + hasStrategic
                        + " pos=" + child.GlobalPosition);
                    if (isTacticalChild) tacticalChildren.Add(child.GlobalPosition);
                }
                if (totalChildren == 0)
                    TORCommon.Log(LogLevel.Info,
                        "[AmbushMission] spot " + groupTag + " has no child entities");
                var zone = e.GetFirstScriptOfType<AmbushTacticalActivationZone>();
                list.Add(new AmbushSpot
                {
                    GroupTag = groupTag,
                    Position = frame.origin,
                    Yaw = frame.rotation.f.AsVec2.RotationInRadians,
                    MaxFormations = script?.MaxFormations ?? 0,
                    MaxWidth = script?.MaxWidth ?? 0f,
                    PreferredClass = (int)(script?.PreferredClass ?? AmbushSpotPreferredClass.None),
                    TacticalChildren = tacticalChildren,
                    ActivationRadius = zone?.Radius ?? 30f,
                    ActivationIgnoreHeight = zone?.IgnoreHeight ?? false,
                });
            }
            return list;
        }

        private List<Vec3> CollectPositionsForTag(string tag)
        {
            var list = new List<Vec3>();
            foreach (var e in Mission.Scene.FindEntitiesWithTag(tag))
                list.Add(e.GetGlobalFrame().origin);
            return list;
        }

        /// <summary>
        /// Fires per agent at creation time. For attacker humans we route to
        /// a role-matching scener-tagged slot; for defenders we don't touch
        /// anything (vanilla sp_defender_* placement is the authored default
        /// now, no more `dynamic_patrol_area_tag` teleport).
        /// </summary>
        public override void OnAgentCreated(Agent agent)
        {
            base.OnAgentCreated(agent);
            if (agent == null || !agent.IsHuman) return;

            // Our shared-supplier custom spawn runs Mission.SpawnAgent
            // with AgentBuildData.InitialPosition pre-set to the grid
            // coordinates we want; teleporting the agent here would snap
            // every troop of the same class onto one point and undo the
            // line/grid layout. Skip the safety-net teleport while we're
            // the ones doing the spawning. The teleport still runs for
            // any vanilla-triggered spawn later (reinforcement etc.)
            // so the (0, 0, 0) walkable-area hang stays covered.
            if (_customSpawnInProgress) return;

            // Note: Agent.Position at OnAgentCreated time is (0, 0, 0)
            // even for agents vanilla is about to position via AgentBuildData.
            // The engine's "Agent initial frame is out of walkable area"
            // warning fires for the same transient reason. Do not attempt
            // to "fix" the position here; positioning happens after this
            // callback returns.

            // Only reject agents we know are on the WRONG side. A null team
            // (sometimes the player hero at first-tick) still gets our
            // teleport so they don't drop at (0, 0, 0) and hang the walkable
            // area check.
            if (agent.Team != null && agent.Team.Side != _ambusherSide) return;

            TORCommon.Log(LogLevel.Info,
                "[AmbushMission] OnAgentCreated char=" + (agent.Character?.StringId ?? "?")
                + " class=" + (agent.Character?.DefaultFormationClass.ToString() ?? "?")
                + " team=" + (agent.Team == null ? "null" : agent.Team.Side.ToString()));

            var formationClass = agent.Character?.DefaultFormationClass ?? FormationClass.Infantry;
            // Flag the engine so its walkable-area check waits for our
            // teleport before firing. Without this, the check runs on the
            // agent's (0, 0, 0) initial frame and hangs the deployment.
            bool prevTeleport = Mission.IsTeleportingAgents;
            Mission.IsTeleportingAgents = true;
            try
            {
                if (_attackerSpawnPositions.TryGetValue(formationClass, out var positions)
                    && positions.Count > 0)
                {
                    int idx = _attackerSpawnIndex[formationClass]++;
                    int at = idx % positions.Count;
                    TORCommon.Log(LogLevel.Info,
                        "[AmbushMission] teleport class=" + formationClass
                        + " idx=" + at + "/" + positions.Count + " to " + positions[at]);
                    agent.TeleportToPosition(positions[at]);
                    if (_attackerSpawnYaws.TryGetValue(formationClass, out var yaws)
                        && at < yaws.Count)
                    {
                        float yaw = yaws[at];
                        var dir = new Vec2((float)System.Math.Cos(yaw), (float)System.Math.Sin(yaw));
                        agent.SetMovementDirection(in dir);
                    }
                    return;
                }
                TORCommon.Log(LogLevel.Info,
                    "[AmbushMission] NO class match for " + formationClass
                    + " falling back to SceneSpots");

            // Class has no bound chunk (e.g. a hero whose FormationClass
            // doesn't match any troop class in the roster, so autochunk
            // didn't produce a chunk for it). Drop the agent at any spot
            // the scene has rather than let vanilla spawn them at (0, 0),
            // which triggers the "agent initial frame is out of walkable
            // area" fatal + hangs the deployment screen.
            if (SceneSpots != null && SceneSpots.Count > 0)
            {
                var spot = SceneSpots[0];
                agent.TeleportToPosition(spot.Position);
                var dir = new Vec2((float)System.Math.Cos(spot.Yaw),
                                   (float)System.Math.Sin(spot.Yaw));
                agent.SetMovementDirection(in dir);
                return;
            }

            // Legacy fallback: scene has no typed spots and no new scheme
            // either; use the flat `ambush_attacker` tag list.
            if (_legacyAttackerSpawnPositions.Count > 0)
            {
                int idx = _legacyAttackerSpawnIndex++;
                agent.TeleportToPosition(
                    _legacyAttackerSpawnPositions[idx % _legacyAttackerSpawnPositions.Count]);
            }
            }
            finally
            {
                Mission.IsTeleportingAgents = prevTeleport;
            }
        }

        public override void OnDeploymentFinished()
        {
            TORCommon.Log(LogLevel.Info,
                "[AmbushMission] OnDeploymentFinished enter"
                + " initialPlayerAgent=" + (Mission.InitialPlayerAgent != null)
                + " mainAgent=" + (Mission.MainAgent != null)
                + " defenderTeam=" + (Mission.DefenderTeam != null));
            base.OnDeploymentFinished();

            // Attacker initial placement happens per-agent via OnAgentCreated
            // (role-aware teleport to scener-tagged slots). Defender initial
            // placement is left to vanilla sp_defender_* spawns inside the
            // sp_battle_set subtree — the scener dropped dynamic_patrol_area_tag
            // in favour of this. Reinforcement spawns on both sides use the
            // sp_*_reinforcement subchildren naturally.

            // Snapshot the initial-patrol defender agent set now. Reinforcement
            // waves spawn later via DefaultBattleMissionAgentSpawnLogic and are
            // NOT members. Phase 2 begins when all of these go inactive.
            _initialPatrol.Clear();
            if (Mission.DefenderTeam != null)
            {
                foreach (var a in Mission.DefenderTeam.ActiveAgents)
                {
                    if (a != null && a.IsHuman) _initialPatrol.Add(a);
                }
            }

            // Team membership diagnostic: the ambush-victory check in
            // AmbushEndLogic triggers TotalVictory on CountActiveHumans
            // (Mission.DefenderTeam) == 0, so if defenders landed on a
            // different team the fight ends the moment the mission starts.
            int attackerActive = 0, attackerHuman = 0;
            if (Mission.AttackerTeam != null)
                foreach (var a in Mission.AttackerTeam.ActiveAgents)
                { attackerActive++; if (a != null && a.IsHuman) attackerHuman++; }
            int defenderActive = 0, defenderHuman = 0;
            if (Mission.DefenderTeam != null)
                foreach (var a in Mission.DefenderTeam.ActiveAgents)
                { defenderActive++; if (a != null && a.IsHuman) defenderHuman++; }
            TORCommon.Log(LogLevel.Info,
                "[AmbushMission] team census: attacker active=" + attackerActive
                + " human=" + attackerHuman
                + " | defender active=" + defenderActive
                + " human=" + defenderHuman
                + " | initialPatrol=" + _initialPatrol.Count);

            // Start the grace clock for the procedural-fallback decision. The
            // actual check happens in OnMissionTick once script OnInits have
            // reliably fired — doing it here races with the script lifecycle
            // and plants the fallback on the player, which auto-triggers
            // extraction. See FallbackGraceSeconds.
            _fallbackGraceTimer = new BasicMissionTimer();
            TORCommon.Log(LogLevel.Info, "[AmbushMission] OnDeploymentFinished exit");
        }

        public override InquiryData OnEndMissionRequest(out bool canLeave)
        {
            // Tab-leave is disabled. The only exits are the extraction bar,
            // dying, getting wiped, or the stalemate timer. See mission-design.md.
            canLeave = false;
            TOR_Core.Utilities.TORCommon.Say("You can't flee the ambush like this — look for the extraction point.");
            return null;
        }

        public override void OnMissionTick(float dt)
        {
            base.OnMissionTick(dt);
            if (!_fallbackResolved && _fallbackGraceTimer != null
                && _fallbackGraceTimer.ElapsedTime >= FallbackGraceSeconds)
            {
                _fallbackResolved = true;
                if (AmbushRetreatZone.ActiveZones.Count == 0)
                {
                    var mainAgent = Mission.MainAgent;
                    if (mainAgent != null && mainAgent.IsActive())
                        AmbushRetreatZone.ProceduralFallbackPosition = mainAgent.Position;
                }
            }

            // Phase 1 → Phase 2 transition. Check once per tick while Phase 1
            // is still active; cheap since _initialPatrol is small.
            if (!_initialPatrolDefeated && _initialPatrol.Count > 0)
            {
                bool allDown = true;
                for (int i = 0; i < _initialPatrol.Count; i++)
                {
                    var a = _initialPatrol[i];
                    if (a != null && a.IsActive() && a.IsHuman) { allDown = false; break; }
                }
                if (allDown)
                {
                    _initialPatrolDefeated = true;
                    _reinforcementTimer?.OnInitialPatrolDefeated();
                    InitialPatrolDefeated?.Invoke();

                    // Who's still on the defender side at the moment Phase 2
                    // transitions? If this list is empty, Priority 3 should
                    // fire TotalVictory the same tick. If anyone shows up
                    // here, that's the agent blocking the clean win — their
                    // char / isHuman / isActive / running / team membership
                    // should tell us whether it's a routed stragger,
                    // a late-added reinforcement, or something else.
                    if (Mission.DefenderTeam != null)
                    {
                        int idx = 0;
                        foreach (var a in Mission.DefenderTeam.ActiveAgents)
                        {
                            if (a == null) continue;
                            TORCommon.Log(LogLevel.Info,
                                "[AmbushMission] at Phase2 DefenderTeam[" + idx + "]"
                                + " char=" + (a.Character?.StringId ?? "?")
                                + " isHuman=" + a.IsHuman
                                + " isActive=" + a.IsActive()
                                + " isRunningAway=" + a.IsRunningAway
                                + " inInitialPatrol=" + _initialPatrol.Contains(a));
                            idx++;
                        }
                    }
                }
            }
        }

        private void SetupInitialSpawn()
        {
            if (_spawnLogic == null) return;

            // Read counts off the two parties directly. MapEventSide.TroopCount
            // is the battle-ready count (vanilla folds in wounded / etc.) so
            // we use that over raw roster size.
            int ambusherTotal = _ambusherParty?.MapEventSide?.TroopCount
                                ?? _ambusherParty?.MemberRoster?.TotalManCount ?? 0;
            int victimTotal = _victimParty?.MapEventSide?.TroopCount
                              ?? _victimParty?.MemberRoster?.TotalManCount ?? 0;

            // Battle-size budget split. Victim side gets the larger slice
            // because the "patrol" is the mission's visible force.
            float ratio = _model.TargetTotalTroopRatio;
            float inverse = 1f - ratio;
            int victimCap = (int)(_spawnLogic.BattleSize * ratio);
            int ambusherCap = (int)(_spawnLogic.BattleSize * inverse);

            // Tactics scaling. The player's hero contributes the bonus; it
            // applies to whichever side the player is on (ambusher in Phase D,
            // victim in Phase F — design doc § 4 asks for Tactics scaling on
            // the Phase F defensive side too).
            int tactics = Hero.MainHero?.GetSkillValue(DefaultSkills.Tactics) ?? 0;
            int tacticsBonus = (int)(tactics * _model.TacticsPerAmbushTroopBonus);
            bool playerIsAmbusher = _ambusherParty == PartyBase.MainParty;
            if (playerIsAmbusher) ambusherCap += tacticsBonus;
            else victimCap += tacticsBonus;

            if (ambusherTotal > ambusherCap) ambusherTotal = ambusherCap;
            if (victimTotal > victimCap) victimTotal = victimCap;

            // Map "ambusher" / "victim" totals back onto the Attacker /
            // Defender slot slots the vanilla spawn pipeline expects.
            int attackerTotal, defenderTotal;
            if (_ambusherSide == BattleSideEnum.Attacker)
            {
                attackerTotal = ambusherTotal;
                defenderTotal = victimTotal;
            }
            else
            {
                attackerTotal = victimTotal;
                defenderTotal = ambusherTotal;
            }

            // Victim (patrol): entire force spawns upfront. Fiction is "the
            // patrol being ambushed" — a 10% scout wave feels like 3 lost
            // scouts, not the real force. Reinforcements arrive as alarm
            // response later; 54 dynamic_patrol_area_tag spawn points.
            // Ambusher: entire force spawns upfront; no reinforcements for
            // the ambusher in the first-pass model.
            _spawnLogic.SetSpawnHorses(BattleSideEnum.Defender, spawnHorses: true);
            _spawnLogic.SetSpawnHorses(BattleSideEnum.Attacker, spawnHorses: false);

            var spawnSettings = new MissionSpawnSettings(
                MissionSpawnSettings.InitialSpawnMethod.FreeAllocation,
                MissionSpawnSettings.ReinforcementTimingMethod.CustomTimer,
                MissionSpawnSettings.ReinforcementSpawnMethod.Fixed,
                0f, 0f, 0f, 0f, 0,
                defenderReinforcementBatchPercentage: _model.ReinforcementBatchFraction,
                attackerReinforcementBatchPercentage: _model.PlayerReinforcementBatchFraction);

            _spawnLogic.InitWithSinglePhase(
                defenderTotal, attackerTotal,
                defenderTotal, attackerTotal,
                spawnDefenders: false, spawnAttackers: false,
                in spawnSettings);

            _reinforcementTimer = new AmbushReinforcementSpawnTimer(_model);
            _spawnLogic.SetCustomReinforcementSpawnTimer(_reinforcementTimer);
        }
    }
}
