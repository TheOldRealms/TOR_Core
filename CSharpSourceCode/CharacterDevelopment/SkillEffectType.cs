namespace TOR_Core.CharacterDevelopment
{
    /// <summary>
    /// Effect categories a TOR skill-tree perk can register against.
    /// Model code calls <see cref="TORSkillPerkHelper.ApplyPerkPassives"/>
    /// with one of these and receives all registered perks folded in.
    /// Add a new entry only when a perk targets it.
    /// </summary>
    public enum SkillEffectType
    {
        /// <summary>Bespoke effects that do not use the generic pipeline.</summary>
        Special,

        /// <summary>Damage the perk holder deals in person.</summary>
        Damage,

        /// <summary>Damage dealt by troops in the holder's formation.</summary>
        TroopDamage,

        /// <summary>Movement or melee speed for troops in the holder's formation.</summary>
        TroopSpeed,

        /// <summary>Flat skill bonus on formation troops.</summary>
        TroopSkill,

        /// <summary>Flat tariff revenue for the settlement governor.</summary>
        GovernorTariff,

        /// <summary>Flat militia per day in a besieged governed settlement.</summary>
        GovernorBesiegedMilitia,

        /// <summary>Flat food per day in a besieged governed settlement.</summary>
        GovernorBesiegedFood,

        /// <summary>Flat denars per day in a governed city with an active tournament.</summary>
        GovernorTournamentIncome,

        /// <summary>Multiplier on <c>AgentDrivenProperties.KickStunDurationMultiplier</c>.</summary>
        KickStunDuration,
    }
}
