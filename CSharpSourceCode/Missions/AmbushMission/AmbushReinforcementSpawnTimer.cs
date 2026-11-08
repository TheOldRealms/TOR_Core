using NLog;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TOR_Core.Models;
using TOR_Core.Utilities;

namespace TOR_Core.Missions.AmbushMission
{
    /// <summary>
    /// Escalating reinforcement cadence for the enemy side. Pressure comes from
    /// numbers rather than a hard timeout — the player can take as long as they
    /// want, but every N seconds (per <see cref="TORMissionAmbushModel.SteadyWaveInterval"/>)
    /// another wave crashes in once the schedule plateaus. Retreat is the only
    /// way out.
    ///
    /// Attacker side (our player, by MapEvent convention) gets no reinforcements
    /// per the locked Phase D design — enforced by the spawn settings, not here.
    /// </summary>
    public class AmbushReinforcementSpawnTimer : ICustomReinforcementSpawnTimer
    {
        private readonly float[] _initialWaveIntervals;
        private readonly float _steadyWaveInterval;
        private readonly float _patrolClearedDelayBonus;
        private BasicMissionTimer _timer = new BasicMissionTimer();
        private int _waveIndex;
        private float _lastCheckLogSeconds;
        // One-shot bump applied to the current wave's effective interval when
        // the player kills the initial patrol while this wave is still ticking.
        // Cleared after the bonus wave fires so later waves resume normal cadence.
        private float _currentWaveDelayBonus;
        private bool _patrolClearedBonusConsumed;

        public AmbushReinforcementSpawnTimer(TORMissionAmbushModel model)
        {
            _initialWaveIntervals = model.InitialWaveIntervals;
            _steadyWaveInterval = model.SteadyWaveInterval;
            _patrolClearedDelayBonus = model.InitialPatrolClearedWaveDelayBonus;
        }

        public bool Check(BattleSideEnum side)
        {
            float elapsed = _timer.ElapsedTime;
            // Rate-limited log on Defender side only: prints once every
            // ~5 seconds of elapsed-timer time. Visible in the log file
            // regardless of whether the interval has been reached, so we
            // can tell Check() is being driven at all.
            if (side == BattleSideEnum.Defender && elapsed - _lastCheckLogSeconds >= 5f)
            {
                _lastCheckLogSeconds = elapsed;
                TORCommon.Log(LogLevel.Info,
                    "[AmbushReinforcement][poll] side=Defender waveIdx=" + _waveIndex
                    + " elapsed=" + elapsed.ToString("F1") + "s");
            }

            if (side != BattleSideEnum.Defender) return false;

            float interval = _waveIndex < _initialWaveIntervals.Length
                ? _initialWaveIntervals[_waveIndex]
                : _steadyWaveInterval;
            interval += _currentWaveDelayBonus;

            if (elapsed < interval) return false;

            TORCommon.Log(LogLevel.Info,
                "[AmbushReinforcement] wave " + (_waveIndex + 1)
                + " fired after " + _timer.ElapsedTime.ToString("F1")
                + "s (interval " + interval.ToString("F1") + "s)");
            LogDeploymentPlanFrames();
            LogDefenderFormationStates();
            _timer.Reset();
            _waveIndex++;
            _currentWaveDelayBonus = 0f;
            return true;
        }

        /// <summary>
        /// Diagnostic (task #79 follow-up): dumps the deployment plan's
        /// reinforcement frame for each defender class alongside the frame
        /// vanilla will use to position the formation on the first wave.
        /// Compares planned-vs-actual so we can see exactly where vanilla
        /// is placing the reinforcement arrival.
        /// </summary>
        private static void LogDeploymentPlanFrames()
        {
            var mission = Mission.Current;
            if (mission?.DefenderTeam == null) return;
            FormationClass[] classes = {
                FormationClass.Infantry,
                FormationClass.Ranged,
                FormationClass.Cavalry,
                FormationClass.HorseArcher,
            };
            foreach (var cls in classes)
            {
                mission.GetFormationSpawnFrame(mission.DefenderTeam, cls, true,
                    out var pos, out var dir);
                var ground = pos.IsValid ? pos.GetGroundVec3() : TaleWorlds.Library.Vec3.Zero;
                TORCommon.Log(LogLevel.Info,
                    "[AmbushReinforcement][plan] side=Defender class=" + cls
                    + " frame=" + ground
                    + " dir=" + dir
                    + " valid=" + pos.IsValid);
            }
        }

        /// <summary>
        /// Diagnostic: logs each defender formation's plate + whether
        /// vanilla has already set its positioning (HasBeenPositioned).
        /// If HasBeenPositioned is false at wave-fire time, vanilla's
        /// SpawnTroops will call SetFormationPositioningFromDeploymentPlan
        /// and move the plate to the reinforcement frame.
        /// </summary>
        private static void LogDefenderFormationStates()
        {
            var mission = Mission.Current;
            if (mission?.DefenderTeam == null) return;
            foreach (var f in mission.DefenderTeam.FormationsIncludingEmpty)
            {
                if (f == null || f.CountOfUnits <= 0) continue;
                TORCommon.Log(LogLevel.Info,
                    "[AmbushReinforcement][form] class=" + f.FormationIndex
                    + " units=" + f.CountOfUnits
                    + " orderPos=" + f.OrderPosition
                    + " hasBeenPositioned=" + f.HasBeenPositioned);
            }
        }

        public void ResetTimer(BattleSideEnum side)
        {
            if (side == BattleSideEnum.Defender) _timer.Reset();
        }

        /// <summary>
        /// Called by <see cref="AmbushMissionController"/> the frame the initial
        /// patrol becomes fully defeated. Extends the current wave's remaining
        /// interval by <see cref="TORMissionAmbushModel.InitialPatrolClearedWaveDelayBonus"/>
        /// (default 30%), one-shot. "You cleared Phase 1 fast — buy yourself a
        /// regroup window before the next wave hits."
        /// </summary>
        public void OnInitialPatrolDefeated()
        {
            if (_patrolClearedBonusConsumed) return;
            _patrolClearedBonusConsumed = true;
            float currentInterval = _waveIndex < _initialWaveIntervals.Length
                ? _initialWaveIntervals[_waveIndex]
                : _steadyWaveInterval;
            float remaining = currentInterval - _timer.ElapsedTime;
            if (remaining <= 0f) return;  // next wave is already overdue; nothing to delay
            _currentWaveDelayBonus = remaining * _patrolClearedDelayBonus;
        }
    }
}
