﻿using HarmonyLib;
using TaleWorlds.Core;
using TaleWorlds.ObjectSystem;
using TOR_Core.CharacterDevelopment;

namespace TOR_Core.HarmonyPatches
{
    [HarmonyPatch]
    public static class ObjectManagerPatches
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(MBObjectManager), "GetMergedXmlForManaged")]
        public static bool SkipValidationForSettlements(string id, ref bool skipValidation)
        {
            if (id == "Settlements")
            {
                skipValidation = true;
            }
            return true;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Game), "InitializeDefaultGameObjects")]
        public static void LoadAdditionalSkillsAndAttributes()
        {
            _ = new TORAttributes();
            _ = new TORSkills();
            _ = new TORSkillEffects();
        }
    }
}
