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
        /// spawn finishes. For each side's active phase:
        ///  - <c>OnInitialTroopsSpawned()</c> zeros <c>InitialSpawnNumber</c>
        ///    so vanilla's <c>IsInitialSpawnOver</c> flips true, unblocking
        ///    the <c>CheckDeployment</c> gate at the top of the vanilla
        ///    spawn-logic <c>OnMissionTick</c> that otherwise permanently
        ///    blocks reinforcement checks.
        ///  - Decrement <c>RemainingSpawnNumber</c> by the actual count
        ///    we spawned. Vanilla's <c>SpawnTroops</c> does this itself;
        ///    we bypass that path, so the phase otherwise keeps a
        ///    <c>RemainingSpawnNumber</c> inflated by the initial batch
        ///    and nothing can ever report the pool as depleted.
        /// Then <c>OnSideDeploymentOver</c> for both sides, which fires
        /// <c>Mission.OnInitialSpawnCompleted</c> and the formation
        /// query-system refresh. Not strictly required for end-condition
        /// correctness but matches the sequence vanilla uses and keeps
        /// listener events firing consistently.
        /// </summary>
        public void MarkInitialSpawnComplete(int defenderSpawned, int attackerSpawned)
        {
            var defPhase = DefenderActivePhase;
            if (defPhase != null)
            {
                defPhase.OnInitialTroopsSpawned();
                defPhase.RemainingSpawnNumber -= defenderSpawned;
                if (defPhase.RemainingSpawnNumber < 0)
                    defPhase.RemainingSpawnNumber = 0;
            }
            var atkPhase = AttackerActivePhase;
            if (atkPhase != null)
            {
                atkPhase.OnInitialTroopsSpawned();
                atkPhase.RemainingSpawnNumber -= attackerSpawned;
                if (atkPhase.RemainingSpawnNumber < 0)
                    atkPhase.RemainingSpawnNumber = 0;
            }
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
