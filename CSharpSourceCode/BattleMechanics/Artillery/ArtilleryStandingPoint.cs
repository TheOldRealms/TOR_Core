using TaleWorlds.MountAndBlade;
using TOR_Core.Extensions;
using static TOR_Core.Utilities.TORConstants;

namespace TOR_Core.BattleMechanics.Artillery
{
    public class ArtilleryStandingPoint : StandingPoint
    {
        public override bool IsDisabledForAgent(Agent agent)
        {
            return !agent.CanOperateArtillery() || base.IsDisabledForAgent(agent);
        }
    }

    public class TrebuchetStandingPoint : StandingPoint
    {
        public override bool IsDisabledForAgent(Agent agent)
        {
            return agent.IsPlayerControlled ? false : base.IsDisabledForAgent(agent);
        }
    }

    public class AmmoPickUpStandingPoint : StandingPointWithWeaponRequirement
    {
        public override bool IsDisabledForAgent(Agent agent)
        {
            return !agent.CanOperateArtillery() || base.IsDisabledForAgent(agent);
        }
    }
}