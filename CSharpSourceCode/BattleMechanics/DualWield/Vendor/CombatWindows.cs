// Vendored from Yotthani's DualWield-v0.10.9 mod (permission granted
// 2026-11-24). Class name prefixed DualWield for TOR namespacing;
// internal logic preserved as-is. See TORTasks/feral-combat/architecture-
// dualwield-vendored.md for the big picture.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Xml;
using TaleWorlds.Library;

namespace TOR_Core.BattleMechanics.DualWield.Vendor;

public static class CombatWindows
{
	public struct Window
	{
		public float Start;

		public float End;

		public bool Contains(float progress)
		{
			if (progress >= Start)
			{
				return progress <= End;
			}
			return false;
		}
	}

	private static readonly Window Fallback = new Window
	{
		Start = 0.3f,
		End = 0.55f
	};

	private static Dictionary<string, Window> _byId;

	public static string ParameterIdFor(string actionName)
	{
		return DwRules.CombatParameterFor(actionName);
	}

	public static Window For(string actionName)
	{
		Load();
		string text = ParameterIdFor(actionName);
		if (text != null && _byId.TryGetValue(text, out var value))
		{
			DwRules.AdjustWindow(actionName, ref value.Start, ref value.End);
			return value;
		}
		return Fallback;
	}

	private static void Load()
	{
		if (_byId != null)
		{
			return;
		}
		_byId = new Dictionary<string, Window>();
		try
		{
			string text = Path.Combine(BasePath.Name, "Modules/Native/ModuleData/combat_parameters.xml");
			if (!File.Exists(text))
			{
				DualWieldLog.Log("[CombatWindows] not found: " + text);
				return;
			}
			XmlDocument xmlDocument = new XmlDocument();
			xmlDocument.Load(text);
			foreach (XmlNode item in xmlDocument.GetElementsByTagName("combat_parameter"))
			{
				XmlAttributeCollection attributes = item.Attributes;
				if (attributes != null)
				{
					XmlAttribute xmlAttribute = attributes["id"];
					XmlAttribute xmlAttribute2 = attributes["collision_check_starting_percent"];
					XmlAttribute xmlAttribute3 = attributes["collision_check_ending_percent"];
					if (xmlAttribute != null && xmlAttribute2 != null && xmlAttribute3 != null && float.TryParse(xmlAttribute2.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var result) && float.TryParse(xmlAttribute3.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var result2))
					{
						_byId[xmlAttribute.Value] = new Window
						{
							Start = result,
							End = result2
						};
					}
				}
			}
			DualWieldLog.Log($"[CombatWindows] {_byId.Count} collision windows from combat_parameters.xml");
		}
		catch (Exception ex)
		{
			DualWieldLog.Log("[CombatWindows] failed: " + ex.Message);
		}
	}
}
