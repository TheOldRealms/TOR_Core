using TaleWorlds.MountAndBlade;
using TOR_Core.Utilities;

namespace TOR_Core.Extensions.ExtendedInfoSystem
{
    public class ExtendedInfoMissionLogic : MissionLogic
    {
        public override void OnMissionStateDeactivated()
        {
            base.OnMissionStateDeactivated();
            TORSpellBlowHelper.Clear();
        }
    }
}
