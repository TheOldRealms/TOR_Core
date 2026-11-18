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
    /// <summary>Battle setup for ambush missions. Owns custom spawn, retreat-zone fallback, phase transition, and tactic registration.</summary>
    public class AmbushMissionController : MissionLogic
    {
        // Grace window before the procedural retreat-zone fallback applies.
        private const float FallbackGraceSeconds = 2f;

        private AmbushMissionAgentSpawnLogic _spawnLogic;
        private AmbushReinforcementMissionLogic _reinforcementLogic;
        // True while our custom spawn runs; suppresses the OnAgentCreated safety-net teleport.
        private bool _customSpawnInProgress;
        // One-shot flag; reposition formation plates to authored spots on first tick (after vanilla's auto-deploy).
        private bool _initialRepositionDone;

        private BasicMissionTimer _fallbackGraceTimer;
        private bool _fallbackResolved;
        private TORMissionAmbushModel _model;
        private readonly PartyBase _ambusherParty;
        private readonly PartyBase _victimParty;
        private BattleSideEnum _ambusherSide;
        private BattleSideEnum _victimSide;
        // Agents actually spawned by the custom pipeline; handed to MarkInitialSpawnComplete.
        private int _ambusherInitialSpawnCount;
        private int _victimInitialSpawnCount;

        // Per-class flat spawn lists; OnAgentCreated round-robins by matching FormationClass.
        private readonly Dictionary<FormationClass, List<Vec3>> _attackerSpawnPositions
            = new Dictionary<FormationClass, List<Vec3>>();
        private readonly Dictionary<FormationClass, List<float>> _attackerSpawnYaws
            = new Dictionary<FormationClass, List<float>>();
        private readonly Dictionary<FormationClass, int> _attackerSpawnIndex
            = new Dictionary<FormationClass, int>();
        // Legacy flat-tag fallback; populated only when role-specific lists are empty.
        private readonly List<Vec3> _legacyAttackerSpawnPositions = new List<Vec3>();
        private int _legacyAttackerSpawnIndex;

        /// <summary>Auto-chunked formation list plus default spot assignments. Null when the scene uses the legacy flat tag.</summary>
        public AmbushLayoutResult Layout { get; private set; }

        /// <summary>Every attacker spot the scener placed, scene-order.</summary>
        public List<AmbushSpot> SceneSpots { get; private set; } = new List<AmbushSpot>();

        /// <summary>Stable 1-based spot number keyed by scene position.</summary>
        public Dictionary<Vec3, int> SpotNumbers { get; private set; }
            = new Dictionary<Vec3, int>();

        // Snapshot of defender agents at OnDeploymentFinished. Phase 2 fires when all are inactive.
        private readonly List<Agent> _initialPatrol = new List<Agent>();
        private bool _initialPatrolDefeated;

        /// <summary>True once every member of the initial defender spawn is inactive.</summary>
        public bool IsInitialPatrolDefeated => _initialPatrolDefeated;

        /// <summary>Fires once when IsInitialPatrolDefeated flips to true.</summary>
        public event System.Action InitialPatrolDefeated;

        /// <summary>True when the local player's party is the ambusher side.</summary>
        public bool IsPlayerAmbusher
            => _ambusherParty != null
               && _ambusherParty.MobileParty == MobileParty.MainParty;

        /// <summary>Leader hero of the ambushed side, or null for leaderless parties.</summary>
        public Hero VictimLeaderHero => _victimParty?.LeaderHero;

        public MobileParty AmbusherParty => _ambusherParty?.MobileParty;

        // Shared supplier instances the mission opener must pass in.
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
            // Do NOT ResetRegistry here; scene OnInit already ran during scene load.
            _spawnLogic = Mission.GetMissionBehavior<AmbushMissionAgentSpawnLogic>();
            _reinforcementLogic = Mission.GetMissionBehavior<AmbushReinforcementMissionLogic>();
            _model = Campaign.Current?.Models?.GetMissionAmbushModel() ?? new TORMissionAmbushModel();
            AmbushRetreatZone.ProceduralFallbackRadius = _model.ProceduralFallbackRadius;

            // Victim path auto-finishes deployment once vanilla team setup is complete.
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

        /// <summary>Drain the ambusher supplier and place agents at AmbushFormation chunk spots.</summary>
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

            // Cap the ambusher's initial spawn via the Tactics-scaled on-field cap; excess stays unspawned.
            int ambusherRequest = _model.ComputeAmbusherInMissionCap(_ambusherParty?.LeaderHero);
            if (ambusherRequest <= 0) ambusherRequest = int.MaxValue;
            var origins = _ambusherSupplier.SupplyTroops(ambusherRequest).ToList();
            bool isPlayerSide = _ambusherSide == BattleSideEnum.Attacker
                                && IsPlayerAmbusher;
            _customSpawnInProgress = true;

            // Group by FormationClass so each class forms a line (or grid under MaxWidth).
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
                        bool isPlayerChar = origin.Troop == CharacterObject.PlayerCharacter;
                        // Place the player hero on a tactical child; scripted positions are ignored for player agents.
                        Vec3 pos = isPlayerChar
                                   && spot.Value.TacticalChildren != null
                                   && spot.Value.TacticalChildren.Count > 0
                            ? spot.Value.TacticalChildren[0]
                            : GridPositionFor(spot.Value.Position, facing, perp,
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

                        if (origin.Troop.IsHero)
                        {
                            buildData.FixedEquipment(true);
                            if (isPlayerChar)
                                buildData.Controller(AgentControllerType.Player);
                        }

                        var agent = Mission.SpawnAgent(buildData, false);
                        _spawnLogic?.BumpSpawnedCount(_ambusherSide);
                        if (chunkFormation == null) chunkFormation = agent.Formation;
                        if (isPlayerChar)
                        {
                            TORCommon.Log(LogLevel.Info,
                                "[AmbushMission] player spawned ctrl=" + agent.Controller
                                + " initialPlayerAgent set=" + (Mission.InitialPlayerAgent != null));
                        }
                        spawned++;
                    }

                    // Pin the formation plate explicitly; vanilla's default auto-deploy would otherwise plant it near the enemy.
                    if (chunkFormation != null && Mission.Scene != null)
                    {
                        chunkFormation.SetPositioning(
                            new WorldPosition(Mission.Scene, spot.Value.Position),
                            facing);
                    }

                    TryAnchorRangedFormation(chunkFormation, formationClass, spot.Value);
                }
            }
            finally
            {
                _customSpawnInProgress = false;
            }
            _ambusherInitialSpawnCount = spawned;
            TORCommon.Log(LogLevel.Info,
                "[AmbushMission] DoCustomAmbusherSpawn: spawned=" + spawned
                + "/" + origins.Count);
        }

        // Shared layout math for attacker + defender spawns.
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
            // Grid offsets disabled; off-navmesh frames crash during deployment to battle mode switch.
            _ = facing;
            _ = perp;
            _ = index;
            _ = groupCount;
            _ = cols;
            return center;
        }

        /// <summary>Attach a tactical-point anchor to a ranged formation so archers pin to the authored child entities.</summary>
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

        /// <summary>Rebind the tactical anchor after the spot picker reassigns a formation.</summary>
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

        /// <summary>Reset every chunked formation to its default spot assignment. Deployment-phase only.</summary>
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
            // Deduplicate by position so share-spot layout runs once with final occupants.
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

        /// <summary>Eagerly bind every layout chunk to its vanilla Formation; the lazy VM binding may not have ticked yet.</summary>
        private void EagerBindVanillaFormations()
        {
            if (Layout?.Formations == null) return;
            var team = _ambusherSide == BattleSideEnum.Attacker
                ? Mission.AttackerTeam
                : Mission.DefenderTeam;
            if (team == null) return;
            foreach (var vanilla in team.FormationsIncludingEmpty)
            {
                if (vanilla == null) continue;
                bool alreadyBound = false;
                foreach (var f in Layout.Formations)
                {
                    if (f.VanillaFormation == vanilla) { alreadyBound = true; break; }
                }
                if (alreadyBound) continue;
                foreach (var f in Layout.Formations)
                {
                    if (f.VanillaFormation == null
                        && f.Class == vanilla.FormationIndex)
                    {
                        f.VanillaFormation = vanilla;
                        break;
                    }
                }
            }
        }

        /// <summary>Reposition every spot the layout touches to its authored scene position.</summary>
        private void RepositionAllTouchedSpots()
        {
            if (Layout?.Formations == null) return;
            var touched = new HashSet<Vec3>();
            foreach (var f in Layout.Formations)
            {
                if (!f.AssignedSpot.HasValue) continue;
                if (f.VanillaFormation == null) continue;
                touched.Add(f.AssignedSpot.Value.Position);
            }
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
        }

        /// <summary>First assigned spot for a class, falling back to SceneSpots[0].</summary>
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
            // Resolve each party's battle side from MapEventSide.
            _ambusherSide = _ambusherParty?.MapEventSide?.MissionSide ?? BattleSideEnum.Attacker;
            _victimSide = _victimParty?.MapEventSide?.MissionSide ?? BattleSideEnum.Defender;
            SetupInitialSpawn();
            CollectAttackerSpawnPositions();
            // Drain the suppliers and place both sides before vanilla's first OnMissionTick.
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

            // Hand phase bookkeeping back to the spawn-logic subclass.
            _spawnLogic?.MarkInitialSpawnComplete(
                _victimSide == BattleSideEnum.Defender
                    ? _victimInitialSpawnCount : _ambusherInitialSpawnCount,
                _victimSide == BattleSideEnum.Defender
                    ? _ambusherInitialSpawnCount : _victimInitialSpawnCount);

            // Register the ambush tactic on the attacker team (ambusher side).
            var attacker = Mission.AttackerTeam;
            if (attacker != null)
            {
                attacker.AddTacticOption(new AmbushTacticHoldPositions(attacker));
                TORCommon.Log(LogLevel.Info,
                    "[AmbushMission] registered AmbushTacticHoldPositions on AttackerTeam");
            }

            ValidatePostSpawnOrThrow();
        }

        /// <summary>Preflight before vanilla's SetupTeams; throws with a scene-author-friendly message if the player agent is missing.</summary>
        private void ValidatePostSpawnOrThrow()
        {
            if (Mission.InitialPlayerAgent != null) return;

            string sceneName = Mission.SceneName ?? "(unknown)";
            int attackerAgents = Mission.AttackerTeam?.ActiveAgents?.Count ?? 0;
            int defenderAgents = Mission.DefenderTeam?.ActiveAgents?.Count ?? 0;
            int layoutChunks = Layout?.Formations?.Count ?? 0;
            int sceneSpots = SceneSpots?.Count ?? 0;

            string msg =
                "\n========================================\n"
                + " AMBUSH SCENE VALIDATION FAILED\n"
                + "========================================\n"
                + " Scene:               " + sceneName + "\n"
                + " Player side:         "
                    + (IsPlayerAmbusher ? "Ambusher (Attacker)" : "Victim (Defender)") + "\n"
                + " Attacker agents:     " + attackerAgents + "\n"
                + " Defender agents:     " + defenderAgents + "\n"
                + " Ambush spots found:  " + sceneSpots + "\n"
                + " Formation chunks:    " + layoutChunks + "\n"
                + "----------------------------------------\n"
                + " The player's hero did not spawn with a Player controller,\n"
                + " so vanilla's deployment phase will crash on the next tick.\n"
                + "\n"
                + " Likely scene-authoring causes (fix in the Bannerlord editor):\n"
                + "   - sp_player entity is outside the walkable navmesh\n"
                + "     (over water, off a cliff, inside geometry).\n"
                + "   - No attacker_ambush_spawnpoint entities with a valid\n"
                + "     attacker_ambush_<group> tag, OR all of them are\n"
                + "     off-navmesh / under terrain.\n"
                + "   - sp_battle_set / battle_set entity missing or misplaced.\n"
                + "   - Scene terrain Z does not match entity Z (entities are\n"
                + "     authored at a height the terrain does not reach).\n"
                + "   - Navmesh not regenerated after moving spawn entities.\n"
                + "----------------------------------------\n"
                + " Compare against TOR_ambush_forest_001 (known working).\n"
                + "========================================";

            TORCommon.Log(LogLevel.Error, msg);
            TORCommon.Say(msg);
            throw new System.InvalidOperationException(msg);
        }


        /// <summary>Drain the victim supplier and place agents at sp_defender_* scene entities.</summary>
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
            // Partial drain; remainder arrives via reinforcement waves. Cap scales with victim leader's Tactics.
            int victimRequest = _model.ComputeVictimInMissionCap(_victimParty?.LeaderHero);
            if (victimRequest <= 0) victimRequest = int.MaxValue;
            var origins = _victimSupplier.SupplyTroops(victimRequest).ToList();
            // Victim is on the player side when player is the victim.
            bool isPlayerSide = !IsPlayerAmbusher;
            // Scener-authored camera anchor used for victim-side player placement.
            Vec3 anchorPos = Vec3.Zero;
            Vec2 anchorFacing = new Vec2(0f, 1f);
            bool hasAnchor = !IsPlayerAmbusher
                             && TryGetCameraAnchor(out anchorPos, out anchorFacing);
            _customSpawnInProgress = true;

            // Same grid layout as the ambusher side; position and yaw read off the scener entity.
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
                    // Victim entities have no MaxWidth script; single line.
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
                        _spawnLogic?.BumpSpawnedCount(_victimSide);
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
            _victimInitialSpawnCount = spawned;
            TORCommon.Log(LogLevel.Info,
                "[AmbushMission] DoCustomVictimSpawn: spawned=" + spawned
                + "/" + origins.Count);
        }

        /// <summary>Look up the scener-authored ambush_camera_anchor entity; returns false if absent.</summary>
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
            // Entity tags omit the sp_ prefix; archer entity carries both defender_archer and defender_ranged.
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

        /// <summary>Collect attacker spot entities, compute the layout, and build per-class teleport lists.</summary>
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

        /// <summary>Read every attacker_ambush_spawnpoint entity and pair it with its group tag.</summary>
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
                    // Accept TacticalPosition or StrategicArea scripts; both are valid authoring.
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

        /// <summary>Route attacker humans to a role-matching scener-tagged slot; defenders keep vanilla placement.</summary>
        public override void OnAgentCreated(Agent agent)
        {
            base.OnAgentCreated(agent);
            if (agent == null || !agent.IsHuman) return;

            // Diagnostic: log late-spawn defender placement; agent.Position is (0,0,0) at OnAgentCreated.
            if (!_customSpawnInProgress
                && agent.Team != null
                && agent.Team.Side == _victimSide)
            {
                TORCommon.Log(LogLevel.Info,
                    "[AmbushReinforcement][agent] side=" + agent.Team.Side
                    + " char=" + (agent.Character?.StringId ?? "?")
                    + " formation=" + (agent.Formation?.FormationIndex.ToString() ?? "null")
                    + " formationOrderPos="
                      + (agent.Formation != null
                            ? agent.Formation.OrderPosition.ToString() : "null")
                    + " formationHasBeenPositioned="
                      + (agent.Formation?.HasBeenPositioned.ToString() ?? "null"));
            }

            // Skip the safety-net teleport while our own spawn is in flight.
            if (_customSpawnInProgress) return;

            // Null-team agents still get teleported; wrong-side agents are skipped.
            if (agent.Team != null && agent.Team.Side != _ambusherSide) return;

            TORCommon.Log(LogLevel.Info,
                "[AmbushMission] OnAgentCreated char=" + (agent.Character?.StringId ?? "?")
                + " class=" + (agent.Character?.DefaultFormationClass.ToString() ?? "?")
                + " team=" + (agent.Team == null ? "null" : agent.Team.Side.ToString()));

            var formationClass = agent.Character?.DefaultFormationClass ?? FormationClass.Infantry;
            // Signal to the engine that we're about to teleport; otherwise the walkable-area check hangs.
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

            // Fallback: use any SceneSpot so unbound classes don't spawn at (0,0,0).
            if (SceneSpots != null && SceneSpots.Count > 0)
            {
                var spot = SceneSpots[0];
                agent.TeleportToPosition(spot.Position);
                var dir = new Vec2((float)System.Math.Cos(spot.Yaw),
                                   (float)System.Math.Sin(spot.Yaw));
                agent.SetMovementDirection(in dir);
                return;
            }

            // Legacy fallback: flat ambush_attacker tag list.
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

            // Snapshot the initial-patrol defender set; reinforcement waves are excluded.
            _initialPatrol.Clear();
            if (Mission.DefenderTeam != null)
            {
                foreach (var a in Mission.DefenderTeam.ActiveAgents)
                {
                    if (a != null && a.IsHuman) _initialPatrol.Add(a);
                }
            }

            // Team census diagnostic for TotalVictory gating in AmbushEndLogic.
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

            // Start the grace clock; the actual check runs in OnMissionTick after script OnInits settle.
            _fallbackGraceTimer = new BasicMissionTimer();
            TORCommon.Log(LogLevel.Info, "[AmbushMission] OnDeploymentFinished exit");
        }

        public override InquiryData OnEndMissionRequest(out bool canLeave)
        {
            // Tab-leave disabled; exits are extraction, death, wipe, or stalemate timer.
            canLeave = false;
            TOR_Core.Utilities.TORCommon.Say("You can't flee the ambush like this. Look for the extraction point.");
            return null;
        }

        public override void OnMissionTick(float dt)
        {
            base.OnMissionTick(dt);

            if (!_initialRepositionDone
                && IsPlayerAmbusher
                && Mission?.Mode == TaleWorlds.Core.MissionMode.Deployment
                && Layout?.Formations != null)
            {
                _initialRepositionDone = true;
                EagerBindVanillaFormations();
                RepositionAllTouchedSpots();
            }
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

            // Phase 1 to Phase 2 transition check (cheap, _initialPatrol is small).
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
                    _reinforcementLogic?.OnInitialPatrolDefeated();
                    InitialPatrolDefeated?.Invoke();

                    // Diagnostic: enumerate remaining defender agents at Phase 2 transition.
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

            // Read full roster per side off MemberRoster; MapEventSide.TroopCount is BattleSize-clamped.
            int ambusherTotal = _ambusherParty?.MemberRoster?.TotalManCount ?? 0;
            int victimTotal = _victimParty?.MemberRoster?.TotalManCount ?? 0;

            // Map ambusher/victim totals back onto Attacker/Defender slots.
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
            TORCommon.Log(LogLevel.Info,
                "[AmbushMission][setup] ambusherRoster=" + ambusherTotal
                + " victimRoster=" + victimTotal
                + " -> defenderTotal=" + defenderTotal
                + " attackerTotal=" + attackerTotal);

            // Full roster spawns upfront on both sides; ambusher has no reinforcements, victim gets alarm response later.
            _spawnLogic.SetSpawnHorses(BattleSideEnum.Defender, spawnHorses: true);
            _spawnLogic.SetSpawnHorses(BattleSideEnum.Attacker, spawnHorses: false);

            var spawnSettings = new MissionSpawnSettings(
                MissionSpawnSettings.InitialSpawnMethod.FreeAllocation,
                MissionSpawnSettings.ReinforcementTimingMethod.CustomTimer,
                MissionSpawnSettings.ReinforcementSpawnMethod.Fixed,
                0f, 0f, 0f, 0f, 0,
                defenderReinforcementBatchPercentage: _model.ReinforcementBatchFraction,
                attackerReinforcementBatchPercentage: _model.PlayerReinforcementBatchFraction);

            // Initial-spawn cap applies to both sides; Tactics-adjusted. Victim-side excess flows through reinforcement waves.
            int ambusherCap = _model.ComputeAmbusherInMissionCap(_ambusherParty?.LeaderHero);
            int victimCap = _model.ComputeVictimInMissionCap(_victimParty?.LeaderHero);
            int defenderInitial = defenderTotal;
            int attackerInitial = attackerTotal;
            // Side-to-cap mapping follows MapEventSide to allow for future opener variants.
            if (_victimSide == BattleSideEnum.Defender)
            {
                defenderInitial = System.Math.Min(defenderTotal, victimCap);
                attackerInitial = System.Math.Min(attackerTotal, ambusherCap);
            }
            else
            {
                defenderInitial = System.Math.Min(defenderTotal, ambusherCap);
                attackerInitial = System.Math.Min(attackerTotal, victimCap);
            }

            _spawnLogic.InitWithSinglePhase(
                defenderTotal, attackerTotal,
                defenderInitial, attackerInitial,
                spawnDefenders: false, spawnAttackers: false,
                in spawnSettings);

            // Vanilla requires a non-null ICustomReinforcementSpawnTimer; the stub always returns false.
            _spawnLogic.SetCustomReinforcementSpawnTimer(new NoOpReinforcementSpawnTimer());
        }
    }
}
