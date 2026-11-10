using System.Collections.Generic;
using System.Linq;
using NLog;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TOR_Core.Models;
using TOR_Core.Utilities;

namespace TOR_Core.Missions.AmbushMission
{
    /// <summary>
    /// Owns reinforcement waves for the ambush mission end-to-end.
    /// Replaces the earlier <c>AmbushReinforcementSpawnTimer</c> + vanilla
    /// <c>SetCustomReinforcementSpawnTimer</c> flow, which required
    /// reaching into vanilla's spawn path and coaxing it to use authored
    /// positions. That path relied on a populated deployment plan we
    /// don't build; the fallback resolved to scene origin and
    /// reinforcements arrived in nonsense locations.
    ///
    /// This logic reads the authored <c>sp_defender_&lt;class&gt;_reinforcement</c>
    /// entities directly (short tag <c>defender_&lt;class&gt;_reinforcement</c>)
    /// and spawns waves there via <c>Mission.SpawnTroopWithAgentBuildData</c>,
    /// matching what vanilla's own <c>DefaultBattleMissionAgentSpawnLogic.SpawnTroops</c>
    /// does for reinforcement agents: the extra isAlarmed / wieldInitialWeapons
    /// flags are what actually make the new troops engage instead of
    /// standing idle in Patrolling WatchState. Bookkeeping is routed
    /// through <see cref="AmbushMissionAgentSpawnLogic.OnReinforcementSpawned"/>
    /// so the shared vanilla context counters stay consistent.
    ///
    /// Only the victim side (always Defender in both Phase D and Phase F
    /// per the ambusher-is-Attacker invariant) receives waves. The
    /// ambusher commits one-shot by design; enabling attacker-side waves
    /// is a future choice not yet surfaced.
    /// </summary>
    public class AmbushReinforcementMissionLogic : MissionLogic
    {
        private readonly IMissionTroopSupplier _victimSupplier;
        private readonly AmbushMissionAgentSpawnLogic _spawnLogic;
        private readonly TORMissionAmbushModel _model;

        private float[] _initialWaveIntervals;
        private float _steadyWaveInterval;
        private float _reinforcementBatchFraction;
        private float _patrolClearedDelayBonus;

        private readonly Dictionary<FormationClass, Vec3> _reinforcementPositions
            = new Dictionary<FormationClass, Vec3>();
        private readonly Dictionary<FormationClass, Vec2> _reinforcementDirections
            = new Dictionary<FormationClass, Vec2>();

        private BasicMissionTimer _waveTimer;
        private int _waveIndex;
        private float _currentWaveDelayBonus;
        private bool _patrolClearedBonusConsumed;
        private bool _deploymentFinished;

        public AmbushReinforcementMissionLogic(IMissionTroopSupplier victimSupplier,
                                                AmbushMissionAgentSpawnLogic spawnLogic,
                                                TORMissionAmbushModel model)
        {
            _victimSupplier = victimSupplier;
            _spawnLogic = spawnLogic;
            _model = model ?? new TORMissionAmbushModel();
        }

        public override void AfterStart()
        {
            base.AfterStart();
            _initialWaveIntervals = _model.InitialWaveIntervals;
            _steadyWaveInterval = _model.SteadyWaveInterval;
            _reinforcementBatchFraction = _model.ReinforcementBatchFraction;
            _patrolClearedDelayBonus = _model.InitialPatrolClearedWaveDelayBonus;
            _waveTimer = new BasicMissionTimer();
            CacheReinforcementScenePositions();
        }

        public override void OnDeploymentFinished()
        {
            base.OnDeploymentFinished();
            _deploymentFinished = true;
            _waveTimer = new BasicMissionTimer();
        }

        public override void OnMissionTick(float dt)
        {
            base.OnMissionTick(dt);
            if (!_deploymentFinished) return;
            if (IsPoolDepleted) return;
            if (_waveTimer == null) return;

            float interval = _waveIndex < _initialWaveIntervals.Length
                ? _initialWaveIntervals[_waveIndex]
                : _steadyWaveInterval;
            interval += _currentWaveDelayBonus;
            if (_waveTimer.ElapsedTime < interval) return;

            TORCommon.Log(LogLevel.Info,
                "[AmbushReinforcement] wave " + (_waveIndex + 1)
                + " fired after " + _waveTimer.ElapsedTime.ToString("F1")
                + "s (interval " + interval.ToString("F1") + "s)");

            FireWave();
            _waveTimer = new BasicMissionTimer();
            _waveIndex++;
            _currentWaveDelayBonus = 0f;
        }

        /// <summary>
        /// Hook called by <see cref="AmbushMissionController"/> the frame
        /// the initial patrol becomes fully defeated. One-shot bonus to
        /// the current wave's remaining interval ("you cleared Phase 1
        /// fast, buy a regroup window").
        /// </summary>
        public void OnInitialPatrolDefeated()
        {
            if (_patrolClearedBonusConsumed) return;
            _patrolClearedBonusConsumed = true;
            if (_waveTimer == null) return;
            float currentInterval = _waveIndex < _initialWaveIntervals.Length
                ? _initialWaveIntervals[_waveIndex]
                : _steadyWaveInterval;
            float remaining = currentInterval - _waveTimer.ElapsedTime;
            if (remaining <= 0f) return;
            _currentWaveDelayBonus = remaining * _patrolClearedDelayBonus;
        }

        /// <summary>
        /// Read by <c>AmbushEndLogic</c> as the "no more waves coming"
        /// half of the TotalVictory gate. Delegates to the spawn-logic
        /// subclass's pool check, which reads
        /// <c>phase.RemainingSpawnNumber</c> directly. That value is set
        /// correctly by <see cref="AmbushMissionAgentSpawnLogic.MarkInitialSpawnComplete"/>
        /// at AfterStart time (phase total minus what the initial spawn
        /// actually took), so IsPoolDepleted is accurate from frame one
        /// even for small-roster parties where every troop fit in the
        /// initial batch. It drops as waves fire and drain the remainder.
        /// </summary>
        public bool IsPoolDepleted
            => _spawnLogic == null || _spawnLogic.IsAmbushPoolDepleted(BattleSideEnum.Defender);

        /// <summary>
        /// Count of defender troops still waiting in the reinforcement
        /// pool (not yet spawned). Read by the mission-objective HUD so
        /// the Phase 2 "fight off reinforcements (N)" readout stays
        /// accurate. 0 once the pool is drained.
        /// </summary>
        public int RemainingPoolCount
            => _spawnLogic?.DefenderActivePhase?.RemainingSpawnNumber ?? 0;

        /// <summary>
        /// Reads scene entities tagged <c>defender_&lt;class&gt;_reinforcement</c>
        /// and caches their world position + facing per FormationClass.
        /// The facing points from the reinforcement entity toward the
        /// scene origin so arriving troops jog forward into the fight
        /// rather than away from it. Missing entities for a class are
        /// logged and skipped; that class falls back to the mission's
        /// scene center at wave time.
        /// </summary>
        private void CacheReinforcementScenePositions()
        {
            if (Mission?.Scene == null) return;
            FormationClass[] classes = {
                FormationClass.Infantry,
                FormationClass.Ranged,
                FormationClass.Cavalry,
                FormationClass.HorseArcher,
            };
            foreach (var cls in classes)
            {
                string tag = "defender_" + cls.GetName().ToLower() + "_reinforcement";
                GameEntity found = null;
                foreach (var e in Mission.Scene.FindEntitiesWithTag(tag))
                {
                    found = e;
                    break;
                }
                if (found == null) continue;
                var frame = found.GetGlobalFrame();
                _reinforcementPositions[cls] = frame.origin;
                _reinforcementDirections[cls] = frame.rotation.f.AsVec2.Normalized();
                TORCommon.Log(LogLevel.Info,
                    "[AmbushReinforcement] cached " + cls + " spawn at "
                    + frame.origin + " (tag " + tag + ")");
            }
            if (_reinforcementPositions.Count == 0)
                TORCommon.Log(LogLevel.Warn,
                    "[AmbushReinforcement] no defender_<class>_reinforcement entities in scene; waves will fall back to scene center");
        }

        /// <summary>
        /// Pulls a batch of origins from the supplier and spawns each via
        /// <c>Mission.SpawnAgent</c> at the class's authored reinforcement
        /// position, with a small per-agent lateral stagger so the batch
        /// arrives in a line rather than a point. Each spawn calls back
        /// into <see cref="AmbushMissionAgentSpawnLogic.OnReinforcementSpawned"/>
        /// to keep the shared vanilla counters consistent.
        /// </summary>
        private void FireWave()
        {
            if (_victimSupplier == null) return;
            int batchSize = ComputeBatchSize();
            if (batchSize <= 0) return;

            var origins = _victimSupplier.SupplyTroops(batchSize).ToList();
            if (origins.Count == 0)
            {
                TORCommon.Log(LogLevel.Info,
                    "[AmbushReinforcement] supplier empty; pool depleted");
                return;
            }

            BattleSideEnum victimSide = DetermineVictimSide();
            bool isPlayerSide = Mission.PlayerTeam != null
                                && Mission.PlayerTeam.Side == victimSide;

            var touchedFormations = new HashSet<Formation>();
            foreach (var origin in origins)
            {
                var cls = origin.Troop.GetFormationClass();
                Vec3 pos;
                Vec2 dir;
                if (!_reinforcementPositions.TryGetValue(cls, out pos))
                {
                    if (_reinforcementPositions.Count > 0)
                    {
                        pos = _reinforcementPositions.Values.First();
                        dir = _reinforcementDirections.Values.First();
                    }
                    else { pos = Vec3.Zero; dir = new Vec2(0f, 1f); }
                }
                else { dir = _reinforcementDirections[cls]; }

                var buildData = Mission.GetAgentBuildDataToSpawnTroop(
                    origin,
                    isPlayerSide,
                    true,
                    true,
                    true,
                    1,
                    0,
                    pos,
                    dir,
                    formationIndex: cls);
                if (origin.Troop.IsHero) buildData.FixedEquipment(true);

                var agent = Mission.SpawnTroopWithAgentBuildData(buildData, true, true);
                if (agent?.Formation != null) touchedFormations.Add(agent.Formation);
                _spawnLogic?.OnReinforcementSpawned(victimSide);
            }

            // Reinforcement formations join whatever movement order the
            // formation was last given, which post initial-wave wipe is
            // typically Hold or stale. Point them at the ambusher team
            // (always Attacker by opener invariant) so the behaviour
            // mirrors in Phase D (player ambusher) and Phase F (AI
            // ambusher). Fall back to generic Charge if the ambusher
            // team is unresolvable or already wiped.
            var ambusherTarget = ResolveAmbusherTarget();
            var chargeOrder = ambusherTarget != null
                ? MovementOrder.MovementOrderChargeToTarget(ambusherTarget)
                : MovementOrder.MovementOrderCharge;
            foreach (var formation in touchedFormations)
                formation.SetMovementOrder(chargeOrder);

            // Waves always spawn on the victim side. "Enemy" reads correctly
            // only when the player is the ambusher; in Phase F (player as
            // the ambushed) the arriving troops are allied rescue.
            var banner = isPlayerSide
                ? new TextObject("{=tor_ambush_allied_reinforcements}Allied reinforcements arrived!")
                : new TextObject("{=tor_ambush_enemy_reinforcements}Enemy reinforcements arrived!");
            MBInformationManager.AddQuickInformation(banner);

            TORCommon.Log(LogLevel.Info,
                "[AmbushReinforcement] wave " + (_waveIndex + 1)
                + " spawned " + origins.Count + " agents"
                + " target=" + (ambusherTarget != null
                    ? "ambusher formation " + ambusherTarget.FormationIndex
                    + " (" + ambusherTarget.CountOfUnits + " units)"
                    : "nearest enemy"));
        }

        /// <summary>
        /// Picks the ambusher-team formation reinforcement waves should
        /// close on. Prefers the formation containing the main hero when
        /// the player is the ambusher (Phase D) so waves home in on the
        /// hero who struck; otherwise returns the largest live formation
        /// on the ambusher team (Phase F, AI ambusher). Null if the
        /// ambusher team has no live formations left, which lets callers
        /// fall back to a generic charge.
        /// </summary>
        private Formation ResolveAmbusherTarget()
        {
            var team = Mission.AttackerTeam;
            if (team == null) return null;
            var mainFormation = Mission.MainAgent?.Formation;
            if (mainFormation != null && mainFormation.Team == team
                && mainFormation.CountOfUnits > 0)
                return mainFormation;
            Formation best = null;
            int bestCount = 0;
            foreach (var f in team.FormationsIncludingEmpty)
            {
                if (f == null || f.CountOfUnits <= 0) continue;
                if (f.CountOfUnits > bestCount)
                {
                    best = f;
                    bestCount = f.CountOfUnits;
                }
            }
            return best;
        }

        /// <summary>
        /// Batch size for a wave = max(floor, totalRoster * fraction),
        /// clamped above by the remaining pool. Reads the active-phase
        /// total (set from the mission opener) so the per-wave count
        /// scales with party size. The floor keeps small parties from
        /// trickling in one troop at a time; the pool clamp is what
        /// sends "the last 3 men" as a final wave even when 3 is below
        /// the floor.
        /// </summary>
        private int ComputeBatchSize()
        {
            if (_spawnLogic == null) return 1;
            var phase = _spawnLogic.DefenderActivePhase;
            if (phase == null || phase.RemainingSpawnNumber <= 0) return 0;
            int total = phase.TotalSpawnNumber;
            int batch = (int)(total * _reinforcementBatchFraction);
            int floor = _model.ReinforcementBatchFloor;
            if (batch < floor) batch = floor;
            return System.Math.Min(batch, phase.RemainingSpawnNumber);
        }

        /// <summary>
        /// Returns the victim side as the opener wired it. Both the
        /// Phase D opener (player ambushes AI) and the Phase F opener
        /// (AI ambushes player) place the victim on <c>Defender</c>, so
        /// in practice this always returns Defender; the method exists
        /// so a future opener that flips sides doesn't silently break
        /// the wave target.
        /// </summary>
        private BattleSideEnum DetermineVictimSide()
        {
            var controller = Mission.GetMissionBehavior<AmbushMissionController>();
            if (controller != null)
            {
                // Victim is the opposite of the ambusher side. Controller
                // resolves this from party-side data at AfterStart.
                return controller.IsPlayerAmbusher
                    ? BattleSideEnum.Defender  // player attacker, AI victim on defender
                    : BattleSideEnum.Defender; // AI attacker, player victim on defender
            }
            return BattleSideEnum.Defender;
        }
    }
}
