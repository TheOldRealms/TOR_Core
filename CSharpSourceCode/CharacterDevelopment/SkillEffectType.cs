namespace TOR_Core.CharacterDevelopment
{
    /// <summary>
    /// Generic effect categories for TOR skill-tree perks. Parallel to
    /// <see cref="CareerSystem.PassiveEffectType"/> but scoped to the
    /// perk side: lets a TOR Roguery / future-skill perk declare what
    /// stat it modifies without each perk requiring its own vanilla
    /// hook site. Pick an entry from here when registering a perk via
    /// <see cref="TORSkillPerkHelper.Register"/>; model code calls
    /// <see cref="TORSkillPerkHelper.ApplyPerkPassives"/> with the same
    /// type and gets all applicable perks folded in.
    ///
    /// Semantically mirrors the PassiveEffectType set — entries are
    /// added as needed, not exhaustively mirrored. Add a new entry
    /// only when there's at least one TOR perk that targets it; empty
    /// entries are dead code.
    /// </summary>
    public enum SkillEffectType
    {
        /// <summary>Sentinel — for perks whose effect is handled by
        /// bespoke code rather than this generic pipeline.</summary>
        Special,

        /// <summary>Damage dealt by the perk-holder's own attacks.
        /// Paired with an <see cref="TaleWorlds.Core.AttackTypeMask"/>
        /// so perks can target specific weapon classes (thrust, melee,
        /// ranged, etc.) matching vanilla's own filter semantics.</summary>
        Damage,

        /// <summary>Damage dealt by troops in the perk-holder's
        /// formation (Captain-role perks).</summary>
        TroopDamage,

        /// <summary>Melee / movement speed for troops in the perk-holder's
        /// formation. Vanilla's own captain-perk pipeline already applies
        /// this via <c>GetCaptainPerksForTroopUsages</c>; the TOR helper
        /// adds TOR perks into the same flow.</summary>
        TroopSpeed,

        /// <summary>Numeric skill bump applied to formation troops
        /// (e.g. vigor, control, athletics). Mirrors vanilla's
        /// <c>TroopSkill</c> pattern.</summary>
        TroopSkill,
    }
}
