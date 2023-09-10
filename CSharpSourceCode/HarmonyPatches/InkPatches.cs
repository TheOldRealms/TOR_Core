using HarmonyLib;
using Ink;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TOR_Core.Utilities;

namespace TOR_Core.HarmonyPatches
{
    [HarmonyPatch]
    public static class InkPatches
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(DefaultFileHandler), "ResolveInkFilename")]
        public static bool Prefix(ref string __result, string includeName)
        {
            __result = Path.Combine(TORPaths.TORCoreModuleRootPath + "InkStories/", includeName);
            return false;
        }
    }
}
