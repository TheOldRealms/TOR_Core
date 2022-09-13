using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using TaleWorlds.Core;
using TaleWorlds.InputSystem;
using TaleWorlds.MountAndBlade.Options;
using TaleWorlds.MountAndBlade.ViewModelCollection.GameOptions;
using TaleWorlds.MountAndBlade.ViewModelCollection.GameOptions.GameKeys;
using TOR_Core.GameManagers;

namespace TOR_Core.HarmonyPatches
{
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