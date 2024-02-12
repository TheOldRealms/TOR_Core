using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.MountAndBlade;

namespace TOR_Core.Utilities
{
    public class TORAnimationLogger : MissionLogic
    {
        private string _actionName = string.Empty;

        public override void OnMissionTick(float dt)
        {
            if(Agent.Main != null)
            {
                var action = Agent.Main.GetCurrentAction(1);
                if(action != null)
                {
                    if(_actionName != action.Name)
                    {
                        _actionName = action.Name;
                      //  TORCommon.Say("New action: " +  _actionName);
                    }
                }
            }
        }
    }
}
