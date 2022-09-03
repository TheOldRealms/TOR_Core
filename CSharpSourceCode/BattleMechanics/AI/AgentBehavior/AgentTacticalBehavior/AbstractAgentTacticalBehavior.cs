using System;
using System.Collections.Generic;
using TaleWorlds.MountAndBlade;
using TOR_Core.BattleMechanics.AI.Decision;

namespace TOR_Core.BattleMechanics.AI.AgentBehavior.AgentTacticalBehavior
{
    public abstract class AbstractAgentTacticalBehavior : IAgentBehavior
    {
        protected HumanAIComponent AIComponent;
        protected Agent Agent;

        protected AbstractAgentTacticalBehavior(Agent agent, HumanAIComponent aiComponent)
        {
            Agent = agent;
            AIComponent = aiComponent;
        }

        protected OrderType? GetMovementOrderType()
        {
            var moveOrder = Agent?.Formation?.GetReadonlyMovementOrderReference();
            var currentOrderType = moveOrder?.OrderType;
            return currentOrderType;
        }

        public void Execute()
        {
            ApplyBehaviorParams();
            Tick();
        }

        public abstract void Tick();
        public abstract void Terminate();

        public float GetLatestScore()
        {
            throw new NotImplementedException();
        }

        public List<BehaviorOption> CalculateUtility()
        {
            throw new NotImplementedException();
        }

        public bool IsPositional()
        {
            return false;
        }

        public abstract void ApplyBehaviorParams();
    }
}