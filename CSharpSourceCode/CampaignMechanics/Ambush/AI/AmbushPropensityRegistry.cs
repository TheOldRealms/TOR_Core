using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using static TOR_Core.Utilities.TORConstants;

namespace TOR_Core.CampaignMechanics.Ambush
{
    /// <summary>
    /// Static per-culture and per-hero ambush propensity in [0, 1]. Culture value is overridden by hero value when present.
    /// </summary>
    public static class AmbushPropensityRegistry
    {
        // Culture StringId to propensity. Unregistered cultures fall through to the model default.
        private static readonly Dictionary<string, float> _culturePropensity
            = new Dictionary<string, float>
        {
            // Aggressive
            { Cultures.ASRAI, 0.7f },            // wood elves
            { Cultures.EONIR, 0.7f },            // northern forest elves
            { Cultures.BEASTMEN, 0.6f },
            { Cultures.DRUCHII, 0.6f },          // dark elves
            { Cultures.MOUSILLON, 0.5f },        // Vampire Counts
            { Cultures.SYLVANIA, 0.5f },         // Vampire Counts
            { Cultures.GREENSKIN, 0.6f },

            // Bandit sub-cultures: hiders, not strikers.
            { Cultures.GOBLIN_BANDIT, 0.4f },
            { Cultures.GREENSKIN_BANDIT, 0.35f },
            { Cultures.BEASTMEN_BANDIT, 0.4f },
            { Cultures.CHAOS_CULTIST, 0.35f },
            { Cultures.EMPIRE_DESERTERS, 0.3f },
            { Cultures.HERRIMAULT, 0.3f },

            // Moderate
            { Cultures.EMPIRE, 0.15f },

            // Low (chivalric, formal, glory-focused).
            { Cultures.BRETONNIA, 0.05f },
            { Cultures.DAWI, 0.05f },
            { Cultures.CHAOS, 0.05f },
        };

        // Hero StringId overrides, negative value means no override.
        private static readonly Dictionary<string, float> _heroPropensity
            = new Dictionary<string, float>
        {
            { "hero_bertram_of_beregac", 0.5f },
        };

        /// <summary>
        /// Culture propensity, or the provided default if unregistered.
        /// </summary>
        public static float GetCulturePropensity(CultureObject culture, float defaultValue)
        {
            if (culture == null || string.IsNullOrEmpty(culture.StringId)) return defaultValue;
            return _culturePropensity.TryGetValue(culture.StringId, out var v) ? v : defaultValue;
        }

        /// <summary>
        /// Hero propensity override, or -1 if none registered.
        /// </summary>
        public static float GetHeroPropensity(Hero hero)
        {
            if (hero == null || string.IsNullOrEmpty(hero.StringId)) return -1f;
            return _heroPropensity.TryGetValue(hero.StringId, out var v) ? v : -1f;
        }
    }
}
