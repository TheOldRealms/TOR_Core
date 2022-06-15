using TaleWorlds.MountAndBlade;
using TOR_Core.Utilities;

namespace TOR_Core.Extensions.ExtendedInfoSystem
{
    public class ExtendedInfoMissionLogic : MissionLogic
    {
        public override void OnMissionDeactivate()
        {
            base.OnMissionDeactivate();
            TORSpellBlowHelper.Clear();
        }
    }
}
