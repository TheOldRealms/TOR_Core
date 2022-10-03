using HarmonyLib;
using psai.net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.Engine;
using TaleWorlds.ModuleManager;
using TaleWorlds.MountAndBlade;

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
                string corePath = ModuleHelper.GetModuleFullPath("TOR_Core") + "music/soundtrack.xml";
                PsaiCore.Instance.LoadSoundtrackFromProjectFile(corePath);
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(MBMusicManager), "ActivateMenuMode")]
        public static bool UseTowMenuMusicId(ref MBMusicManager __instance)
        {
            typeof(MBMusicManager).GetProperty("CurrentMode").SetValue(__instance, MusicMode.Menu);
            PsaiCore.Instance.MenuModeEnter(401, 0.5f);
            return false;
        }
    }
}
