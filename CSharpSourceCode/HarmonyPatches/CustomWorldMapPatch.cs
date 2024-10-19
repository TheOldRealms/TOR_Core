using HarmonyLib;
using SandBox;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
using System.Xml;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
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

        [HarmonyPostfix]
        [HarmonyPatch(typeof(MapScene), "GetMapBorders")]
        public static void CustomBorders(MapScene __instance, ref Vec2 minimumPosition, ref Vec2 maximumPosition, ref float maximumHeight)
        {
            minimumPosition = new Vec2(650, 400);
            maximumPosition = new Vec2(1750, 1400);
            maximumHeight = 350;
        }
        
        [HarmonyPrefix]
        [HarmonyPatch(typeof(MobileParty), "RecoverPositionsForNavMeshUpdate")]
        public static bool WorldMapNavMeshDebugPatch(ref MobileParty __instance)
        {
            if (!__instance.Position2D.IsNonZero() || !PartyBase.IsPositionOkForTraveling(__instance.Position2D))
            {
                //teleport party to a valid navmesh position.
                __instance.Position2D = Settlement.All.First().GatePosition;
            }
            return true;
        }
    }
}
