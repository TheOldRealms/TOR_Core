using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.MountAndBlade;

namespace TOR_Core.BattleMechanics.Atmosphere
{
    internal class ForceAtmosphereMissionLogic : MissionLogic
    {
        private readonly string _forceAtmosphereKey = "forceatmo";

        public override void EarlyStart()
        {
            base.EarlyStart();
            if (Mission.Scene != null && Mission.SceneName.Contains(_forceAtmosphereKey))
            {
                Mission.Scene.SetAtmosphereWithName(Mission.SceneName);
            }
        }
    }
}
