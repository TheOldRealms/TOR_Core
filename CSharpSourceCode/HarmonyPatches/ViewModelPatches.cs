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
    public static class ViewModelPatches
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(ViewModel), MethodType.Constructor)]
        public static void PatchVMConstructor(ViewModel __instance)
        {
            if (__instance.HasExtension())
            {
                var VMExtensionType = __instance.GetExtensionType();
                if(VMExtensionType != null)
                {
                    var VMExtensionInstance = Activator.CreateInstance(VMExtensionType, __instance) as IViewModelExtension;
                    var exists = Traverse.Create(__instance).Field("_propertiesAndMethods").FieldExists();
                    if (exists && VMExtensionInstance != null)
                    {
                        var field = Traverse.Create(__instance).Field("_propertiesAndMethods").GetValue();
                        var props = Traverse.Create(field).Property("Properties").GetValue() as Dictionary<string, PropertyInfo>;
                        var methods = Traverse.Create(field).Property("Methods").GetValue() as Dictionary<string, MethodInfo>;
                        foreach(var prop in VMExtensionInstance.GetProperties())
                        {
                            props.AddItem(prop);
                        }
                        foreach (var method in VMExtensionInstance.GetMethods())
                        {
                            methods.AddItem(method);
                        }
                    }
                }
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(ViewModel), "OnFinalize")]
        public static void PatchVMDestructor(ViewModel __instance)
        {
            if (__instance.HasExtension())
            {
                __instance.GetExtension().OnFinalize();
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(ViewModel), "GetViewModelAtPath", typeof(BindingPath))]
        public static bool PatchPathFinder(ViewModel __instance, BindingPath path, ref object __result)
        {
            if(__instance.HasExtension())
            {
                __result = __instance.GetExtension().GetViewModelAtPath(path);
                return false;
            }
            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(ViewModel), "GetPropertyValue", typeof(string))]
        public static bool PatchPropertyGetter(ViewModel __instance, string name, ref object __result)
        {
            if (__instance.HasExtension())
            {
                __result = __instance.GetExtension().GetPropertyValue(name);
                return false;
            }
            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(ViewModel), "SetPropertyValue")]
        public static bool PatchPropertySetter(ViewModel __instance, string name, object value)
        {
            if (__instance.HasExtension())
            {
                __instance.GetExtension().SetPropertyValue(name, value);
                return false;
            }
            return true;
        }
    }
}
