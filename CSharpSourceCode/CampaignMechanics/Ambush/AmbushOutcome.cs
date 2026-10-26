namespace TOR_Core.CampaignMechanics.Ambush
{
    /// <summary>
    /// Result bucket Phase E (post-mission aftermath) reads to pick the follow-up
    /// path. Set by <c>AmbushEndLogic</c> before the mission closes.
    /// </summary>
    public enum AmbushOutcome
    {
        /// <summary>No ambush ran — default when the behavior boots or after a
        /// successful press-attack / escape follow-up has already consumed the
        /// previous outcome.</summary>
        None,

        /// <summary>Player made it into a retreat zone (either held long enough,
        /// or was inside when the pressure clock elapsed). Escape vs press-attack
        /// menu lands next.</summary>
        ExtractionSuccessful,

        /// <summary>Pressure clock ran out without the player reaching a retreat
        /// zone. Troops wounded, hero wounded, follow-up disorganisation debuff
        /// (Phase E).</summary>
        ExtractionFailed,

        /// <summary>Player's spawned ambush roster was wiped before extraction.
        /// Treated like a normal lost battle (vanilla BattleEndLogic handles
        /// casualty bookkeeping).</summary>
        PlayerDefeated,

        /// <summary>Main hero went down. Captivity flow applies via vanilla
        /// handling.</summary>
        PlayerDied,
    }
}
