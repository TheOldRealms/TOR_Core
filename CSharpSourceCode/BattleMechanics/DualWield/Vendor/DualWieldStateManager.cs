// Vendored from Yotthani's DualWield-v0.10.9 mod (permission granted
// 2026-11-24). Class name prefixed DualWield for TOR namespacing;
// internal logic preserved as-is. See TORTasks/feral-combat/architecture-
// dualwield-vendored.md for the big picture.

using System.Collections.Generic;
using TaleWorlds.MountAndBlade;

namespace TOR_Core.BattleMechanics.DualWield.Vendor;

public static class DualWieldStateManager
{
	private static readonly HashSet<int> _registered = new HashSet<int>();

	private static readonly HashSet<int> _paired = new HashSet<int>();

	public static void Register(Agent agent)
	{
		if (agent != null)
		{
			_registered.Add(agent.Index);
		}
	}

	public static void Unregister(Agent agent)
	{
		if (agent != null)
		{
			_registered.Remove(agent.Index);
			_paired.Remove(agent.Index);
		}
	}

	public static bool IsDualWielding(Agent agent)
	{
		if (agent != null)
		{
			return _registered.Contains(agent.Index);
		}
		return false;
	}

	public static bool IsPaired(Agent agent)
	{
		if (agent != null)
		{
			return _paired.Contains(agent.Index);
		}
		return false;
	}

	public static void SetPaired(Agent agent, bool paired)
	{
		if (agent != null)
		{
			if (paired)
			{
				_paired.Add(agent.Index);
			}
			else
			{
				_paired.Remove(agent.Index);
			}
		}
	}

	public static void Clear()
	{
		_registered.Clear();
		_paired.Clear();
	}
}
