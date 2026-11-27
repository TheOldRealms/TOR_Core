// Vendored from Yotthani's DualWield-v0.10.9 mod (permission granted
// 2026-11-24). Full port, ilspycmd decompile artifacts hand-fixed.
//
// Harmony patch rationale: see the class-level <summary> below for
// Problem / Why not non-patching / Scope (per feedback_harmony_patch_rationale).

using System;
using System.Collections.Generic;
using TOR_Core.BattleMechanics.DualWield;
using TOR_Core.Extensions;
using HarmonyLib;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;

namespace TOR_Core.HarmonyPatches.DualWield;

[HarmonyPatchCategory("LatePatches")]
[HarmonyPatch(typeof(Agent), "OnWieldedItemIndexChange")]
public static class DualWieldWieldingPatches
{
	private sealed class LeftGesture
	{
		public EquipmentIndex Slot;

		public string Equip;

		public float Arrived;
	}

	private sealed class LeftSheath
	{
		public int Action;

		public float TakeOutAt;

		public float Until;

		public string Name;
	}

	private static bool _isAutoEquipping;

	private static readonly Dictionary<int, EquipmentIndex> _trackedOffHand = new Dictionary<int, EquipmentIndex>();

	private static readonly HashSet<int> _singleByChoice = new HashSet<int>();

	private static readonly Dictionary<int, EquipmentIndex> _playerLeft = new Dictionary<int, EquipmentIndex>();

	private const float LeftGestureSeconds = 2f;

	private static readonly Dictionary<int, KeyValuePair<EquipmentIndex, float>> _leftDrawing = new Dictionary<int, KeyValuePair<EquipmentIndex, float>>();

	private static readonly Dictionary<int, LeftGesture> _leftGesture = new Dictionary<int, LeftGesture>();

	private static readonly Dictionary<int, LeftGesture> _leftSeen = new Dictionary<int, LeftGesture>();

	private static readonly Dictionary<int, LeftSheath> _leftSheathing = new Dictionary<int, LeftSheath>();

	private static bool LeftGestures
	{
		get
		{
			try
			{
				return DualWieldSettings.Get().LeftDrawGesture;
			}
			catch
			{
				return true;
			}
		}
	}

	[HarmonyPostfix]
	public static void Postfix(Agent __instance, bool isOffHand, bool isWieldedInstantly, bool isWieldedOnSpawn)
	{
		long started = DwPerf.Start();
		try
		{
			if (!DualWieldMissionBehavior.IsActive || _isAutoEquipping)
			{
				return;
			}
			if (DualWieldLog.Unlimited)
			{
				DualWieldLog.Log(string.Format("[Wield] OnWieldedItemIndexChange: agent={0}, isOffHand={1}, instant={2}, onSpawn={3}", new object[4]
				{
					(__instance != null) ? __instance.Name : null,
					isOffHand,
					isWieldedInstantly,
					isWieldedOnSpawn
				}));
			}
			if (isOffHand || Mission.Current == null || !DualWieldSettings.Get().EnableDualWield || __instance == null || !__instance.IsHuman)
			{
				return;
			}
			if (!DualWieldStateManager.IsDualWielding(__instance))
			{
				if (DualWieldLog.Unlimited)
				{
					DualWieldLog.Log("[Wield] Agent " + __instance.Name + ": NOT in StateManager, skipping");
				}
			}
			else
			{
				ForceOffHandPairing(__instance, isWieldedInstantly, isWieldedOnSpawn);
			}
		}
		catch (Exception ex)
		{
			DualWieldLog.Log($"[Wield] ERROR: {ex}");
			DualWieldSettings.DebugLog("WieldingPatch error: " + ex.Message);
		}
		finally
		{
			DwPerf.Stop(5, started);
		}
	}

	public static void ForceOffHandPairing(Agent agent, bool instant = true, bool onSpawn = false)
	{
		if (agent == null || Mission.Current == null)
		{
			return;
		}
		DualWieldMissionBehavior missionBehavior = Mission.Current.GetMissionBehavior<DualWieldMissionBehavior>();
		if (missionBehavior == null)
		{
			return;
		}
		EquipmentIndex primaryWieldedItemIndex = agent.GetPrimaryWieldedItemIndex();
		if (DualWieldLog.Unlimited)
		{
			DualWieldLog.Log($"[Wield] ForceOffHandPairing: agent={agent.Name}, mainSlot={primaryWieldedItemIndex}, onSpawn={onSpawn}");
		}
		if ((int)primaryWieldedItemIndex == -1)
		{
			_trackedOffHand.Remove(agent.Index);
			if (DualWieldLog.Unlimited)
			{
				DualWieldLog.Log("[Wield] No weapon wielded → removing off-hand");
			}
			missionBehavior.RemoveOffHandAttachment(agent);
			return;
		}
		MissionWeapon weapon = agent.Equipment[primaryWieldedItemIndex];
		if (weapon.IsEmpty || !IsOneHandedMeleeWeapon(weapon))
		{
			_trackedOffHand.Remove(agent.Index);
			if (DualWieldLog.Unlimited)
			{
				DualWieldLog.Log("[Wield] Main weapon not 1h melee → removing off-hand");
			}
			missionBehavior.RemoveOffHandAttachment(agent);
			return;
		}
		if (agent.IsPlayerControlled && _singleByChoice.Contains(agent.Index))
		{
			_trackedOffHand.Remove(agent.Index);
			missionBehavior.RemoveOffHandAttachment(agent);
			ClearHeldOffhand(agent, "single weapon by choice");
			return;
		}
		if (agent.IsPlayerControlled && !HasXmlOffhandItem(agent))
		{
			EquipmentIndex val = ChooseLeft(agent, primaryWieldedItemIndex);
			if ((int)val != -1)
			{
				SetLeftSlot(agent, val);
				AttachPartner(agent, missionBehavior, val, primaryWieldedItemIndex);
			}
			else
			{
				_trackedOffHand.Remove(agent.Index);
				missionBehavior.RemoveOffHandAttachment(agent);
			}
			return;
		}
		for (EquipmentIndex val2 = (EquipmentIndex)0; (int)val2 < 5; val2 = (EquipmentIndex)(val2 + 1))
		{
			if (val2 == primaryWieldedItemIndex || ((int)val2 != 1 && (int)val2 != 4 && !CarriesOffhandFlag(agent.Equipment[val2])))
			{
				continue;
			}
			MissionWeapon weapon2 = agent.Equipment[val2];
			if (!weapon2.IsEmpty && IsOneHandedMeleeWeapon(weapon2))
			{
				if (DualWieldLog.Unlimited)
				{
					DualWieldLog.Log($"[Wield] Found partner: slot {(int)val2}, main: slot {(int)primaryWieldedItemIndex}");
				}
				AttachPartner(agent, missionBehavior, val2, primaryWieldedItemIndex);
				return;
			}
		}
		_trackedOffHand.Remove(agent.Index);
		if (DualWieldLog.Unlimited)
		{
			DualWieldLog.Log("[Wield] No partner 1h weapon found → removing off-hand");
		}
		missionBehavior.RemoveOffHandAttachment(agent);
	}

	public static void AdoptHeldOffhand(Agent agent, DualWieldMissionBehavior behavior)
	{
		EquipmentIndex offhandWieldedItemIndex = agent.GetOffhandWieldedItemIndex();
		EquipmentIndex primaryWieldedItemIndex = agent.GetPrimaryWieldedItemIndex();
		if ((int)offhandWieldedItemIndex == -1 || (int)primaryWieldedItemIndex == -1 || offhandWieldedItemIndex == primaryWieldedItemIndex || !IsOneHandedMeleeWeapon(agent.Equipment[offhandWieldedItemIndex]) || !IsOneHandedMeleeWeapon(agent.Equipment[primaryWieldedItemIndex]))
		{
			return;
		}
		if (agent.IsPlayerControlled && _singleByChoice.Contains(agent.Index))
		{
			if (!_leftSheathing.ContainsKey(agent.Index))
			{
				ClearHeldOffhand(agent, "single weapon by choice");
			}
			return;
		}
		_isAutoEquipping = true;
		try
		{
			behavior.AttachOffHandWeapon(agent, offhandWieldedItemIndex);
			_trackedOffHand[agent.Index] = offhandWieldedItemIndex;
		}
		finally
		{
			_isAutoEquipping = false;
		}
		string name = agent.Name;
		object arg = (int)offhandWieldedItemIndex;
		MissionWeapon val = agent.Equipment[offhandWieldedItemIndex];
		ItemObject item = val.Item;
		DualWieldLog.Log($"[Wield] {name}: left hand took slot {arg} '{((item != null) ? ((MBObjectBase)item).StringId : null)}' on its own - dual wield on");
	}

	public static void ClearTrackingState()
	{
		_trackedOffHand.Clear();
		_singleByChoice.Clear();
		_playerLeft.Clear();
		_leftDrawing.Clear();
		_leftGesture.Clear();
		_leftSeen.Clear();
		_leftSheathing.Clear();
	}

	private static void AttachPartner(Agent agent, DualWieldMissionBehavior behavior, EquipmentIndex slot, EquipmentIndex mainIndex)
	{
		_isAutoEquipping = true;
		try
		{
			behavior.AttachOffHandWeapon(agent, slot);
			_trackedOffHand[agent.Index] = slot;
			if (DualWieldSettings.Get().DebugMode)
			{
				DualWieldSettings.DebugLog($"Paired off-hand slot {(int)slot} (main: slot {(int)mainIndex})");
			}
		}
		finally
		{
			_isAutoEquipping = false;
		}
	}

	internal static bool TryGetPlayerLeft(Agent agent, out EquipmentIndex left)
	{
		left = (EquipmentIndex)(-1);
		if (agent != null)
		{
			return _playerLeft.TryGetValue(agent.Index, out left);
		}
		return false;
	}

	internal static bool OwnsPlayerWield(Agent agent, out DualWieldMissionBehavior behavior)
	{
		behavior = null;
		if (!DualWieldMissionBehavior.IsActive || _isAutoEquipping)
		{
			return false;
		}
		if (agent == null || !agent.IsMainAgent || !agent.IsActive() || Mission.Current == null)
		{
			return false;
		}
		if (!DualWieldSettings.Get().EnableDualWield || !DualWieldStateManager.IsDualWielding(agent))
		{
			return false;
		}
		behavior = Mission.Current.GetMissionBehavior<DualWieldMissionBehavior>();
		return behavior != null;
	}

	private static bool HasXmlOffhandItem(Agent agent)
	{
		for (EquipmentIndex val = (EquipmentIndex)0; (int)val <= 3; val = (EquipmentIndex)(val + 1))
		{
			if (CarriesOffhandFlag(agent.Equipment[val]))
			{
				return true;
			}
		}
		return false;
	}

	private static EquipmentIndex LeftSlotOf(Agent agent)
	{
		for (EquipmentIndex val = (EquipmentIndex)0; (int)val <= 3; val = (EquipmentIndex)(val + 1))
		{
			if (CarriesOffhandFlag(agent.Equipment[val]))
			{
				return val;
			}
		}
		if (_playerLeft.TryGetValue(agent.Index, out var value))
		{
			return value;
		}
		if (IsOneHandedMeleeWeapon(agent.Equipment[(EquipmentIndex)1]))
		{
			return (EquipmentIndex)1;
		}
		return (EquipmentIndex)(-1);
	}

	private static EquipmentIndex ChooseLeft(Agent agent, EquipmentIndex main)
	{
		EquipmentIndex val = LeftSlotOf(agent);
		if ((int)val != -1 && val != main && IsOneHandedMeleeWeapon(agent.Equipment[val]))
		{
			return val;
		}
		if (!HasXmlOffhandItem(agent))
		{
			bool[] array = new bool[4];
			for (int i = 0; i < 4; i++)
			{
				array[i] = IsOneHandedMeleeWeapon(agent.Equipment[(EquipmentIndex)i]);
			}
			int num = DwRules.LeftSlot((int)main, -1, array);
			if (num < 0)
			{
				return (EquipmentIndex)(-1);
			}
			return (EquipmentIndex)num;
		}
		return (EquipmentIndex)(-1);
	}

	private static bool MainHandCandidate(MissionWeapon weapon)
	{
		if (weapon.IsEmpty || weapon.Item == null || weapon.CurrentUsageItem == null)
		{
			return false;
		}
		switch (((object)weapon.Item.Type/*cast due to constrained. prefix*/).ToString())
		{
		case "Arrows":
		case "Bolts":
		case "SlingStones":
		case "Bullets":
		case "Shield":
		case "Banner":
			return false;
		default:
			return true;
		}
	}

	internal static bool ScrollUp(Agent agent, DualWieldMissionBehavior behavior, EquipmentIndex main)
	{
		FinishLeftSheath(agent, "put away, the wheel was turned again");
		_leftDrawing.Remove(agent.Index);
		bool flag = DualWieldStateManager.IsPaired(agent);
		EquipmentIndex val = LeftSlotOf(agent);
		int num = (((int)main > 3) ? (-1) : ((int)main));
		for (int i = 1; i <= 4; i++)
		{
			int num2 = (num + i) % 4;
			if (num2 == (int)main)
			{
				break;
			}
			EquipmentIndex val2 = (EquipmentIndex)num2;
			if ((!flag || val2 != val) && MainHandCandidate(agent.Equipment[val2]) && !CarriesOffhandFlag(agent.Equipment[val2]))
			{
				string arg = (flag ? "pair" : "single");
				object arg2 = num2 + 1;
				MissionWeapon val3 = agent.Equipment[val2];
				ItemObject item = val3.Item;
				DualWieldLog.Log($"[Wield] wheel up ({arg}): slot {arg2} '{((item != null) ? ((MBObjectBase)item).StringId : null)}'");
				agent.TryToWieldWeaponInSlot(val2, (Agent.WeaponWieldActionType)0, false);
				return true;
			}
		}
		if (flag)
		{
			PutLeftAway(agent, behavior, "wheel up past the last combination");
		}
		return true;
	}

	private static string EquipActionNow(Agent agent, out float progress)
	{
		progress = 0f;
		for (int num = 1; num >= 0; num--)
		{
			try
			{
				string text = ActionCache.NameOf(agent.GetCurrentAction(num));
				if (!string.IsNullOrEmpty(text) && text.StartsWith("act_equip_", StringComparison.Ordinal))
				{
					progress = agent.GetCurrentActionProgress(num);
					return text;
				}
			}
			catch
			{
			}
		}
		return null;
	}

	private static ActionIndexCache SheathTwin(string equip, out string name)
	{
		string text = equip.Substring("act_equip_".Length);
		name = "act_tor_dwc_unequip_" + text;
		ActionIndexCache result = ActionCache.Get(name);
		if (result.Index < 0 && text.EndsWith("_left_stance", StringComparison.Ordinal))
		{
			name = "act_tor_dwc_unequip_" + text.Substring(0, text.Length - "_left_stance".Length);
			result = ActionCache.Get(name);
		}
		return result;
	}

	private static bool StartLeftSheath(Agent agent, EquipmentIndex held)
	{
		try
		{
			if (!_leftGesture.TryGetValue(agent.Index, out var value) || value.Slot != held || string.IsNullOrEmpty(value.Equip))
			{
				return false;
			}
			ActionIndexCache val = SheathTwin(value.Equip, out var name);
			if (val.Index < 0)
			{
				return false;
			}
			if (!agent.SetActionChannel(1, ref val, true, (AnimFlags)0, 0f, 1f, -0.2f, 0.4f, 0f, false, -0.2f, 0, true))
			{
				return false;
			}
			float takeOutAt = Math.Max(0.35f, Math.Min(0.9f, 1f - value.Arrived));
			_leftSheathing[agent.Index] = new LeftSheath
			{
				Action = val.Index,
				TakeOutAt = takeOutAt,
				Until = Mission.Current.CurrentTime + 2f,
				Name = name
			};
			return true;
		}
		catch (Exception ex)
		{
			DualWieldLog.Log("[Wield] the sheath gesture failed: " + ex.Message);
			return false;
		}
	}

	private static void FinishLeftSheath(Agent agent, string why)
	{
		if (_leftSheathing.TryGetValue(agent.Index, out var _))
		{
			_leftSheathing.Remove(agent.Index);
			ClearHeldOffhand(agent, why);
			PlayLeftSound(agent, "event:/mission/combat/equip/sword_unequip");
		}
	}

	private static bool DrawLeft(Agent agent, EquipmentIndex left)
	{
		if (!LeftGestures)
		{
			return false;
		}
		try
		{
			SetLeftSlot(agent, left);
			_isAutoEquipping = true;
			try
			{
				agent.TryToWieldWeaponInSlot(left, (Agent.WeaponWieldActionType)0, false);
			}
			finally
			{
				_isAutoEquipping = false;
			}
			_leftDrawing[agent.Index] = new KeyValuePair<EquipmentIndex, float>(left, Mission.Current.CurrentTime + 2f);
			return true;
		}
		catch (Exception ex)
		{
			DualWieldLog.Log("[Wield] drawing the left weapon failed: " + ex.Message);
			return false;
		}
	}

	internal static void TickLeftGestures(Agent agent, DualWieldMissionBehavior behavior)
	{
		if (agent == null || Mission.Current == null)
		{
			return;
		}
		float currentTime = Mission.Current.CurrentTime;
		if (_leftDrawing.TryGetValue(agent.Index, out var value))
		{
			string text = EquipActionNow(agent, out var progress);
			if (text != null)
			{
				_leftSeen[agent.Index] = new LeftGesture
				{
					Slot = value.Key,
					Equip = text,
					Arrived = progress
				};
			}
			if (agent.GetOffhandWieldedItemIndex() == value.Key)
			{
				_leftDrawing.Remove(agent.Index);
				if (_leftSeen.TryGetValue(agent.Index, out var value2) && value2.Slot == value.Key)
				{
					_leftGesture[agent.Index] = value2;
					DualWieldLog.Log($"[Wield] left draw: the engine played {value2.Equip}, the weapon was in the hand at progress {value2.Arrived:0.00}");
				}
				else
				{
					_leftGesture.Remove(agent.Index);
					DualWieldLog.Log("[Wield] left draw: no equip action seen on the agent - no gesture to put it away with");
				}
				_leftSeen.Remove(agent.Index);
				if (!DualWieldStateManager.IsPaired(agent))
				{
					AdoptHeldOffhand(agent, behavior);
				}
			}
			else if (currentTime >= value.Value)
			{
				_leftDrawing.Remove(agent.Index);
				_leftSeen.Remove(agent.Index);
				DualWieldLog.Log($"[Wield] {agent.Name}: the engine did not draw the left weapon (slot {value.Key + 1}) - set into the hand");
				if (!_singleByChoice.Contains(agent.Index))
				{
					ForceOffHandPairing(agent);
				}
			}
		}
		if (!_leftSheathing.TryGetValue(agent.Index, out var value3))
		{
			return;
		}
		bool flag = false;
		float num = 1f;
		try
		{
			ActionIndexCache currentAction = agent.GetCurrentAction(1);
			flag = currentAction.Index == value3.Action;
			if (flag)
			{
				num = agent.GetCurrentActionProgress(1);
			}
		}
		catch
		{
		}
		if (!flag || num >= value3.TakeOutAt || currentTime >= value3.Until)
		{
			FinishLeftSheath(agent, flag ? "put away" : "put away, the gesture was cut short");
		}
	}

	internal static void StartSingle(Agent agent)
	{
		if (agent != null)
		{
			_singleByChoice.Add(agent.Index);
		}
	}

	private static bool IsShield(MissionWeapon weapon)
	{
		if (!weapon.IsEmpty && weapon.CurrentUsageItem != null)
		{
			return weapon.CurrentUsageItem.IsShield;
		}
		return false;
	}

	private static EquipmentIndex NextShield(Agent agent, EquipmentIndex after)
	{
		for (int i = (((int)after != -1) ? (int)(after + 1) : 0); i <= 3; i++)
		{
			if (IsShield(agent.Equipment[(EquipmentIndex)i]))
			{
				return (EquipmentIndex)i;
			}
		}
		return (EquipmentIndex)(-1);
	}

	internal static bool ScrollDown(Agent agent, DualWieldMissionBehavior behavior, EquipmentIndex main)
	{
		FinishLeftSheath(agent, "put away, the wheel was turned again");
		_leftDrawing.Remove(agent.Index);
		bool flag = DualWieldStateManager.IsPaired(agent);
		bool flag2 = IsOneHandedMeleeWeapon(agent.Equipment[main]);
		EquipmentIndex offhandWieldedItemIndex = agent.GetOffhandWieldedItemIndex();
		bool flag3 = (int)offhandWieldedItemIndex != -1 && IsShield(agent.Equipment[offhandWieldedItemIndex]);
		EquipmentIndex val = NextShield(agent, (EquipmentIndex)((!flag3) ? (-1) : ((int)offhandWieldedItemIndex)));
		EquipmentIndex val2 = (EquipmentIndex)(-1);
		if (flag2)
		{
			if (HasXmlOffhandItem(agent))
			{
				val2 = (EquipmentIndex)(flag ? (-1) : ((int)ChooseLeft(agent, main)));
			}
			else
			{
				bool[] array = new bool[4];
				for (int i = 0; i < 4; i++)
				{
					array[i] = IsOneHandedMeleeWeapon(agent.Equipment[(EquipmentIndex)i]);
				}
				int num = ((!flag || (int)offhandWieldedItemIndex == -1 || !IsOneHandedMeleeWeapon(agent.Equipment[offhandWieldedItemIndex])) ? (-1) : ((int)offhandWieldedItemIndex));
				int num2 = ((flag && num < 0) ? (-1) : DwRules.NextLeftOnWheel((int)main, num, array));
				val2 = (EquipmentIndex)((num2 < 0) ? (-1) : num2);
			}
		}
		MissionWeapon val3;
		switch (DwRules.WheelDownOffhand(flag, flag2, (int)val2 != -1, (int)val != -1))
		{
		case 0:
			PutLeftAway(agent, behavior, "wheel down");
			return true;
		case 1:
		{
			object arg = val + 1;
			val3 = agent.Equipment[val];
			ItemObject item2 = val3.Item;
			DualWieldLog.Log($"[Wield] wheel down: shield slot {arg} '{((item2 != null) ? ((MBObjectBase)item2).StringId : null)}' in");
			agent.TryToWieldWeaponInSlot(val, (Agent.WeaponWieldActionType)0, false);
			return true;
		}
		case 2:
		{
			if (flag)
			{
				_trackedOffHand.Remove(agent.Index);
				behavior.RemoveOffHandAttachment(agent);
				ClearHeldOffhand(agent, "wheel down to the next left weapon");
			}
			_singleByChoice.Remove(agent.Index);
			bool flag4 = DrawLeft(agent, val2);
			object[] array2 = new object[5]
			{
				val2 + 1,
				null,
				null,
				null,
				null
			};
			val3 = agent.Equipment[val2];
			ItemObject item = val3.Item;
			array2[1] = ((item != null) ? ((MBObjectBase)item).StringId : null);
			array2[2] = (flag3 ? " (the shield goes)" : "");
			array2[3] = (flag ? " (the other left weapon goes)" : "");
			array2[4] = (flag4 ? " - drawn" : " - at once");
			DualWieldLog.Log(string.Format("[Wield] wheel down: left weapon slot {0} '{1}' in{2}{3}{4}", array2));
			if (!flag4)
			{
				SetLeftSlot(agent, val2);
				ForceOffHandPairing(agent);
			}
			return true;
		}
		default:
			DualWieldLog.Log($"[Wield] wheel down: vanilla's (right hand one-handed: {flag2}, a blade for the left: {(int)val2 != -1})");
			return false;
		}
	}

	private static void PutLeftAway(Agent agent, DualWieldMissionBehavior behavior, string why)
	{
		_singleByChoice.Add(agent.Index);
		_trackedOffHand.Remove(agent.Index);
		_leftDrawing.Remove(agent.Index);
		bool flag = false;
		EquipmentIndex val = (EquipmentIndex)(-1);
		try
		{
			val = agent.GetOffhandWieldedItemIndex();
			flag = (int)val != -1 && IsOneHandedMeleeWeapon(agent.Equipment[val]);
		}
		catch
		{
		}
		if (flag && LeftGestures)
		{
			behavior.RemoveOffHandAttachment(agent, keepInHand: true);
			if (StartLeftSheath(agent, val))
			{
				DualWieldLog.Log(string.Format("[Wield] {0}: left weapon away ({1}) with {2}, out of the hand at progress {3:0.00} - single weapon", new object[4]
				{
					agent.Name,
					why,
					_leftSheathing[agent.Index].Name,
					_leftSheathing[agent.Index].TakeOutAt
				}));
				return;
			}
		}
		behavior.RemoveOffHandAttachment(agent);
		ClearHeldOffhand(agent, why);
		if (flag && LeftGestures)
		{
			PlayLeftSound(agent, "event:/mission/combat/equip/sword_unequip");
		}
		DualWieldLog.Log("[Wield] " + agent.Name + ": left weapon away (" + why + ") - single weapon");
	}

	private static void PlayLeftSound(Agent agent, string path)
	{
		try
		{
			int eventIdFromString = SoundEvent.GetEventIdFromString(path);
			if (eventIdFromString >= 0)
			{
				Mission.Current.MakeSound(eventIdFromString, agent.Position, false, true, agent.Index, -1);
			}
		}
		catch (Exception ex)
		{
			DualWieldLog.Log("[Wield] left weapon sound failed: " + ex.Message);
		}
	}

	internal static void PrepareLeftAsMain(Agent agent, DualWieldMissionBehavior behavior, EquipmentIndex slot)
	{
		if ((int)slot == -1 || HasXmlOffhandItem(agent))
		{
			return;
		}
		if (slot != LeftSlotOf(agent) && !IsOneHandedMeleeWeapon(agent.Equipment[slot]))
		{
			EquipmentIndex offhandWieldedItemIndex = agent.GetOffhandWieldedItemIndex();
			if ((int)offhandWieldedItemIndex != -1 && IsOneHandedMeleeWeapon(agent.Equipment[offhandWieldedItemIndex]))
			{
				FinishLeftSheath(agent, "another weapon drawn");
				_trackedOffHand.Remove(agent.Index);
				_leftDrawing.Remove(agent.Index);
				behavior.RemoveOffHandAttachment(agent);
				object arg = slot + 1;
				MissionWeapon val = agent.Equipment[slot];
				ItemObject item = val.Item;
				ClearHeldOffhand(agent, $"slot {arg} '{((item != null) ? ((MBObjectBase)item).StringId : null)}' needs the left hand");
				if (LeftGestures)
				{
					PlayLeftSound(agent, "event:/mission/combat/equip/sword_unequip");
				}
			}
		}
		else if (slot == LeftSlotOf(agent))
		{
			if (agent.GetOffhandWieldedItemIndex() == slot)
			{
				_trackedOffHand.Remove(agent.Index);
				behavior.RemoveOffHandAttachment(agent);
				ClearHeldOffhand(agent, "left weapon drawn to the right hand");
			}
			SetLeftSlot(agent, (EquipmentIndex)(-1));
		}
	}

	private static void SetLeftSlot(Agent agent, EquipmentIndex left)
	{
		EquipmentIndex val = LeftSlotOf(agent);
		if (val != left)
		{
			_playerLeft[agent.Index] = left;
			Reequip(agent, val, "plain (right hand)");
			Reequip(agent, left, "as the left weapon");
		}
	}

	private static void Reequip(Agent agent, EquipmentIndex slot, string how)
	{
		if ((int)slot == -1)
		{
			return;
		}
		MissionWeapon val = agent.Equipment[slot];
		if (val.IsEmpty)
		{
			return;
		}
		if (agent.GetPrimaryWieldedItemIndex() == slot || agent.GetOffhandWieldedItemIndex() == slot)
		{
			DualWieldLog.Log($"[Wield] {agent.Name}: slot {slot + 1} is in the hand - not equipped again");
			return;
		}
		_isAutoEquipping = true;
		try
		{
			agent.EquipWeaponWithNewEntity(slot, ref val);
		}
		finally
		{
			_isAutoEquipping = false;
		}
		object[] array = new object[4]
		{
			agent.Name,
			slot + 1,
			null,
			null
		};
		ItemObject item = val.Item;
		array[2] = ((item != null) ? ((MBObjectBase)item).StringId : null);
		array[3] = how;
		DualWieldLog.Log(string.Format("[Wield] {0}: slot {1} '{2}' equipped {3}", array));
	}

	internal static void ReleaseLeftForRanged(Agent agent, DualWieldMissionBehavior behavior, string why)
	{
		_trackedOffHand.Remove(agent.Index);
		behavior.RemoveOffHandAttachment(agent);
		ClearHeldOffhand(agent, why);
		if (DualWieldStateManager.IsPaired(agent))
		{
			DualWieldStateManager.SetPaired(agent, paired: false);
			try
			{
				agent.UpdateAgentProperties();
			}
			catch
			{
			}
		}
		DualWieldLog.Log("[Ranged] " + agent.Name + ": left blade away (" + why + ") - the AI may take the bow");
	}

	private static void ClearHeldOffhand(Agent agent, string why)
	{
		try
		{
			EquipmentIndex offhandWieldedItemIndex = agent.GetOffhandWieldedItemIndex();
			// Claws (GovernedBy=Berserk) are part of the body and must stay in
			// hand for the agent's lifetime. Covers both single-hand-claw and
			// dual-wielded-claw agents; the engine must not sheath the claw
			// mid-combat.
			if ((int)offhandWieldedItemIndex != -1 && agent.Equipment[offhandWieldedItemIndex].Item?.IsDualWieldItem() == true)
			{
				return;
			}
			if ((int)offhandWieldedItemIndex != -1 && IsOneHandedMeleeWeapon(agent.Equipment[offhandWieldedItemIndex]))
			{
				_isAutoEquipping = true;
				try
				{
					agent.SetWieldedItemIndexAsClient((Agent.HandIndex)1, (EquipmentIndex)(-1), true, false, 0);
				}
				finally
				{
					_isAutoEquipping = false;
				}
				DualWieldLog.Log("[Wield] " + agent.Name + ": left hand emptied (" + why + ")");
			}
		}
		catch (Exception ex)
		{
			DualWieldLog.Log("[Wield] clear left hand failed: " + ex.Message);
		}
	}

	private static bool CarriesOffhandFlag(MissionWeapon weapon)
	{
		if (!weapon.IsEmpty && weapon.Item != null && (weapon.Item.ItemFlags & (ItemFlags)0x80000) != 0)
		{
			return IsOneHandedMeleeWeapon(weapon);
		}
		return false;
	}

	internal static bool IsOneHandedMeleeWeapon(MissionWeapon weapon)
	{
		if (weapon.IsEmpty || weapon.CurrentUsageItem == null)
		{
			return false;
		}
		if (weapon.CurrentUsageItem.IsShield)
		{
			return false;
		}
		WeaponClass weaponClass = weapon.CurrentUsageItem.WeaponClass;
		if ((int)weaponClass != 2 && (int)weaponClass != 1 && (int)weaponClass != 4)
		{
			return (int)weaponClass == 6;
		}
		return true;
	}
}
