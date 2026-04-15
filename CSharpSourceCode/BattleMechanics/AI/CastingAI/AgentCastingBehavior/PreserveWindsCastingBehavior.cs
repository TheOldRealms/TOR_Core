using TaleWorlds.MountAndBlade;
using TOR_Core.AbilitySystem;
using TOR_Core.BattleMechanics.AI.CommonAIFunctions;

namespace TOR_Core.BattleMechanics.AI.CastingAI.AgentCastingBehavior
{
    public class PreserveWindsAgentCastingBehavior : AbstractAgentCastingBehavior
    {
        public PreserveWindsAgentCastingBehavior(Agent agent, AbilityTemplate abilityTemplate, int abilityIndex) : base(agent, abilityTemplate, abilityIndex)
        {
        }

        public override void Execute()
        {
            //Do nothing. I am hoping that we will add some sort of "Channeling" which allows us to restore magic over time later on.
        }
    }
}