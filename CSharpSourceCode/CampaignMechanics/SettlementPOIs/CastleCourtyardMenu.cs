using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TOR_Core.Extensions;
using TOR_Core.Framework;

namespace TOR_Core.CampaignMechanics.SettlementPOIs
{
    /// <summary>
    /// Castle equivalent of the <c>town_artisan</c> submenu — a sub-menu that castle POIs
    /// hang their "Visit the X" entries off. Castle POIs opt in via <c>ParentMenuId</c>
    /// or (for multi-attach POIs) <c>MenuAttachments</c>.
    ///
    /// Entry label and intro text route through <see cref="TORTextHelper.GetSettlementComplexText"/>,
    /// which resolves per-settlement overrides first, then per-culture, then falls back to the
    /// English default below. See the helper for the lookup order.
    /// </summary>
    public static class CastleCourtyardMenu
    {
        public const string MenuId = "castle_courtyard";
        public const string EntryOptionId = "castle_courtyard_enter";

        // Anchor the entry option above the vanilla "Take a walk around the castle"
        // row — the courtyard is a routine castle activity, so it belongs next to
        // the roam/stroll options rather than the siege/besiege/leave block.
        private const string CastleWalkOptionId = "castle_take_a_walk";

        private const string DefaultLabel = "Visit the Quarters";
        private const string DefaultIntro = "You walk the quarters, trading nods with the hangers-on.";

        /// <summary>
        /// Register the submenu and the castle-menu entry that opens it. Called once from
        /// <see cref="SettlementPOIManagerCampaignBehavior"/> at session launch, before POIs
        /// register themselves.
        /// </summary>
        public static void Register(CampaignGameStarter starter)
        {
            starter.AddGameMenu(MenuId, "{CASTLE_QUARTERS_INTRO}", OnMenuInit);

            // Final position comes from Rearrange() in AfterSessionLaunched; the index
            // here is just the initial drop-in slot before vanilla's full option set
            // is assembled.
            starter.AddGameMenuOption("castle", EntryOptionId,
                "{CASTLE_QUARTERS_LABEL}",
                OnEnterCondition,
                args => GameMenu.SwitchToMenu(MenuId),
                false, 4);

            starter.AddGameMenuOption(MenuId, "castle_courtyard_leave",
                "{=tor_castle_courtyard.leave}Back to the keep...",
                args => { args.optionLeaveType = GameMenuOption.LeaveType.Leave; return true; },
                args => GameMenu.SwitchToMenu("castle"),
                isLeave: true);
        }

        private static bool OnEnterCondition(MenuCallbackArgs args)
        {
            args.optionLeaveType = GameMenuOption.LeaveType.Submenu;
            var s = Settlement.CurrentSettlement;
            if (s == null || s.IsUnderSiege) return false;

            MBTextManager.SetTextVariable("CASTLE_QUARTERS_LABEL",
                TORTextHelper.GetSettlementComplexText("tor_castlecomplex", s, DefaultLabel));
            return true;
        }

        private static void OnMenuInit(MenuCallbackArgs args)
        {
            var s = Settlement.CurrentSettlement;
            MBTextManager.SetTextVariable("SETTLEMENT_NAME", s?.Name ?? TextObject.GetEmpty());
            MBTextManager.SetTextVariable("CASTLE_QUARTERS_INTRO",
                TORTextHelper.GetSettlementComplexTextObject("tor_castlecomplex_intro", s, DefaultIntro));
        }

        /// <summary>
        /// Move the courtyard entry above the vanilla "Take a walk around the castle"
        /// row. Called from <see cref="SettlementPOIManagerCampaignBehavior"/>'s
        /// OnAfterSessionLaunched, i.e. after the vanilla castle menu is fully assembled
        /// and its options have their final indices.
        /// </summary>
        public static void Rearrange()
        {
            var menu = Campaign.Current?.GameMenuManager?.GetGameMenu("castle");
            if (menu == null) return;
            TORSettlementMenuHelpers.RearrangeTownMenus(menu, EntryOptionId, CastleWalkOptionId, above: true);
        }
    }
}
