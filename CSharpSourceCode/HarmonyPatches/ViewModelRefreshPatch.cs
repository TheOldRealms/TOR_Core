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
            extension?.RefreshValues(); //Sly : this lead to duplicate Religion descriptors on heroes because the postfix would trigger on the base refresh method as well as all of the overrides and it would keep appending a new Stat on for each inheritance level implicated. There should be a better way to do this which doesn't require a bandaid of deleting the previous texts and adding one back afterward.
        }
    }
}