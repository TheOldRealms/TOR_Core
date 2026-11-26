// Temporary stubs for Task 9 (Harmony patches vendor) — remove when the real
// patches land. The Core files vendored in Task 7 reference these types via
// cross-module calls; this file provides just enough surface for the build to
// succeed. Replaced wholesale by Task 9.

using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace TOR_Core.BattleMechanics.BerserkWield.Vendor;

/// <summary>
/// Stub for Yotthani's DualWieldWieldingPatches — real patches ported in Task 9.
/// Methods here are no-op so the build works.
/// </summary>
internal static class DualWieldWieldingPatches
{
    public static bool IsOneHandedMeleeWeapon(MissionWeapon weapon) => false;
    public static void ReleaseLeftForRanged(Agent agent, BerserkWieldMissionBehavior behavior, string reason) { }
    public static void ForceOffHandPairing(Agent agent) { }
}
