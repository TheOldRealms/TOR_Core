// Vendored from Yotthani's DualWield-v0.10.9 mod (permission granted
// 2026-11-24). Full port, ilspycmd decompile artifacts hand-fixed.

using System;
using System.Collections.Generic;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;

namespace TOR_Core.BattleMechanics.DualWield.Vendor;

internal static class SeparateKeyOverride
{
	private const string CombatCategory = "CombatHotKeyCategory";

	private const int DefendKeyId = 10;

	private const int AttackKeyId = 9;

	private const InputKey BlockKey = (InputKey)226;

	private static readonly List<KeyValuePair<Key, InputKey>> _moved = new List<KeyValuePair<Key, InputKey>>();

	private static InputKey _appliedFor = (InputKey)(-1);

	private static bool _appliedLeftHanded;

	private static Key _defend;

	private static Key _attack;

	private static bool _toldPlayer;

	private static bool _toldDefendElsewhere;

	private static readonly List<KeyValuePair<Key, InputKey>> _suspended = new List<KeyValuePair<Key, InputKey>>();

	public static bool Active => _moved.Count > 0;

	public static void Apply(InputKey leftHandKey, bool leftHanded = false)
	{
		if (Active && _appliedFor == leftHandKey && _appliedLeftHanded == leftHanded)
		{
			bool num = _defend == (Key)null || (int)_defend.InputKey == 226;
			bool flag = _attack == (Key)null || _attack.InputKey == leftHandKey;
			if (num & flag)
			{
				return;
			}
			Key defend = _defend;
			object arg = ((defend != null) ? (object)defend.InputKey : (object)null);
			Key attack = _attack;
			DualWieldLog.Log($"[Keys] bindings were reloaded (Defend {arg}, Attack {((attack != null) ? (object)attack.InputKey : (object)null)}) - moving them again");
			_moved.Clear();
			_appliedFor = (InputKey)(-1);
			_defend = (_attack = null);
		}
		if (Active)
		{
			Restore();
		}
		try
		{
			GameKeyContext category = HotKeyManager.GetCategory("CombatHotKeyCategory");
			if (leftHanded && (int)leftHandKey != 224)
			{
				object obj;
				if (category == null)
				{
					obj = null;
				}
				else
				{
					GameKey gameKey = category.GetGameKey(9);
					obj = ((gameKey != null) ? gameKey.KeyboardKey : null);
				}
				Key val = (Key)obj;
				if (val != (Key)null && (int)val.InputKey == 224)
				{
					_moved.Add(new KeyValuePair<Key, InputKey>(val, val.InputKey));
					val.ChangeKey(leftHandKey);
					_attack = val;
					_appliedFor = leftHandKey;
					_appliedLeftHanded = true;
					DualWieldLog.Log($"[Keys] Left-handed: Attack {(object)(InputKey)224} -> {leftHandKey} (left strike on the left mouse button)");
				}
			}
			if ((int)leftHandKey == 226)
			{
				return;
			}
			object obj2;
			if (category == null)
			{
				obj2 = null;
			}
			else
			{
				GameKey gameKey2 = category.GetGameKey(10);
				obj2 = ((gameKey2 != null) ? gameKey2.KeyboardKey : null);
			}
			Key val2 = (Key)obj2;
			if (val2 == (Key)null || val2.InputKey != leftHandKey)
			{
				if (!_toldDefendElsewhere)
				{
					_toldDefendElsewhere = true;
					DualWieldLog.Log($"[Keys] Defend is on {((val2 != null) ? (object)val2.InputKey : (object)null)}, not on the left-hand key {leftHandKey} - left as it is");
				}
				return;
			}
			foreach (GameKey item in (List<GameKey>)(object)category.RegisteredGameKeys)
			{
				Key val3 = ((item != null) ? item.KeyboardKey : null);
				if (!(val3 == (Key)null) && val3 != val2 && (int)val3.InputKey == 226)
				{
					_moved.Add(new KeyValuePair<Key, InputKey>(val3, val3.InputKey));
					val3.ChangeKey((InputKey)(-1));
					DualWieldLog.Log($"[Keys] Separate: '{item.StringId}' off {(object)(InputKey)226} while the block is there");
				}
			}
			_moved.Add(new KeyValuePair<Key, InputKey>(val2, val2.InputKey));
			val2.ChangeKey((InputKey)226);
			_defend = val2;
			_appliedFor = leftHandKey;
			_appliedLeftHanded = leftHanded;
			DualWieldLog.Log($"[Keys] Separate: Defend {leftHandKey} -> {(object)(InputKey)226} (left-hand strike on {leftHandKey})");
			if (!_toldPlayer)
			{
				_toldPlayer = true;
				InformationManager.DisplayMessage(new InformationMessage("Separate mode: the right mouse button strikes with the left hand - block with the middle mouse button.", Colors.Yellow));
			}
		}
		catch (Exception ex)
		{
			DualWieldLog.Log("[Keys] Separate override failed: " + ex.Message);
			Restore();
		}
	}

	public static bool SuspendForSave()
	{
		if (!Active)
		{
			return false;
		}
		_suspended.Clear();
		foreach (KeyValuePair<Key, InputKey> item in _moved)
		{
			try
			{
				_suspended.Add(new KeyValuePair<Key, InputKey>(item.Key, item.Key.InputKey));
				item.Key.ChangeKey(item.Value);
			}
			catch (Exception ex)
			{
				DualWieldLog.Log("[Keys] suspend for save failed: " + ex.Message);
			}
		}
		DualWieldLog.Log("[Keys] bindings saved with the player's own keys (our moves kept out of the file)");
		return true;
	}

	public static void ResumeAfterSave()
	{
		foreach (KeyValuePair<Key, InputKey> item in _suspended)
		{
			try
			{
				item.Key.ChangeKey(item.Value);
			}
			catch (Exception ex)
			{
				DualWieldLog.Log("[Keys] resume after save failed: " + ex.Message);
			}
		}
		_suspended.Clear();
	}

	public static void Restore()
	{
		if (!Active)
		{
			return;
		}
		foreach (KeyValuePair<Key, InputKey> item in _moved)
		{
			try
			{
				item.Key.ChangeKey(item.Value);
			}
			catch (Exception ex)
			{
				DualWieldLog.Log("[Keys] restore failed: " + ex.Message);
			}
		}
		_moved.Clear();
		_appliedFor = (InputKey)(-1);
		_appliedLeftHanded = false;
		_defend = (_attack = null);
		DualWieldLog.Log("[Keys] keys restored");
	}

	public static void Reset()
	{
		Restore();
		_toldPlayer = false;
	}

	static SeparateKeyOverride()
	{
	}
}
