// Vendored from Yotthani's DualWield-v0.10.9 mod (permission granted
// 2026-11-24). Class name prefixed DualWield for TOR namespacing;
// internal logic preserved as-is. See TORTasks/feral-combat/architecture-
// dualwield-vendored.md for the big picture.

using System.Collections.Generic;
using TaleWorlds.MountAndBlade;

namespace TOR_Core.BattleMechanics.DualWield;

internal static class ActionCache
{
	private static readonly Dictionary<string, ActionIndexCache> _byName = new Dictionary<string, ActionIndexCache>();

	private static readonly Dictionary<int, string> _names = new Dictionary<int, string>();

	private static readonly Dictionary<string, string> _blockedOf = new Dictionary<string, string>();

	public static ActionIndexCache Get(string name)
	{
		if (string.IsNullOrEmpty(name))
		{
			return ActionIndexCache.act_none;
		}
		if (_byName.TryGetValue(name, out var value))
		{
			return value;
		}
		value = ActionIndexCache.Create(name);
		if (value.Index >= 0)
		{
			_byName[name] = value;
			_names[value.Index] = name;
		}
		return value;
	}

	public static string NameOf(ActionIndexCache act)
	{
		if (act.Index < 0)
		{
			return "";
		}
		if (_names.TryGetValue(act.Index, out var value))
		{
			return value;
		}
		value = act.GetName() ?? "";
		_names[act.Index] = value;
		return value;
	}

	public static string BlockedOf(string releaseName)
	{
		if (string.IsNullOrEmpty(releaseName))
		{
			return releaseName;
		}
		if (_blockedOf.TryGetValue(releaseName, out var value))
		{
			return value;
		}
		value = DwRules.BlockedOf(releaseName);
		_blockedOf[releaseName] = value;
		return value;
	}
}
