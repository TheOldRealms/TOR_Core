using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TOR_Core.Extensions;

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

            starter.AddGameMenuOption("castle", "castle_courtyard_enter",
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
    }
}
