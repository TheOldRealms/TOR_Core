using System.Collections.Generic;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ViewModelCollection.GameMenu.Overlay;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Information;

namespace TOR_Core.HarmonyPatches
{
    /// <summary>
    /// Problem: the encounter-screen party-count tooltip still returns a full formation-class breakdown for parties whose number label is hidden.
    /// Fix: postfix the tooltip builder. If any party on the queried side has IsInfoHidden true, replace the result with a single "???" entry.
    /// </summary>
    [HarmonyPatch(typeof(EncounterMenuOverlayVM), "GetEncounterSideTroopsTooltip")]
    public static class EncounterSideTroopsBluffPatch
    {
        /// <summary>Masks the real roster breakdown when any party on the queried side is marked info-hidden.</summary>
        public static void Postfix(EncounterMenuOverlayVM __instance,
            BattleSideEnum side, ref List<TooltipProperty> __result)
        {
            var list = side == BattleSideEnum.Attacker
                ? __instance.AttackerPartyList
                : __instance.DefenderPartyList;
            if (list == null) return;

            foreach (var item in list)
            {
                if (item?.Party?.MobileParty?.IsInfoHidden == true)
                {
                    __result = new List<TooltipProperty>
                    {
                        new TooltipProperty(string.Empty, "???", 0)
                    };
                    return;
                }
            }
        }
    }
}
