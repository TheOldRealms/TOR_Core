using TaleWorlds.CampaignSystem;
using TaleWorlds.TwoDimension;
using TOR_Core.Extensions;
using TOR_Core.Utilities;

namespace TOR_Core.CampaignMechanics.Religion
{
    /// <summary>
    /// Helper class for religion and pantheon-based compatibility calculations.
    /// Returns scores from -1.0 (bitter enemies) to +1.0 (same religion/strong allies).
    /// </summary>
    public static class ReligionObjectHelper
    {
        /// <summary>
        /// Calculates the overall compatibility score between two religions.
        /// Combines: same religion check, Pantheon compatibility, and specific hostility factors.
        /// </summary>
        /// <returns>Score from -1.0 (enemies) to +1.0 (same religion)</returns>
        public static float CalculateReligionCompatibility(ReligionObject x, ReligionObject y)
        {
            if (x == null || y == null) return 0f;

            // Same religion = perfect compatibility
            if (x == y) return 1.0f;

            // Base compatibility from Pantheon
            float compatibility = GetPantheonCompatibility(x.Pantheon, y.Pantheon);

            // Add hostility factor if religions are specifically hostile to each other
            compatibility += x.GetHostilityFactor(y);

            // Clamp to valid range
            return Mathf.Clamp(compatibility, -1.0f, 1.0f);
        }

        /// <summary>
        /// Gets the Pantheon for a culture string ID.
        /// </summary>
        public static Pantheon GetPantheon(string cultureId)
        {
            if (string.IsNullOrEmpty(cultureId))
                return Pantheon.Human;

            return cultureId switch
            {
                TORConstants.Cultures.EMPIRE => Pantheon.Human,
                TORConstants.Cultures.BRETONNIA => Pantheon.Human,
                TORConstants.Cultures.ASRAI => Pantheon.Elven,
                TORConstants.Cultures.EONIR => Pantheon.Elven,
                TORConstants.Cultures.DRUCHII => Pantheon.Elven,
                TORConstants.Cultures.DAWI => Pantheon.Dwarven,
                TORConstants.Cultures.SYLVANIA => Pantheon.Undead,
                TORConstants.Cultures.MOUSILLON => Pantheon.Undead,
                TORConstants.Cultures.GREENSKIN => Pantheon.Greenskin,
                TORConstants.Cultures.CHAOS => Pantheon.Chaos,
                TORConstants.Cultures.BEASTMEN => Pantheon.Chaos,
                _ => Pantheon.Human
            };
        }

        /// <summary>
        /// Gets the Pantheon for a hero, falling back to their culture's default pantheon
        /// if they don't have a religion assigned.
        /// </summary>
        public static Pantheon GetPantheonForHero(Hero hero)
        {
            if (hero == null)
                return Pantheon.Human;

            var religion = hero.GetDominantReligion();
            if (religion != null)
                return religion.Pantheon;

            return GetPantheon(hero.Culture?.StringId);
        }

        /// <summary>
        /// Gets the compatibility score between two Pantheons.
        /// </summary>
        public static float GetPantheonCompatibility(Pantheon p1, Pantheon p2)
        {
            if (p1 == p2)
            {
                return p1 switch
                {
                    Pantheon.Human => 0.5f,
                    Pantheon.Elven => 0.5f,
                    Pantheon.Dwarven => 1f,
                    Pantheon.Undead => 0.3f,
                    Pantheon.Greenskin => 0.1f,
                    Pantheon.Chaos => 0.1f,
                    _ => 0.5f
                };
            }

            // Normalize ordering for consistent lookups
            var (first, second) = p1 < p2 ? (p1, p2) : (p2, p1);

            return (first, second) switch
            {
                // Order vs Chaos - eternal enemies
                (Pantheon.Human, Pantheon.Chaos) => -1.0f,
                (Pantheon.Elven, Pantheon.Chaos) => -1.0f,
                (Pantheon.Dwarven, Pantheon.Chaos) => -1.0f,

                // Order vs Greenskin
                (Pantheon.Human, Pantheon.Greenskin) => -0.8f,
                (Pantheon.Elven, Pantheon.Greenskin) => -0.8f,
                (Pantheon.Dwarven, Pantheon.Greenskin) => -1.0f,

                // Order vs Undead
                (Pantheon.Human, Pantheon.Undead) => -0.8f,
                (Pantheon.Elven, Pantheon.Undead) => -0.8f,
                (Pantheon.Dwarven, Pantheon.Undead) => -0.8f,

                // Destruction forces
                (Pantheon.Chaos, Pantheon.Greenskin) => -0.4f,
                (Pantheon.Chaos, Pantheon.Undead) => -0.8f,
                (Pantheon.Greenskin, Pantheon.Undead) => -0.4f,

                // Order factions
                (Pantheon.Human, Pantheon.Elven) => 0.1f,
                (Pantheon.Human, Pantheon.Dwarven) => 0.4f,
                (Pantheon.Dwarven, Pantheon.Elven) => -0.1f,

                _ => 0f
            };
        }

        /// <summary>
        /// Calculates the cultural compatibility score between two cultures.
        /// Returns a value from -1.0 (bitter enemies) to +1.0 (same culture/strong allies).
        /// </summary>
        public static float CalculateCultureCompatibility(string culture1Id, string culture2Id)
        {
            if (string.IsNullOrEmpty(culture1Id) || string.IsNullOrEmpty(culture2Id))
                return 0f;

            if (culture1Id == culture2Id)
                return 1.0f;

            return GetPantheonCompatibility(GetPantheon(culture1Id), GetPantheon(culture2Id));
        }
    }
}
