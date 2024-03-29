﻿using System.Collections.Generic;
using System.Xml;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.ObjectSystem;

namespace TOR_Core.HarmonyPatches
{
    [HarmonyPatch]
    public static class FactionBannerPatches
    {
		private static Dictionary<string, Banner> _cache = new Dictionary<string, Banner>();

		[HarmonyPostfix]
		[HarmonyPatch(typeof(Clan),"Deserialize")]
		public static void Postfix(MBObjectManager objectManager, XmlNode node, Clan __instance)
        {
			string code = node?.Attributes?.GetNamedItem("banner_key").Value;
			if (code != null)
			{
				_cache[__instance.StringId] = new Banner(code);
			}
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(Kingdom), "Deserialize")]
		public static void Postfix2(MBObjectManager objectManager, XmlNode node, Kingdom __instance)
		{
			string code = node?.Attributes?.GetNamedItem("banner_key").Value;
			if (code != null)
			{
				_cache[__instance.StringId] = new Banner(code);
			}
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(Clan),"Banner", MethodType.Getter)]
		public static void Postfix3(ref Banner __result, Clan __instance, ref Banner ____banner)
        {
			Banner banner = null;
			_cache.TryGetValue(__instance.StringId, out banner);
			if (banner != null)
			{
				__result = banner;
				____banner = banner;
			}
		}

		[HarmonyPrefix]
		[HarmonyPatch(typeof(Clan), "UpdateBannerColorsAccordingToKingdom")]
		public static bool Prefix(Clan __instance)
        {
			return false;
        }

		[HarmonyPostfix]
		[HarmonyPatch(typeof(Kingdom), "Banner", MethodType.Getter)]
		public static void Postfix4(ref Banner __result, Kingdom __instance)
		{
			Banner banner = null;
			_cache.TryGetValue(__instance.StringId, out banner);
			if (banner != null)
			{
				__result = banner;
			}
		}
	}
}
