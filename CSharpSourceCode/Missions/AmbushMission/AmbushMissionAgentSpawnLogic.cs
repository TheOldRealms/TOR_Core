using System;
using System.Reflection;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace TOR_Core.Missions.AmbushMission
{
    /// <summary>
    /// Spawn-logic subclass for ambush missions. Keeps the vanilla type
    /// identity so dependents that do
    /// <c>Mission.GetMissionBehavior&lt;DefaultBattleMissionAgentSpawnLogic&gt;()</c>
    /// (vanilla <c>BattleEndLogic</c>, TOR's <c>ReinforcementMissionLogic</c>,
    /// our own end-logic) still find it, but exposes a small API that
    /// encapsulates all the per-ambush bookkeeping the controller used to
    /// poke across several callsites.
    ///
    /// Problem it solves: ambush missions bypass vanilla's initial spawn
    /// (we drain the supplier ourselves at authored scene positions so
    /// agents don't spawn at (0, 0, 0) and hang the walkable-area check).
    /// Vanilla's <c>CheckDeployment</c>, <c>IsSideDepleted</c>, and
    /// reinforcement-pool math then all assume the initial spawn flowed
    /// through them. Without matching bookkeeping, the reinforcement
    /// loop never ticks, mortality counts drift negative, and the end
    /// condition can never resolve. This class exposes two public
    /// methods (<see cref="BumpSpawnedCount"/>,
    /// <see cref="MarkInitialSpawnComplete"/>) that bring those counters
    /// in line in one place, and one read helper
    /// (<see cref="IsAmbushPoolDepleted"/>) our end-logic uses as the
    /// authoritative pool-depleted signal.
    ///
    /// The reflection on <c>_numSpawnedTroops</c> lives here rather than
    /// on the controller because the field is private to vanilla's
    /// <c>MissionBattleSideSpawnContext</c> and no subclass can reach it
    /// directly. Encapsulated in one spot it's survivable; sprayed
    /// across the controller it wasn't.
    /// </summary>
    public class AmbushMissionAgentSpawnLogic : DefaultBattleMissionAgentSpawnLogic
    {
        private readonly FieldInfo _numSpawnedTroopsField;
        private readonly Array _battleSideSpawnContextsArray;

        public AmbushMissionAgentSpawnLogic(IMissionTroopSupplier[] suppliers,
                                             BattleSideEnum playerSide,
                                             Mission.BattleSizeType battleSizeType)
            : base(suppliers, playerSide, battleSizeType)
        {
            var contextsField = typeof(DefaultBattleMissionAgentSpawnLogic).GetField(
                "_battleSideSpawnContexts",
                BindingFlags.Instance | BindingFlags.NonPublic);
            _battleSideSpawnContextsArray = contextsField?.GetValue(this) as Array;
            if (_battleSideSpawnContextsArray == null
                || _battleSideSpawnContextsArray.Length == 0)
                return;
            var sampleContext = _battleSideSpawnContextsArray.GetValue(0);
            if (sampleContext == null) return;
            _numSpawnedTroopsField = sampleContext.GetType().GetField(
                "_numSpawnedTroops",
                BindingFlags.Instance | BindingFlags.NonPublic);
        }

        /// <summary>
        /// Increments the per-side spawned-troop counter on
        /// <c>MissionBattleSideSpawnContext</c>. Called once per agent
        /// our custom spawn produces, mirroring the <c>_numSpawnedTroops++</c>
        /// vanilla does inside its own <c>SpawnTroops</c> path. Without
        /// this, <c>NumberOfActiveTroops = _numSpawnedTroops - supplier
        /// .NumRemovedTroops</c> would go negative as agents die, and the
        /// post-battle state machine that reads it would stall.
        /// </summary>
        public void BumpSpawnedCount(BattleSideEnum side)
        {
            if (_battleSideSpawnContextsArray == null
                || _numSpawnedTroopsField == null) return;
            int idx = (int)side;
            if (idx < 0 || idx >= _battleSideSpawnContextsArray.Length) return;
            var ctx = _battleSideSpawnContextsArray.GetValue(idx);
            if (ctx == null) return;
            int current = (int)_numSpawnedTroopsField.GetValue(ctx);
            _numSpawnedTroopsField.SetValue(ctx, current + 1);
        }

        /// <summary>
        /// One-shot bookkeeping call after the controller's custom initial
        /// spawn finishes. Calls <c>MissionSpawnPhase.OnInitialTroopsSpawned()</c>
        /// on each side's active phase, which zeros <c>InitialSpawnNumber</c>
        /// so vanilla's <c>IsInitialSpawnOver</c> flips true. Without that
        /// the <c>CheckDeployment</c> gate at the top of the vanilla
        /// spawn-logic <c>OnMissionTick</c> would permanently block the
        /// reinforcement-timer check.
        ///
        /// Phase <c>RemainingSpawnNumber</c> is deliberately NOT touched:
        /// vanilla's <c>AddPhase</c> (called by <c>InitWithSinglePhase</c>)
        /// already sets it to <c>totalSpawn - initialSpawn</c>, so by the
        /// time this method runs the pool count is already correct.
        /// A second subtraction here would double-count and leave the
        /// pool stuck at zero even when real reinforcements are queued.
        ///
        /// Then <c>OnSideDeploymentOver</c> for both sides fires
        /// <c>Mission.OnInitialSpawnCompleted</c> and the formation
        /// query-system refresh. Not strictly required for end-condition
        /// correctness but matches the sequence vanilla uses.
        ///
        /// The <c>defenderSpawned</c> / <c>attackerSpawned</c> args are
        /// no longer load-bearing for the pool math; kept for symmetry
        /// with any future divergence between "what the phase expected"
        /// and "what we actually spawned" (e.g. partial-supplier cases).
        /// </summary>
        public void MarkInitialSpawnComplete(int defenderSpawned, int attackerSpawned)
        {
            _ = defenderSpawned;
            _ = attackerSpawned;
            DefenderActivePhase?.OnInitialTroopsSpawned();
            AttackerActivePhase?.OnInitialTroopsSpawned();
            OnSideDeploymentOver(BattleSideEnum.Defender);
            OnSideDeploymentOver(BattleSideEnum.Attacker);
        }

        /// <summary>
        /// Called once per reinforcement agent our own wave spawner
        /// emits. Does the two bookkeeping touches the per-agent path
        /// needs: bump <c>_numSpawnedTroops</c> so mortality math on the
        /// context stays correct as the new agent eventually dies, and
        /// decrement the phase's <c>RemainingSpawnNumber</c> so the pool
        /// visibly drains toward <c>IsAmbushPoolDepleted</c> returning
        /// true after the last wave fires.
        /// </summary>
        public void OnReinforcementSpawned(BattleSideEnum side)
        {
            BumpSpawnedCount(side);
            var phase = side == BattleSideEnum.Defender
                ? DefenderActivePhase
                : AttackerActivePhase;
            if (phase != null && phase.RemainingSpawnNumber > 0)
                phase.RemainingSpawnNumber--;
        }

        /// <summary>
        /// Our own pool-depleted check, read by <c>AmbushEndLogic</c> and
        /// <c>AmbushedEndLogic</c> for the "no more reinforcement waves
        /// can arrive" half of the TotalVictory / AmbushRepelled gate.
        /// Decoupled from vanilla's <c>IsSideDepleted</c> which also
        /// requires <c>NumberOfActiveTroops == 0</c>: callers compose the
        /// two conditions themselves (field clear AND pool empty).
        /// </summary>
        public bool IsAmbushPoolDepleted(BattleSideEnum side)
        {
            var phase = side == BattleSideEnum.Defender
                ? DefenderActivePhase
                : AttackerActivePhase;
            return phase == null || phase.RemainingSpawnNumber <= 0;
        }
    }
}
