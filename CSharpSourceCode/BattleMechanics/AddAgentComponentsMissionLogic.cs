using TaleWorlds.MountAndBlade;
using TOR_Core.BattleMechanics.AI;
using TOR_Core.BattleMechanics.Morale;
using TOR_Core.BattleMechanics.Voice;
using TOR_Core.Extensions;

namespace TOR_Core.BattleMechanics
{
    public class AddAgentComponentsMissionLogic : MissionLogic
    {
        public override void OnAgentCreated(Agent agent)
        {
            if (agent.IsUndead())
            {
                agent.AddComponent(new UndeadMoraleAgentComponent(agent));
            }
            
            if (agent.IsHuman)
            {
                agent.AddComponent(new AgentVoiceComponent(agent));
                if (!agent.IsMonstrous())
                {
                    agent.AddComponent(new AIKickAgentComponent(agent));
                }
            }
        }
    }
}