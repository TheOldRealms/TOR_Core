namespace TOR_Core.Models
{
    /// <summary>Two-tier classification for the ambush widget's settlement
    /// icon + the model's settlement factor (see campaign-stealth-design.md §21).
    /// Hostile wins over neutral when both are in range of a party at once.</summary>
    public enum SettlementProximityKind
    {
        None,
        Neutral,
        Hostile,
    }
}
