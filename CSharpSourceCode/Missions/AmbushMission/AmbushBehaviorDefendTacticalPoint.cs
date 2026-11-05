using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace TOR_Core.Missions.AmbushMission
{
    /// <summary>
    /// Captain-AI behavior for ranged ambush formations.
    ///
    /// Problem: vanilla <see cref="BehaviorCharge"/> pulls archers off
    /// their high ground the moment a player clicks Charge, because the
    /// movement order resolves to the closest enemy formation every
    /// tick. Sieges avoid this via <see cref="BehaviorDefendKeyPosition"/>
    /// which pins the formation at a fixed <c>DefensePosition</c> with
    /// weight 10 so it dominates other behaviors.
    ///
    /// This behavior is the ambush analogue. The formation plate stays
    /// at the ambush spawn-point (<see cref="_parentPosition"/>) so the
    /// group reads as one deployment; individual agents are scripted
    /// onto the tactical-position children authored under the spawnpoint
    /// so they physically stand on the authored firing slots. Weight
    /// 10f, same as vanilla.
    ///
    /// Player override is handled externally by
    /// <see cref="AmbushTacticalAnchorLogic"/>: it watches
    /// <see cref="Formation.IsAIControlled"/> and clears scripted
    /// positions when the player issues any direct order (vanilla's
    /// <c>OrderController.BeforeSetOrder</c> flips AIControlOff for any
    /// non-AI-toggle order on player-owned formations).
    /// </summary>
    public class AmbushBehaviorDefendTacticalPoint : BehaviorComponent
    {
        private readonly WorldPosition _parentPosition;
        private readonly List<WorldPosition> _tacticalChildren;

        public IReadOnlyList<WorldPosition> TacticalChildren => _tacticalChildren;

        public AmbushBehaviorDefendTacticalPoint(Formation formation,
                                                  WorldPosition parentPosition,
                                                  IEnumerable<WorldPosition> tacticalChildren)
            : base(formation)
        {
            _parentPosition = parentPosition;
            _tacticalChildren = tacticalChildren?.ToList() ?? new List<WorldPosition>();
            CalculateCurrentOrder();
        }

        protected override void CalculateCurrentOrder()
        {
            CurrentOrder = MovementOrder.MovementOrderMove(_parentPosition);
            var facingEnemy = Formation.CachedClosestEnemyFormation;
            if (facingEnemy != null)
            {
                var dir = (facingEnemy.Formation.CachedMedianPosition.AsVec2
                           - Formation.CachedAveragePosition).Normalized();
                CurrentFacingOrder = FacingOrder.FacingOrderLookAtDirection(dir);
            }
            else
            {
                CurrentFacingOrder = FacingOrder.FacingOrderLookAtDirection(Formation.Direction);
            }
        }

        public override void TickOccasionally()
        {
            CalculateCurrentOrder();
            Formation.SetMovementOrder(CurrentOrder);
            Formation.SetFacingOrder(CurrentFacingOrder);
            Formation.SetFiringOrder(FiringOrder.FiringOrderFireAtWill);
        }

        protected override void OnBehaviorActivatedAux()
        {
            Formation.SetArrangementOrder(ArrangementOrder.ArrangementOrderLoose);
            Formation.SetFiringOrder(FiringOrder.FiringOrderFireAtWill);
            Formation.SetFormOrder(FormOrder.FormOrderWide);
        }

        protected override float GetAiWeight() => 10f;

        /// <summary>
        /// Assigns one tactical child position per agent in the formation,
        /// round-robin. Called by <see cref="AmbushTacticalAnchorLogic"/>
        /// once per few ticks while the formation is captain-controlled.
        /// Flags: <see cref="Agent.AIScriptedFrameFlags.RangerCanMoveForClearTarget"/>
        /// so archers can shift laterally for a clean shot.
        /// </summary>
        public void AssignScriptedPositionsToAgents()
        {
            if (_tacticalChildren.Count == 0) return;
            int childIdx = 0;
            var agents = new List<Agent>();
            Formation.ApplyActionOnEachUnit(a => agents.Add(a));
            agents.Sort((a, b) => a.Index.CompareTo(b.Index));
            foreach (var agent in agents)
            {
                if (agent == null || !agent.IsActive()) continue;
                var pos = _tacticalChildren[childIdx % _tacticalChildren.Count];
                agent.SetScriptedPosition(ref pos,
                    addHumanLikeDelay: false,
                    additionalFlags: Agent.AIScriptedFrameFlags.RangerCanMoveForClearTarget);
                childIdx++;
            }
        }

        public static void ClearScriptedPositions(Formation formation)
        {
            if (formation == null) return;
            formation.ApplyActionOnEachUnit(a =>
            {
                if (a == null || !a.IsActive()) return;
                var flags = a.GetScriptedFlags();
                if ((flags & Agent.AIScriptedFrameFlags.GoToPosition) == 0) return;
                a.DisableScriptedMovement();
            });
        }
    }
}
