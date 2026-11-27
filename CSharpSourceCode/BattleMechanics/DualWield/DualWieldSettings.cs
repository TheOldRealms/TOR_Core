// Vendored from Yotthani's DualWield-v0.10.9 (permission 2026-11-24).
// Original (MCM-driven user settings) rewritten as a minimal static-config
// singleton: TOR doesn't ship MCM, so there is no in-game toggle UI; the
// values below are the DEFAULTS from Yotthani's original class.
//
// This class IS the "DualWieldConfig" referenced in feral-combat Task 10
// (always-on config). The DualWieldSettings name is kept so the ~200
// vendored call sites don't churn; the content is the always-on config.
//
// If any field proves worth exposing to end-users later, wire it up when TOR
// adopts a settings story. Until then it's compile-time constants.

using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace TOR_Core.BattleMechanics.DualWield;

/// <summary>
/// Compile-time configuration for the vendored DualWield system. Mirrors
/// Yotthani's <c>DualWieldSettings</c> property surface so the vendored
/// files compile unmodified, but every value is hard-coded — no MCM, no
/// runtime UI, no user override.
/// </summary>
public class DualWieldSettings
{
    private static readonly DualWieldSettings _instance = new DualWieldSettings();
    public static DualWieldSettings Get() => _instance;

    // BISECT: every patch early-returns when this is false, and the spawn
    // patch's lazy AddMissionBehavior fallback is gated on it too. Flipping
    // false fully disables DualWield runtime while leaving the Harmony patches
    // registered via LatePatches (so the action cache stays healthy).
    public bool EnableDualWield { get; } = false;
    public bool DebugMode { get; } = false;
    public bool AlternatingCrossCombo { get; } = false;
    public bool TestWeaponBothHands { get; } = false;
    public bool TestLeftHanded { get; } = false;
    public bool FreeReinHand { get; } = true;
    public bool TestRiderZones { get; } = false;
    public bool TestRiderAiTarget { get; } = true;
    public bool TestLogLookBend { get; } = false;
    public bool TestLeftLookSlope { get; } = false;
    public bool TestNpcDualWield { get; } = false;
    public bool NpcTroops { get; } = true;
    public bool LeftDrawGesture { get; } = true;
    public bool TestSameWeaponBothHands => false;

    public string TestMainHandItemId => null;
    public string TestOffhandItemId => null;

    // Combat-mode constants: 1=Alternating (LMB), 2=Separate (left-click right / own key left)
    public int AttackMode { get; } = 1;

    // Left-hand hotkey fallback. Vanilla's "unassigned" InputKey value.
    public InputKey LeftHandKey => (InputKey)225;

    // Foot-channel constants: true = channel 1 (follows the look), false = channel 0.
    public bool FootLeftChannel1 => true;
    public bool NpcFootLeftChannel1 => true;

    // Rider-mounted-left channel: -1 = off, 0 = channel 0, 1 = channel 1.
    public int MountedLeftChannel => 1;

    // Limb-ray debug mode: 0=Off, 1=Shadow compare, 2=Live. Default live.
    public int LimbRayMode => 2;

    // Slowdown for left-hand strike: false = apply slowdown (by skill), true = off.
    public bool LeftStrikeSlowdownOff => false;

    // Offhand weapon pose roll: fixed 0 degrees.
    public float OffhandWeaponRoll => 0f;

    // Combat tuning (from Yotthani's defaults).
    public float OffHandDamageMultiplier { get; } = 0.85f;
    public float AttackSpeedBonus { get; } = 0.15f;
    public float ArmorPenalty { get; } = 0.2f;
    public float MovementSpeedBonus { get; } = 0.1f;
    public float ParryWindowReduction { get; } = 0.3f;

    // NPC behaviour tuning.
    public float NpcLeftLeadShare { get; } = 0.2f;
    public float NpcOffhandChance { get; } = 0.33f;
    public float NpcOffhandFollowUpChance { get; } = 0.5f;
    public float NpcBonusShare { get; } = 0f;

    /// <summary>
    /// Scales any bonus share by whether the agent is player-controlled
    /// (full bonus) or an NPC (<see cref="NpcBonusShare"/> fraction).
    /// </summary>
    public static float BonusShare(Agent agent)
    {
        if (!DualWieldDetector.IsPlayer(agent))
        {
            return Get().NpcBonusShare;
        }
        return 1f;
    }

    public static void DebugLog(string message)
    {
        if (Get().DebugMode)
        {
            InformationManager.DisplayMessage(new InformationMessage("[DualWield] " + message, Colors.Cyan));
        }
    }

    public static void DebugMessage(string text, Color color)
    {
        if (Get().DebugMode)
        {
            InformationManager.DisplayMessage(new InformationMessage(text, color));
        }
    }
}
