using HarmonyLib;
using psai.net;
using SandBox.View;
using System;
using TaleWorlds.MountAndBlade;
using TOR_Core.CampaignMechanics;

namespace TOR_Core.HarmonyPatches
{
    [HarmonyPatch]
    public static class MBMusicManagerPatches
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(MBMusicManager), "ActivateMenuMode")]
        public static bool UseTORMusicHandler(ref MBMusicManager __instance)
        {

            Random rnd = new Random();
            var index = rnd.Next(500, 502); //why on earth are upper bounds exclusive, but lower bounds inclusive?
            
            typeof(MBMusicManager).GetProperty("CurrentMode").SetValue(__instance, MusicMode.Menu);
            PsaiCore.Instance.MenuModeEnter(index, 0.5f);
            return false;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(CampaignMusicHandler), "Create")]
        public static void UseTORMenuMusicId()
        {
            TORCampaignMusicHandler.Create();
        }
    }
}
