// Stub placeholder for the full vendored MissionBehavior (Yotthani ownership).
// Full port lands in Task 8.

// Vendored stub — the real 5204-line port lands in Task 8. This stub exists
// only so the other vendored Core files (specifically NpcRangedHybrids) can
// compile with a reference to the type. Task 8 replaces this file wholesale.
//
// Members mirror only what the current Task 7 Core files consume. Expand on
// demand if a vendor port surfaces another member.

using TaleWorlds.MountAndBlade;

namespace TOR_Core.BattleMechanics.BerserkWield.Vendor;

/// <summary>
/// Stub placeholder for the full vendored MissionBehavior. Replaced in Task 8.
/// </summary>
public class BerserkWieldMissionBehavior : MissionLogic
{
    /// <summary>
    /// Global activity flag the WeaponReachProbe patch + a few Core files
    /// read. Phase 1 behaviour is driven by the separate
    /// <c>BerserkWieldMissionLogic</c>; this stub keeps IsActive=false until
    /// Task 8 lands the real implementation.
    /// </summary>
    public static bool IsActive { get; set; } = false;
}
