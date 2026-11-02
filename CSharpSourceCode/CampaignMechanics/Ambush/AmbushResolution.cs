namespace TOR_Core.CampaignMechanics.Ambush
{
    /// <summary>
    /// Unified resolution of a single ambush exchange — Phase D mission,
    /// Phase F mission, and AI-on-AI campaign-map skirmish all stamp the
    /// same enum on their <see cref="AmbushRecord"/>. Replaces the older
    /// split <c>AmbushOutcome</c> / <c>AmbushedOutcome</c> enums.
    ///
    /// Describes "who held the field" only. Per-hero fate rides on the
    /// orthogonal <see cref="AmbushRecord.AmbusherLeaderDown"/> and
    /// <see cref="AmbushRecord.VictimLeaderDown"/> flags so a mutual-
    /// casualty engagement (currently reachable only on the AI-on-AI
    /// path, since both missions terminate on the first leader down) is
    /// representable without a combinatorial enum.
    ///
    /// <see cref="VictimFled"/> is currently only produced by
    /// <c>AmbushedEndLogic</c> (soft-border exit). AI-on-AI resolution
    /// could grow a parallel producer later; Phase D is locked by
    /// design (the ambusher cannot flee their own strike).
    /// </summary>
    public enum AmbushResolution
    {
        /// <summary>No record / unset. Used for solo stealth-exit
        /// cooldown stamps that aren't actually a resolved exchange.</summary>
        None,

        /// <summary>Ambusher cleared the field. Phase D TotalVictory,
        /// AI-on-AI ambusher won the strength roll.</summary>
        AmbusherTotalVictory,

        /// <summary>Ambusher won the exchange but the victim broke off
        /// with survivors. Phase D Extracted; no AI-on-AI analogue.</summary>
        AmbusherPartialVictory,

        /// <summary>Victim held the field; ambusher wiped or routed.
        /// Phase D Defeated, Phase F AmbushRepelled, AI-on-AI
        /// ambusher lost the strength roll.</summary>
        AmbusherDefeated,

        /// <summary>Ambusher held the field; victim lost. Phase F
        /// AmbushedWiped (victim troops all down) and AmbushedHeroWounded
        /// (victim's hero down, mission over even if troops remain).
        /// Use <see cref="AmbushRecord.VictimLeaderDown"/> to distinguish
        /// the two sub-cases when that matters for post-mission flow.</summary>
        VictimDefeated,

        /// <summary>Ambusher survives; victim escaped across the soft
        /// mission border. Phase F AmbushedFled. Treated as repelled
        /// for cooldown purposes (ambusher got no kills), but post-
        /// mission flow runs its own follow-up branch.</summary>
        VictimFled,

        /// <summary>Soft stalemate timer elapsed; no clear winner.
        /// Phase D Stalemate path (force-defeated so the mission can't
        /// run forever on a navmesh bug).</summary>
        Stalemate,
    }
}
