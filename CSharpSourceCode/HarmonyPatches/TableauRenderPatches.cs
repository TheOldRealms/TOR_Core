using HarmonyLib;
using SandBox.View.Map;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.View;

namespace TOR_Core.HarmonyPatches
{
    [HarmonyPatch]
    public static class TableauRenderPatches
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(MapConversationTableau), "FirstTimeInit")]
        public static void PrefixMapConversationRender(ref Camera ____continuousRenderCamera, List<AgentVisuals> ____agentVisuals)
        {
            if(____continuousRenderCamera != null && ____agentVisuals != null && ____agentVisuals.Count > 0)
            {
                var eyePos = ____agentVisuals[0].GetGlobalStableEyePoint(true);
                var cameraFrame = ____continuousRenderCamera.Frame;
                cameraFrame.origin.z = eyePos.z - 0.15f;
                ____continuousRenderCamera.Frame = cameraFrame;
            }
        }
    }
}
