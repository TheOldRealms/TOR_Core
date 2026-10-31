using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TOR_Core.Extensions;
using TOR_Core.Models;

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
        private AmbushReinforcementSpawnTimer _reinforcementTimer;
        private BasicMissionTimer _fallbackGraceTimer;
        private bool _fallbackResolved;
        private TORMissionAmbushModel _model;
        private readonly int _playerTroopCount;
        private readonly int _targetTroopCount;

        // Scener-authored attacker spawn positions, grouped by formation class.
        // Each class maps to a list of world-space Vec3s pulled from entities
        // tagged `attacker_<role>`. Agents land at a role-matching position via
        // OnAgentCreated; the counter in _attackerSpawnIndex per class round-robins
        // through the list when a class has more agents than slots.
        private readonly Dictionary<FormationClass, List<Vec3>> _attackerSpawnPositions
            = new Dictionary<FormationClass, List<Vec3>>();
        private readonly Dictionary<FormationClass, int> _attackerSpawnIndex
            = new Dictionary<FormationClass, int>();
        // Legacy fallback for scenes authored before the role-tag scheme —
        // uses the flat `ambush_attacker` tag. Only populated if the role-
        // specific lists come up empty.
        private readonly List<Vec3> _legacyAttackerSpawnPositions = new List<Vec3>();
        private int _legacyAttackerSpawnIndex;

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

        public AmbushMissionController(int playerTroopCount, int targetTroopCount)
        {
            _playerTroopCount = playerTroopCount;
            _targetTroopCount = targetTroopCount;
        }

        public override void OnBehaviorInitialize()
        {
            Debug.Print("[AmbushMission] OnBehaviorInitialize enter");
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
            Debug.Print("[AmbushMission] OnBehaviorInitialize done (spawnLogic=" + (_spawnLogic != null) + ", activeZones=" + AmbushRetreatZone.ActiveZones.Count + ")");
        }

        public override void AfterStart()
        {
            Debug.Print("[AmbushMission] AfterStart enter");
            base.AfterStart();
            SetupInitialSpawn();
            CollectAttackerSpawnPositions();
            Debug.Print("[AmbushMission] AfterStart done");
        }

        /// <summary>
        /// Reads the scener-tagged attacker spawn entities and buckets them by
        /// formation class, so <see cref="OnAgentCreated"/> can teleport each
        /// agent to a role-matching position. The scener's convention is:
        /// `attacker_infantry` / `attacker_archer` / `attacker_cavalry` /
        /// `attacker_horsearcher`. If none of those exist in the scene, we
        /// fall back to a flat `ambush_attacker` tag (legacy pre-slot scheme).
        /// </summary>
        private void CollectAttackerSpawnPositions()
        {
            _attackerSpawnPositions.Clear();
            _attackerSpawnIndex.Clear();
            _attackerSpawnPositions[FormationClass.Infantry]    = CollectPositionsForTag("attacker_infantry");
            _attackerSpawnPositions[FormationClass.Ranged]      = CollectPositionsForTag("attacker_archer");
            _attackerSpawnPositions[FormationClass.Cavalry]     = CollectPositionsForTag("attacker_cavalry");
            _attackerSpawnPositions[FormationClass.HorseArcher] = CollectPositionsForTag("attacker_horsearcher");
            foreach (var kv in _attackerSpawnPositions) _attackerSpawnIndex[kv.Key] = 0;

            _legacyAttackerSpawnPositions.Clear();
            _legacyAttackerSpawnPositions.AddRange(CollectPositionsForTag("ambush_attacker"));
            _legacyAttackerSpawnIndex = 0;

            Debug.Print(
                "[AmbushMission] attacker spawn slots:"
                + " infantry=" + _attackerSpawnPositions[FormationClass.Infantry].Count
                + " archer=" + _attackerSpawnPositions[FormationClass.Ranged].Count
                + " cavalry=" + _attackerSpawnPositions[FormationClass.Cavalry].Count
                + " horsearcher=" + _attackerSpawnPositions[FormationClass.HorseArcher].Count
                + " legacy=" + _legacyAttackerSpawnPositions.Count);
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
            if (agent.Team != Mission.AttackerTeam) return;

            var formationClass = agent.Character?.DefaultFormationClass ?? FormationClass.Infantry;
            if (_attackerSpawnPositions.TryGetValue(formationClass, out var positions)
                && positions.Count > 0)
            {
                int idx = _attackerSpawnIndex[formationClass]++;
                agent.TeleportToPosition(positions[idx % positions.Count]);
                return;
            }

            // Legacy fallback: scene has no role-typed slots but has the flat
            // `ambush_attacker` tag. Mixes all classes in one list; worse but
            // still better than default spawn.
            if (_legacyAttackerSpawnPositions.Count > 0)
            {
                int idx = _legacyAttackerSpawnIndex++;
                agent.TeleportToPosition(
                    _legacyAttackerSpawnPositions[idx % _legacyAttackerSpawnPositions.Count]);
            }
        }

        public override void OnDeploymentFinished()
        {
            Debug.Print("[AmbushMission] OnDeploymentFinished enter");
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
            Debug.Print("[AmbushMission] initial patrol snapshot: " + _initialPatrol.Count + " agents");

            // Start the grace clock for the procedural-fallback decision. The
            // actual check happens in OnMissionTick once script OnInits have
            // reliably fired — doing it here races with the script lifecycle
            // and plants the fallback on the player, which auto-triggers
            // extraction. See FallbackGraceSeconds.
            _fallbackGraceTimer = new BasicMissionTimer();
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
                    {
                        AmbushRetreatZone.ProceduralFallbackPosition = mainAgent.Position;
                        Debug.Print("[AmbushMission] No authored zones after grace; procedural fallback at " + mainAgent.Position);
                    }
                }
                else
                {
                    Debug.Print("[AmbushMission] Authored zones present: " + AmbushRetreatZone.ActiveZones.Count + "; procedural fallback skipped.");
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
                    Debug.Print("[AmbushMission] initial patrol defeated → Phase 2 (extraction) armed");
                    _reinforcementTimer?.OnInitialPatrolDefeated();
                    InitialPatrolDefeated?.Invoke();
                }
            }
        }

        private void SetupInitialSpawn()
        {
            Debug.Print("[AmbushMission] SetupInitialSpawn (playerTroops=" + _playerTroopCount + " targetTroops=" + _targetTroopCount + ")");
            if (_spawnLogic == null)
            {
                Debug.Print("[AmbushMission] SetupInitialSpawn: _spawnLogic is null, aborting");
                return;
            }

            // Mirror vanilla's AdjustTotalTroopCounts shape: clamp both sides
            // against the battle-size budget split, then rebalance so one side's
            // roster doesn't dwarf the other's spawn allocation.
            int besiegedTotal = _targetTroopCount;
            int besiegerTotal = _playerTroopCount;

            float ratio = _model.TargetTotalTroopRatio;
            float inverse = 1f - ratio;
            int besiegedCap = (int)(_spawnLogic.BattleSize * ratio);
            int besiegerCap = (int)(_spawnLogic.BattleSize * inverse);
            if (besiegedTotal > besiegedCap) besiegedTotal = besiegedCap;
            if (besiegerTotal > besiegerCap) besiegerTotal = besiegerCap;

            int sum = besiegedTotal + besiegerTotal;
            // Defender (ambushed patrol): the entire visible patrol spawns upfront.
            // Fiction is "the patrol you're ambushing" — a 10% scout wave feels
            // like 3 lost scouts, not the actual force. Reinforcements then come
            // in as "second patrol responding to the alarm" waves on top of this.
            // There are 54 dynamic_patrol_area_tag points to receive them.
            int defenderInitial = besiegedTotal;
            // Attacker (player ambush party): entire force spawns upfront. There
            // are no player reinforcements — the ambush is a fixed-roster strike,
            // intended to be preselected like TrollCave later. Cap only against
            // the battle-size slice we already clamped above.
            int attackerInitial = besiegerTotal;

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
                besiegedTotal, besiegerTotal,
                defenderInitial, attackerInitial,
                spawnDefenders: false, spawnAttackers: false,
                in spawnSettings);

            _reinforcementTimer = new AmbushReinforcementSpawnTimer(_model);
            _spawnLogic.SetCustomReinforcementSpawnTimer(_reinforcementTimer);
        }
    }
}
