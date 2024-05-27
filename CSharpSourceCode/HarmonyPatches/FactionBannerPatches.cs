using System;
using System.Collections.Generic;
using System.Xml;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.ObjectSystem;
using TOR_Core.BattleMechanics.Banners;
using TOR_Core.Extensions;
using TOR_Core.Extensions.ExtendedInfoSystem;

namespace TOR_Core.HarmonyPatches
{
    [HarmonyPatch]
    public static class FactionBannerPatches
    {
        private static readonly Dictionary<string, Banner> _cache = [];

		[HarmonyPostfix]
		[HarmonyPatch(typeof(Clan),"Deserialize")]
        public static void Postfix(XmlNode node, Clan __instance)
        {
			string code = node?.Attributes?.GetNamedItem("banner_key")?.Value;
			if (code != null)
			{
				_cache[__instance.StringId] = new Banner(code);
			}
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(Kingdom), "Deserialize")]
		public static void Postfix2(XmlNode node, Kingdom __instance)
		{
			string code = node?.Attributes?.GetNamedItem("banner_key")?.Value;
			if (code != null)
			{
				_cache[__instance.StringId] = new Banner(code);
			}
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(Clan),"Banner", MethodType.Getter)]
		public static void Postfix3(ref Banner __result, Clan __instance, ref Banner ____banner)
        {
            _cache.TryGetValue(__instance.StringId, out var banner);
			if (banner != null)
			{
				__result = banner;
				____banner = banner;
			}
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(Kingdom), "Banner", MethodType.Getter)]
		public static void Postfix4(ref Banner __result, Kingdom __instance)
		{
            if (_cache.TryGetValue(__instance.StringId, out var banner)) __result = banner;
		}

        [HarmonyPrefix]
        [HarmonyPatch(typeof(Clan), "UpdateBannerColorsAccordingToKingdom")]
        public static bool Prefix()
        {
            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(Banner), "GetBannerDataFromBannerCode")]
        public static void StripImageResource(ref string bannerCode)
        {
			var array = bannerCode.Split([':']);
            if (array.Length > 1)
			{
				bannerCode = array[0];
				ExtendedInfoManager.AddBannerImageResource(array[0], array[1]);
			}
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(BannerVisual), "ConvertToMultiMesh")]
        public static void AddImageResourceToMetaMesh(BannerVisual __instance, ref MetaMesh __result)
        {
			if(__instance.Banner.GetBannerImageResource() is string resource)
			{
				__instance.AddBannerImageResource(ref __result, resource);
			}
        }
    }
}
