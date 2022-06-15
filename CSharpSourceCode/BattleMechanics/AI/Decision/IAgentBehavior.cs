using System.Collections.Generic;

namespace TOR_Core.BattleMechanics.AI.Decision
{
    public interface IAgentBehavior
    {
        void Execute();
        void Terminate();
        List<BehaviorOption> CalculateUtility();
    }
}