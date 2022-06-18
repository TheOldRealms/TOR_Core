using SandBox.GameComponents;
using TaleWorlds.MountAndBlade;
using TOR_Core.AbilitySystem;
using TOR_Core.Extensions;

namespace TOR_Core.Models
{
    public class TORBattleMoraleModel : SandboxBattleMoraleModel
    {
        public override bool CanPanicDueToMorale(Agent agent)
        {
            if (agent.IsUndead() || agent.IsUnbreakable() || agent.Origin is SummonedAgentOrigin) return false;
            else return base.CanPanicDueToMorale(agent);
        }

        public override float GetEffectiveInitialMorale(Agent agent, float baseMorale)
        {
            if (agent.Origin is SummonedAgentOrigin) return baseMorale;
            else return base.GetEffectiveInitialMorale(agent, baseMorale);
        }
    }

    public class TORCustomBattleMoraleModel : CustomBattleMoraleModel
    {
        public override bool CanPanicDueToMorale(Agent agent)
        {
            if (agent.IsUndead() || agent.IsUnbreakable() || agent.Origin is SummonedAgentOrigin) return false;
            else return base.CanPanicDueToMorale(agent);
        }
    }
}
