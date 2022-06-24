using HarmonyLib;
using TaleWorlds.ObjectSystem;

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
    }
}
