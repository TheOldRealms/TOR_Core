using HarmonyLib;
using TaleWorlds.Engine;
using TaleWorlds.Engine.Screens;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TOR_Core.Utilities;

namespace TOR_Core.HarmonyPatches
{
    [HarmonyPatch(typeof(MBInitialScreenBase))]
    public static class RotatingMainMenuScenePatch
    {
		[HarmonyPrefix]
		[HarmonyPatch("RefreshScene")]
		public static bool PreFix(MBInitialScreenBase __instance, ref Scene ____scene, Camera ____camera,
			SceneLayer ____sceneLayer, ref GameEntity ____cameraAnimationEntity)
		{
			if (____scene == null)
			{
				____scene = Scene.CreateNewScene(true);
				____scene.SetName("MBInitialScreenBase");
				____scene.SetPlaySoundEventsAfterReadyToRender(true);
				____scene.Read(TORCommon.GetRandomSceneForMainMenu());
				for (int i = 0; i < 40; i++)
				{
					____scene.Tick(0.1f);
				}
				Vec3 vec = default(Vec3);
				____scene.FindEntityWithTag("camera_instance").GetCameraParamsFromCameraScript(____camera, ref vec);
			}
			SoundManager.SetListenerFrame(____camera.Frame);
			if (____sceneLayer != null)
			{
				____sceneLayer.SetScene(____scene);
				____sceneLayer.SceneView.SetEnable(true);
				____sceneLayer.SceneView.SetSceneUsesShadows(true);
			}
			____cameraAnimationEntity = GameEntity.CreateEmpty(____scene, true);
			return false;
		}
	}
}
