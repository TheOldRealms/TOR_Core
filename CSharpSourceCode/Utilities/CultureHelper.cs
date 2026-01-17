using System.Collections.Generic;
using TaleWorlds.CampaignSystem;

namespace TOR_Core.Utilities
{
    /// <summary>
    /// Helper class for calculating cultural compatibility between factions.
    /// Returns a score from -1.0 (bitter enemies) to +1.0 (same culture/strong allies).
    /// </summary>
    public static class CultureHelper
    {
        /// <summary>
        /// Culture group classifications for base compatibility.
        /// </summary>
        public enum CultureGroup
        {
            Human,
            Elven,
            Dwarf,
            Undead,
            Greenskin,
            Chaos
        }

        // Cache culture group mappings
        private static readonly Dictionary<string, CultureGroup> CultureGroups = new()
        {
            // Human cultures
            { TORConstants.Cultures.EMPIRE, CultureGroup.Human },
            { TORConstants.Cultures.BRETONNIA, CultureGroup.Human },

            // Elven cultures
            { TORConstants.Cultures.ASRAI, CultureGroup.Elven },      // Wood Elves
            { TORConstants.Cultures.EONIR, CultureGroup.Elven },      // High Elves
            { TORConstants.Cultures.DRUCHII, CultureGroup.Elven },    // Dark Elves

            // Dwarf
            { TORConstants.Cultures.DAWI, CultureGroup.Dwarf },

            // Undead
            { TORConstants.Cultures.SYLVANIA, CultureGroup.Undead },  // Vampire Counts
            { TORConstants.Cultures.MOUSILLON, CultureGroup.Undead }, // Mousillon

            // Greenskin
            { TORConstants.Cultures.GREENSKIN, CultureGroup.Greenskin },

            // Chaos
            { TORConstants.Cultures.CHAOS, CultureGroup.Chaos },
            { TORConstants.Cultures.BEASTMEN, CultureGroup.Chaos },
        };

        // Special relationship overrides (culture1, culture2) -> score
        // These override the default group-based calculations
        private static readonly Dictionary<(string, string), float> SpecialRelationships = new()
        {
            // Empire-Dwarf: Strong historical alliance (Sigmar's pact)
            { (TORConstants.Cultures.EMPIRE, TORConstants.Cultures.DAWI), 0.7f },

            // Bretonnia-Dwarf: Respectful but distant
            { (TORConstants.Cultures.BRETONNIA, TORConstants.Cultures.DAWI), 0.3f },

            // Dwarf-Eonir: Ancient grudges, distrust
            { (TORConstants.Cultures.DAWI, TORConstants.Cultures.EONIR), -0.3f },

            // Dwarf-Wood Elves: Old grievances but not as bad as High Elves
            { (TORConstants.Cultures.DAWI, TORConstants.Cultures.ASRAI), -0.2f },

            // Dwarf-Greenskin: Eternal hatred (The Great Grudge)
            { (TORConstants.Cultures.DAWI, TORConstants.Cultures.GREENSKIN), -1.0f },

            // Empire-Asrai: Distant but not hostile
            { (TORConstants.Cultures.EMPIRE, TORConstants.Cultures.ASRAI), 0.1f },

            // Empire-Eonir: Trading partners, generally positive
            { (TORConstants.Cultures.EMPIRE, TORConstants.Cultures.EONIR), 0.3f },

            // Elves-Greenskin: Natural enemies
            { (TORConstants.Cultures.ASRAI, TORConstants.Cultures.GREENSKIN), -0.8f },
            { (TORConstants.Cultures.EONIR, TORConstants.Cultures.GREENSKIN), -0.8f },

            // Dark Elves hate everyone, especially other elves
            { (TORConstants.Cultures.DRUCHII, TORConstants.Cultures.ASRAI), -0.9f },
            { (TORConstants.Cultures.DRUCHII, TORConstants.Cultures.EONIR), -0.9f },
            { (TORConstants.Cultures.DRUCHII, TORConstants.Cultures.EMPIRE), -0.7f },
            { (TORConstants.Cultures.DRUCHII, TORConstants.Cultures.BRETONNIA), -0.7f },

            // Mousillon is undead Bretonnia - hated by living Bretonnia
            { (TORConstants.Cultures.MOUSILLON, TORConstants.Cultures.BRETONNIA), -0.9f },

            // Sylvania vs Empire - bitter enemies
            { (TORConstants.Cultures.SYLVANIA, TORConstants.Cultures.EMPIRE), -0.8f },
        };

        // Group-based compatibility scores
        private static readonly Dictionary<(CultureGroup, CultureGroup), float> GroupCompatibility = new()
        {
            // Same group bonuses
            { (CultureGroup.Human, CultureGroup.Human), 0.5f },
            { (CultureGroup.Elven, CultureGroup.Elven), 0.3f },  // Elves are fractious
            { (CultureGroup.Dwarf, CultureGroup.Dwarf), 0.8f },  // Dwarfs stick together
            { (CultureGroup.Undead, CultureGroup.Undead), 0.4f },
            { (CultureGroup.Greenskin, CultureGroup.Greenskin), 0.2f }, // Orcs fight each other too
            { (CultureGroup.Chaos, CultureGroup.Chaos), 0.3f },  // Chaos is internally divided

            // Order vs Chaos/Destruction
            { (CultureGroup.Human, CultureGroup.Chaos), -1.0f },
            { (CultureGroup.Elven, CultureGroup.Chaos), -1.0f },
            { (CultureGroup.Dwarf, CultureGroup.Chaos), -1.0f },
            { (CultureGroup.Human, CultureGroup.Greenskin), -0.7f },
            { (CultureGroup.Elven, CultureGroup.Greenskin), -0.8f },
            { (CultureGroup.Dwarf, CultureGroup.Greenskin), -1.0f },

            // Undead vs Living
            { (CultureGroup.Human, CultureGroup.Undead), -0.6f },
            { (CultureGroup.Elven, CultureGroup.Undead), -0.6f },
            { (CultureGroup.Dwarf, CultureGroup.Undead), -0.5f },

            // Chaos and Greenskin - chaotic but not allies
            { (CultureGroup.Chaos, CultureGroup.Greenskin), -0.3f },
            { (CultureGroup.Chaos, CultureGroup.Undead), -0.2f },
            { (CultureGroup.Greenskin, CultureGroup.Undead), -0.4f },

            // Order factions - generally positive
            { (CultureGroup.Human, CultureGroup.Elven), 0.1f },
            { (CultureGroup.Human, CultureGroup.Dwarf), 0.4f },
            { (CultureGroup.Elven, CultureGroup.Dwarf), -0.1f }, // Historical tensions
        };

        /// <summary>
        /// Gets the culture group for a culture string ID.
        /// </summary>
        public static CultureGroup? GetCultureGroup(string cultureId)
        {
            if (string.IsNullOrEmpty(cultureId))
                return null;

            return CultureGroups.TryGetValue(cultureId, out var group) ? group : null;
        }

        /// <summary>
        /// Calculates the cultural compatibility score between two cultures.
        /// Returns a value from -1.0 (bitter enemies) to +1.0 (same culture/strong allies).
        /// </summary>
        public static float CalculateCultureCompatibility(string culture1Id, string culture2Id)
        {
            if (string.IsNullOrEmpty(culture1Id) || string.IsNullOrEmpty(culture2Id))
                return 0f;

            // Same culture = maximum compatibility
            if (culture1Id == culture2Id)
                return 1.0f;

            // Check for special relationship override (check both orderings)
            if (SpecialRelationships.TryGetValue((culture1Id, culture2Id), out float special1))
                return special1;
            if (SpecialRelationships.TryGetValue((culture2Id, culture1Id), out float special2))
                return special2;

            // Fall back to group-based compatibility
            var group1 = GetCultureGroup(culture1Id);
            var group2 = GetCultureGroup(culture2Id);

            if (group1 == null || group2 == null)
                return 0f;

            // Check group compatibility (check both orderings)
            if (GroupCompatibility.TryGetValue((group1.Value, group2.Value), out float groupScore1))
                return groupScore1;
            if (GroupCompatibility.TryGetValue((group2.Value, group1.Value), out float groupScore2))
                return groupScore2;

            // Default: neutral
            return 0f;
        }

        /// <summary>
        /// Calculates cultural compatibility between two kingdoms.
        /// </summary>
        public static float CalculateCultureCompatibility(Kingdom kingdom1, Kingdom kingdom2)
        {
            if (kingdom1?.Culture == null || kingdom2?.Culture == null)
                return 0f;

            return CalculateCultureCompatibility(kingdom1.Culture.StringId, kingdom2.Culture.StringId);
        }

        /// <summary>
        /// Calculates cultural compatibility between two factions.
        /// </summary>
        public static float CalculateCultureCompatibility(IFaction faction1, IFaction faction2)
        {
            if (faction1?.Culture == null || faction2?.Culture == null)
                return 0f;

            return CalculateCultureCompatibility(faction1.Culture.StringId, faction2.Culture.StringId);
        }

        /// <summary>
        /// Returns true if two cultures are considered hostile (compatibility below threshold).
        /// </summary>
        public static bool AreCulturesHostile(string culture1Id, string culture2Id, float threshold = -0.5f)
        {
            return CalculateCultureCompatibility(culture1Id, culture2Id) <= threshold;
        }

        /// <summary>
        /// Returns true if two cultures are considered friendly (compatibility above threshold).
        /// </summary>
        public static bool AreCulturesFriendly(string culture1Id, string culture2Id, float threshold = 0.3f)
        {
            return CalculateCultureCompatibility(culture1Id, culture2Id) >= threshold;
        }
    }
}
