using TaleWorlds.Localization;
using TOR_Core.Extensions;

namespace TOR_Core.CampaignMechanics.TownPOIs
{
    /// <summary>
    /// Localized denial reasons for the Wizard Hall POI. Each hall POI owns its own
    /// <c>XxxReasons</c> class so designers can edit flavor per hall (strings grouped
    /// under <c>category="POIs" subcategory="Wizard Hall"</c> in <c>tor_strings.xml</c>)
    /// without cross-POI coupling.
    ///
    /// <para>
    /// Field vs. method split:
    /// </para>
    /// <list type="bullet">
    ///   <item><b>static readonly</b> — cached once, reference-comparable. The menu-condition
    ///         translator in <see cref="TownPOIBehavior"/> uses <c>ReferenceEquals</c> against
    ///         these to decide hide-vs-gray.</item>
    ///   <item><b>methods</b> — fill in text variables, return a fresh <see cref="TextObject"/>
    ///         each call. Not reference-comparable; always gray-with-tooltip.</item>
    /// </list>
    /// </summary>
    internal static class WizardHallReasons
    {
        // ------- Hide-eligible (reference-comparable sentinels)

        public static readonly TextObject Undiscovered =
            TORTextHelper.GetTextObject(
                "tor_poi_wizardhall.Undiscovered",
                "You have not yet found this place.");

        /// <summary>
        /// Defensive fallback — the current settlement is not in the Wizard Hall whitelist.
        /// Hidden by the menu-condition translator; the player should never see this text.
        /// </summary>
        public static readonly TextObject Sealed =
            TORTextHelper.GetTextObject(
                "tor_poi_wizardhall.Sealed",
                "This place is sealed.");

        // ------- Gray-with-tooltip

        public static readonly TextObject SiegeSealed =
            TORTextHelper.GetTextObject(
                "tor_poi_wizardhall.SiegeSealed",
                "The gates of the College are sealed during the siege.");

        /// <summary>
        /// Race / culture / standing rejection. The Imperial College's wards reject
        /// non-humans; same message doubles for any future hard-no on identity.
        /// </summary>
        public static readonly TextObject Rejected =
            TORTextHelper.GetTextObject(
                "tor_poi_wizardhall.Rejected",
                "The wards of the College turn you away.");

        public static TextObject MustHaveCareer(TextObject requiredCareerName) =>
            TORTextHelper
                .GetTextObject(
                    "tor_poi_wizardhall.MustHaveCareer",
                    "Only {REQUIRED_CAREER} may enter the College.")
                .SetTextVariable("REQUIRED_CAREER", requiredCareerName);
    }
}
