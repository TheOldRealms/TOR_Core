using TaleWorlds.CampaignSystem;

namespace TOR_Core.Utilities
{
    /// <summary>
    /// Helper class for culture and race-related functionality.
    /// </summary>
    public static class TORHireHelper
    {
        /// <summary>
        /// Determines if a player can hire a wanderer based on their racial/cultural compatibility.
        /// Rules:
        /// - Greenskins can only hire other greenskins
        /// - Dwarfs can hire dwarfs and humans
        /// - Humans can hire humans, dwarfs and eonir
        /// - Vampires (Mousillon and Sylvania) can only hire Mousillon and Sylvania
        /// - Asrai can hire Eonir and Asrai
        /// - Eonir can hire humans, Eonir, and Asrai
        /// - Chaos can hire vampires and humans
        /// </summary>
        /// <param name="player">The player hero attempting to hire.</param>
        /// <param name="wanderer">The wanderer hero being hired.</param>
        /// <returns>True if the player can hire the wanderer based on race/culture compatibility.</returns>
        public static bool CanPlayerHireBasedOnRace(Hero player, Hero wanderer)
        {
            string playerCulture = player.Culture?.StringId;
            string wandererCulture = wanderer.Culture?.StringId;

            if (string.IsNullOrEmpty(playerCulture) || string.IsNullOrEmpty(wandererCulture))
                return false; // Can't determine, block hiring

            // Greenskins can only hire other greenskins
            if (playerCulture == TORConstants.Cultures.GREENSKIN)
            {
                return wandererCulture == TORConstants.Cultures.GREENSKIN;
            }

            // Dwarfs can hire dwarfs and humans
            if (playerCulture == TORConstants.Cultures.DAWI)
            {
                return wandererCulture == TORConstants.Cultures.DAWI ||
                       IsHumanCulture(wandererCulture);
            }

            // Humans can hire dwarfs and eonir
            if (IsHumanCulture(playerCulture))
            {
                return wandererCulture == TORConstants.Cultures.DAWI ||
                       wandererCulture == TORConstants.Cultures.EONIR||
                       IsHumanCulture(wandererCulture);
            }

            // Vampires (Mousillon and Sylvania) can only hire Mousillon and Sylvania
            if (IsVampireCulture(playerCulture))
            {
                return IsVampireCulture(wandererCulture);
            }

            // Asrai can hire Eonir and Asrai
            if (playerCulture == TORConstants.Cultures.ASRAI)
            {
                return wandererCulture == TORConstants.Cultures.EONIR ||
                       wandererCulture == TORConstants.Cultures.ASRAI;
            }

            // Eonir can hire humans, Eonir, and Asrai
            if (playerCulture == TORConstants.Cultures.EONIR)
            {
                return IsHumanCulture(wandererCulture) ||
                       wandererCulture == TORConstants.Cultures.EONIR ||
                       wandererCulture == TORConstants.Cultures.ASRAI;
            }

            // Chaos can hire vampires and humans - this is just fan service, nothing is here set in stone.
            if (playerCulture == TORConstants.Cultures.CHAOS)
            {
                return IsVampireCulture(wandererCulture) ||
                       IsHumanCulture(wandererCulture);
            }

            // Default: block hiring for unknown cultures
            return false;
        }

        /// <summary>
        /// Checks if the given culture is a human culture (Empire or Bretonnia).
        /// </summary>
        /// <param name="cultureId">The culture StringId to check.</param>
        /// <returns>True if the culture is human.</returns>
        public static bool IsHumanCulture(string cultureId)
        {
            return cultureId == TORConstants.Cultures.EMPIRE ||
                   cultureId == TORConstants.Cultures.BRETONNIA;
        }

        /// <summary>
        /// Checks if the given culture is a vampire culture (Mousillon or Sylvania).
        /// </summary>
        /// <param name="cultureId">The culture StringId to check.</param>
        /// <returns>True if the culture is vampire.</returns>
        public static bool IsVampireCulture(string cultureId)
        {
            return cultureId == TORConstants.Cultures.MOUSILLON ||
                   cultureId == TORConstants.Cultures.SYLVANIA;
        }
    }
}
