// Vendored from Yotthani's DualWield-v0.10.9 mod (permission granted
// 2026-11-24). Full port, ilspycmd decompile artifacts hand-fixed.
//
// Harmony patch rationale: see the class-level <summary> below for
// Problem / Why not non-patching / Scope (per feedback_harmony_patch_rationale).

using System;
using System.Reflection;
using TOR_Core.BattleMechanics.DualWield.Vendor;
using HarmonyLib;
using TaleWorlds.Core;
using TaleWorlds.DotNet;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;

namespace TOR_Core.HarmonyPatches.DualWield;

[HarmonyPatch]
public static class WeaponReachProbe
{
	private const sbyte L_CLAVICLE = 14;

	private const sbyte L_FINGER0 = 20;

	private const sbyte R_CLAVICLE = 21;

	private const sbyte R_FINGER0 = 27;

	private const int MAX_LINES = 60;

	private static int _lines;

	private static int _playerHits;

	private static MethodBase TargetMethod()
	{
		return AccessTools.Method(typeof(Mission), "MeleeHitCallback", (Type[])null, (Type[])null);
	}

	[HarmonyPrefix]
	public static void Prefix(ref AttackCollisionData collisionData, Agent attacker, Agent victim, Vec3 blowDir, Vec3 swingDir)
	{
		if (!DualWieldMissionBehavior.IsActive || !DualWieldSettings.Get().DebugMode)
		{
			return;
		}
		PlayerHitProbe(ref collisionData, attacker, victim);
		if (_lines >= 60)
		{
			return;
		}
		try
		{
			if (attacker == null || !attacker.IsHuman || LeftBladeStrike.ApplyingLeftHit)
			{
				return;
			}
			MBAgentVisuals agentVisuals = attacker.AgentVisuals;
			Skeleton val = (((NativeObject)(object)agentVisuals == (NativeObject)null) ? null : agentVisuals.GetSkeleton());
			if ((NativeObject)(object)val == (NativeObject)null)
			{
				return;
			}
			sbyte attackBoneIndex = collisionData.AttackBoneIndex;
			if (attackBoneIndex < 0)
			{
				return;
			}
			MatrixFrame val2 = agentVisuals.GetGlobalFrame();
			MatrixFrame val3 = val.GetBoneEntitialFrame(attackBoneIndex);
			MatrixFrame val4 = val2.TransformToParent(ref val3);
			Vec3 val5 = collisionData.CollisionGlobalPosition - val4.origin;
			float length = val5.Length;
			if (!(length < 0.05f))
			{
				val5 *= 1f / length;
				sbyte b = (sbyte)((attackBoneIndex >= 21) ? 21 : 14);
				val3 = agentVisuals.GetGlobalFrame();
				val2 = val.GetBoneEntitialFrame(b);
				MatrixFrame val6 = val3.TransformToParent(ref val2);
				Vec3 b2 = val4.origin - val6.origin;
				if (b2.Normalize() < 0.0001f)
				{
					b2 = val5;
				}
				EquipmentIndex val7 = (EquipmentIndex)collisionData.AffectorWeaponSlotOrMissileIndex;
				MissionWeapon val8 = (((int)val7 >= 0 && (int)val7 < 5) ? attacker.Equipment[val7] : MissionWeapon.Invalid);
				string text = ((val8.Item != null) ? ((MBObjectBase)val8.Item).StringId : "(none)");
				string text2 = ((val8.Item != null && !string.IsNullOrEmpty(val8.Item.SkeletonName)) ? val8.Item.SkeletonName : "-");
				float num = ((val8.CurrentUsageItem != null) ? val8.CurrentUsageItem.GetRealWeaponLength() : 0f);
				_lines++;
				DualWieldLog.Log(string.Format("[LH-Reach] '{0}' skel {1} bone {2} {3} | reach {4:0.00} m of {5:0.00} (engine says {6:0.00} along the weapon) | axis {7:0} deg, swing {8:0}, blow {9:0}, radial {10:0} | lastSegment {11} | progress {12:0.00} -> {13}", new object[14]
				{
					text,
					text2,
					attackBoneIndex,
					(victim != null) ? "agent" : "scene",
					length,
					num,
					collisionData.CollisionDistanceOnWeapon,
					Angle(val5, val4.rotation.u),
					Angle(val5, swingDir),
					Angle(val5, blowDir),
					Angle(val5, b2),
					collisionData.CollidedWithLastBoneSegment,
					collisionData.AttackProgress,
					collisionData.CollisionResult
				}));
			}
		}
		catch (Exception ex)
		{
			_lines = 60;
			DualWieldLog.Log("[LH-Reach] failed: " + ex.Message);
		}
	}

	private static void PlayerHitProbe(ref AttackCollisionData cd, Agent attacker, Agent victim)
	{
		if (_playerHits >= 40)
		{
			return;
		}
		try
		{
			if (victim != null && victim == Agent.Main && attacker != null)
			{
				_playerHits++;
				string text = "?";
				string text2 = "?";
				ActionIndexCache currentAction;
				try
				{
					currentAction = victim.GetCurrentAction(0);
					text = currentAction.GetName();
				}
				catch
				{
				}
				try
				{
					currentAction = victim.GetCurrentAction(1);
					text2 = currentAction.GetName();
				}
				catch
				{
				}
				string text3 = "?";
				try
				{
					text3 = ((object)victim.GetCurrentActionType(1)/*cast due to constrained. prefix*/).ToString();
				}
				catch
				{
				}
				string text4 = "?";
				try
				{
					text4 = ((object)victim.GetCurrentActionDirection(1)/*cast due to constrained. prefix*/).ToString();
				}
				catch
				{
				}
				DualWieldLog.Log($"[Hit-on-player] {attacker.Name}: attack {cd.AttackDirection} -> engine says {cd.CollisionResult}" + string.Format(" (shield {0}, dmg {1}) | player ch0 '{2}' ch1 '{3}' type1 {4} dir1 {5}", new object[6] { cd.AttackBlockedWithShield, cd.InflictedDamage, text, text2, text3, text4 }));
			}
		}
		catch (Exception ex)
		{
			_playerHits = 40;
			DualWieldLog.Log("[Hit-on-player] failed: " + ex.Message);
		}
	}

	private static float Angle(Vec3 a, Vec3 b)
	{
		float length = b.Length;
		if (length < 0.0001f)
		{
			return 999f;
		}
		b *= 1f / length;
		return MathF.Acos(MathF.Max(-1f, MathF.Min(1f, Vec3.DotProduct(a, b)))) * 57.29578f;
	}
}
