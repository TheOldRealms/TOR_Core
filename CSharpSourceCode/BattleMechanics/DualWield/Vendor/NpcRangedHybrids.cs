// Vendored from Yotthani's DualWield-v0.10.9 mod (permission granted
// 2026-11-24). Class name prefixed DualWield for TOR namespacing;
// internal logic preserved as-is. See TORTasks/feral-combat/architecture-
// dualwield-vendored.md for the big picture.

using System;
using System.Collections.Generic;
using TOR_Core.HarmonyPatches.DualWield;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace TOR_Core.BattleMechanics.DualWield.Vendor;

public sealed class NpcRangedHybrids
{
	private const float Every = 1f;

	private readonly Dictionary<int, RangedHybridRule> _rules = new Dictionary<int, RangedHybridRule>();

	private readonly List<int> _snapshot = new List<int>();

	private float _clock;

	private int _logged;

	public void Add(Agent agent)
	{
		if (agent != null && !agent.IsPlayerControlled && HasTwoHandedRanged(agent))
		{
			_rules[agent.Index] = new RangedHybridRule();
			if (_logged++ < 8)
			{
				DualWieldLog.Log($"[Ranged] {agent.Name}: dual wielder with a bow - left blade away while no enemy is within {20f:0} m");
			}
		}
	}

	public void Remove(int agentIndex)
	{
		_rules.Remove(agentIndex);
	}

	public void Clear()
	{
		_rules.Clear();
		_clock = 0f;
		_logged = 0;
	}

	public void Tick(float dt, DualWieldMissionBehavior behavior)
	{
		if (_rules.Count == 0)
		{
			return;
		}
		_clock += dt;
		if (_clock < 1f)
		{
			return;
		}
		float clock = _clock;
		_clock = 0f;
		Mission current = Mission.Current;
		if (current == null)
		{
			return;
		}
		_snapshot.Clear();
		_snapshot.AddRange(_rules.Keys);
		foreach (int item in _snapshot)
		{
			Agent val = current.FindAgentWithIndex(item);
			if (val == null || !val.IsActive())
			{
				_rules.Remove(item);
			}
			else
			{
				if (val.IsPlayerControlled)
				{
					continue;
				}
				EquipmentIndex offhandWieldedItemIndex = val.GetOffhandWieldedItemIndex();
				bool leftHeld = (int)offhandWieldedItemIndex != -1 && DualWieldWieldingPatches.IsOneHandedMeleeWeapon(val.Equipment[offhandWieldedItemIndex]);
				EquipmentIndex primaryWieldedItemIndex = val.GetPrimaryWieldedItemIndex();
				bool mainOneHanded = (int)primaryWieldedItemIndex != -1 && DualWieldWieldingPatches.IsOneHandedMeleeWeapon(val.Equipment[primaryWieldedItemIndex]);
				float num = NearestEnemy(current, val);
				switch (_rules[item].Step(clock, leftHeld, mainOneHanded, RangedReady(val), num))
				{
				case 1:
					DualWieldWieldingPatches.ReleaseLeftForRanged(val, behavior, (num == float.MaxValue) ? "no enemy" : $"nearest enemy {num:0} m");
					break;
				case 2:
					DualWieldWieldingPatches.ForceOffHandPairing(val);
					if (DualWieldLog.Unlimited)
					{
						DualWieldLog.Log($"[Ranged] {val.Name}: left blade back (nearest enemy {num:0} m)");
					}
					break;
				}
			}
		}
	}

	private static bool HasTwoHandedRanged(Agent agent)
	{
		for (EquipmentIndex val = (EquipmentIndex)0; (int)val < 5; val = (EquipmentIndex)(val + 1))
		{
			MissionWeapon val2 = agent.Equipment[val];
			if (!val2.IsEmpty && val2.CurrentUsageItem != null && val2.CurrentUsageItem.IsRangedWeapon && !val2.CurrentUsageItem.IsConsumable && (val2.CurrentUsageItem.WeaponFlags & (WeaponFlags)0x10) != 0)
			{
				return true;
			}
		}
		return false;
	}

	private static bool RangedReady(Agent agent)
	{
		if (!HasTwoHandedRanged(agent))
		{
			return false;
		}
		for (EquipmentIndex val = (EquipmentIndex)0; (int)val < 5; val = (EquipmentIndex)(val + 1))
		{
			MissionWeapon val2 = agent.Equipment[val];
			if (!val2.IsEmpty && val2.CurrentUsageItem != null && val2.CurrentUsageItem.IsAmmo && val2.Amount > 0)
			{
				return true;
			}
		}
		return false;
	}

	private static float NearestEnemy(Mission mission, Agent agent)
	{
		if (agent.Team == null)
		{
			return float.MaxValue;
		}
		Vec3 position = agent.Position;
		float num = float.MaxValue;
		foreach (Agent item in (List<Agent>)(object)mission.Agents)
		{
			if (item.IsHuman && item.IsActive() && item.Team != null && item.Team.IsEnemyOf(agent.Team))
			{
				Vec3 val = item.Position - position;
				float lengthSquared = val.LengthSquared;
				if (lengthSquared < num)
				{
					num = lengthSquared;
				}
			}
		}
		if (num != float.MaxValue)
		{
			return (float)Math.Sqrt(num);
		}
		return num;
	}
}
