using HarmonyLib;
using System.Collections.Generic;
using TaleWorlds.InputSystem;

namespace TOR_Core.HarmonyPatches
{
    [HarmonyPatch]
    public static class InputContextPatches
    {

        /// <summary>
        /// Registering keys in a new scene context only adds entries up to the native count.
        /// This adds additional entries up to the count of the category being registered to prevent index out of range exceptions.
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch(typeof(InputContext), "RegisterHotKeyCategory")]
        public static bool AddGameKeyEntriesBeyondNativeCount(GameKeyContext category, ref List<GameKey> ____registeredGameKeys)
        {
            for (int i = ____registeredGameKeys.Count; i < category.RegisteredGameKeys.Count; i++)
            {
                ____registeredGameKeys.Add(null);
            }
            return true;
        }
    }
}
