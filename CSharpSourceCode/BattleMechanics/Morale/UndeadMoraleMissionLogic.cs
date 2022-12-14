using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.MountAndBlade;
using TOR_Core.Extensions;

namespace TOR_Core.BattleMechanics.Morale
{
    public class UndeadMoraleMissionLogic : MissionLogic
    {
        public override void OnAgentCreated(Agent agent)
        {
            if (agent.IsUndead())
            {
                agent.AddComponent(new UndeadMoraleAgentComponent(agent));
            }
        }
    }
}
