using TaleWorlds.MountAndBlade;
using TOR_Core.AbilitySystem;
using TOR_Core.BattleMechanics.AI.Decision;

namespace TOR_Core.BattleMechanics.AI.AgentBehavior.AgentCastingBehavior
{
    public class AoETargetedCastingBehavior : MissileCastingBehavior
    {
        public AoETargetedCastingBehavior(Agent agent, AbilityTemplate template, int abilityIndex) : base(agent, template, abilityIndex)
        {
        }

        protected override bool HaveLineOfSightToTarget(Target target)
        {
            return true;
        }

    }
}