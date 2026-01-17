using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TaleWorlds.Library;
using TOR_Core.Extensions;
using TOR_Core.Extensions.UI;

namespace TOR_Core.HarmonyPatches
{
    [HarmonyPatch]
    [HarmonyPatchCategory("LatePatches")]
    public static class ViewModelRefreshPatch
    {
        [HarmonyTargetMethods]
        static IEnumerable<MethodBase> PatchInventoryMethods()
        {
            foreach (var type in ViewModelExtensionManager.Instance.ExtensionTypes.Values.Distinct())
            {
                var attribute = type.GetCustomAttribute<ViewModelExtensionAttribute>();
                var refreshMethod = attribute.BaseType.GetMethod(
                    attribute.RefreshMethodName,
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

                if (refreshMethod != null)
                    yield return refreshMethod;
            }
        }

        [HarmonyPostfix]
        static void Postfix(ViewModel __instance)
        {
            var extension = __instance.GetExtensionInstance();
            extension?.RefreshValues();
        }
    }
}