using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace TOR_Core.BaseGameDebug
{
    public class BaseGameDebugMissionLogic : MissionLogic
    {
        public override void OnRenderingStarted()
        {
            if (Mission.Scene.ContainsTerrain)
            {
                Vec2i nodeDim;
                float nodeSize;
                int layerCount;
                int layerVersion;
                Mission.Scene.GetTerrainData(out nodeDim, out nodeSize, out layerCount, out layerVersion);
                List<float> heightMapData = new List<float>();
                for(int i = 0; i < nodeDim.X; i++)
                {
                    for(int j = 0; j < nodeDim.Y; j++)
                    {
                        var f = Mission.Scene.GetTerrainHeightData(i, j);
                        heightMapData.AddRange(f);
                    }
                }
            }
        }
    }
}
