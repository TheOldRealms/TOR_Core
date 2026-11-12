namespace TOR_Core.CampaignMechanics.WitchHunter.Bounties
{
    /// <summary>
    /// Shape of a bounty objective. Phase B ships <see cref="KillTroops"/> only
    /// (count-N-kills-of-type-X); the other three land in Phase C as the quest
    /// logic for each is distinct enough to warrant its own subclass or branch.
    /// </summary>
    public enum BountyObjectiveType
    {
        /// <summary>Count enemy-troop kills that match the template's predicate until <c>TargetCount</c>.</summary>
        KillTroops,
        /// <summary>(Phase C) Return N captured troops of a type via the prisoner turn-in flow.</summary>
        CaptureTroops,
        /// <summary>(Phase C) Clear a specific hideout type.</summary>
        ClearHideout,
        /// <summary>(Phase C) Defeat N enemy parties of a type.</summary>
        DefeatParties,
    }
}
