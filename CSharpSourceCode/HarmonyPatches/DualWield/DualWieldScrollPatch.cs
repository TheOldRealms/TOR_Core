// Vendored from Yotthani's DualWield-v0.10.9 mod (permission granted
// 2026-11-24). Full port, ilspycmd decompile artifacts hand-fixed.
//
// Harmony patch rationale: see the class-level <summary> below for
// Problem / Why not non-patching / Scope (per feedback_harmony_patch_rationale).

using System;
using TOR_Core.BattleMechanics.DualWield;
using HarmonyLib;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace TOR_Core.HarmonyPatches.DualWield;

[HarmonyPatchCategory("LatePatches")]
[HarmonyPatch(typeof(Agent), "WieldNextWeapon")]
public static class DualWieldScrollPatch
{
	[HarmonyPrefix]
	public static bool Prefix(Agent __instance, Agent.HandIndex weaponIndex)
	{
		try
		{
			if (!DualWieldWieldingPatches.OwnsPlayerWield(__instance, out var behavior))
			{
				return true;
			}
			EquipmentIndex primaryWieldedItemIndex = __instance.GetPrimaryWieldedItemIndex();
			if ((int)primaryWieldedItemIndex == -1)
			{
				return true;
			}
			return !(((int)weaponIndex == 0) ? DualWieldWieldingPatches.ScrollUp(__instance, behavior, primaryWieldedItemIndex) : DualWieldWieldingPatches.ScrollDown(__instance, behavior, primaryWieldedItemIndex));
		}
		catch (Exception ex)
		{
			DualWieldLog.Log("[Wield] scroll failed: " + ex.Message);
			return true;
		}
	}
}
