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
        private readonly float _activationRadius;
        private readonly float _activationRadiusSq;
        private readonly bool _ignoreHeight;
        private readonly Dictionary<Agent, int> _lastAssignments = new Dictionary<Agent, int>();

        public IReadOnlyList<WorldPosition> TacticalChildren => _tacticalChildren;

        public AmbushBehaviorDefendTacticalPoint(Formation formation,
                                                  WorldPosition parentPosition,
                                                  IEnumerable<WorldPosition> tacticalChildren,
                                                  float activationRadius,
                                                  bool ignoreHeight)
            : base(formation)
        {
            _parentPosition = parentPosition;
            _tacticalChildren = tacticalChildren?.ToList() ?? new List<WorldPosition>();
            _activationRadius = activationRadius > 0f ? activationRadius : 30f;
            _activationRadiusSq = _activationRadius * _activationRadius;
            _ignoreHeight = ignoreHeight;
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
        /// Proximity-gated reconciliation pass. Only tactical children
        /// within <see cref="ActiveRadius"/> of the formation's plate
        /// position (<see cref="Formation.OrderPosition"/>) are
        /// considered "reachable" this tick. Carries previous
        /// agent-child pairings forward where still valid (so agents
        /// don't shuffle between points each tick), fills unassigned
        /// reachable children with the closest living unassigned agent
        /// (so when an archer dies a nearby squadmate steps in), and
        /// clears scripted positions on agents that lost their
        /// assignment (so they rejoin the formation at its plate).
        ///
        /// Flags: <see cref="Agent.AIScriptedFrameFlags.RangerCanMoveForClearTarget"/>
        /// so assigned archers can shift laterally for a clean shot.
        /// </summary>
        public void AssignScriptedPositionsToAgents()
        {
            if (_tacticalChildren.Count == 0) return;
            var plate = Formation.OrderPosition;
            var agents = new List<Agent>();
            Formation.ApplyActionOnEachUnit(a =>
            {
                if (a != null && a.IsActive()) agents.Add(a);
            });

            var newAssignments = new Dictionary<Agent, int>();
            var claimedChildren = new HashSet<int>();

            // Pass 1: carry over previous assignments where still valid.
            foreach (var kv in _lastAssignments)
            {
                var agent = kv.Key;
                int idx = kv.Value;
                if (agent == null || !agent.IsActive()) continue;
                if (agent.Formation != Formation) continue;
                if (idx < 0 || idx >= _tacticalChildren.Count) continue;
                if (!IsChildReachable(_tacticalChildren[idx], plate)) continue;
                newAssignments[agent] = idx;
                claimedChildren.Add(idx);
            }

            // Pass 2: fill remaining reachable children with closest free agent.
            for (int i = 0; i < _tacticalChildren.Count; i++)
            {
                if (claimedChildren.Contains(i)) continue;
                var child = _tacticalChildren[i];
                if (!IsChildReachable(child, plate)) continue;
                Agent best = null;
                float bestDistSq = float.MaxValue;
                foreach (var agent in agents)
                {
                    if (newAssignments.ContainsKey(agent)) continue;
                    float d = child.AsVec2.DistanceSquared(agent.Position.AsVec2);
                    if (d < bestDistSq)
                    {
                        bestDistSq = d;
                        best = agent;
                    }
                }
                if (best == null) continue;
                newAssignments[best] = i;
                claimedChildren.Add(i);
            }

            // Apply new assignments.
            foreach (var kv in newAssignments)
            {
                var pos = _tacticalChildren[kv.Value];
                kv.Key.SetScriptedPosition(ref pos,
                    addHumanLikeDelay: false,
                    additionalFlags: Agent.AIScriptedFrameFlags.RangerCanMoveForClearTarget);
            }

            // Release agents that used to have a scripted spot but lost it
            // (out of range, or their child was claimed by someone else).
            foreach (var kv in _lastAssignments)
            {
                var agent = kv.Key;
                if (agent == null || !agent.IsActive()) continue;
                if (newAssignments.ContainsKey(agent)) continue;
                var flags = agent.GetScriptedFlags();
                if ((flags & Agent.AIScriptedFrameFlags.GoToPosition) == 0) continue;
                agent.DisableScriptedMovement();
            }

            _lastAssignments.Clear();
            foreach (var kv in newAssignments) _lastAssignments[kv.Key] = kv.Value;
        }

        /// <summary>
        /// 2D circle or 3D sphere check against <see cref="_activationRadius"/>.
        /// In 3D mode we approximate the plate's elevation with the
        /// authored parent-spawn-point Y, which matches scener intent:
        /// the volume is centred on the spawn point, not on the
        /// terrain below the plate.
        /// </summary>
        private bool IsChildReachable(WorldPosition child, Vec2 plate)
        {
            if (_ignoreHeight)
            {
                return child.AsVec2.DistanceSquared(plate) <= _activationRadiusSq;
            }
            var childV3 = child.GetGroundVec3();
            var parentV3 = _parentPosition.GetGroundVec3();
            var plateApproxV3 = new Vec3(plate.X, plate.Y, parentV3.Z);
            return plateApproxV3.DistanceSquared(childV3) <= _activationRadiusSq;
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
