// Vendored from Yotthani's DualWield-v0.10.9 mod (permission granted
// 2026-11-24). Full port, ilspycmd decompile artifacts hand-fixed.
//
// Harmony patch rationale: see the class-level <summary> below for
// Problem / Why not non-patching / Scope (per feedback_harmony_patch_rationale).

using System;
using TOR_Core.BattleMechanics.DualWield.Vendor;
using TaleWorlds.Core;
using TaleWorlds.DotNet;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace TOR_Core.HarmonyPatches.DualWield;

public static class AnimDiagnostics
{
	private static void L(string label, string msg)
	{
		DualWieldLog.Log("[Diag " + label + "] " + msg);
	}

	public static void Dump(Agent agent, string label)
	{
		if (agent == null || !agent.IsActive())
		{
			L(label, "no active agent");
			return;
		}
		L(label, "=== begin ===");
		MBAgentVisuals agentVisuals = agent.AgentVisuals;
		if ((NativeObject)(object)agentVisuals == (NativeObject)null)
		{
			L(label, "AgentVisuals NULL");
			return;
		}
		try
		{
			Skeleton skeleton = agentVisuals.GetSkeleton();
			if ((NativeObject)(object)skeleton == (NativeObject)null)
			{
				L(label, "skeleton NULL");
				return;
			}
			try
			{
				L(label, "anim@ch0='" + skeleton.GetAnimationAtChannel(0) + "' idx=" + skeleton.GetAnimationIndexAtChannel(0));
			}
			catch (Exception ex)
			{
				L(label, "GetAnimationAtChannel THREW " + ex.Message);
			}
			try
			{
				L(label, "param=" + skeleton.GetAnimationParameterAtChannel(0) + " speed=" + skeleton.GetAnimationSpeedAtChannel(0));
			}
			catch (Exception ex2)
			{
				L(label, "GetAnimationParameter THREW " + ex2.Message);
			}
		}
		catch (Exception ex3)
		{
			L(label, "skeleton access THREW " + ex3.Message);
		}
		try
		{
			L(label, "ragdoll=" + ((object)agentVisuals.GetCurrentRagdollState()/*cast due to constrained. prefix*/).ToString());
		}
		catch (Exception ex4)
		{
			L(label, "GetCurrentRagdollState THREW " + ex4.Message);
		}
		try
		{
			L(label, "animFlag ch0=0x" + ((ulong)(long)agent.GetCurrentAnimationFlag(0)).ToString("X") + " ch1=0x" + ((ulong)(long)agent.GetCurrentAnimationFlag(1)).ToString("X"));
		}
		catch (Exception ex5)
		{
			L(label, "GetCurrentAnimationFlag THREW " + ex5.Message);
		}
		try
		{
			L(label, "crouch=" + agent.CrouchMode + " sitting=" + agent.IsSitting() + " scale=" + agent.AgentScale + " mounted=" + (agent.MountAgent != null));
		}
		catch (Exception ex6)
		{
			L(label, "agent state THREW " + ex6.Message);
		}
		try
		{
			Monster monster = agent.Monster;
			L(label, "monster standEye=" + monster.StandingEyeHeight + " crouchEye=" + monster.CrouchEyeHeight + " thoraxBone=" + monster.ThoraxLookDirectionBoneIndex + " headBone=" + monster.HeadLookDirectionBoneIndex);
		}
		catch (Exception ex7)
		{
			L(label, "Monster THREW " + ex7.Message);
		}
		try
		{
			Vec3 position = agent.Position;
			L(label, "agentPos=(" + F(position.x) + "," + F(position.y) + "," + F(position.z) + ")");
		}
		catch (Exception ex8)
		{
			L(label, "Position THREW " + ex8.Message);
		}
		try
		{
			Vec3 origin = agentVisuals.GetFrame().origin;
			L(label, "visualFrame=(" + F(origin.x) + "," + F(origin.y) + "," + F(origin.z) + ")");
		}
		catch (Exception ex9)
		{
			L(label, "GetFrame THREW " + ex9.Message);
		}
		try
		{
			Vec3 globalStableEyePoint = agentVisuals.GetGlobalStableEyePoint(agent.IsHuman);
			L(label, "eyePoint=(" + F(globalStableEyePoint.x) + "," + F(globalStableEyePoint.y) + "," + F(globalStableEyePoint.z) + ")");
		}
		catch (Exception ex10)
		{
			L(label, "GetGlobalStableEyePoint THREW " + ex10.Message);
		}
		try
		{
			agentVisuals.GetSkeleton().ForceUpdateBoneFrames();
			L(label, "ForceUpdateBoneFrames OK");
		}
		catch (Exception ex11)
		{
			L(label, "ForceUpdateBoneFrames THREW " + ex11.Message);
		}
		sbyte index = 0;
		sbyte index2 = 0;
		try
		{
			index = agent.Monster.ThoraxLookDirectionBoneIndex;
			index2 = agent.Monster.HeadLookDirectionBoneIndex;
		}
		catch
		{
		}
		DumpBone(agent, label, "thorax", index);
		DumpBone(agent, label, "head", index2);
		L(label, "=== end (survived) ===");
	}

	private static void DumpBone(Agent agent, string label, string name, sbyte index)
	{
		try
		{
			Vec3 origin = agent.GetBoneEntitialFrame(index, true).origin;
			bool flag = Bad(origin.x) || Bad(origin.y) || Bad(origin.z);
			L(label, "bone " + name + "(#" + index + ") origin=(" + F(origin.x) + "," + F(origin.y) + "," + F(origin.z) + ") " + (flag ? "*** INVALID ***" : "ok"));
		}
		catch (Exception ex)
		{
			L(label, "bone " + name + "(#" + index + ") THREW " + ex.Message);
		}
	}

	private static bool Bad(float v)
	{
		if (!float.IsNaN(v) && !float.IsInfinity(v))
		{
			return Math.Abs(v) > 10000f;
		}
		return true;
	}

	private static string F(float v)
	{
		return v.ToString("0.###");
	}
}
