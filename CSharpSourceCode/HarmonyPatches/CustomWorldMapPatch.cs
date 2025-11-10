using HarmonyLib;
using SandBox;
using SandBox.View.Map;
using System.IO;
using System.Xml;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;
using TOR_Core.Utilities;

namespace TOR_Core.HarmonyPatches
{
    [HarmonyPatch]
    public static class CustomWorldMapPatch
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(GameSceneDataManager), "LoadSPBattleScenes", argumentTypes: typeof(XmlDocument))]
        public static void LoadSinglePlayerBattleScenes(GameSceneDataManager __instance, ref XmlDocument doc)
        {
            var path = TORPaths.TOREnvironmentModuleDataPath + "tor_singleplayerbattlescenes.xml";
            if (File.Exists(path))
            {
                XmlDocument moredoc = new XmlDocument();
                moredoc.Load(path);
                doc = moredoc;
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(SettlementPositionScript), "SettlementsXmlPath", MethodType.Getter)]
        public static bool ChangePathToTOR(ref string __result)
        {
            __result = TORPaths.TORCoreModuleDataPath + "tor_settlements.xml";
            return false;
        }
    }
}
