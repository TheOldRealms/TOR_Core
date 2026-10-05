using TaleWorlds.Localization;
using TOR_Core.Extensions;

namespace TOR_Core.CampaignMechanics.TownPOIs
{
    /// <summary>
    /// Catalog of localized denial reasons returned by <c>TORSettlementAccessModel</c> through
    /// <see cref="Access.Deny"/>. Reasons split into two kinds by signature:
    /// <list type="bullet">
    ///   <item>
    ///     <description>
    ///       <b>Static reasons</b> (<c>static readonly</c> fields): no variables, instance cached once
    ///       at type init. The menu-condition translator compares these by reference to decide
    ///       whether to hide the option (vs. show grayed with tooltip).
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <description>
    ///       <b>Parametric reasons</b> (methods): fill in variables (settlement name, required
    ///       career, etc.) and return a fresh <see cref="TextObject"/> each call. Never compared
    ///       by reference.
    ///     </description>
    ///   </item>
    /// </list>
    /// String ids resolve against <c>tor_strings.xml</c>; the default text here is the fallback
    /// used if the id is missing and also serves as the English wording.
    /// </summary>
    internal static class Reasons
    {
        // ------- Static reasons (hide-eligible — compared by reference in menu translator)

        /// <summary>
        /// Discovery feature not yet implemented; this never trips in v1 because
        /// <c>TORSettlementAccessModel.IsDiscovered</c> is stubbed to always return true.
        /// Reserved so the discovery-aware menu translator can already recognise it today.
        /// </summary>
        public static readonly TextObject Undiscovered =
            TORTextHelper.GetTextObject(
                "tor_poi.Undiscovered",
                "You have not yet found this place.");

        /// <summary>
        /// Defensive. Returned when a POI's per-settlement whitelist does not include the
        /// current settlement. Should not surface to the player if the menu option is only
        /// registered on eligible settlements, but we fail safely rather than silently.
        /// </summary>
        public static readonly TextObject NotAHallHere =
            TORTextHelper.GetTextObject(
                "tor_poi.NotAHallHere",
                "There is no hall of this kind here.");

        // ------- Static reasons (gray-with-tooltip)

        /// <summary>
        /// Siege gate applied at the top of <c>CanMainHeroAccessLocation</c> for every TOR POI.
        /// </summary>
        public static readonly TextObject SiegeSealed =
            TORTextHelper.GetTextObject(
                "tor_poi.SiegeSealed",
                "The gates are sealed during the siege.");

        /// <summary>
        /// Fallback for an unknown TOR-prefixed location id (fail-closed default).
        /// </summary>
        public static readonly TextObject Sealed =
            TORTextHelper.GetTextObject(
                "tor_poi.Sealed",
                "This place is sealed.");

        /// <summary>
        /// Race-based denial. Generic wording — the current hero race is implied by context
        /// (the player sees it when hovering over a hall they cannot enter).
        /// </summary>
        public static readonly TextObject WardsRejectRace =
            TORTextHelper.GetTextObject(
                "tor_poi.WardsRejectRace",
                "The wards of this place reject your kind.");

        // ------- Parametric reasons (gray-with-tooltip only — never hide-eligible)

        /// <summary>
        /// Career-based denial, with <c>{REQUIRED_CAREER}</c> substituted. Reusable across any
        /// career-locked POI (Wizard Hall → "Imperial Magister", Runehall → "Runelord", …).
        /// </summary>
        public static TextObject MustHaveCareer(TextObject requiredCareerName) =>
            TORTextHelper
                .GetTextObject(
                    "tor_poi.MustHaveCareer",
                    "Only {REQUIRED_CAREER} may enter.")
                .SetTextVariable("REQUIRED_CAREER", requiredCareerName);
    }
}
