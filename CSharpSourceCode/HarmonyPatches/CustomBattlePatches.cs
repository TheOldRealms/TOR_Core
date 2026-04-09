using HarmonyLib;
using System;
using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade.CustomBattle;
using TaleWorlds.MountAndBlade.CustomBattle.CustomBattle;
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
                list.Add(Game.Current.ObjectManager.GetObject<BasicCharacterObject>("tor_hm_lord"));
                list.Add(Game.Current.ObjectManager.GetObject<BasicCharacterObject>("tor_dw_lord"));
                list.Add(Game.Current.ObjectManager.GetObject<BasicCharacterObject>("tor_bret_lord"));
                list.Add(Game.Current.ObjectManager.GetObject<BasicCharacterObject>("tor_dwarf_lord"));
                list.Add(Game.Current.ObjectManager.GetObject<BasicCharacterObject>("tor_slayer_lord"));
                list.Add(Game.Current.ObjectManager.GetObject<BasicCharacterObject>("tor_shaman_lord"));
                list.Add(Game.Current.ObjectManager.GetObject<BasicCharacterObject>("tor_warboss_lord"));
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
                list.Add(Game.Current.ObjectManager.GetObject<BasicCultureObject>(TORConstants.Cultures.EMPIRE));
                list.Add(Game.Current.ObjectManager.GetObject<BasicCultureObject>(TORConstants.Cultures.SYLVANIA));
                list.Add(Game.Current.ObjectManager.GetObject<BasicCultureObject>(TORConstants.Cultures.BRETONNIA));
                list.Add(Game.Current.ObjectManager.GetObject<BasicCultureObject>(TORConstants.Cultures.ASRAI));
                list.Add(Game.Current.ObjectManager.GetObject<BasicCultureObject>(TORConstants.Cultures.DAWI));
                list.Add(Game.Current.ObjectManager.GetObject<BasicCultureObject>(TORConstants.Cultures.GREENSKIN));
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
            __result = culture.StringId.ToLower() switch
            {
                TORConstants.Cultures.EMPIRE => Game.Current.ObjectManager.GetObject<BasicCharacterObject>("tor_empire_recruit"),
                TORConstants.Cultures.SYLVANIA => Game.Current.ObjectManager.GetObject<BasicCharacterObject>("tor_vc_skeleton_recruit"),
                TORConstants.Cultures.BRETONNIA => Game.Current.ObjectManager.GetObject<BasicCharacterObject>("tor_br_peasant_levy"),
                TORConstants.Cultures.ASRAI => Game.Current.ObjectManager.GetObject<BasicCharacterObject>("tor_we_eternal_guard"),
                TORConstants.Cultures.DAWI => Game.Current.ObjectManager.GetObject<BasicCharacterObject>("tor_dw_dawi_recruit"),
                TORConstants.Cultures.GREENSKIN => Game.Current.ObjectManager.GetObject<BasicCharacterObject>("tor_gs_orc_boy"),
                _ => Game.Current.ObjectManager.GetObject<BasicCharacterObject>("tor_empire_recruit"),
            };
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(ArmyCompositionItemVM), "IsValidUnitItem")]
        public static bool Prefix(BasicCharacterObject o, ref bool __result, BasicCultureObject ____culture, ArmyCompositionItemVM.CompositionType ____type)
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
            return type switch
            {
                ArmyCompositionItemVM.CompositionType.MeleeInfantry => FormationClass.Infantry,
                ArmyCompositionItemVM.CompositionType.RangedInfantry => FormationClass.Ranged,
                ArmyCompositionItemVM.CompositionType.MeleeCavalry => FormationClass.Cavalry,
                ArmyCompositionItemVM.CompositionType.RangedCavalry => FormationClass.HorseArcher,
                _ => FormationClass.Infantry,
            };
        }
    }
}