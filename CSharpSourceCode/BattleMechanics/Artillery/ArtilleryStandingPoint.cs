using TaleWorlds.MountAndBlade;
using TOR_Core.Extensions;

namespace TOR_Core.BattleMechanics.Artillery
{
    public class ArtilleryStandingPoint : StandingPoint
    {
        public override bool IsDisabledForAgent(Agent agent)
        {
            return !agent.HasAttribute("ArtilleryCrew") || base.IsDisabledForAgent(agent);
        }
    }

    public class AmmoPickUpStandingPoint : StandingPointWithWeaponRequirement
    {
        public override bool IsDisabledForAgent(Agent agent)
        {
            return !agent.HasAttribute("ArtilleryCrew") || base.IsDisabledForAgent(agent);
        }
    }
}
