namespace TOR_Core.CampaignMechanics.WitchHunter.Bounties
{
    /// <summary>
    /// Which track of the Inquisition's reputation ladder this bounty counts
    /// toward on completion. Minor and Major are separate caps + separate
    /// progression counters per the Chapterhouse design doc; a single
    /// <see cref="BountyQuest"/>/<see cref="BountyIssue"/> class pair covers
    /// both tiers with this enum discriminating per-instance.
    /// </summary>
    public enum BountyTier
    {
        Minor,
        Major,
    }
}
