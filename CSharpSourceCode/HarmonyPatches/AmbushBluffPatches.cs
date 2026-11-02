using System.Collections.Generic;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ViewModelCollection.GameMenu.Overlay;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Information;

namespace TOR_Core.HarmonyPatches
{
    /// <summary>
    /// Problem: when a stealthed ambusher is standing in a MapEvent next to
    /// the player, the vanilla encounter screen hides the party-count label
    /// (renders "?" because MobileParty.IsInfoHidden is true), but hovering
    /// the center "?" still fires GetEncounterSideTroopsTooltip. That
    /// builder walks every party's member roster unconditionally and
    /// returns a full "Infantry 29, Ranged 13" formation-class breakdown.
    /// The number hides, the composition tooltip leaks. That defeats the
    /// bluff: a player who hovers the icon learns the ambusher's real
    /// strength from the tooltip even though the label was a question
    /// mark.
    ///
    /// Fix: postfix the builder. If any party on the queried side has
    /// IsInfoHidden true, replace the returned tooltip property list with
    /// a single "???" entry. Scoped to the encounter screen tooltip
    /// specifically so other tooltip pipelines are untouched.
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

    // Formation marker count hide: handled elsewhere via the TOR VM
    // extension pattern (see AmbushBluffFormationMarkerExtension).
    //
    // In-mission scoreboard suppression: attempted via a Harmony prefix on
    // ScoreboardBaseVM.ShowScoreboard setter that skipped the setter body.
    // Removed: something (SPScoreboardVM.OnMissionResultReady is the current
    // suspect) sets ShowScoreboard = true every frame. When the prefix
    // blocked the setter body, the backing field never flipped, so the
    // caller kept retrying (~200 setter calls per second). Side effect
    // was visible input freeze: TOR mission behaviors still ticked, vanilla
    // input stopped responding. Captured as a general lesson in the
    // feedback_harmony_setter_block_trap memory.
    //
    // Correct approach for scoreboard hide: VM extension on ScoreboardBaseVM
    // with an IsHidden-driving property, plus an SPScoreboardOverlay.xml
    // prefab override binding IsHidden to that property. Lets vanilla's
    // internal state stay as it wants; we just hide the rendered widget.
    // Same pattern used by AmbushBluffFormationMarkerExtension.
}
