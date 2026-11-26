// Vendored from Yotthani's DualWield-v0.10.9 mod (permission granted
// 2026-11-24). Full port, ilspycmd decompile artifacts hand-fixed.
//
// Harmony patch rationale: see the class-level <summary> below for
// Problem / Why not non-patching / Scope (per feedback_harmony_patch_rationale).

using TOR_Core.BattleMechanics.DualWield.Vendor;
using HarmonyLib;
using TaleWorlds.InputSystem;

namespace TOR_Core.HarmonyPatches.DualWield;

[HarmonyPatch(typeof(HotKeyManager), "SaveAsync")]
public static class DualWieldKeySavePatch
{
	[HarmonyPrefix]
	public static void Prefix(out bool __state)
	{
		__state = SeparateKeyOverride.SuspendForSave();
	}

	[HarmonyPostfix]
	public static void Postfix(bool __state)
	{
		if (__state)
		{
			SeparateKeyOverride.ResumeAfterSave();
		}
	}
}
