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
        private BasicMissionTimer _timer = new BasicMissionTimer();
        private int _waveIndex;

        public AmbushReinforcementSpawnTimer(TORMissionAmbushModel model)
        {
            _initialWaveIntervals = model.InitialWaveIntervals;
            _steadyWaveInterval = model.SteadyWaveInterval;
        }

        public bool Check(BattleSideEnum side)
        {
            if (side != BattleSideEnum.Defender) return false;

            float interval = _waveIndex < _initialWaveIntervals.Length
                ? _initialWaveIntervals[_waveIndex]
                : _steadyWaveInterval;

            if (_timer.ElapsedTime < interval) return false;

            _timer.Reset();
            _waveIndex++;
            return true;
        }

        public void ResetTimer(BattleSideEnum side)
        {
            if (side == BattleSideEnum.Defender) _timer.Reset();
        }
    }
}
