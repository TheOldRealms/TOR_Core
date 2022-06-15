using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade.CustomBattle;
using TaleWorlds.MountAndBlade.CustomBattle.CustomBattle;
using TaleWorlds.ObjectSystem;
using TOR_Core.Utilities;

namespace TOR_Core.HarmonyPatches
{
    [HarmonyPatch]
    public static class CustomBattlePatches
    {
        //Fill available characters
        [HarmonyPostfix]
        [HarmonyPatch(typeof(CustomBattleData), "Characters", MethodType.Getter)]
        public static void Postfix2(ref IEnumerable<BasicCharacterObject> __result)
        {
            var list = new List<BasicCharacterObject>();
            try
            {
                //Ideally this should not be hardcoded. 
                list.Add(Game.Current.ObjectManager.GetObject<BasicCharacterObject>("tor_emp_lord"));
                list.Add(Game.Current.ObjectManager.GetObject<BasicCharacterObject>("tor_vc_lord"));
                list.Add(Game.Current.ObjectManager.GetObject<BasicCharacterObject>("tor_bw_lord"));
                list.Add(Game.Current.ObjectManager.GetObject<BasicCharacterObject>("tor_lw_lord"));
                list.Add(Game.Current.ObjectManager.GetObject<BasicCharacterObject>("tor_cw_lord"));
                list.Add(Game.Current.ObjectManager.GetObject<BasicCharacterObject>("tor_necromancer_lord"));
            }
            catch (Exception e)
            {
                TORCommon.Log(e.Message, NLog.LogLevel.Error);
            }
            if (list.Count > 1) __result = list;
        }

        //Fill available cultures
        [HarmonyPostfix]
        [HarmonyPatch(typeof(CustomBattleData), "Factions", MethodType.Getter)]
        public static void Postfix3(ref IEnumerable<BasicCultureObject> __result)
        {
            var list = new List<BasicCultureObject>();
            try
            {
                list.Add(Game.Current.ObjectManager.GetObject<BasicCultureObject>("empire"));
                list.Add(Game.Current.ObjectManager.GetObject<BasicCultureObject>("khuzait"));
            }
            catch (Exception e)
            {
                TORCommon.Log(e.Message, NLog.LogLevel.Error);
            }
            if (list.Count > 1) __result = list;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(CustomBattleHelper), "GetDefaultTroopOfFormationForFaction")]
        public static void Postfix(ref BasicCharacterObject __result, BasicCultureObject culture)
        {
            switch (culture.GetCultureCode())
            {
                case CultureCode.Empire:
                    __result = Game.Current.ObjectManager.GetObject<BasicCharacterObject>("tor_empire_recruit");
                    break;
                case CultureCode.Khuzait:
                    __result = Game.Current.ObjectManager.GetObject<BasicCharacterObject>("tor_vc_skeleton_recruit");
                    break;
                default:
                    __result = Game.Current.ObjectManager.GetObject<BasicCharacterObject>("tor_empire_recruit");
                    break;
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(CustomGame), "LoadCustomBattleScenes")]
        public static void Postfix5(ref CustomGame __instance, ref XmlDocument doc)
        {
            var path = TORPaths.TOREnvironmentModuleDataPath + "tor_custombattlescenes.xml";
            if (File.Exists(path))
            {
                XmlDocument moredoc = new XmlDocument();
                moredoc.Load(path);
                doc = MBObjectManager.MergeTwoXmls(doc, moredoc);
            }
        }
    }
}
