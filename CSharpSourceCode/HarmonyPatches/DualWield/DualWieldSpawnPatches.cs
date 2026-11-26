// Vendored from Yotthani's DualWield-v0.10.9 mod (permission granted
// 2026-11-24). Full port, ilspycmd decompile artifacts hand-fixed.
//
// Harmony patch rationale: see the class-level <summary> below for
// Problem / Why not non-patching / Scope (per feedback_harmony_patch_rationale).

using System;
using System.Collections.Generic;
using System.Text;
using TOR_Core.BattleMechanics.DualWield.Vendor;
using HarmonyLib;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;

namespace TOR_Core.HarmonyPatches.DualWield;

[HarmonyPatch(typeof(Mission), "SpawnAgent")]
public static class DualWieldSpawnPatches
{
	private static readonly Random _npcTestRng = new Random();

	private static int _npcTestLogs;

	private static readonly Dictionary<string, List<ItemObject>> _culturePools = new Dictionary<string, List<ItemObject>>();

	private static readonly Dictionary<string, MBActionSet> _dwSets = new Dictionary<string, MBActionSet>();

	private static int _actionSetLogs;

	private static readonly HashSet<string> _clipsLogged = new HashSet<string>();

	private static readonly string[] ProbeActions = new string[5] { "act_run_forward_1h", "act_run_forward_1h_left_stance", "act_walk_forward_1h", "act_guard_up_1h", "act_quick_release_slashright_1h_left_stance" };

	[HarmonyPrefix]
	public static void Prefix(AgentBuildData agentBuildData)
	{
		TestNpcDualWield(agentBuildData);
		try
		{
			string text = ((agentBuildData == null) ? null : DualWieldSettings.Get().TestOffhandItemId);
			if (text == null)
			{
				return;
			}
			BasicCharacterObject agentCharacter = agentBuildData.AgentCharacter;
			if (((int)agentBuildData.AgentController != 2 && (agentCharacter == null || Game.Current == null || agentCharacter != Game.Current.PlayerTroop)) || agentBuildData.AgentOverridenSpawnMissionEquipment != null)
			{
				return;
			}
			Equipment val = agentBuildData.AgentOverridenSpawnEquipment ?? ((agentCharacter != null) ? agentCharacter.FirstBattleEquipment : null);
			if (val == null)
			{
				return;
			}
			ItemObject val2 = MBObjectManager.Instance.GetObject<ItemObject>(text);
			if (val2 == null)
			{
				DualWieldLog.Log("[Spawn] TestOffhand: item " + text + " not loaded (restart the game?)");
				return;
			}
			Equipment val3 = val.Clone(false);
			EquipmentIndex val4 = (EquipmentIndex)(-1);
			EquipmentIndex val5 = (EquipmentIndex)(-1);
			EquipmentElement val7;
			for (EquipmentIndex val6 = (EquipmentIndex)0; (int)val6 < 5; val6 = (EquipmentIndex)(val6 + 1))
			{
				val7 = val3[val6];
				if (val7.Item == null)
				{
					continue;
				}
				val7 = val3[val6];
				if (((MBObjectBase)val7.Item).StringId == "dw_test_offhand")
				{
					val4 = val6;
					continue;
				}
				val7 = val3[val6];
				if (((MBObjectBase)val7.Item).StringId == "dw_test_mainhand")
				{
					val5 = val6;
				}
			}
			if ((int)val4 == -1)
			{
				return;
			}
			if (!((MBObjectBase)val2).StringId.StartsWith("dw_test_") && (int)val5 != -1 && val4 < val5)
			{
				val3[val4] = val3[val5];
				val3[val5] = new EquipmentElement(val2, (ItemModifier)null, (ItemObject)null, false);
			}
			else
			{
				val3[val4] = new EquipmentElement(val2, (ItemModifier)null, (ItemObject)null, false);
			}
			string testMainHandItemId = DualWieldSettings.Get().TestMainHandItemId;
			if (testMainHandItemId != null)
			{
				ItemObject val8 = MBObjectManager.Instance.GetObject<ItemObject>(testMainHandItemId);
				if (val8 == null)
				{
					DualWieldLog.Log("[Spawn] TestMainHand: item " + testMainHandItemId + " not loaded");
				}
				else
				{
					for (EquipmentIndex val9 = (EquipmentIndex)0; (int)val9 < 5; val9 = (EquipmentIndex)(val9 + 1))
					{
						val7 = val3[val9];
						if (val7.Item != null)
						{
							val7 = val3[val9];
							if (((MBObjectBase)val7.Item).StringId == "dw_test_mainhand")
							{
								val3[val9] = new EquipmentElement(val8, (ItemModifier)null, (ItemObject)null, false);
							}
						}
					}
				}
			}
			agentBuildData.Equipment(val3);
			DualWieldLog.Log("[Spawn] TestOffhand: dw_test_offhand -> " + text + ((testMainHandItemId != null) ? (" (and dw_test_mainhand -> " + testMainHandItemId + ")") : ""));
		}
		catch (Exception ex)
		{
			DualWieldLog.Log("[Spawn] TestOffhandDagger failed: " + ex.Message);
		}
	}

	private static bool IsOneHandedMelee(ItemObject item)
	{
		if (item == null || ((MBObjectBase)item).StringId.StartsWith("dw_test_"))
		{
			return false;
		}
		WeaponComponentData primaryWeapon = item.PrimaryWeapon;
		if (primaryWeapon == null || primaryWeapon.IsShield)
		{
			return false;
		}
		WeaponClass weaponClass = primaryWeapon.WeaponClass;
		if ((int)weaponClass != 2 && (int)weaponClass != 4 && (int)weaponClass != 6)
		{
			return (int)weaponClass == 1;
		}
		return true;
	}

	private static List<ItemObject> OwnOneHanded(BasicCharacterObject ch)
	{
		List<ItemObject> list = new List<ItemObject>();
		foreach (Equipment battleEquipment in ch.BattleEquipments)
		{
			for (EquipmentIndex val = (EquipmentIndex)0; (int)val < 5; val = (EquipmentIndex)(val + 1))
			{
				EquipmentElement val2 = battleEquipment[val];
				if (IsOneHandedMelee(val2.Item))
				{
					val2 = battleEquipment[val];
					if (!list.Contains(val2.Item))
					{
						val2 = battleEquipment[val];
						list.Add(val2.Item);
					}
				}
			}
		}
		return list;
	}

	private static List<ItemObject> CulturePool(BasicCultureObject culture, int race)
	{
		if (culture == null)
		{
			return new List<ItemObject>();
		}
		string key = ((MBObjectBase)culture).StringId + "#" + race;
		if (_culturePools.TryGetValue(key, out var value))
		{
			return value;
		}
		value = new List<ItemObject>();
		int num = 0;
		foreach (BasicCharacterObject item in (List<BasicCharacterObject>)(object)MBObjectManager.Instance.GetObjectTypeList<BasicCharacterObject>())
		{
			if (item == null || item.IsHero || item.Race != race || item.Culture == null || ((MBObjectBase)item.Culture).StringId != ((MBObjectBase)culture).StringId)
			{
				continue;
			}
			num++;
			foreach (ItemObject item2 in OwnOneHanded(item))
			{
				if (!value.Contains(item2))
				{
					value.Add(item2);
				}
			}
		}
		_culturePools[key] = value;
		DualWieldLog.Log(string.Format("[Spawn] TestNpc: culture '{0}' race {1}: {2} one-handed weapons from {3} troops", new object[4]
		{
			((MBObjectBase)culture).StringId,
			race,
			value.Count,
			num
		}));
		return value;
	}

	private static bool HasDualWieldSet(Monster monster, bool female)
	{
		MBActionSet val = DualWieldSet(monster, female, out var _);
		return val.IsValid;
	}

	private static MBActionSet DualWieldSet(Monster monster, bool female, out string code)
	{
		string text;
		if (monster == null)
		{
			text = null;
		}
		else
		{
			text = ((female && !string.IsNullOrEmpty(monster.FemaleActionSetCode)) ? monster.FemaleActionSetCode : monster.ActionSetCode);
		}
		string text2 = ActionSetCode.GenerateActionSetNameWithSuffix(monster, female, "_warrior_dw");
		string[] array = new string[2]
		{
			string.IsNullOrEmpty(text) ? null : (text + "_dw"),
			text2
		};
		foreach (string text3 in array)
		{
			if (text3 == null)
			{
				continue;
			}
			if (!_dwSets.TryGetValue(text3, out var value))
			{
				value = MBActionSet.GetActionSet(text3);
				string text4 = (value.IsValid ? value.GetName() : null);
				if (text4 != text3)
				{
					value = MBActionSet.InvalidActionSet;
				}
				_dwSets[text3] = value;
				DualWieldLog.Log(value.IsValid ? ("[ActionSet] '" + text3 + "' found") : ("[ActionSet] no '" + text3 + "'" + ((text4 != null) ? (" (engine answered '" + text4 + "')") : "")));
			}
			if (value.IsValid)
			{
				code = text3;
				return value;
			}
		}
		code = text2;
		return MBActionSet.InvalidActionSet;
	}

	private static void TestNpcDualWield(AgentBuildData data)
	{
		try
		{
			if (data == null || !DualWieldSettings.Get().TestNpcDualWield)
			{
				return;
			}
			BasicCharacterObject agentCharacter = data.AgentCharacter;
			if (agentCharacter == null || data.AgentMonster == null || ((MBObjectBase)data.AgentMonster).StringId == null || data.AgentOverridenSpawnMissionEquipment != null)
			{
				return;
			}
			Equipment val = data.AgentOverridenSpawnEquipment ?? agentCharacter.FirstBattleEquipment;
			if (val == null)
			{
				return;
			}
			EquipmentElement val2 = val[(EquipmentIndex)0];
			if (val2.IsEmpty)
			{
				return;
			}
			bool flag = (int)data.AgentController == 2 || (Game.Current != null && agentCharacter == Game.Current.PlayerTroop);
			if (flag)
			{
				for (EquipmentIndex val3 = (EquipmentIndex)0; (int)val3 < 5; val3 = (EquipmentIndex)(val3 + 1))
				{
					val2 = val[val3];
					if (val2.Item != null)
					{
						val2 = val[val3];
						if (((MBObjectBase)val2.Item).StringId.StartsWith("dw_test_"))
						{
							return;
						}
					}
				}
			}
			if (!HasDualWieldSet(data.AgentMonster, data.AgentIsFemale))
			{
				return;
			}
			List<ItemObject> list = OwnOneHanded(agentCharacter);
			List<ItemObject> list2 = new List<ItemObject>(CulturePool(agentCharacter.Culture, agentCharacter.Race));
			foreach (ItemObject item in list)
			{
				if (!list2.Contains(item))
				{
					list2.Add(item);
				}
			}
			if (list2.Count != 0)
			{
				ItemObject val4 = ((list.Count > 0) ? list[_npcTestRng.Next(list.Count)] : list2[_npcTestRng.Next(list2.Count)]);
				float rightLen = val4.PrimaryWeapon.WeaponLength;
				List<ItemObject> list3 = list2.FindAll((ItemObject i) => Math.Abs((float)i.PrimaryWeapon.WeaponLength - rightLen) <= 0.25f * rightLen);
				ItemObject val5 = ((list3.Count > 0) ? list3[_npcTestRng.Next(list3.Count)] : val4);
				Equipment val6 = val.Clone(false);
				val6[(EquipmentIndex)0] = new EquipmentElement(val4, (ItemModifier)null, (ItemObject)null, false);
				val6[(EquipmentIndex)1] = new EquipmentElement(val5, (ItemModifier)null, (ItemObject)null, false);
				val6[(EquipmentIndex)2] = EquipmentElement.Invalid;
				val6[(EquipmentIndex)3] = EquipmentElement.Invalid;
				data.Equipment(val6);
				if (flag || _npcTestLogs++ < 12)
				{
					object[] array = new object[7] { agentCharacter.Name, null, null, null, null, null, null };
					BasicCultureObject culture = agentCharacter.Culture;
					array[1] = ((culture != null) ? ((MBObjectBase)culture).StringId : null);
					array[2] = (flag ? " (player)" : "");
					array[3] = ((MBObjectBase)val4).StringId;
					array[4] = (list.Contains(val4) ? " (own)" : "");
					array[5] = ((MBObjectBase)val5).StringId;
					array[6] = list2.Count;
					DualWieldLog.Log(string.Format("[Spawn] TestNpc: {0} ({1}){2} gets '{3}' right{4}, '{5}' left - pool {6}", array));
				}
			}
		}
		catch (Exception ex)
		{
			DualWieldLog.Log("[Spawn] TestNpc failed: " + ex.Message);
		}
	}

	[HarmonyPostfix]
	public static void Postfix(Mission __instance, Agent __result, AgentBuildData agentBuildData)
	{
		try
		{
			if (__result == null || !__result.IsHuman || __instance == null || !DualWieldSettings.Get().EnableDualWield)
			{
				return;
			}
			if (!DualWieldMissionBehavior.IsActive)
			{
				if (__instance.GetMissionBehavior<DualWieldMissionBehavior>() == null)
				{
					__instance.AddMissionBehavior(new DualWieldMissionBehavior());
				}
				if (!DualWieldMissionBehavior.IsActive)
				{
					return;
				}
			}
			DualWieldMissionBehavior missionBehavior = __instance.GetMissionBehavior<DualWieldMissionBehavior>();
			if (missionBehavior == null)
			{
				return;
			}
			bool flag = DualWieldDetector.IsPlayer(__result);
			bool flag2 = DualWieldDetector.IsDualWieldEquipped(__result.Equipment, flag);
			bool flag3 = (!flag2 & flag) && DualWieldDetector.PlayerCanDualWield(__result.Equipment);
			MissionWeapon val;
			if (__result.IsPlayerControlled)
			{
				MissionEquipment equipment = __result.Equipment;
				val = equipment[(EquipmentIndex)0];
				object obj;
				if (!val.IsEmpty)
				{
					val = equipment[(EquipmentIndex)0];
					ItemObject item = val.Item;
					obj = ((item != null) ? ((MBObjectBase)item).StringId : null) ?? "?";
				}
				else
				{
					obj = "empty";
				}
				string text = (string)obj;
				val = equipment[(EquipmentIndex)1];
				object obj2;
				if (!val.IsEmpty)
				{
					val = equipment[(EquipmentIndex)1];
					ItemObject item2 = val.Item;
					obj2 = ((item2 != null) ? ((MBObjectBase)item2).StringId : null) ?? "?";
				}
				else
				{
					obj2 = "empty";
				}
				string text2 = (string)obj2;
				int itemUsageIndex = MBItem.GetItemUsageIndex("dw_offhand");
				int itemUsageIndex2 = MBItem.GetItemUsageIndex("dw_mainhand_swing_thrust");
				int itemUsageIndex3 = MBItem.GetItemUsageIndex("hand_shield");
				string arg = "none";
				int num = -1;
				for (EquipmentIndex val2 = (EquipmentIndex)0; (int)val2 <= 3; val2 = (EquipmentIndex)(val2 + 1))
				{
					if (val2 != __result.GetPrimaryWieldedItemIndex())
					{
						MissionWeapon val3 = equipment[val2];
						if (!val3.IsEmpty && val3.CurrentUsageItem != null)
						{
							arg = val3.CurrentUsageItem.ItemUsage ?? "null";
							num = (string.IsNullOrEmpty(val3.CurrentUsageItem.ItemUsage) ? (-1) : MBItem.GetItemUsageIndex(val3.CurrentUsageItem.ItemUsage));
							break;
						}
					}
				}
				DualWieldLog.Log(string.Format("[Spawn] PLAYER: slot0={0}, slot1={1}, isDW={2}{3}", new object[4]
				{
					text,
					text2,
					flag2,
					flag3 ? " - two one-handed weapons in other slots: pairs with the wheel" : ""
				}));
				DualWieldLog.Log($"[Spawn] UsageIdx: dw_offhand={itemUsageIndex}, dw_mainhand={itemUsageIndex2}, hand_shield={itemUsageIndex3}");
				DualWieldLog.Log($"[Spawn] Offhand weapon usage: '{arg}' idx={num}");
				DualWieldSettings.DebugMessage($"[DW] UsageIdx: offhand={itemUsageIndex} main={itemUsageIndex2} shield={itemUsageIndex3}", (itemUsageIndex >= 0) ? Colors.Green : Colors.Red);
				DualWieldSettings.DebugMessage($"[DW] OffWpn usage='{arg}' idx={num}", (num >= 0) ? Colors.Green : Colors.Red);
			}
			if (!(flag2 | flag3))
			{
				return;
			}
			DualWieldStateManager.Register(__result);
			if (!flag)
			{
				missionBehavior.RangedHybrids.Add(__result);
			}
			EquipmentIndex offhandWieldedItemIndex = __result.GetOffhandWieldedItemIndex();
			int num2;
			if ((int)offhandWieldedItemIndex != -1)
			{
				val = __result.Equipment[offhandWieldedItemIndex];
				if (!val.IsEmpty)
				{
					val = __result.Equipment[offhandWieldedItemIndex];
					if (val.CurrentUsageItem != null)
					{
						val = __result.Equipment[offhandWieldedItemIndex];
						num2 = (val.CurrentUsageItem.IsShield ? 1 : 0);
						goto IL_0306;
					}
				}
			}
			num2 = 0;
			goto IL_0306;
			IL_0306:
			bool flag4 = (byte)num2 != 0;
			if (flag3 || (flag & flag4))
			{
				DualWieldWieldingPatches.StartSingle(__result);
				DualWieldLog.Log("[Spawn] " + __result.Name + ": starts " + (flag4 ? "with the shield" : "with one weapon") + " - wheel down takes the left weapon");
			}
			else if ((int)offhandWieldedItemIndex != -1)
			{
				MissionWeapon val4 = __result.Equipment[offhandWieldedItemIndex];
				object obj3;
				if (!val4.IsEmpty)
				{
					ItemObject item3 = val4.Item;
					obj3 = ((item3 != null) ? ((MBObjectBase)item3).StringId : null) ?? "?";
				}
				else
				{
					obj3 = "empty";
				}
				string text3 = (string)obj3;
				if (DualWieldLog.Unlimited || __result.IsMainAgent)
				{
					DualWieldLog.Log($"[Spawn] {__result.Name}: Engine AUTO-WIELDED offhand slot {(int)offhandWieldedItemIndex} ({text3}) — skipping ForceOffHandPairing");
				}
				DualWieldStateManager.SetPaired(__result, paired: true);
				DualWieldSettings.DebugMessage($"[DW] Engine auto-wielded offhand: {text3} (slot {(int)offhandWieldedItemIndex})", Colors.Green);
			}
			else
			{
				if (DualWieldLog.Unlimited || __result.IsMainAgent)
				{
					DualWieldLog.Log("[Spawn] " + __result.Name + ": No auto-wield → ForceOffHandPairing");
				}
				DualWieldWieldingPatches.ForceOffHandPairing(__result, instant: true, onSpawn: true);
			}
			if (DualWieldLog.Unlimited || __result.IsMainAgent)
			{
				DualWieldLog.Log("[Spawn] " + __result.Name + " registered as dual wielder");
			}
			UseDualWieldActionSet(__result);
		}
		catch (Exception ex)
		{
			DualWieldSettings.DebugLog("SpawnAgent patch error: " + ex.Message);
		}
	}

	private static void LogClipsOnce(Agent agent, string code, MBActionSet dwSet)
	{
		if (!_clipsLogged.Add(code))
		{
			return;
		}
		try
		{
			MBActionSet actionSet = agent.ActionSet;
			MBActionSet[] array = new MBActionSet[2] { actionSet, dwSet };
			for (int i = 0; i < array.Length; i++)
			{
				MBActionSet val = array[i];
				StringBuilder stringBuilder = new StringBuilder();
				string[] probeActions = ProbeActions;
				foreach (string text in probeActions)
				{
					ActionIndexCache val2 = ActionIndexCache.Create(text);
					stringBuilder.Append(" | ").Append(text.Substring(4)).Append('=')
						.Append((val2.Index < 0) ? "?" : MBActionSet.GetActionAnimationName(val, ref val2));
				}
				DualWieldLog.Log(string.Format("[ActionSet] {0}{1}: '{2}' skeleton {3}{4}", new object[5]
				{
					((MBObjectBase)agent.Monster).StringId,
					agent.IsFemale ? " female" : "",
					val.GetName(),
					val.GetSkeletonName(),
					stringBuilder
				}));
			}
		}
		catch (Exception ex)
		{
			DualWieldLog.Log("[ActionSet] clip probe failed: " + ex.Message);
		}
	}

	internal static void RegisterPlayerLate(Agent agent)
	{
		if (agent != null && agent.IsActive())
		{
			DualWieldStateManager.Register(agent);
			EquipmentIndex offhandWieldedItemIndex = agent.GetOffhandWieldedItemIndex();
			bool flag = (int)offhandWieldedItemIndex != -1 && DualWieldWieldingPatches.IsOneHandedMeleeWeapon(agent.Equipment[offhandWieldedItemIndex]);
			if (!flag)
			{
				DualWieldWieldingPatches.StartSingle(agent);
			}
			DualWieldLog.Log("[Spawn] " + agent.Name + ": a second one-handed weapon in the battle - registered as dual wielder" + (flag ? ", the left one is in the hand" : ", wheel down takes it"));
			UseDualWieldActionSet(agent);
		}
	}

	private static void UseDualWieldActionSet(Agent agent)
	{
		try
		{
			MBActionSet val = DualWieldSet(agent.Monster, agent.IsFemale, out var code);
			if (val.IsValid)
			{
				LogClipsOnce(agent, code, val);
				AnimationSystemData val2 = MonsterExtensions.FillAnimationSystemData(agent.Monster, val, agent.Character.GetStepSize(), false);
				agent.SetActionSet(ref val2);
				if (_actionSetLogs < 20 || agent.IsPlayerControlled)
				{
					_actionSetLogs++;
					DualWieldLog.Log("[ActionSet] " + agent.Name + (agent.IsPlayerControlled ? " (player)" : "") + ": -> '" + code + "'");
				}
			}
		}
		catch (Exception ex)
		{
			DualWieldLog.Log("[ActionSet] failed: " + ex.Message);
		}
	}
}
