using HarmonyLib;
using TaleWorlds.MountAndBlade.GauntletUI;
using TOR_Core.Utilities;

namespace TOR_Core.HarmonyPatches
{
    [HarmonyPatch]
    public static class LoadingScreenPatches
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(LoadingWindowViewModel), "SetTotalGenericImageCount")]
        public static void PostFix(ref int ____totalGenericImageCount)
        {
            ____totalGenericImageCount = TORConstants.TotalNumberOfUniqueLoadingScreenImages;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(LoadingWindowViewModel), "Update")]
        public static void PostFix2(LoadingWindowViewModel __instance)
        {
            var num = TaleWorlds.Engine.Utilities.GetNumberOfShaderCompilationsInProgress();
            if (num > 0)
            {
                __instance.DescriptionText = "Shader compilation in progress. Remaining shaders to compile: " + num;
            }
        }
    }
}
