using TaleWorlds.MountAndBlade;
using TOR_Core.AbilitySystem;
using TOR_Core.Extensions;

namespace TOR_Core.Models.CustomBattleModels
{
    public class TORCustomBattleMoraleModel : CustomBattleMoraleModel
    {
        public override bool CanPanicDueToMorale(Agent agent)
        {
            if (agent.IsUndead() || agent.IsUnbreakable() || agent.Origin is SummonedAgentOrigin) return false;
            else return base.CanPanicDueToMorale(agent);
        }
    }
}
