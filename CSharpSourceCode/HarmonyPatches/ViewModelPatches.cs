using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.LinQuick;
using TaleWorlds.MountAndBlade;
using TOR_Core.CharacterDevelopment;
using TOR_Core.Extensions;
using TOR_Core.Extensions.UI;
using TOR_Core.Utilities;

namespace TOR_Core.HarmonyPatches
{
    // naber: the goal here is to stop mutating vms internal property/method maps and instead only consult
    // extensions afor a fallback when the base resolution fails. also cache extension member names per extension
    // type so we dont reflect every call. overall less fragile coupling to internal vm structures + less repeated reflection.

    [HarmonyPatch]
    public static class ViewModelPatches
    {
        private const BindingFlags ExtensionMemberFlags =
    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        private static readonly Dictionary<Type, HashSet<string>> _extensionPropertyNamesByType = new();
        private static readonly Dictionary<Type, HashSet<string>> _extensionMethodNamesByType = new();

        private static bool ExtensionDefinesProperty(IViewModelExtension extension, string propertyName)
        {
            var extensionType = extension.GetType();
            if (!_extensionPropertyNamesByType.TryGetValue(extensionType, out var propertyNames))
            {
                propertyNames = new HashSet<string>(StringComparer.Ordinal);
                var properties = extensionType.GetProperties(ExtensionMemberFlags);
                for (var i = 0; i < properties.Length; i++)
                {
                    propertyNames.Add(properties[i].Name);
                }

                _extensionPropertyNamesByType[extensionType] = propertyNames;
            }

            return propertyNames.Contains(propertyName);
        }

        private static bool ExtensionDefinesMethod(IViewModelExtension extension, string methodName)
        {
            var extensionType = extension.GetType();
            if (!_extensionMethodNamesByType.TryGetValue(extensionType, out var methodNames))
            {
                methodNames = new HashSet<string>(StringComparer.Ordinal);
                var methods = extensionType.GetMethods(ExtensionMemberFlags);
                for (var i = 0; i < methods.Length; i++)
                {
                    methodNames.Add(methods[i].Name);
                }

                _extensionMethodNamesByType[extensionType] = methodNames;
            }

            return methodNames.Contains(methodName);
        }

        private const string Headposition = "HeadPosition";
        private const string Position = "Position";
        private const string DistanceToCamera = "DistanceToCamera";


        [HarmonyPostfix]
        [HarmonyPatch(typeof(ViewModel), MethodType.Constructor)]
        public static void PatchVMConstructor(ViewModel __instance)
        {
            if (!__instance.HasExtensionType())
                return;

            var vmExtensionType = __instance.GetExtensionType();
            if (vmExtensionType == null)
                return;
            if (__instance.HasExtensionInstance())
                return;

            var propertiesAndMethodsExists = Traverse.Create(__instance).Field("_propertiesAndMethods").FieldExists();
            if (!propertiesAndMethodsExists)
                return;

            // crash proof, only once
            _ = Activator.CreateInstance(vmExtensionType, __instance) as IViewModelExtension;
        }


        [HarmonyPrefix]
        [HarmonyPatch(typeof(ViewModel), "OnFinalize")]
        public static void PatchVMDestructor(ViewModel __instance)
        {
            if (!__instance.HasExtensionType())
            {
                return;
            }
            var extension = __instance.GetExtensionInstance();
            extension?.OnFinalize();
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(ViewModel), "GetViewModelAtPath", typeof(BindingPath))]
        public static void PatchPathFinder(ViewModel __instance, BindingPath path, ref object __result)
        {
            if (__result != null)
                return;

            if (!__instance.HasExtensionType())
                return;

            var extension = __instance.GetExtensionInstance();
            if (extension == null)
                return;

            var extendedResult = extension.GetViewModelAtPath(path);
            if (extendedResult != null)
                __result = extendedResult;
        }


        [HarmonyPostfix]
        [HarmonyPatch(typeof(ViewModel), "GetPropertyValue", typeof(string))]
        public static void PatchPropertyGetter(ViewModel __instance, string name, ref object __result)
        {
            if (__result != null)
            {
                return;
            }

            if (!__instance.HasExtensionType())
            {
                return;
            }

            var extension = __instance.GetExtensionInstance();
            if (extension == null)
            {
                return;
            }
            if (!ExtensionDefinesProperty(extension, name))
                return;

            __result = extension.GetPropertyValue(name);
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(ViewModel), "SetPropertyValue")]
        public static bool PatchPropertySetter(ViewModel __instance, string name, object value)
        {
            if (name == DistanceToCamera) return true;
            if (name == Position) return true;
            if (name == Headposition) return true;
            if (!__instance.HasExtensionType())
            {
                return true;
            }

            var extension = __instance.GetExtensionInstance();
            if (extension == null)
            {
                return true;
            }
            if (!ExtensionDefinesProperty(extension, name))
                return true;

            extension.SetPropertyValue(name, value);
            return false;

        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(ViewModel), "ExecuteCommand")]
        public static bool PatchExecutor(ViewModel __instance, string commandName, object[] parameters)
        {
            if (!__instance.HasExtensionType())
            {
                return true;
            }

            var extension = __instance.GetExtensionInstance();
            if (extension == null)
            {
                return true;
            }
            if (!ExtensionDefinesMethod(extension, commandName))
                return true;

            extension.ExecuteCommand(commandName, parameters);
            return false;

        }
    }

    // Patch to prevent death UI from showing when Harbinger (summoned champion) dies
    [HarmonyPatch]
    [HarmonyPatchCategory("LatePatches")]
    public static class ScoreboardBaseVMPatches
    {
        [HarmonyPrefix]
        [HarmonyPatch("TaleWorlds.MountAndBlade.ViewModelCollection.Scoreboard.ScoreboardBaseVM", "OnMainHeroDeath")]
        public static bool OnMainHeroDeath_Prefix()
        {
            if (Game.Current.GameType is Campaign && TaleWorlds.MountAndBlade.Agent.Main == null && Hero.MainHero.HasCareer(TORCareers.Necromancer))
            {
                if (Mission.Current.Agents.AnyQ(x => x.IsHero && x.IsActive() && x.GetHero() == Hero.MainHero))
                {
                    return false;
                }
            }

            // For all other cases, let the original method run
            return true;
        }
    }
}