namespace TOR_Core.CharacterDevelopment
{
    /// <summary>
    /// Effect categories a TOR skill-tree perk can register against.
    /// Model code calls <see cref="TORSkillPerkHelper.ApplyPerkPassives"/>
    /// with one of these and receives all registered perks folded in.
    /// </summary>
    public enum SkillEffectType
    {
        Special,                      // bespoke, not routed through the generic pipeline
        Damage,                       // damage the perk holder deals
        TroopDamage,                  // damage troops in the holder's formation deal
        TroopSpeed,                   // movement or melee speed for troops in formation
        TroopSkill,                   // flat skill bonus on formation troops
        GovernorTariff,               // flat tariff revenue for a governed settlement
        GovernorBesiegedMilitia,      // flat militia per day while besieged
        GovernorBesiegedFood,         // flat food per day while besieged
        GovernorTournamentIncome,     // flat denars per day when a tournament is active
        KickStunDuration,             // multiplier on AgentDrivenProperties.KickStunDurationMultiplier
    }
}
