using TaleWorlds.Localization;

namespace TOR_Core.CampaignMechanics.TownPOIs
{
    /// <summary>
    /// Tiny helpers for returning from <c>SettlementAccessModel.CanMainHeroAccessLocation</c>-style
    /// methods. Keeps per-POI check code readable top-to-bottom.
    /// </summary>
    internal static class Access
    {
        public static bool Allow(out bool disableOption, out TextObject disabledText)
        {
            disableOption = false;
            disabledText = null;
            return true;
        }

        /// <summary>
        /// Deny with a reason. Always sets <paramref name="disableOption"/> to true so vanilla
        /// renders a grayed option with a hover tooltip; the menu-condition translator may still
        /// hide the option entirely for specific <see cref="Reasons"/> (e.g. <see cref="Reasons.Undiscovered"/>).
        /// </summary>
        public static bool Deny(out bool disableOption, out TextObject disabledText, TextObject reason)
        {
            disableOption = true;
            disabledText = reason;
            return false;
        }
    }
}
