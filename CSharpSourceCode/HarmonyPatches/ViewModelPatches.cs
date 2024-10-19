using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using SandBox.ViewModelCollection.Nameplate;
using TaleWorlds.Library;
using TOR_Core.Extensions;
using TOR_Core.Extensions.UI;

namespace TOR_Core.HarmonyPatches
{
    [HarmonyPatch]
    public static class ViewModelPatches
    {
        private const string Headposition = "HeadPosition";
        private const string Position = "Position";
        private const string DistanceToCamera = "DistanceToCamera";
        
        
        [HarmonyPostfix]
        [HarmonyPatch(typeof(ViewModel), MethodType.Constructor)]
        public static void PatchVMConstructor(ViewModel __instance)
        {
            if (__instance.HasExtensionType())
            {
                var VMExtensionType = __instance.GetExtensionType();
                if(VMExtensionType != null)
                {
                    var exists = Traverse.Create(__instance).Field("_propertiesAndMethods").FieldExists();
                    if (exists && Activator.CreateInstance(VMExtensionType, __instance) is IViewModelExtension VMExtensionInstance)
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
            if (__instance.HasExtensionInstance())
            {
                __instance.GetExtensionInstance().OnFinalize();
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(ViewModel), "GetViewModelAtPath", typeof(BindingPath))]
        public static bool PatchPathFinder(ViewModel __instance, BindingPath path, ref object __result)
        {
            if(__instance.HasExtensionInstance())
            {
                __result = __instance.GetExtensionInstance().GetViewModelAtPath(path);
                return false;
            }
            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(ViewModel), "GetPropertyValue", typeof(string))]
        public static bool PatchPropertyGetter(ViewModel __instance, string name, ref object __result)
        {
            if (__instance.HasExtensionInstance())
            {
                __result = __instance.GetExtensionInstance().GetPropertyValue(name);
                return false;
            }
            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(ViewModel), "SetPropertyValue")]
        public static bool PatchPropertySetter(ViewModel __instance, string name, object value)
        {
            if (name == DistanceToCamera) return true;
            if (name == Position) return true;
            if (name == Headposition) return true;
            else if (__instance.HasExtensionInstance())
            {
                __instance.GetExtensionInstance().SetPropertyValue(name, value);
                return false;
            }
            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(ViewModel), "ExecuteCommand")]
        public static bool PatchExecutor(ViewModel __instance, string commandName, object[] parameters)
        {
            if (__instance.HasExtensionInstance())
            {
                __instance.GetExtensionInstance().ExecuteCommand(commandName, parameters);
                return false;
            }
            return true;
        }
    }
}
