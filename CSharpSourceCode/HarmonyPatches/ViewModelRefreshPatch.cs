using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.Library;
using TOR_Core.Extensions;
using TOR_Core.Extensions.UI;

namespace TOR_Core.HarmonyPatches
{
    [HarmonyPatch]
    public static class ViewModelRefreshPatch
    {
        [HarmonyTargetMethods]
        static IEnumerable<MethodBase> PatchInventoryMethods()
        {
            foreach(var type in ViewModelExtensionManager.Instance.ExtensionTypes.Values.Distinct())
            {
                var attribute = type.GetCustomAttribute<ViewModelExtensionAttribute>();
                yield return attribute.BaseType.GetMethod(attribute.RefreshMethodName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            }
        }

        [HarmonyPostfix]
        static void Postfix(ViewModel __instance)
        {
            if (__instance.HasExtensionInstance()) __instance.GetExtensionInstance().RefreshValues();
        }
    }
}
