namespace TOR_Core.CampaignMechanics.Ambush
{
    // Unified resolution stamped on every AmbushRecord. Per-hero fate rides on the leader-down flags.
    public enum AmbushResolution
    {
        // No record / unset. Used for solo stealth-exit cooldown stamps.
        None,

        // Ambusher cleared the field.
        AmbusherTotalVictory,

        // Ambusher won; victim broke off with survivors.
        AmbusherPartialVictory,

        // Victim held the field; ambusher wiped or routed.
        AmbusherDefeated,

        // Ambusher held the field; victim lost. Use VictimLeaderDown to distinguish hero-down sub-case.
        VictimDefeated,

        // Victim escaped across the soft mission border. Treated as repelled for cooldown purposes.
        VictimFled,

        // Soft stalemate timer elapsed; no clear winner.
        Stalemate,
    }
}
