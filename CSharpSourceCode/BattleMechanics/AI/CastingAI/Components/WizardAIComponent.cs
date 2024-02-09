using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.MountAndBlade;
using TOR_Core.BattleMechanics.AI.CastingAI.AgentCastingBehavior;
using TOR_Core.BattleMechanics.AI.CommonAIFunctions;

namespace TOR_Core.BattleMechanics.AI.CastingAI.Components
{
    public class WizardAIComponent : HumanAIComponent
    {
        private static readonly Random Rand = new Random();
        private static readonly float EvalInterval = 3;
        private float _dtSinceLastOccasional = (float) Rand.NextDouble() * EvalInterval; //Randomly distribute ticks

        public AbstractAgentCastingBehavior CurrentCastingBehavior;

        private List<IAgentBehavior> _availableCastingBehaviors; //Do not access this directly. Use the generator function public method below.
        public List<IAgentBehavior> AvailableCastingBehaviors => _availableCastingBehaviors ?? (_availableCastingBehaviors = new List<IAgentBehavior>(AgentCastingBehaviorConfiguration.PrepareCastingBehaviors(Agent)));

        public WizardAIComponent(Agent agent) : base(agent)
        {
            var toRemove = agent.Components.OfType<HumanAIComponent>().ToList();
            foreach (var item in toRemove) // This is intentional. Components is read-only
                agent.RemoveComponent(item);
        }


        public override void OnTickAsAI(float dt)
        {
            _dtSinceLastOccasional += dt;
            if (_dtSinceLastOccasional >= EvalInterval) TickOccasionally();
            if (Agent?.Formation?.FiringOrder.OrderType != OrderType.HoldFire)
            {
                CurrentCastingBehavior?.TacticalBehavior?.Execute();
                CurrentCastingBehavior?.Execute();
            }
            else
            {
                CurrentCastingBehavior?.Terminate();
                CurrentCastingBehavior?.TacticalBehavior?.Terminate();
            }

            base.OnTickAsAI(dt);
        }


        private void TickOccasionally()
        {
            _dtSinceLastOccasional = 0;
            CurrentCastingBehavior = DetermineBehavior(AvailableCastingBehaviors, CurrentCastingBehavior);
        }

        private AbstractAgentCastingBehavior DetermineBehavior(List<IAgentBehavior> availableCastingBehaviors, AbstractAgentCastingBehavior current)
        {
            var option = DecisionManager.EvaluateCastingBehaviors(availableCastingBehaviors);
            if (option.Behavior != current)
            {
                current?.Terminate();
                current?.TacticalBehavior?.Terminate();
            }

            var returnBehavior = option.Behavior as AbstractAgentCastingBehavior;
            if (returnBehavior != null)
            {
                returnBehavior.CurrentTarget = option.Target;
            }

            return returnBehavior;
        }

        public void UpdateArtilleryTargetPosition(Target chosenPosition)
        {
            var agentBehaviors = AvailableCastingBehaviors.FindAll(behavior => behavior.GetType() == typeof(ArtilleryPlacementCastingBehavior));
            agentBehaviors.ForEach(behavior => behavior.SetCurrentTarget(chosenPosition));
        }
    }
}