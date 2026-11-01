using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using static TOR_Core.Utilities.TORConstants;

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
        // Culture StringId → propensity. Keys come from TORConstants.Cultures;
        // mapping of in-engine ID to lore faction is NOT what the raw ID
        // suggests (e.g. ASRAI = "battania", GREENSKIN = "aserai").
        //
        // Any culture not registered falls through to the model's
        // DefaultCulturePropensity (0).
        //
        // Propensity is PURE inclination — "does this culture try?". The
        // CAPABILITY ("is this specific party any good at hiding?") is
        // delegated entirely to AmbushTroopStealthScore, which already
        // folds in race / mount / weight / knightly tier at precompute
        // time. An orc-led greenskin party has normal propensity but
        // its roster score is low → entry chance roll naturally fails.
        // No per-culture race gates here.
        private static readonly Dictionary<string, float> _culturePropensity
            = new Dictionary<string, float>
        {
            // ---- Aggressive (defining tactic / culture-coded for ambush) ----
            { Cultures.ASRAI, 0.7f },            // wood elves — defining tactic
            { Cultures.EONIR, 0.7f },            // northern forest elves — defining tactic
            { Cultures.BEASTMEN, 0.6f },         // beast-herds — iconic ambushers
            { Cultures.DRUCHII, 0.6f },          // dark elves — treacherous raiders
            { Cultures.MOUSILLON, 0.5f },        // Vampire Counts (Mousillon) — night ambushes
            { Cultures.SYLVANIA, 0.5f },         // Vampire Counts (Sylvania) — same flavour
            { Cultures.GREENSKIN, 0.6f },

            // ---- Bandit sub-cultures: hiders, not strikers ----
            // Bandit parties enter stealth to HIDE from hostile AI (which
            // in practice means lord patrols) — not to strike. They can
            // still ambush the player when the player walks past, so
            // propensity drives that. Values intentionally moderate:
            // bandits aren't aggressive about hiding either, just
            // instinctive. See AiAmbushBehavior.IsHiderOnly for the
            // archetype gate.
            { Cultures.GOBLIN_BANDIT, 0.4f },        // "looters"
            { Cultures.GREENSKIN_BANDIT, 0.35f },
            { Cultures.BEASTMEN_BANDIT, 0.4f },      // beastmen offshoot bandits
            { Cultures.CHAOS_CULTIST, 0.35f },       // "forest_bandits"
            { Cultures.EMPIRE_DESERTERS, 0.3f },     // "mountain_bandits"
            { Cultures.HERRIMAULT, 0.3f },           // "desert_bandits"

            // ---- Moderate ----
            { Cultures.EMPIRE, 0.15f },          // professional soldiery, formation warfare

            // ---- Low (chivalric / formal / glory-focused / disciplined) ----
            { Cultures.BRETONNIA, 0.05f },       // chivalry — § 7's canonical "low"
            { Cultures.DAWI, 0.05f },            // dwarfs — formal mountain warfare, grudge-driven
            { Cultures.CHAOS, 0.05f },           // Warriors of Chaos — glory-seeking charge
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
