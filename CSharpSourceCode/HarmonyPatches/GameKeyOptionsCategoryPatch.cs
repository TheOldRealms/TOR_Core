using System.Collections.Generic;
using HarmonyLib;
using TaleWorlds.MountAndBlade.Options;
using TOR_Core.GameManagers;

namespace TOR_Core.HarmonyPatches
{
    /// <summary>
    /// I hope this patch is one day not needed. During the check for categories, taleworlds decided to leave out non vanilla contexts inside the OptionsVM.
    /// the only way to put those in is setting up the GameKeyContext different, then you can create custom Categories, however, you can't change the inputs.
    /// One way or the other, i hope this stays a temporary solution
    /// </summary>
    [HarmonyPatch]
    public static class GameKeyOptionsCategoryPatch
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(OptionsProvider), "GetGameKeyCategoriesList")]
        public static IEnumerable<string> Postfix( IEnumerable<string> __result)
        {
            return __result.AddItem(nameof(TORGameKeyContext));
        }
    }
}