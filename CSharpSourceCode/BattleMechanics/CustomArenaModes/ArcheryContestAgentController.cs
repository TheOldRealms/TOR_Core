using SandBox.Tournaments.MissionLogics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TOR_Core.Missions;

namespace TOR_Core.BattleMechanics.CustomArenaModes
{
    public class ArcheryContestAgentController : AgentController
    {
        private List<DestructableComponent> _targetList;
        private DestructableComponent _target;

        public void OnTick()
        {
            if (!Owner.IsAIControlled)
            {
                return;
            }
            UpdateTarget();
        }

        public void SetTargets(List<DestructableComponent> targetList)
        {
            _targetList = targetList;
            _target = null;
        }

        private void UpdateTarget()
        {
            if (_target == null || _target.IsDestroyed)
            {
                SelectNewTarget();
            }
        }

        private void SelectNewTarget()
        {
            if (Owner.IsPlayerControlled) return;
            List<KeyValuePair<float, DestructableComponent>> list = [];
            foreach (DestructableComponent destructableComponent in _targetList)
            {
                float score = GetScore(destructableComponent);
                if (score > 0f)
                {
                    list.Add(new KeyValuePair<float, DestructableComponent>(score, destructableComponent));
                }
            }
            if (list.Count == 0)
            {
                _target = null;
                Owner.DisableScriptedCombatMovement();
                WorldPosition worldPosition = Owner.GetWorldPosition();
                Owner.SetScriptedPosition(ref worldPosition, false, Agent.AIScriptedFrameFlags.None);
            }
            else
            {
                List<KeyValuePair<float, DestructableComponent>> list2 = [.. (from x in list
                                                                          orderby x.Key descending
                                                                          select x)];
                _target = list2.MaxBy(x => x.Key).Value;
            }
            if (_target != null)
            {
                Owner.SetScriptedTargetEntityAndPosition(_target.GameEntity, Owner.GetWorldPosition(), Agent.AISpecialCombatModeFlags.IgnoreAmmoLimitForRangeCalculation, false);
            }
        }

        private float GetScore(DestructableComponent target)
        {
            if (!target.IsDestroyed)
            {
                return 1f / Owner.Position.DistanceSquared(target.GameEntity.GlobalPosition);
            }
            return 0f;
        }

        public void OnTargetHit(Agent agent, DestructableComponent target)
        {
            if (agent == Owner || target == _target)
            {
                SelectNewTarget();
            }
        }
    }
}
