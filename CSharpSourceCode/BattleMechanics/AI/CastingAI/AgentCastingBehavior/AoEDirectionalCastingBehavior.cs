using TaleWorlds.MountAndBlade;
using TOR_Core.AbilitySystem;
using TOR_Core.BattleMechanics.AI.CastingAI.AgentTacticalBehavior;
using TOR_Core.BattleMechanics.AI.CastingAI.Components;
using TOR_Core.BattleMechanics.AI.CommonAIFunctions;
using TOR_Core.Extensions;

namespace TOR_Core.BattleMechanics.AI.CastingAI.AgentCastingBehavior
{
    public class AoEDirectionalCastingBehavior : AbstractAgentCastingBehavior
    {
        public AoEDirectionalCastingBehavior(Agent agent, AbilityTemplate template, int abilityIndex) : base(agent, template, abilityIndex)
        {
            Hysteresis = 0.35f;
            TacticalBehavior = new DirectionalAoETacticalBehavior(agent, agent.GetComponent<WizardAIComponent>(), this);
        }

        public override void Execute()
        {
            var castingPosition = ((DirectionalAoETacticalBehavior) TacticalBehavior)?.CastingPosition;
            
            if (castingPosition.HasValue && Agent.Position.AsVec2.Distance(castingPosition.Value.AsVec2) > 6) return;

            base.Execute();
        }

        protected override float CalculateUtility(Target target)
        {
            if (!CommonAIStateFunctions.CanAgentMoveFreely(Agent) || Agent.GetAbility(AbilityIndex).IsOnCooldown())
                return 0.0f;

            return base.CalculateUtility(target);
        }
    }
}