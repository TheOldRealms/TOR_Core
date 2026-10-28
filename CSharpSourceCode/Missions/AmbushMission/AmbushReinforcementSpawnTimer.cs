using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TOR_Core.Models;

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
            if (side != BattleSideEnum.Defender) return false;

            float interval = _waveIndex < _initialWaveIntervals.Length
                ? _initialWaveIntervals[_waveIndex]
                : _steadyWaveInterval;
            interval += _currentWaveDelayBonus;

            if (_timer.ElapsedTime < interval) return false;

            _timer.Reset();
            _waveIndex++;
            _currentWaveDelayBonus = 0f;
            return true;
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
            TaleWorlds.Library.Debug.Print(
                "[AmbushReinforcementSpawnTimer] patrol cleared bonus: +"
                + _currentWaveDelayBonus.ToString("F1") + "s on current wave (interval "
                + currentInterval + "s, remaining " + remaining.ToString("F1") + "s)");
        }
    }
}
