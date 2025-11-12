using HarmonyLib;
using SandBox.View.Map;
using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.View.Tableaus.Thumbnails;
using FaceGen = TaleWorlds.Core.FaceGen;

namespace TOR_Core.HarmonyPatches
{
    [HarmonyPatch]
    public static class TableauRenderPatches
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(MapConversationTableau), "FirstTimeInit")]
        public static void PostfixMapConversationRender(ref Camera ____continuousRenderCamera, List<AgentVisuals> ____agentVisuals)
        {
            if (____continuousRenderCamera != null && ____agentVisuals != null && ____agentVisuals.Count > 0)
            {
                var eyePos = ____agentVisuals[0].GetGlobalStableEyePoint(true);
                var cameraFrame = ____continuousRenderCamera.Frame;
                cameraFrame.origin.z = eyePos.z - 0.15f;
                ____continuousRenderCamera.Frame = cameraFrame;
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(CharacterThumbnailCache), "CreateCharacterBaseEntity")]
        public static void PostFixCreateCharacter(CharacterCode characterCode, Scene scene, ref Camera camera, bool isBig)
        {
            //do we need a depth offset to be added for orcs or other troops that are wide and can be out of shot?
            if (FaceGen.GetRaceNames()[characterCode.Race] == "dwarf")
            {
                var cameraFrame = camera.Frame;
                cameraFrame.origin.z -= 0.22f;
                camera.Frame = cameraFrame;
            }
            if (FaceGen.GetRaceNames()[characterCode.Race] == "goblin") //troop cards are a good height, but the preview on the left for them is still only shoulders+head
            {
                var cameraFrame = camera.Frame;
                cameraFrame.origin.z -= 0.5f;
                camera.Frame = cameraFrame;
            }
        }
    }
}