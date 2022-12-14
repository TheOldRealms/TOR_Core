using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace TOR_Core.HarmonyPatches
{
    [HarmonyPatch]
    public static class GameTextPatches
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(GameText), "AddVariationWithId")]
        public static void LoadXmlWithOverride(List<GameText.GameTextVariation> ____variationList, string variationId)
        {
            if (variationId == "" || string.IsNullOrEmpty(variationId)) return;
            var list = ____variationList.Where(x => x.Id == variationId).ToList();
            if(list.Count() > 0)
            {
                foreach(var item in list)
                {
                    ____variationList.Remove(item);
                }
            }
		}
    }
}
