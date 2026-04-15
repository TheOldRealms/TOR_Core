using TaleWorlds.MountAndBlade;
﻿using TaleWorlds.Library;
using TOR_Core.AbilitySystem;
using TOR_Core.BattleMechanics.AI.CommonAIFunctions;

namespace TOR_Core.BattleMechanics.AI.CastingAI.AgentCastingBehavior
{
    public class AoETargetedCastingBehavior : MissileCastingBehavior
    {
        public AoETargetedCastingBehavior(Agent agent, AbilityTemplate template, int abilityIndex) : base(agent, template, abilityIndex)
        {
        }

        protected override bool HaveLineOfSightToTarget(Target target)
        {
            switch (AbilityTemplate.AbilityEffectType)
            {
                case AbilityEffectType.Bombardment:
                case AbilityEffectType.Vortex:
                case AbilityEffectType.Hex:
                case AbilityEffectType.Augment:
                case AbilityEffectType.Heal:
                    {
                        var targetPoint = target.GetPositionPrioritizeCalculated();
                        if (targetPoint == Vec3.Invalid)
                        {
                            return false;
                        }
                        var distanceToTarget = Agent.Position.Distance(targetPoint);
                        return distanceToTarget >= AbilityTemplate.MinDistance && distanceToTarget <= AbilityTemplate.MaxDistance;
                    }
                default:
                    return base.HaveLineOfSightToTarget(target);
            }
        }
    }
}