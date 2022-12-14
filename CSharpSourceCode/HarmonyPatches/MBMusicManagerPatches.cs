using HarmonyLib;
using psai.net;
using System;
using TaleWorlds.Engine;
using TaleWorlds.ModuleManager;
using TaleWorlds.MountAndBlade;
using TOR_Core.Utilities;

namespace TOR_Core.HarmonyPatches
{
    [HarmonyPatch]
    public static class MBMusicManagerPatches
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(MBMusicManager), "Initialize")]
        public static void OverrideCreation()
        {
            if (!NativeConfig.DisableSound)
            {
                string corePath = TORPaths.TORCoreModuleRootPath + "music/soundtrack.xml";
                if (TORPaths.IsPlatformSteamWorkshop())
                {
                    corePath = TORPaths.TORCoreModuleRootPath + "music/soundtrack_steam.xml";
                }
                PsaiCore.Instance.LoadSoundtrackFromProjectFile(corePath);
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(MBMusicManager), "ActivateMenuMode")]
        public static bool UseTowMenuMusicId(ref MBMusicManager __instance)
        {

            Random rnd = new Random();
            var index = rnd.Next(401, 403); //why on earth are upper bounds exclusive, but lower bounds inclusive?
            
            typeof(MBMusicManager).GetProperty("CurrentMode").SetValue(__instance, MusicMode.Menu);
            PsaiCore.Instance.MenuModeEnter(index, 0.5f);
            return false;
        }
    }
}
