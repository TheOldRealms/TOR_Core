using System.Collections;
using System.Collections.Generic;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace TOR_Core.BattleMechanics.TriggeredEffect.Scripts
{
    public interface ITriggeredScript
    {
        void OnTrigger(Vec3 position, Agent triggeredByAgent, IEnumerable<Agent> triggeredAgents, float duration);
    }
}
