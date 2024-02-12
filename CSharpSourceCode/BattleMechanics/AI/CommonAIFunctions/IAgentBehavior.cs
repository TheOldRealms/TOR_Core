using System.Collections.Generic;

namespace TOR_Core.BattleMechanics.AI.CommonAIFunctions
{
    public interface IAgentBehavior
    {
        void Execute();
        void Terminate();
        List<BehaviorOption> CalculateUtility();
        void SetCurrentTarget(Target target);
    }
}