namespace TOR_Core.CampaignMechanics.Ambush
{
    /// <summary>
    /// Qualitative tier of an ambush strike's outcome. One value per
    /// (chance, roll) pair, picked via <c>TORAmbushModel.RollOutcomeTier</c>.
    /// Scopes to AI-vs-AI skirmishes; player-involved ambushes stay
    /// binary (NormalSuccess on pass, MinorFail on fail) — see
    /// campaign-stealth-design.md § 14 for the "player stays deterministic"
    /// principle.
    /// </summary>
    public enum AmbushOutcomeTier
    {
        /// <summary>Decisive hit — victim takes 1.5× base wounds,
        /// ambusher takes 0.5×. Press-attack favoured.</summary>
        CriticalSuccess,

        /// <summary>Clean strike — both sides take base-rate wounds.
        /// Press / retreat decision based on the usual strength ratio.</summary>
        NormalSuccess,

        /// <summary>Spotted early — half wounds both sides. Ambusher
        /// typically retreats; no decisive outcome.</summary>
        MinorFail,

        /// <summary>Botched — ambusher takes 1.5× wounds, victim is
        /// largely untouched. AI-vs-AI only; player ambushers are
        /// capped at MinorFail per the design.</summary>
        TerribleFail,
    }
}
