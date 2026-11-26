// Vendored from Yotthani's DualWield-v0.10.9 mod (permission granted
// 2026-11-24). Full port, ilspycmd decompile artifacts hand-fixed.
//
// Harmony patch rationale: see the class-level <summary> below for
// Problem / Why not non-patching / Scope (per feedback_harmony_patch_rationale).

using System;
using TOR_Core.BattleMechanics.DualWield.Vendor;
using HarmonyLib;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace TOR_Core.HarmonyPatches.DualWield;

[HarmonyPatch(typeof(Agent), "TryToWieldWeaponInSlot")]
public static class DualWieldDrawLeftAsMainPatch
{
	[HarmonyPrefix]
	public static void Prefix(Agent __instance, EquipmentIndex slotIndex, bool isWieldedOnSpawn)
	{
		try
		{
			if (!isWieldedOnSpawn && DualWieldWieldingPatches.OwnsPlayerWield(__instance, out var behavior))
			{
				DualWieldWieldingPatches.PrepareLeftAsMain(__instance, behavior, slotIndex);
			}
		}
		catch (Exception ex)
		{
			DualWieldLog.Log("[Wield] draw left as main failed: " + ex.Message);
		}
	}
}
