// Vendored from Yotthani's DualWield-v0.10.9 mod (permission granted
// 2026-11-24). Class name prefixed DualWield for TOR namespacing;
// internal logic preserved as-is. See TORTasks/feral-combat/architecture-
// dualwield-vendored.md for the big picture.

using System;
using System.Collections.Generic;
using System.IO;
using TaleWorlds.Library;
using TaleWorlds.ModuleManager;
using TaleWorlds.ObjectSystem;

namespace TOR_Core.BattleMechanics.DualWield.Vendor;

internal static class DwTroopSetLoader
{
	private const string Module = "DualWield";

	private static readonly string[] GameTypes = new string[4] { "Campaign", "CampaignStoryMode", "CustomGame", "EditorGame" };

	private static bool _oldAddOnTold;

	internal static void Sync(string offWhy)
	{
		try
		{
			List<MbObjectXmlInformation> xmlInformationList = XmlResource.XmlInformationList;
			xmlInformationList.RemoveAll((MbObjectXmlInformation x) =>
			{
				return x.ModuleName == "DualWield" && x.Name != null && x.Name.StartsWith("troops/");
			});
			string text = DwTroopSets.Choose(offWhy, (Func<string, bool>)ModuleHelper.IsModuleActive, out string why);
			if (text == null)
			{
				DualWieldLog.Log("[Troops] no NPC dual-wield troops: " + why);
				TellOldAddOn();
				return;
			}
			int num = 0;
			string[][] array = DwTroopSets.Files(text);
			foreach (string[] array2 in array)
			{
				if (!File.Exists(ModuleHelper.GetXmlPath("DualWield", array2[1])))
				{
					DualWieldLog.Log("[Troops] ModuleData/" + array2[1] + ".xml is missing - the module is not complete");
					continue;
				}
				xmlInformationList.Add(new MbObjectXmlInformation
				{
					Id = array2[0],
					Name = array2[1],
					ModuleName = "DualWield",
					GameTypesIncluded = new List<string>(GameTypes)
				});
				num++;
			}
			DualWieldLog.Log("[Troops] NPC dual-wield troops: set '" + text + "' (" + why + "), " + num + " file(s) added to the game's XML list");
		}
		catch (Exception ex)
		{
			DualWieldLog.Log("[Troops] failed: " + ex.GetType().Name + ": " + ex.Message);
		}
	}

	private static void TellOldAddOn()
	{
		if (!_oldAddOnTold)
		{
			string text = DwTroopSets.OldAddOnActive((Func<string, bool>)ModuleHelper.IsModuleActive);
			if (text != null)
			{
				_oldAddOnTold = true;
				InformationManager.DisplayMessage(new InformationMessage("Dual Wield: the add-on module " + text + " is no longer needed - Dual Wield brings its troops itself now. Disable it in the launcher and delete it.", Colors.Yellow));
			}
		}
	}
}
