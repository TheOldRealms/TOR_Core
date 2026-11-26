// Temporary stubs for Task 9 (Harmony patches vendor) — remove when the real
// patches land. The Core files vendored in Task 7 + MissionBehavior in Task 8
// reference these types via cross-module calls; this file provides just
// enough surface for the build to succeed. Replaced wholesale by Task 9.
//
// Yotthani's DualWield-v0.10.9 source for the real implementations is at
// /tmp/dualwield_decompiled/DualWield.Patches/.

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
    public static void ForceOffHandPairing(Agent agent, bool instant = true, bool onSpawn = false) { }
    public static void AdoptHeldOffhand(Agent agent, BerserkWieldMissionBehavior behavior) { }
    public static void ClearTrackingState() { }
    internal static void TickLeftGestures(Agent agent, BerserkWieldMissionBehavior behavior) { }
}

/// <summary>
/// Stub for Yotthani's DualWieldSpawnPatches — real patches ported in Task 9.
/// </summary>
internal static class DualWieldSpawnPatches
{
    internal static void RegisterPlayerLate(Agent agent) { }
}

/// <summary>
/// Stub for Yotthani's AnimDiagnostics — real diagnostics ported in Task 9.
/// </summary>
internal static class AnimDiagnostics
{
    public static void Enable() { }
    public static void Disable() { }
    public static void Dump(Agent agent, string label) { }
    public static void OnAgentWielded(Agent agent) { }
    public static void OnAttack(Agent agent) { }
    public static void OnHit(Agent agent) { }
}
