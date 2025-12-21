using TaleWorlds.MountAndBlade;

namespace TOR_Core.Extensions.ExtendedInfoSystem
{
    public class ExtendedInfoMissionLogic : MissionLogic
    {
        public override void OnMissionStateDeactivated()
        {
            base.OnMissionStateDeactivated();
            // Queue cleanup removed - spell damage is now calculated upfront
        }
    }
}
