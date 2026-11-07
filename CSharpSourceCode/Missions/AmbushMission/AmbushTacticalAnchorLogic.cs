using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace TOR_Core.Missions.AmbushMission
{
    /// <summary>
    /// Mission-level owner of the ranged-formation tactical-point anchors.
    /// Holds one <see cref="AmbushBehaviorDefendTacticalPoint"/> per
    /// anchored formation and periodically re-asserts the per-agent
    /// scripted positions so agents stay on their tactical children for
    /// the whole mission. Design preference (confirmed in the task
    /// thread): archers do NOT release when the player issues a direct
    /// order; the formation plate can still be commanded, but individual
    /// agents stay pinned to the tactical children via
    /// <c>Agent.SetScriptedPosition</c>. Only an explicit
    /// <see cref="RebindAnchor"/> or <see cref="UnregisterAnchor"/> call
    /// (hit by the sidebar spot picker reassigning the formation) can
    /// change that.
    ///
    /// Reassignment rebinds on the new spot's children; a reassignment
    /// to a spot with no tactical children unregisters and releases.
    ///
    /// Tick rate is coarse (<see cref="ReassignIntervalSeconds"/>):
    /// per-agent native calls aren't free and positions are stable.
    /// </summary>
    public class AmbushTacticalAnchorLogic : MissionLogic
    {
        private const float ReassignIntervalSeconds = 2.0f;

        private readonly Dictionary<Formation, AmbushBehaviorDefendTacticalPoint> _anchors
            = new Dictionary<Formation, AmbushBehaviorDefendTacticalPoint>();
        private float _reassignAccumulator;

        /// <summary>
        /// First-time anchor for a formation (initial spawn placement).
        /// Teleports agents to their tactical children so the opening
        /// scene reads correctly without a visible walk from the
        /// formation plate to the hill ledges.
        /// </summary>
        public void RegisterAnchor(Formation formation,
                                   AmbushBehaviorDefendTacticalPoint behavior)
        {
            if (formation == null || behavior == null) return;
            _anchors[formation] = behavior;
            behavior.AssignScriptedPositionsToAgents(true);
        }

        /// <summary>
        /// Switch an already-anchored formation to a new tactical spot
        /// mid-mission (tactic re-evaluated, picker reassigned, etc.).
        /// Does NOT teleport. Agents walk to the new tactical children
        /// via their updated scripted positions. Mid-battle snapping
        /// looks bad to the player and can drag archers through enemy
        /// formations.
        /// </summary>
        public void RebindAnchor(Formation formation,
                                 AmbushBehaviorDefendTacticalPoint behavior)
        {
            if (formation == null || behavior == null) return;
            _anchors[formation] = behavior;
            behavior.AssignScriptedPositionsToAgents();
        }

        public void UnregisterAnchor(Formation formation)
        {
            if (formation == null) return;
            if (!_anchors.Remove(formation)) return;
            AmbushBehaviorDefendTacticalPoint.ClearScriptedPositions(formation);
        }

        public override void OnMissionTick(float dt)
        {
            base.OnMissionTick(dt);
            _reassignAccumulator += dt;
            if (_reassignAccumulator < ReassignIntervalSeconds) return;
            _reassignAccumulator = 0f;

            // Drop anchors whose formation has either emptied or shifted
            // away from a ranged PhysicalClass (archers dying off → the
            // formation's dominant class flips to Infantry, our tactical
            // scripted positions start pulling the surviving melee heroes
            // onto hill firing spots). Clean up before reassigning so the
            // stale ticks stop.
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

        private static bool IsRangedClass(FormationClass cls)
        {
            return cls == FormationClass.Ranged
                || cls == FormationClass.Skirmisher
                || cls == FormationClass.HorseArcher;
        }
    }
}
