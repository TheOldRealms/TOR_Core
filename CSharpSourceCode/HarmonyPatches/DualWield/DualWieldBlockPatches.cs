// Vendored from Yotthani's DualWield-v0.10.9 mod (permission granted
// 2026-11-24). Full port, ilspycmd decompile artifacts hand-fixed.
//
// Harmony patch rationale: see the class-level <summary> below for
// Problem / Why not non-patching / Scope (per feedback_harmony_patch_rationale).

using System;
using System.Collections.Generic;
using System.Reflection;
using TOR_Core.BattleMechanics.DualWield;
using HarmonyLib;
using TaleWorlds.MountAndBlade;

namespace TOR_Core.HarmonyPatches.DualWield;

[HarmonyPatchCategory("LatePatches")]
[HarmonyPatch]
public static class DualWieldBlockPatches
{
	private static int _depth;

	public static IEnumerable<MethodBase> TargetMethods()
	{
		return StatModelTargets.Find("InitializeAgentStats");
	}

	[HarmonyPrefix]
	public static void Prefix()
	{
		_depth++;
	}

	[HarmonyFinalizer]
	public static Exception Finalizer(Exception __exception)
	{
		if (_depth > 0)
		{
			_depth--;
		}
		return __exception;
	}

	[HarmonyPostfix]
	public static void Postfix(Agent agent, AgentDrivenProperties agentDrivenProperties)
	{
		if (_depth != 1)
		{
			return;
		}
		try
		{
			if (DualWieldMissionBehavior.IsActive && DualWieldSettings.Get().EnableDualWield && DualWieldStateManager.IsPaired(agent))
			{
				DualWieldSettings dualWieldSettings = DualWieldSettings.Get();
				float num = 1f + dualWieldSettings.ParryWindowReduction * DualWieldSettings.BonusShare(agent);
				agentDrivenProperties.HandlingMultiplier *= num;
				if (DualWieldSettings.Get().DebugMode)
				{
					DualWieldSettings.DebugLog($"Initial parry boost for {agent.Name}: Handling x{num:F2}");
				}
			}
		}
		catch (Exception ex)
		{
			DualWieldSettings.DebugLog("BlockPatch error: " + ex.Message);
		}
	}
}
