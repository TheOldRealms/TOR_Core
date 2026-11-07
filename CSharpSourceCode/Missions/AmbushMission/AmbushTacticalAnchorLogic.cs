using System.Collections.Generic;
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

        public void RegisterAnchor(Formation formation,
                                   AmbushBehaviorDefendTacticalPoint behavior)
        {
            if (formation == null || behavior == null) return;
            _anchors[formation] = behavior;
            behavior.AssignScriptedPositionsToAgents(teleport: true);
        }

        public void RebindAnchor(Formation formation,
                                 AmbushBehaviorDefendTacticalPoint behavior)
        {
            if (formation == null || behavior == null) return;
            _anchors[formation] = behavior;
            behavior.AssignScriptedPositionsToAgents(teleport: true);
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
            foreach (var kv in _anchors)
            {
                if (kv.Key == null) continue;
                kv.Value.AssignScriptedPositionsToAgents();
            }
        }
    }
}
