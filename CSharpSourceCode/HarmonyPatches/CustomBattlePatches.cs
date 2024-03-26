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
        [HarmonyPrefix]
        [HarmonyPatch(typeof(CustomBattleData), "Characters", MethodType.Getter)]
        public static bool GetCustomBattleCommanders(ref IEnumerable<BasicCharacterObject> __result)
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
                list.Add(Game.Current.ObjectManager.GetObject<BasicCharacterObject>("tor_mw_lord"));
                list.Add(Game.Current.ObjectManager.GetObject<BasicCharacterObject>("tor_necromancer_lord"));
                list.Add(Game.Current.ObjectManager.GetObject<BasicCharacterObject>("tor_prophetess_lw"));
                list.Add(Game.Current.ObjectManager.GetObject<BasicCharacterObject>("tor_prophetess_bw"));
                list.Add(Game.Current.ObjectManager.GetObject<BasicCharacterObject>("tor_glade_lord"));
            }
            catch (Exception e)
            {
                TORCommon.Log(e.Message, NLog.LogLevel.Error);
                return true;
            }
            if (list.Count > 1) __result = list;
            else return true;
            return false;
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
                list.Add(Game.Current.ObjectManager.GetObject<BasicCultureObject>("vlandia"));
                list.Add(Game.Current.ObjectManager.GetObject<BasicCultureObject>("battania"));
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
                case CultureCode.Vlandia:
                    __result = Game.Current.ObjectManager.GetObject<BasicCharacterObject>("tor_br_peasant_levy");
                    break;
                case CultureCode.Battania:
                    __result = Game.Current.ObjectManager.GetObject<BasicCharacterObject>("tor_we_eternal_guard");
                    break;
                default:
                    __result = Game.Current.ObjectManager.GetObject<BasicCharacterObject>("tor_empire_recruit");
                    break;
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(ArmyCompositionItemVM), "IsValidUnitItem")]
        public static bool Prefix(ref ArmyCompositionItemVM __instance, BasicCharacterObject o, ref bool __result, BasicCultureObject ____culture, ArmyCompositionItemVM.CompositionType ____type)
        {
            if (o != null && o.StringId.StartsWith("tor_") && o.Culture.StringId == ____culture.StringId && o.DefaultFormationClass == GetFormationFor(____type))
            {
                __result = true;
            }
            else __result = false;
            return false;
        }

        private static FormationClass GetFormationFor(ArmyCompositionItemVM.CompositionType type)
        {
            switch (type)
            {
                case ArmyCompositionItemVM.CompositionType.MeleeInfantry:
                    return FormationClass.Infantry;
                case ArmyCompositionItemVM.CompositionType.RangedInfantry:
                    return FormationClass.Ranged;
                case ArmyCompositionItemVM.CompositionType.MeleeCavalry:
                    return FormationClass.Cavalry;
                case ArmyCompositionItemVM.CompositionType.RangedCavalry:
                    return FormationClass.HorseArcher;
                default:
                    return FormationClass.Infantry;
            }
        }
    }
}
