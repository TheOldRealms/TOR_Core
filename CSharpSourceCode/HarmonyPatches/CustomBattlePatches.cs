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
            IEnumerable<BasicCharacterObject> list = [];
            try
            {
                list = Game.Current.ObjectManager.GetObjects<BasicCharacterObject>(character => character.IsHero && character.StringId.StartsWith("tor"));
            }
            catch (Exception e)
            {
                TORCommon.Log(e.Message, NLog.LogLevel.Error);
                return true;
            }
            if (!list.IsEmpty()) __result = list;
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
                //Sly : any culture can be added as long as it has both colours assigned so they can be used on the banner, including bandit cultures, eg. druchii.
                list.Add(Game.Current.ObjectManager.GetObject<BasicCultureObject>(TORConstants.Cultures.EMPIRE));
                list.Add(Game.Current.ObjectManager.GetObject<BasicCultureObject>(TORConstants.Cultures.SYLVANIA));
                list.Add(Game.Current.ObjectManager.GetObject<BasicCultureObject>(TORConstants.Cultures.BRETONNIA));
                list.Add(Game.Current.ObjectManager.GetObject<BasicCultureObject>(TORConstants.Cultures.ASRAI));
                list.Add(Game.Current.ObjectManager.GetObject<BasicCultureObject>(TORConstants.Cultures.DAWI));
                list.Add(Game.Current.ObjectManager.GetObject<BasicCultureObject>(TORConstants.Cultures.GREENSKIN));
                list.Add(Game.Current.ObjectManager.GetObject<BasicCultureObject>(TORConstants.Cultures.EONIR));
                list.Add(Game.Current.ObjectManager.GetObject<BasicCultureObject>(TORConstants.Cultures.CHAOS));
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
            //Sly : if a default troop is missing, the game includes a fallback to take all troops of that culture for the formation category and assign the first one as the default. This is only needed for renamed cultures that may have other existing CharacterObject entries which could be taken first, or if we want to choose a specific default troop.
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