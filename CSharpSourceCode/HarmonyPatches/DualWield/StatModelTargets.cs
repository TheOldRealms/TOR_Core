// Vendored from Yotthani's DualWield-v0.10.9 mod (permission granted
// 2026-11-24). Full port, ilspycmd decompile artifacts hand-fixed.
//
// Harmony patch rationale: see the class-level <summary> below for
// Problem / Why not non-patching / Scope (per feedback_harmony_patch_rationale).

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TOR_Core.BattleMechanics.DualWield.Vendor;
using TaleWorlds.MountAndBlade;

namespace TOR_Core.HarmonyPatches.DualWield;

internal static class StatModelTargets
{
	public static IEnumerable<MethodBase> Find(string method)
	{
		List<MethodBase> list = new List<MethodBase>();
		Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
		foreach (Assembly assembly in assemblies)
		{
			if (assembly.IsDynamic)
			{
				continue;
			}
			Type[] array;
			try
			{
				array = assembly.GetTypes();
			}
			catch (ReflectionTypeLoadException ex)
			{
				array = ex.Types.Where((Type t) => t != null).ToArray();
			}
			catch
			{
				continue;
			}
			Type[] array2 = array;
			foreach (Type type in array2)
			{
				try
				{
					if (type == null || type.IsAbstract || !typeof(AgentStatCalculateModel).IsAssignableFrom(type))
					{
						continue;
					}
					MethodInfo methodInfo = type.GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).FirstOrDefault((MethodInfo x) => x.Name == method && !x.IsAbstract && x.GetParameters().Any((ParameterInfo p) => p.ParameterType == typeof(AgentDrivenProperties)));
					if (methodInfo != null)
					{
						list.Add(methodInfo);
					}
				}
				catch
				{
				}
			}
		}
		DualWieldLog.Log("[Stats] " + method + " patched on: " + string.Join(", ", list.Select((MethodBase m) => m.DeclaringType?.Name)));
		return list;
	}
}
