// Vendored from Yotthani's DualWield-v0.10.9 mod (permission granted
// 2026-11-24). Class name prefixed DualWield for TOR namespacing;
// internal logic preserved as-is. See TORTasks/feral-combat/architecture-
// dualwield-vendored.md for the big picture.

using System.Runtime.CompilerServices;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace TOR_Core.BattleMechanics.DualWield;

public static class DualWieldDetector
{
	private static readonly WeaponClass[] ValidWeaponClasses;

	public static bool IsDualWieldEquipped(Equipment spawnEquipment, bool allowShield = true)
	{
		if (spawnEquipment == null)
		{
			return false;
		}
		if (!allowShield)
		{
			for (EquipmentIndex val = (EquipmentIndex)0; (int)val < 5; val = (EquipmentIndex)(val + 1))
			{
				EquipmentElement val2 = spawnEquipment[val];
				if (!val2.IsEmpty)
				{
					ItemObject item = val2.Item;
					if (((item != null) ? item.PrimaryWeapon : null) != null && val2.Item.PrimaryWeapon.IsShield)
					{
						return false;
					}
				}
			}
		}
		EquipmentElement element = spawnEquipment[(EquipmentIndex)0];
		EquipmentElement element2 = spawnEquipment[(EquipmentIndex)1];
		EquipmentElement element3 = spawnEquipment[(EquipmentIndex)4];
		if (IsValidOneHandedWeapon(element))
		{
			if (!IsValidOneHandedWeapon(element2))
			{
				return IsValidOneHandedWeapon(element3);
			}
			return true;
		}
		return false;
	}

	public static bool IsDualWieldEquipped(MissionEquipment equipment, bool allowShield = true)
	{
		if (equipment == null)
		{
			return false;
		}
		if (!allowShield)
		{
			for (EquipmentIndex val = (EquipmentIndex)0; (int)val < 5; val = (EquipmentIndex)(val + 1))
			{
				MissionWeapon val2 = equipment[val];
				if (!val2.IsEmpty)
				{
					ItemObject item = val2.Item;
					if (((item != null) ? item.PrimaryWeapon : null) != null && val2.Item.PrimaryWeapon.IsShield)
					{
						return false;
					}
				}
			}
		}
		MissionWeapon weapon = equipment[(EquipmentIndex)0];
		MissionWeapon weapon2 = equipment[(EquipmentIndex)1];
		MissionWeapon weapon3 = equipment[(EquipmentIndex)4];
		if (IsValidOneHandedMissionWeapon(weapon))
		{
			if (!IsValidOneHandedMissionWeapon(weapon2))
			{
				return IsValidOneHandedMissionWeapon(weapon3);
			}
			return true;
		}
		return false;
	}

	public static bool PlayerCanDualWield(MissionEquipment equipment)
	{
		if (equipment == null)
		{
			return false;
		}
		int num = 0;
		for (EquipmentIndex val = (EquipmentIndex)0; (int)val <= 3; val = (EquipmentIndex)(val + 1))
		{
			if (IsValidOneHandedMissionWeapon(equipment[val]))
			{
				num++;
			}
		}
		return num >= 2;
	}

	public static bool IsPlayer(Agent agent)
	{
		if (agent != null)
		{
			if ((int)agent.Controller != 2)
			{
				if (agent.Character != null && Game.Current != null)
				{
					return agent.Character == Game.Current.PlayerTroop;
				}
				return false;
			}
			return true;
		}
		return false;
	}

	private static bool IsValidOneHandedWeapon(EquipmentElement element)
	{
		if (!element.IsEmpty)
		{
			ItemObject item = element.Item;
			if (((item != null) ? item.PrimaryWeapon : null) != null)
			{
				WeaponComponentData primaryWeapon = element.Item.PrimaryWeapon;
				if (primaryWeapon.IsShield)
				{
					return false;
				}
				WeaponClass[] validWeaponClasses = ValidWeaponClasses;
				foreach (WeaponClass val in validWeaponClasses)
				{
					if (primaryWeapon.WeaponClass == val)
					{
						return true;
					}
				}
				return false;
			}
		}
		return false;
	}

	private static bool IsValidOneHandedMissionWeapon(MissionWeapon weapon)
	{
		if (weapon.IsEmpty || weapon.CurrentUsageItem == null)
		{
			return false;
		}
		if (weapon.CurrentUsageItem.IsShield)
		{
			return false;
		}
		WeaponClass[] validWeaponClasses = ValidWeaponClasses;
		foreach (WeaponClass val in validWeaponClasses)
		{
			if (weapon.CurrentUsageItem.WeaponClass == val)
			{
				return true;
			}
		}
		return false;
	}

	static DualWieldDetector()
	{
		// The four one-handed melee weapon classes that qualify for dual-wield
		// (non-shield, non-two-handed, non-ranged). Hand-written to replace the
		// ilspycmd-emitted __ldtoken init that doesn't compile.
		ValidWeaponClasses = new WeaponClass[]
		{
			WeaponClass.OneHandedSword,
			WeaponClass.OneHandedAxe,
			WeaponClass.Mace,
			WeaponClass.Dagger
		};
	}
}
