using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.Core;
using TaleWorlds.InputSystem;
using TaleWorlds.MountAndBlade;

namespace TOR_Core.BattleMechanics.DualWield
{
    public class DualWieldMissionLogic : MissionLogic
    {
        public override void OnAgentBuild(Agent agent, Banner banner)
        {
            if(agent.IsHuman)
            {
                var comp = new DualWieldAgentComponent(agent, banner);
                agent.AddComponent(comp);
                agent.OnAgentWieldedItemChange += comp.OnWieldedItemChanged;
            }
        }

        public override void OnPreMissionTick(float dt)
        {
            if(Agent.Main != null)
            {
                var comp = Agent.Main.GetComponent<DualWieldAgentComponent>();
                if(comp != null) comp.Tick(dt);
            }
        }
    }
}
