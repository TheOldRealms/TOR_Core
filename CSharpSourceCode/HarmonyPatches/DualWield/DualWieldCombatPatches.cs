// Vendored from Yotthani's DualWield-v0.10.9 mod (permission granted
// 2026-11-24). Full port, ilspycmd decompile artifacts hand-fixed.
//
// Harmony patch rationale: see the class-level <summary> below for
// Problem / Why not non-patching / Scope (per feedback_harmony_patch_rationale).

using System;
using System.Reflection;
using TOR_Core.BattleMechanics.DualWield;
using HarmonyLib;
using TaleWorlds.MountAndBlade;

namespace TOR_Core.HarmonyPatches.DualWield;

[HarmonyPatchCategory("LatePatches")]
[HarmonyPatch]
public static class DualWieldCombatPatches
{
	private static bool _armoryLogged;

	private static MethodBase TargetMethod()
	{
		return AccessTools.Method(typeof(MissionCombatMechanicsHelper), "ComputeBlowMagnitudeMelee", (Type[])null, (Type[])null);
	}

	internal static bool ArmoryActive()
	{
		bool flag = false;
		try
		{
			MissionGameModels current = MissionGameModels.Current;
			flag = ((current == null) ? null : ((object)current.StrikeMagnitudeModel)?.GetType().Name) == "ArmoryStrikeMagnitudeModel";
		}
		catch
		{
		}
		if (flag && !_armoryLogged)
		{
			_armoryLogged = true;
			DualWieldLog.Log("[Combat] Armory computes the damage - our armor penalty on dual wielders is off");
		}
		return flag;
	}

	[HarmonyPostfix]
	public static void Postfix(AttackInformation attackInformation, ref float baseMagnitude, ref float specialMagnitude)
	{
		long started = DwPerf.Start();
		try
		{
			if (!DualWieldMissionBehavior.IsActive || !DualWieldSettings.Get().EnableDualWield)
			{
				return;
			}
			DualWieldSettings dualWieldSettings = DualWieldSettings.Get();
			Agent attackerAgent = attackInformation.AttackerAgent;
			// Gate on IsDualWielding not IsPaired so the offhand-damage multiplier
			// fires for every offhand strike while the loadout is active, not only
			// during Yotthani's narrow synchronous-swing window.
			if (attackerAgent != null && (DualWieldStateManager.IsDualWielding(attackerAgent) || LeftBladeStrike.ApplyingLeftHit))
			{
				if (DwRules.OffHandHit(LeftBladeStrike.ApplyingLeftHit, attackerAgent.IsMainAgent && DualWieldMissionBehavior.LeftHanded))
				{
					float offhandMult = BerserkBonusModel.OffHandDamageMultiplier(attackerAgent);
					baseMagnitude *= offhandMult;
					specialMagnitude *= offhandMult;
					if (DualWieldSettings.Get().DebugMode)
					{
						DualWieldSettings.DebugLog($"[Combat] Off-hand strike: magnitude {baseMagnitude:F1}");
					}
					if (DualWieldLog.Unlimited)
					{
						DualWieldLog.Log($"[Combat] off-hand magnitude {baseMagnitude:F1} special {specialMagnitude:F1}");
					}
				}
				else
				{
					if (DualWieldSettings.Get().DebugMode)
					{
						DualWieldSettings.DebugLog($"[Combat] Main-hand strike: magnitude {baseMagnitude:F1}");
					}
					if (DualWieldLog.Unlimited)
					{
						DualWieldLog.Log($"[Combat] main-hand magnitude {baseMagnitude:F1} special {specialMagnitude:F1}");
					}
				}
			}
			Agent victimAgent = attackInformation.VictimAgent;
			if (victimAgent != null && DualWieldStateManager.IsDualWielding(victimAgent) && dualWieldSettings.ArmorPenalty > 0f && !ArmoryActive())
			{
				float num = 1f + dualWieldSettings.ArmorPenalty;
				baseMagnitude *= num;
				specialMagnitude *= num;
				if (DualWieldSettings.Get().DebugMode)
				{
					DualWieldSettings.DebugLog($"Victim armor penalty: magnitude increased to {baseMagnitude:F1}");
				}
			}
		}
		catch (Exception ex)
		{
			DualWieldSettings.DebugLog("CombatPatch error: " + ex.Message);
		}
		finally
		{
			DwPerf.Stop(7, started);
		}
	}
}
