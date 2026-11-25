namespace TOR_Core.CampaignMechanics.WitchHunter
{
    /// <summary>
    /// Player's standing with the Order of Sigmar's Inquisition — derived from
    /// completed-bounty counters on <see cref="WitchHunterInquisitionBehavior"/>.
    /// Ranks only progress forward; thresholds and gated benefits are defined in
    /// the Witch Hunter Chapterhouse design doc under TORTasks.
    /// </summary>
    public enum InquisitionRank
    {
        Unknown,
        Known,
        Trusted,
        Respected,
    }
}
