using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TOR_Core.Extensions;

namespace TOR_Core.CampaignMechanics.SettlementPOIs
{
    /// <summary>
    /// Parallel to the vanilla <c>town_artisan</c> submenu, but for castles.
    /// Castle POIs set <c>ParentMenuId = "castle_courtyard"</c> to appear here.
    ///
    /// The menu-option label on the main <c>castle</c> menu is culture-aware via the
    /// <c>str_tor_castlecomplex.&lt;culture&gt;</c> override pattern — an orc-held keep
    /// doesn't have a "courtyard" in the Imperial sense, so each culture can rename
    /// it via <c>tor_strings.xml</c>. Owner-clan culture is used (settlement culture
    /// is cosmetic / fixed; ownership transfers and should swing the vocabulary).
    ///
    /// Called once from <see cref="SettlementPOIManagerCampaignBehavior"/> at
    /// session launch, before POIs register themselves.
    /// </summary>
    public static class CastleCourtyardMenu
    {
        public const string MenuId = "castle_courtyard";

        /// <summary>
        /// Default fallback label when no culture override exists in tor_strings.
        /// Culture overrides live at <c>str_tor_castlecomplex.&lt;culture&gt;</c>.
        /// </summary>
        private const string DefaultLabel = "Visit the Courtyard";

        /// <summary>
        /// Register the submenu and the castle-menu entry that opens it. Safe to call
        /// multiple times — Bannerlord's GameMenuManager tolerates duplicate menu ids
        /// (not duplicate options on the same menu, so the entry must be unique).
        /// </summary>
        public static void Register(CampaignGameStarter starter)
        {
            // Submenu — stays empty here; castle POIs register their own options
            // against it via SettlementPOIManagerCampaignBehavior.
            starter.AddGameMenu(MenuId,
                new TextObject("{COURTYARD_INTRO}").ToString(),
                OnMenuInit);

            // Entry on the castle main menu that opens the courtyard submenu.
            starter.AddGameMenuOption("castle", "castle_courtyard_enter",
                "{CASTLE_COURTYARD_LABEL}",
                OnEnterCondition,
                args => GameMenu.SwitchToMenu(MenuId),
                false, 4);

            // Submenu's own Leave option, back to the castle menu.
            starter.AddGameMenuOption(MenuId, "castle_courtyard_leave",
                "{=tor_castle_courtyard.leave}Back to the keep...",
                args => { args.optionLeaveType = GameMenuOption.LeaveType.Leave; return true; },
                args => GameMenu.SwitchToMenu("castle"),
                isLeave: true);
        }

        /// <summary>
        /// Resolves the menu-option label from the current castle's owner culture.
        /// Example lookup: owner is a Dawi clan → tries <c>str_tor_castlecomplex.sturgia</c>,
        /// falls back to the English default if no override is registered.
        /// </summary>
        public static string GetCultureAwareLabel(Settlement s)
        {
            var cultureId = s?.OwnerClan?.Culture?.StringId;
            if (string.IsNullOrEmpty(cultureId)) return DefaultLabel;
            return TORTextHelper.GetText("tor_castlecomplex", cultureId, DefaultLabel, true);
        }

        private static bool OnEnterCondition(MenuCallbackArgs args)
        {
            args.optionLeaveType = GameMenuOption.LeaveType.Submenu;
            MBTextManager.SetTextVariable("CASTLE_COURTYARD_LABEL",
                GetCultureAwareLabel(Settlement.CurrentSettlement));
            return true;
        }

        private static void OnMenuInit(MenuCallbackArgs args)
        {
            // Minimal intro — the submenu primarily hosts POI options. Culture-specific
            // flavor text can be added later with a tor_castlecomplex_intro.<culture>
            // override pattern.
            var intro = TORTextHelper.GetTextObject("tor_castlecomplex_intro",
                "You walk the courtyard, trading nods with the hangers-on.");
            MBTextManager.SetTextVariable("COURTYARD_INTRO", intro);
        }
    }
}
