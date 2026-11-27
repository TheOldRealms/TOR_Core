// Vendored from Yotthani's DualWield-v0.10.9 mod (permission granted
// 2026-11-24). Full port, ilspycmd decompile artifacts hand-fixed.
//
// Harmony patch rationale: see the class-level <summary> below for
// Problem / Why not non-patching / Scope (per feedback_harmony_patch_rationale).

using System;
using TOR_Core.BattleMechanics.DualWield;
using HarmonyLib;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;

namespace TOR_Core.HarmonyPatches.DualWield;

[HarmonyPatch(typeof(Agent), "InitializeSpawnEquipment")]
public static class DualWieldNpcBannerSlotPatch
{
	private static int _logged;

	[HarmonyPrefix]
	public static void Prefix(Agent __instance, Equipment spawnEquipment)
	{
		try
		{
			if (spawnEquipment == null || __instance == null || !__instance.IsHuman || !DualWieldSettings.Get().EnableDualWield || (int)__instance.Controller == 2)
			{
				return;
			}
			BasicCharacterObject character = __instance.Character;
			if (character != null && Game.Current != null && character == Game.Current.PlayerTroop)
			{
				return;
			}
			EquipmentElement val = spawnEquipment[(EquipmentIndex)4];
			if (val.IsEmpty && DualWieldDetector.IsDualWieldEquipped(spawnEquipment, allowShield: false))
			{
				EquipmentElement val2 = (spawnEquipment[(EquipmentIndex)4] = spawnEquipment[(EquipmentIndex)1]);
				spawnEquipment[(EquipmentIndex)1] = EquipmentElement.Invalid;
				if (_logged++ < 12)
				{
					TextObject arg = ((character != null) ? character.Name : null);
					ItemObject item = val2.Item;
					string arg2 = ((item != null) ? ((MBObjectBase)item).StringId : null);
					val = spawnEquipment[(EquipmentIndex)0];
					ItemObject item2 = val.Item;
					DualWieldLog.Log($"[Spawn] {arg}: left weapon '{arg2}' -> slot 4 (banner slot), right '{((item2 != null) ? ((MBObjectBase)item2).StringId : null)}'");
				}
			}
		}
		catch (Exception ex)
		{
			DualWieldLog.Log("[Spawn] banner slot failed: " + ex.Message);
		}
	}
}
