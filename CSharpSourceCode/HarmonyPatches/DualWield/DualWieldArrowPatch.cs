// Vendored from Yotthani's DualWield-v0.10.9 mod (permission granted
// 2026-11-24). Full port, ilspycmd decompile artifacts hand-fixed.
//
// Harmony patch rationale: see the class-level <summary> below for
// Problem / Why not non-patching / Scope (per feedback_harmony_patch_rationale).

using HarmonyLib;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.ViewModelCollection.HUD;
using TOR_Core.BattleMechanics.DualWield.Vendor;

namespace TOR_Core.HarmonyPatches.DualWield;

[HarmonyPatch(typeof(CrosshairVM), "SetArrowProperties")]
public static class DualWieldArrowPatch
{
	[HarmonyPrefix]
	public static void Prefix(ref double topArrowOpacity, ref double rightArrowOpacity, ref double bottomArrowOpacity, ref double leftArrowOpacity)
	{
		Agent.UsageDirection playerLeftArrow = DualWieldMissionBehavior.PlayerLeftArrow;
		if ((int)playerLeftArrow != -1 && DualWieldMissionBehavior.IsActive && BannerlordConfig.DisplayAttackDirection)
		{
			topArrowOpacity = (((int)playerLeftArrow == 0) ? 0.7 : 0.0);
			rightArrowOpacity = (((int)playerLeftArrow == 3) ? 0.7 : 0.0);
			bottomArrowOpacity = (((int)playerLeftArrow == 1) ? 0.7 : 0.0);
			leftArrowOpacity = (((int)playerLeftArrow == 2) ? 0.7 : 0.0);
		}
	}
}
