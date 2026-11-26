// Vendored from Yotthani's DualWield-v0.10.9 mod (permission granted
// 2026-11-24). Full port, ilspycmd decompile artifacts hand-fixed.
//
// Harmony patch rationale: see the class-level <summary> below for
// Problem / Why not non-patching / Scope (per feedback_harmony_patch_rationale).

using System;
using System.Collections.Generic;
using System.Reflection;
using TOR_Core.BattleMechanics.DualWield.Vendor;
using HarmonyLib;
using TaleWorlds.MountAndBlade;

namespace TOR_Core.HarmonyPatches.DualWield;

[HarmonyPatch]
public static class DualWieldStatPatches
{
	private static int _depth;

	public static IEnumerable<MethodBase> TargetMethods()
	{
		return StatModelTargets.Find("UpdateAgentStats");
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
		long started = DwPerf.Start();
		try
		{
			if (!DualWieldMissionBehavior.IsActive || !DualWieldSettings.Get().EnableDualWield || !DualWieldStateManager.IsPaired(agent))
			{
				return;
			}
			if (agent.IsMainAgent)
			{
				if (DualWieldMissionBehavior.PlayerMainLeftStriking)
				{
					agentDrivenProperties.MaxSpeedMultiplier *= agentDrivenProperties.CombatMaxSpeedMultiplier;
				}
				if (DualWieldMissionBehavior.PlayerOffHandStriking)
				{
					float num = DualWieldMissionBehavior.LeftStrikeSpeed(agent);
					agentDrivenProperties.MaxSpeedMultiplier *= num;
					agentDrivenProperties.CombatMaxSpeedMultiplier *= num;
				}
			}
			DualWieldSettings dualWieldSettings = DualWieldSettings.Get();
			float num2 = DualWieldSettings.BonusShare(agent);
			if (!(num2 <= 0f))
			{
				agentDrivenProperties.SwingSpeedMultiplier *= 1f + dualWieldSettings.AttackSpeedBonus * num2;
				agentDrivenProperties.HandlingMultiplier *= 1f + dualWieldSettings.AttackSpeedBonus * 0.5f * num2;
				agentDrivenProperties.MaxSpeedMultiplier *= 1f + dualWieldSettings.MovementSpeedBonus * num2;
				agentDrivenProperties.CombatMaxSpeedMultiplier *= 1f + dualWieldSettings.MovementSpeedBonus * num2;
				if (DualWieldSettings.Get().DebugMode)
				{
					DualWieldSettings.DebugLog($"Stats applied to {agent.Name}: SwingSpeed={agentDrivenProperties.SwingSpeedMultiplier:F2}, MaxSpeed={agentDrivenProperties.MaxSpeedMultiplier:F2}");
				}
			}
		}
		catch (Exception ex)
		{
			DualWieldSettings.DebugLog("UpdateAgentStats patch error: " + ex.Message);
		}
		finally
		{
			DwPerf.Stop(6, started);
		}
	}
}
