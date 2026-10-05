using TaleWorlds.Localization;
using TOR_Core.Extensions;

namespace TOR_Core.CampaignMechanics.TownPOIs
{
    /// <summary>
    /// Localized denial reasons for the Witch Hunter Lodge POI. Follows the same per-POI
    /// block pattern as <see cref="WizardHallReasons"/>, dropping <c>MustHaveCareer</c>
    /// because the lodge is race-only (no career gate).
    ///
    /// <para>Strings grouped under <c>category="POIs" subcategory="Witch Hunter Lodge"</c>
    /// in <c>tor_strings.xml</c>.</para>
    /// </summary>
    internal static class WitchHunterLodgeReasons
    {
        // ------- Hide-eligible (reference-comparable sentinels)

        public static readonly TextObject Undiscovered =
            TORTextHelper.GetTextObject(
                "tor_poi_witchhunter_lodge.Undiscovered",
                "You have not yet found this place.");

        /// <summary>
        /// Defensive fallback — the current settlement is not in the Witch Hunter Lodge
        /// whitelist. Hidden by the menu-condition translator; player should never see it.
        /// </summary>
        public static readonly TextObject Sealed =
            TORTextHelper.GetTextObject(
                "tor_poi_witchhunter_lodge.Sealed",
                "This place is sealed.");

        // ------- Gray-with-tooltip

        public static readonly TextObject SiegeSealed =
            TORTextHelper.GetTextObject(
                "tor_poi_witchhunter_lodge.SiegeSealed",
                "The lodge is closed during the siege.");

        /// <summary>
        /// Race rejection — the lodge admits humans only. Deliberately neutral wording;
        /// the Order of Sigmar's doorkeepers simply refuse non-humans entry.
        /// </summary>
        public static readonly TextObject Rejected =
            TORTextHelper.GetTextObject(
                "tor_poi_witchhunter_lodge.Rejected",
                "The templars turn you away.");
    }
}
