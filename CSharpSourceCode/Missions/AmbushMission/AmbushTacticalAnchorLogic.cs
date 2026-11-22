using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace TOR_Core.Missions.AmbushMission
{
    /// <summary>
    /// Mission-level owner of the ranged-formation tactical-point anchors.
    /// </summary>
    public class AmbushTacticalAnchorLogic : MissionLogic
    {
        private const float ReassignIntervalSeconds = 2.0f;

        private readonly Dictionary<Formation, AmbushBehaviorDefendTacticalPoint> _anchors
            = new Dictionary<Formation, AmbushBehaviorDefendTacticalPoint>();
        private float _reassignAccumulator;
        private Team _subscribedPlayerTeam;

        /// <summary>
        /// Register an anchor and teleport agents onto tactical children at spawn.
        /// </summary>
        public void RegisterAnchor(Formation formation,
                                   AmbushBehaviorDefendTacticalPoint behavior)
        {
            if (formation == null || behavior == null) return;
            _anchors[formation] = behavior;
            behavior.AssignScriptedPositionsToAgents(true);
        }

        /// <summary>
        /// Switch an anchored formation to a new spot without teleporting.
        /// </summary>
        public void RebindAnchor(Formation formation,
                                 AmbushBehaviorDefendTacticalPoint behavior)
        {
            if (formation == null || behavior == null) return;
            _anchors[formation] = behavior;
            behavior.AssignScriptedPositionsToAgents();
        }

        /// <summary>
        /// Unregister an anchor and clear scripted positions on its agents.
        /// </summary>
        public void UnregisterAnchor(Formation formation)
        {
            if (formation == null) return;
            if (!_anchors.Remove(formation)) return;
            AmbushBehaviorDefendTacticalPoint.ClearScriptedPositions(formation);
        }

        /// <summary>
        /// Subscribe to the player order controller so plate-moving orders release matching anchors.
        /// </summary>
        public override void AfterStart()
        {
            base.AfterStart();
            if (Mission.PlayerTeam?.PlayerOrderController != null)
            {
                _subscribedPlayerTeam = Mission.PlayerTeam;
                _subscribedPlayerTeam.PlayerOrderController.OnOrderIssued += OnPlayerOrderIssued;
            }
        }

        protected override void OnEndMission()
        {
            if (_subscribedPlayerTeam?.PlayerOrderController != null)
            {
                _subscribedPlayerTeam.PlayerOrderController.OnOrderIssued -= OnPlayerOrderIssued;
                _subscribedPlayerTeam = null;
            }
            base.OnEndMission();
        }

        /// <summary>
        /// Release the anchor on any formation the player just ordered to move.
        /// </summary>
        private void OnPlayerOrderIssued(OrderType orderType, MBReadOnlyList<Formation> appliedFormations,
                                          OrderController orderController, params object[] delegateParams)
        {
            if (!IsPlateMovingOrder(orderType)) return;
            if (appliedFormations == null || _anchors.Count == 0) return;
            // Can't mutate _anchors while iterating. Collect first.
            List<Formation> toRelease = null;
            for (int i = 0; i < appliedFormations.Count; i++)
            {
                var f = appliedFormations[i];
                if (f != null && _anchors.ContainsKey(f))
                    (toRelease ??= new List<Formation>()).Add(f);
            }
            if (toRelease == null) return;
            foreach (var f in toRelease) UnregisterAnchor(f);
        }

        /// <summary>
        /// True for player orders that move the formation plate rather than just reshape it.
        /// </summary>
        private static bool IsPlateMovingOrder(OrderType orderType)
        {
            // Plate-moving orders only; shape/firing/stance don't release the anchor.
            return orderType == OrderType.Move
                || orderType == OrderType.MoveToLineSegment
                || orderType == OrderType.MoveToLineSegmentWithHorizontalLayout
                || orderType == OrderType.FollowMe
                || orderType == OrderType.Charge
                || orderType == OrderType.ChargeWithTarget
                || orderType == OrderType.Retreat
                || orderType == OrderType.AdvanceTenPaces
                || orderType == OrderType.FallBackTenPaces
                || orderType == OrderType.Advance
                || orderType == OrderType.FallBack;
        }

        public override void OnMissionTick(float dt)
        {
            base.OnMissionTick(dt);
            _reassignAccumulator += dt;
            if (_reassignAccumulator < ReassignIntervalSeconds) return;
            _reassignAccumulator = 0f;

            // Drop anchors whose formation emptied or shifted away from a ranged class.
            List<Formation> toUnregister = null;
            foreach (var kv in _anchors)
            {
                var f = kv.Key;
                if (f == null
                    || f.CountOfUnits == 0
                    || !IsRangedClass(f.PhysicalClass))
                {
                    (toUnregister ??= new List<Formation>()).Add(f);
                }
            }
            if (toUnregister != null)
                foreach (var f in toUnregister) UnregisterAnchor(f);

            foreach (var kv in _anchors)
            {
                if (kv.Key == null) continue;
                kv.Value.AssignScriptedPositionsToAgents();
            }
        }

        /// <summary>
        /// True when the formation class fires projectiles.
        /// </summary>
        private static bool IsRangedClass(FormationClass cls)
        {
            return cls == FormationClass.Ranged
                || cls == FormationClass.Skirmisher
                || cls == FormationClass.HorseArcher;
        }

    }
}
