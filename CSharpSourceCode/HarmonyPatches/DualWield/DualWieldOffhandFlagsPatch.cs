// Vendored from Yotthani's DualWield-v0.10.9 mod (permission granted
// 2026-11-24). Full port, ilspycmd decompile artifacts hand-fixed.
//
// Harmony patch rationale: see the class-level <summary> below for
// Problem / Why not non-patching / Scope (per feedback_harmony_patch_rationale).

using System;
using TOR_Core.BattleMechanics.DualWield;
using HarmonyLib;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;

namespace TOR_Core.HarmonyPatches.DualWield;

[HarmonyPatchCategory("LatePatches")]
[HarmonyPatch(typeof(Agent), "WeaponEquipped")]
public static class DualWieldOffhandFlagsPatch
{
	private const uint OffHandBits = 524544u;

	private const uint AttachMask = 768u;

	private static int _logged;

	private static int _loggedRein;

	private static int _dwOffhandUsage = -2;

	private const float EdgeLeadRoll = 50f;

	private const float EdgeLeadRollBackwards = -40f;

	[HarmonyPrefix]
	public static void Prefix(Agent __instance, EquipmentIndex equipmentSlot, WeaponStatsData[] weaponStatsData)
	{
		try
		{
			if (weaponStatsData == null || weaponStatsData.Length == 0)
			{
				return;
			}
			if (DualWieldWieldingPatches.TryGetPlayerLeft(__instance, out var left))
			{
				if (equipmentSlot != left)
				{
					if ((int)left != -1)
					{
						FreeReinHand(__instance, equipmentSlot, weaponStatsData);
					}
					return;
				}
				MissionWeapon val = __instance.Equipment[equipmentSlot];
				ItemObject item = val.Item;
				if (item != null)
				{
					SetLeftUsage(weaponStatsData);
					float num = RollLeft(weaponStatsData, item);
					SetLeftFlags(weaponStatsData);
					DualWieldLog.Log($"[OffhandFlags] player left hand: slot {equipmentSlot + 1} '{((MBObjectBase)item).StringId}' (flags, dw_offhand, roll {num:0} deg)");
				}
			}
			else
			{
				if ((int)equipmentSlot < 0 || (int)equipmentSlot > 4 || __instance == null || !__instance.IsHuman || !DualWieldSettings.Get().EnableDualWield)
				{
					return;
				}
				MissionEquipment equipment = __instance.Equipment;
				if (equipment == null || !DualWieldDetector.IsDualWieldEquipped(equipment, DualWieldDetector.IsPlayer(__instance)))
				{
					return;
				}
				if ((int)equipmentSlot != 0 && (int)equipmentSlot != 1 && (int)equipmentSlot != 4)
				{
					FreeReinHand(__instance, equipmentSlot, weaponStatsData);
					return;
				}
				MissionWeapon val2 = equipment[(EquipmentIndex)0];
				EquipmentIndex val3 = (EquipmentIndex)(DualWieldWieldingPatches.IsOneHandedMeleeWeapon(equipment[(EquipmentIndex)1]) ? 1 : 4);
				MissionWeapon val4 = equipment[val3];
				if (val4.IsEmpty || val4.Item == null)
				{
					return;
				}
				bool flag = !val2.IsEmpty && val2.Item != null && (val2.Item.ItemFlags & (ItemFlags)0x80000) > 0;
				bool flag2 = (val4.Item.ItemFlags & (ItemFlags)0x80000) > 0;
				EquipmentIndex val5 = (EquipmentIndex)((!flag) ? ((int)val3) : 0);
				if (equipmentSlot != val5)
				{
					FreeReinHand(__instance, equipmentSlot, weaponStatsData);
				}
				if (equipmentSlot == val5)
				{
					SetLeftUsage(weaponStatsData);
					ItemObject item2 = (((int)val5 == 0) ? val2.Item : val4.Item);
					float num2 = RollLeft(weaponStatsData, item2);
					if (_logged < 6)
					{
						_logged++;
						ItemObject val6 = (((int)val5 == 0) ? val2.Item : val4.Item);
						DualWieldLog.Log(string.Format("[OffhandFlags] left hand '{0}' (slot {1}) usage -> dw_offhand #{2}, roll {3:0} deg, frame u={4} s={5} o={6}", new object[7]
						{
							((MBObjectBase)val6).StringId,
							(int)val5,
							_dwOffhandUsage,
							num2,
							weaponStatsData[0].WeaponFrame.rotation.u,
							weaponStatsData[0].WeaponFrame.rotation.s,
							weaponStatsData[0].WeaponFrame.origin
						}));
					}
				}
				if (equipmentSlot == val3 && !(flag | flag2))
				{
					SetLeftFlags(weaponStatsData);
					if (DualWieldLog.Unlimited || __instance.IsMainAgent)
					{
						DualWieldLog.Log($"[OffhandFlags] {__instance.Name}: slot {(int)val3} '{((MBObjectBase)val4.Item).StringId}' equipped with off-hand flags (native copy only)");
					}
				}
			}
		}
		catch (Exception ex)
		{
			DualWieldLog.Log("[OffhandFlags] failed: " + ex.Message);
		}
	}

	private static void FreeReinHand(Agent agent, EquipmentIndex slot, WeaponStatsData[] data)
	{
		if (DualWieldSettings.Get().FreeReinHand && DualWieldWieldingPatches.IsOneHandedMeleeWeapon(agent.Equipment[slot]))
		{
			for (int i = 0; i < data.Length; i++)
			{
				data[i].WeaponFlags |= 2097152uL;
			}
			if (_loggedRein < 6 || agent.IsMainAgent)
			{
				_loggedRein++;
				string name = agent.Name;
				object arg = slot + 1;
				MissionWeapon val = agent.Equipment[slot];
				ItemObject item = val.Item;
				DualWieldLog.Log($"[OffhandFlags] {name}: slot {arg} '{((item != null) ? ((MBObjectBase)item).StringId : null)}' right hand - left hand off the reins (TwoHandIdleOnMount, native copy only)");
			}
		}
	}

	private static void SetLeftUsage(WeaponStatsData[] data)
	{
		if (_dwOffhandUsage == -2)
		{
			_dwOffhandUsage = MBItem.GetItemUsageIndex("tor_dw_offhand");
		}
		if (_dwOffhandUsage >= 0)
		{
			for (int i = 0; i < data.Length; i++)
			{
				data[i].ItemUsageIndex = _dwOffhandUsage;
			}
		}
	}

	private static float RollLeft(WeaponStatsData[] data, ItemObject item)
	{
		float num = (((item.ItemFlags & (ItemFlags)0x80000) != 0) ? 180f : 270f);
		try
		{
			num += DualWieldSettings.Get().OffhandWeaponRoll;
		}
		catch
		{
		}
		float result = (num + 50f) % 360f;
		for (int i = 0; i < data.Length; i++)
		{
			bool flag = (data[i].WeaponFlags & 0x200000000uL) != 0;
			float num2 = (flag ? (num + -40f) : (num + 50f)) % 360f;
			if (num2 < 0f)
			{
				num2 += 360f;
			}
			MatrixFrame val = data[i].WeaponFrame;
			if (num2 != 0f)
			{
				val.rotation.RotateAboutUp(num2 * ((float)Math.PI / 180f));
			}
			if (flag)
			{
				val = TurnedInHand(val);
			}
			data[i].WeaponFrame = val;
		}
		return result;
	}

	private static MatrixFrame TurnedInHand(MatrixFrame f)
	{
		f.rotation.s = new Vec3(f.rotation.s.x, 0f - f.rotation.s.y, 0f - f.rotation.s.z, -1f);
		f.rotation.f = new Vec3(f.rotation.f.x, 0f - f.rotation.f.y, 0f - f.rotation.f.z, -1f);
		f.rotation.u = new Vec3(f.rotation.u.x, 0f - f.rotation.u.y, 0f - f.rotation.u.z, -1f);
		f.origin = new Vec3(f.origin.x, 0f - f.origin.y, 0f - f.origin.z, -1f);
		return f;
	}

	private static void SetLeftFlags(WeaponStatsData[] data)
	{
		for (int i = 0; i < data.Length; i++)
		{
			data[i].Properties = (data[i].Properties & 0xFFFFFCFFu) | 0x80100;
		}
	}
}
