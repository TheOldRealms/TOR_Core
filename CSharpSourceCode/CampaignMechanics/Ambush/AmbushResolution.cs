namespace TOR_Core.CampaignMechanics.Ambush
{
    /// <summary>Unified resolution stamped on every AmbushRecord. Per-hero fate rides on the leader-down flags.</summary>
    public enum AmbushResolution
    {
        /// <summary>No record / unset. Used for solo stealth-exit cooldown stamps.</summary>
        None,

        /// <summary>Ambusher cleared the field.</summary>
        AmbusherTotalVictory,

        /// <summary>Ambusher won; victim broke off with survivors.</summary>
        AmbusherPartialVictory,

        /// <summary>Victim held the field; ambusher wiped or routed.</summary>
        AmbusherDefeated,

        /// <summary>Ambusher held the field; victim lost. Use VictimLeaderDown to distinguish hero-down sub-case.</summary>
        VictimDefeated,

        /// <summary>Victim escaped across the soft mission border. Treated as repelled for cooldown purposes.</summary>
        VictimFled,

        /// <summary>Soft stalemate timer elapsed; no clear winner.</summary>
        Stalemate,
    }
}
