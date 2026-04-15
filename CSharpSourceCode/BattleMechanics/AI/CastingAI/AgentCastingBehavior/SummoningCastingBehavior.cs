using TaleWorlds.MountAndBlade;
using TOR_Core.AbilitySystem;
using TOR_Core.BattleMechanics.AI.CommonAIFunctions;

namespace TOR_Core.BattleMechanics.AI.CastingAI.AgentCastingBehavior
{
    public class SummoningCastingBehavior : AbstractAgentCastingBehavior
    {
        public SummoningCastingBehavior(Agent agent, AbilityTemplate template, int abilityIndex) : base(agent, template, abilityIndex)
        {
            Hysteresis = 0.1f;
        }

        protected override Target UpdateTarget(Target target)
        {
            target.SelectedWorldPosition = Agent.Position + Agent.LookDirection * 2;
            return target;
        }
    }
}