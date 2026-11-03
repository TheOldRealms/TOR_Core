using System.Collections.Generic;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ViewModelCollection.GameMenu.Overlay;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Information;

namespace TOR_Core.HarmonyPatches
{
    /// <summary>
    /// §6 bluff — vanilla's EncounterMenuOverlayVM builds a formation-
    /// class breakdown tooltip for each side (hovering the center "?" /
    /// troop count beside the crossed swords on the encounter screen).
    /// Vanilla's own party-count label respects MobileParty.IsInfoHidden
    /// to render "?" instead of the number, but
    /// <c>GetEncounterSideTroopsTooltip</c> walks every party's member
    /// roster unconditionally and shows "Infantry 29, Ranged 13" even
    /// when the label itself is "?". That leaks the ambusher's real
    /// strength breakdown and defeats the bluff.
    ///
    /// Postfix: if any party on the queried side has IsInfoHidden=true,
    /// replace the tooltip property list with a single "???" entry so
    /// the hover shows nothing useful.
    /// </summary>
    [HarmonyPatch(typeof(EncounterMenuOverlayVM), "GetEncounterSideTroopsTooltip")]
    public static class EncounterSideTroopsBluffPatch
    {
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

    // Formation marker count hide: see
    // AmbushBluffFormationMarkerExtension (TOR VM extension pattern).
    //
    // Scoreboard suppression — REMOVED. Blocking the ShowScoreboard
    // setter via Harmony prefix caused vanilla to spin: something
    // (SPScoreboardVM.OnMissionResultReady?) sets ShowScoreboard=true
    // every frame, and when our prefix skipped the setter the backing
    // field never flipped so the caller kept retrying. Observed ~200+
    // setter calls per second plus visible input freeze (TOR mission
    // behaviors ticked fine but vanilla input stopped responding).
    //
    // Future approach for scoreboard hide: VM extension on ScoreboardBaseVM
    // + override SPScoreboardOverlay.xml with IsHidden="@IsBluffSuppressed"
    // on the root widget. Vanilla's internal state stays as it wants;
    // we just hide the rendered UI. Same TOR extension pattern as the
    // formation marker.
}
