namespace TOR_Core.CampaignMechanics.Ambush
{
    /// <summary>
    /// Final mission result the campaign side reads to pick the follow-up path.
    /// Set once by <c>AmbushEndLogic</c> immediately before the mission closes;
    /// consumed and reset to <see cref="None"/> by
    /// <c>AmbushCampaignBehavior.OnPlayerBattleEnd</c>.
    ///
    /// Priority / semantics are defined in TORTasks/ambush-feature/mission-design.md.
    /// </summary>
    public enum AmbushOutcome
    {
        /// <summary>No ambush ran, or the previous outcome has already been
        /// consumed by the post-mission flow.</summary>
        None,

        /// <summary>Entire enemy party destroyed — initial patrol plus every
        /// reinforcement wave. "No witnesses": stealth cooldown resets to zero
        /// so the player can immediately re-enter stealth. Loot / prisoner
        /// flow fires via vanilla "destroyed party" bookkeeping.</summary>
        TotalVictory,

        /// <summary>Initial patrol dead and the extraction bar filled with the
        /// hero in the retreat zone. Post-mission "Ambush Escape" menu
        /// (engage-now vs teleport-away). Troops left outside the zone when
        /// the bar completed are wounded post-battle.</summary>
        Extracted,

        /// <summary>Hero (main agent) went down during the mission. Post-mission
        /// "Ambush Failed" menu offers run-with-losses vs auto-battle fight.
        /// The surviving party is not automatically wiped — the player picks.
        /// </summary>
        HeroWounded,

        /// <summary>Attacker team has no active human agents remaining (routed
        /// / wiped) but the hero is still up. Normal vanilla defeat flow —
        /// casualties, prisoners.</summary>
        Defeated,

        /// <summary>Soft stalemate timer elapsed in Phase 1 (initial patrol
        /// still alive). Forced defeat so the mission can't run forever if a
        /// navmesh / geometry bug makes the enemy unable to reach the player.
        /// </summary>
        Stalemate,
    }
}
