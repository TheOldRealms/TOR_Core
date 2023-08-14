using HarmonyLib;
using NLog;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;
using TaleWorlds.InputSystem;
using TaleWorlds.ModuleManager;
using TaleWorlds.MountAndBlade;
using TOR_Core.GameManagers;
using TOR_Core.Utilities;

namespace TOR_Core.HarmonyPatches
{
    [HarmonyPatch]
    public static class BaseGameDebugPatches
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(TroopRoster), "EnsureLength")]
        public static bool EnsureLengthProper(int length, ref TroopRosterElement[] ___data, int ____count)
        {
            if (length > 0 && (___data == null || length > ___data.Length))
            {
                TroopRosterElement[] array = new TroopRosterElement[length];
                for (int i = 0; i < ____count; i++)
                {
                    array[i] = ___data[i];
                }
                ___data = array;
            }
            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(TroopRoster), "WoundNumberOfTroopsRandomly")]
        public static bool PreventDeadlock(int numberOfMen, TroopRoster __instance, int ____totalRegulars, int ____totalWoundedRegulars)
        {
            for (int i = 0; i < numberOfMen; i++)
            {
                CharacterObject characterObject = null;
                int num = ____totalRegulars - ____totalWoundedRegulars;
                bool flag = num > 0;
                int counter = 0;
                while (flag && counter < 50)
                {
                    flag = false;
                    int indexOfTroop = MBRandom.RandomInt(num);
                    characterObject = __instance.GetManAtIndexFromFlattenedRosterWithFilter(indexOfTroop, true, false);
                    if (characterObject == null || characterObject.IsHero)
                    {
                        flag = true;
                        counter++;
                    }
                }
                if (characterObject != null)
                {
                    __instance.WoundTroop(characterObject, 1, default(UniqueTroopDescriptor));
                }
            }
            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(Game), "InitializeParameters")]
        public static bool LoadTORManagedParameters(Game __instance)
        {
            TaleWorlds.Core.ManagedParameters.Instance.Initialize(ModuleHelper.GetXmlPath("TOR_Core", "tor_managed_core_parameters"));
            __instance.GameType.InitializeParameters();
            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(HotKeyManager), "RegisterInitialContexts")]
        public static bool AddTorContext(ref IEnumerable<GameKeyContext> contexts)
        {
            List<GameKeyContext> newcontexts = contexts.ToList();
            if (!newcontexts.Any(x => x is TORGameKeyContext)) newcontexts.Add(new TORGameKeyContext());
            contexts = newcontexts;
            return true;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(HotKeyManager), "RegisterInitialContexts")]
        public static void LogGamekeyContexts(IEnumerable<GameKeyContext> contexts, bool loadKeys)
        {
            TORCommon.Log("STARTING RegisterInitialContexts --------------------", LogLevel.Debug);
            foreach (var context in contexts)
            {
                TORCommon.Log("Registering context: " + context.GameKeyCategoryId, LogLevel.Debug);
            }
        }
    }
}
