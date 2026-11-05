using System.Collections.Generic;
using TaleWorlds.MountAndBlade;

namespace TOR_Core.Missions.AmbushMission
{
    /// <summary>
    /// Mission-level owner of the ranged-formation anchors introduced by
    /// <see cref="AmbushBehaviorDefendTacticalPoint"/>. Re-runs per-agent
    /// scripted-position assignment while a formation is captain-AI-controlled,
    /// and clears those scripted positions when the player takes direct
    /// control (any player order flips IsAIControlled off via
    /// <c>OrderController.BeforeSetOrder</c>).
    ///
    /// Tick rate is deliberately coarse: the AI rewrites agent scripted
    /// positions every <see cref="ReassignIntervalSeconds"/> rather than
    /// every frame, because stable positions matter more than per-tick
    /// correctness and the per-agent native call isn't free.
    /// </summary>
    public class AmbushTacticalAnchorLogic : MissionLogic
    {
        private const float ReassignIntervalSeconds = 2.0f;

        private class AnchorState
        {
            public AmbushBehaviorDefendTacticalPoint Behavior;
            public bool WasAIControlledLastTick;
        }

        private readonly Dictionary<Formation, AnchorState> _anchors
            = new Dictionary<Formation, AnchorState>();
        private float _reassignAccumulator;

        public void RegisterAnchor(Formation formation,
                                   AmbushBehaviorDefendTacticalPoint behavior)
        {
            if (formation == null || behavior == null) return;
            _anchors[formation] = new AnchorState
            {
                Behavior = behavior,
                WasAIControlledLastTick = formation.IsAIControlled,
            };
            if (formation.IsAIControlled) behavior.AssignScriptedPositionsToAgents();
        }

        public override void OnMissionTick(float dt)
        {
            base.OnMissionTick(dt);
            _reassignAccumulator += dt;
            bool reassignNow = _reassignAccumulator >= ReassignIntervalSeconds;
            if (reassignNow) _reassignAccumulator = 0f;

            foreach (var kv in _anchors)
            {
                var formation = kv.Key;
                var state = kv.Value;
                if (formation == null) continue;
                bool isAi = formation.IsAIControlled;
                if (state.WasAIControlledLastTick && !isAi)
                {
                    AmbushBehaviorDefendTacticalPoint.ClearScriptedPositions(formation);
                }
                else if (isAi && reassignNow)
                {
                    state.Behavior.AssignScriptedPositionsToAgents();
                }
                state.WasAIControlledLastTick = isAi;
            }
        }
    }
}
