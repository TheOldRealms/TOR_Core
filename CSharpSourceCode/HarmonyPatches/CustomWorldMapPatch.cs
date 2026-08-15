using HarmonyLib;
using SandBox.View.Map;
using SandBox.ViewModelCollection.Map.Tracker;
using System.IO;
using System.Xml;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TOR_Core.Quests;
using TOR_Core.Utilities;

namespace TOR_Core.HarmonyPatches
{
    [HarmonyPatch]
    public static class CustomWorldMapPatch
    {
        /// <remarks>
        /// Sly : this prevents any submod from adding additional scenes for battle maps. It should instead look at clearing out prior scenes, then adding normally via a LoadSPBattleScenes call to not interfere with other mods loaded after which may intentionally add scenes compatible with TOR.
        /// Alternatively, see Campaign.InitializeScenes for how the game automatically loads them based on name and folder depth.
        /// </remarks>
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
        public static bool ChangeSettlementPathToTOR(ref string __result)
        {
            __result = TORPaths.TORCoreModuleDataPath + "tor_settlements.xml";
            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(SettlementPositionScript), "GetSettlementsDistanceCacheFileForCapability")]
        public static bool ChangeCachePathToTOR(ref bool __result, string moduleId, MobileParty.NavigationType navigationType, ref string filePath)
        {
            if (moduleId == "TOR_Core")
            {
                filePath = TORPaths.TORCoreModuleDataPath + "settlements_distance_cache_Default.bin";
                __result = true;
                return false;
            }
            return true;
        }
    }
    [HarmonyPatch(typeof(MapTrackerProvider))]
    internal static class QuestPartyMapTrackerProviderPatches
    {
        [HarmonyPostfix]
        [HarmonyPatch("CanAddMobileParty")]
        private static void CanAddMobilePartyPostfix(MobileParty party, ref bool __result)
        {
            if (__result)
                return;

            if (!party.IsCurrentlyUsedByAQuest)
                return;

            if (party.PartyComponent is not QuestPartyComponent)
                return;

            if (Campaign.Current.VisualTrackerManager.CheckTracked(party))
                __result = true;
        }

        [HarmonyPostfix]
        [HarmonyPatch("OnPartyQuestStatusChanged")]
        private static void OnPartyQuestStatusChangedPostfix(MapTrackerProvider __instance, MobileParty mobileParty, bool isUsedByQuest)
        {
            if (!isUsedByQuest)
                return;

            if (mobileParty.PartyComponent is not QuestPartyComponent)
                return;

            if (!Campaign.Current.VisualTrackerManager.CheckTracked(mobileParty))
                return;

            var addIfEligibleMethod = AccessTools.Method(typeof(MapTrackerProvider), "AddIfEligible", new[] { typeof(MobileParty) });
            addIfEligibleMethod.Invoke(__instance, new object[] { mobileParty });
        }
    }

}