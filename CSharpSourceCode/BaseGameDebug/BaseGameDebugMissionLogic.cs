using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.InputSystem;
using TaleWorlds.MountAndBlade;
using TOR_Core.Utilities;

namespace TOR_Core.BaseGameDebug
{
    public class BaseGameDebugMissionLogic : MissionLogic
    {
        public override void OnMissionTick(float dt)
        {
            if (Mission.CurrentState == Mission.State.Continuing && InputKey.Tilde.IsPressed())
            {
                TORCommon.WriteHeightMapDataForCurrentScene();
            }
        }
    }
}
