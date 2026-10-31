using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;

namespace TOR_Core.CampaignMechanics.Ambush
{
    /// <summary>
    /// Static lookup for Phase F AI ambush propensity. Values are in [0, 1]
    /// and represent the chance that an eligible lord/bandit party rolls to
    /// try ambushing when the decision tick fires.
    ///
    /// Design values come from TORTasks/ambush-feature/ai-ambushes-player-design.md § 7:
    /// goblins / beastmen / wood elves ambush aggressively; Empire moderate;
    /// Bretonnia low (chivalry). Hero overrides let specific heroes break
    /// their culture's default — e.g. Bertram of Beregac.
    ///
    /// Slice 2a ships the mechanism with a conservative starting table. The
    /// Slice 2c tuning pass fills in the rest and dials values against
    /// playtest telemetry.
    /// </summary>
    public static class AmbushPropensityRegistry
    {
        // Culture StringId → propensity.
        //
        // Note on greenskin: the entry here is the "goblin-led" value. Orcs
        // are too thick to ambush, so the model gates greenskin propensity
        // on the leader hero actually being a goblin (see TORAmbushModel.
        // GetAmbushPropensity). Non-goblin-led greenskin parties fall back
        // to the default (0).
        private static readonly Dictionary<string, float> _culturePropensity
            = new Dictionary<string, float>
        {
            // Aggressive ambushers per § 7.
            { "greenskin", 0.6f },      // only applied when leader IsGoblin
            { "beastmen", 0.6f },
            { "asrai", 0.7f },          // wood elves — ambush is their defining tactic
            { "eonir", 0.6f },          // forest-dwelling elves, similar flavour
            // Moderate.
            { "empire", 0.15f },
            { "dwarf", 0.1f },
            // Chivalric — low per § 7.
            { "bretonnia", 0.05f },
            // Others left to default until the Slice 2c tuning pass.
        };

        // Hero StringId → propensity. Overrides culture when present.
        // Negative value = "no override" sentinel.
        private static readonly Dictionary<string, float> _heroPropensity
            = new Dictionary<string, float>
        {
            // Design doc § 7 called-out example: ambushes despite Bretonnian baseline.
            { "hero_bertram_of_beregac", 0.5f },
        };

        public static float GetCulturePropensity(CultureObject culture, float defaultValue)
        {
            if (culture == null || string.IsNullOrEmpty(culture.StringId)) return defaultValue;
            return _culturePropensity.TryGetValue(culture.StringId, out var v) ? v : defaultValue;
        }

        /// <summary>Returns the hero's propensity override, or -1 if none is
        /// registered (caller falls back to culture).</summary>
        public static float GetHeroPropensity(Hero hero)
        {
            if (hero == null || string.IsNullOrEmpty(hero.StringId)) return -1f;
            return _heroPropensity.TryGetValue(hero.StringId, out var v) ? v : -1f;
        }
    }
}
