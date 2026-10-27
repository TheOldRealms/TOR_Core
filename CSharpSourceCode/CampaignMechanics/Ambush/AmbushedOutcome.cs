namespace TOR_Core.CampaignMechanics.Ambush
{
    /// <summary>
    /// Final result for the Phase F "player is being ambushed" mission.
    /// Set once by <c>AmbushedEndLogic</c> immediately before the mission
    /// closes; consumed and reset to <see cref="None"/> by
    /// <c>AmbushCampaignBehavior.OnPlayerBattleEnd</c>.
    ///
    /// Priority / semantics defined in TORTasks/ambush-feature/ai-ambushes-player-design.md § 3.
    /// </summary>
    public enum AmbushedOutcome
    {
        /// <summary>No ambushed mission ran, or the previous outcome has
        /// already been consumed by the post-mission flow.</summary>
        None,

        /// <summary>Hero (main agent) went down during the mission. Standard
        /// vanilla wounded-hero flow; the surviving party must resolve the
        /// field situation on its own.</summary>
        AmbushedHeroWounded,

        /// <summary>Player's team (initial + any reinforcements) has no active
        /// human agents remaining. Full defeat — vanilla casualty / prisoner
        /// handling.</summary>
        AmbushedWiped,

        /// <summary>AI ambusher team wiped — initial attack force plus any
        /// reinforcements the design later lets them have. Player wins the
        /// mission; the ambusher party is forced out of fog-of-war for a
        /// window so the player can press-attack on the map (see
        /// ai-ambushes-player-design.md § 5).</summary>
        AmbushRepelled,

        /// <summary>Player crossed the soft mission border and stayed outside
        /// long enough for the countdown to expire. Player survives but
        /// all remaining troops wounded and party morale drops; the
        /// ambusher may open a vanilla field battle on the campaign map if
        /// its remaining strength clears the follow-up threshold (see § 6).
        /// </summary>
        AmbushedFled,
    }
}
