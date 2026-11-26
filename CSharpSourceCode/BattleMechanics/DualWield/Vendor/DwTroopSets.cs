// Vendored from Yotthani's DualWield-v0.10.9 mod (permission granted
// 2026-11-24). Class name prefixed DualWield for TOR namespacing;
// internal logic preserved as-is. See TORTasks/feral-combat/architecture-
// dualwield-vendored.md for the big picture.

using System;

namespace TOR_Core.BattleMechanics.DualWield.Vendor;

public static class DwTroopSets
{
	public const string Taom = "taom";

	public const string Calradia = "calradia";

	public static readonly string[] OldAddOns = new string[2] { "DualWield_TAOM", "DualWield_Calradia" };

	public static string[][] Files(string set)
	{
		string text = "troops/" + set + "/";
		if (set == "taom")
		{
			return new string[3][]
			{
				new string[2]
				{
					"NPCCharacters",
					text + "troops_dualwield"
				},
				new string[2]
				{
					"NPCCharacters",
					text + "heroes_dualwield"
				},
				new string[2]
				{
					"EquipmentRosters",
					text + "equipment_dualwield"
				}
			};
		}
		return new string[1][] { new string[2]
		{
			"NPCCharacters",
			text + "troops_dualwield"
		} };
	}

	public static string OldAddOnActive(Func<string, bool> active)
	{
		string[] oldAddOns = OldAddOns;
		foreach (string text in oldAddOns)
		{
			if (active(text))
			{
				return text;
			}
		}
		return null;
	}

	public static string Choose(string offWhy, Func<string, bool> active, out string why)
	{
		if (offWhy != null)
		{
			why = offWhy;
			return null;
		}
		string text = OldAddOnActive(active);
		if (text != null)
		{
			why = "the add-on module " + text + " is still enabled and brings the troops itself";
			return null;
		}
		if (active("TAOM"))
		{
			if (active("LOTRLOME_Armory"))
			{
				why = "TAOM is loaded";
				return "taom";
			}
			why = "TAOM is loaded without LOTRLOME_Armory, whose weapons its troops carry";
			return null;
		}
		why = "TAOM is not loaded";
		return "calradia";
	}
}
