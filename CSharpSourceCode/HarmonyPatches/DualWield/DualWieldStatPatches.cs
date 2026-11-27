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
			// Gate on IsDualWielding (equipment state) rather than IsPaired
			// (Yotthani's transient synchronous-swing substate) so the paired-
			// weapons stat bonus applies continuously while the loadout is
			// active, matching how vanilla OneHanded's level effects fire.
			if (!DualWieldMissionBehavior.IsActive || !DualWieldSettings.Get().EnableDualWield || !DualWieldStateManager.IsDualWielding(agent))
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
			float swingBonus = BerserkBonusModel.SwingSpeedBonus(agent);
			float moveBonus = BerserkBonusModel.MovementSpeedBonus(agent);
			if (swingBonus > 0f || moveBonus > 0f)
			{
				agentDrivenProperties.SwingSpeedMultiplier *= 1f + swingBonus;
				agentDrivenProperties.HandlingMultiplier *= 1f + swingBonus * 0.5f;
				agentDrivenProperties.MaxSpeedMultiplier *= 1f + moveBonus;
				agentDrivenProperties.CombatMaxSpeedMultiplier *= 1f + moveBonus;
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
