using TaleWorlds.MountAndBlade;
using TOR_Core.AbilitySystem;

namespace TOR_Core.BattleMechanics.AI.CastingAI.AgentCastingBehavior
{
    public class SelectMultiTargetCastingBehavior : AoETargetedCastingBehavior
    {
        public SelectMultiTargetCastingBehavior(Agent agent, AbilityTemplate template, int abilityIndex) : base(agent, template, abilityIndex)
        {
            Hysteresis = 0.1f;
        }
    }
}