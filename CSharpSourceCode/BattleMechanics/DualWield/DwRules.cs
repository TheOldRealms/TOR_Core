// Vendored from Yotthani's DualWield-v0.10.9 mod (permission granted
// 2026-11-24). Class name prefixed DualWield for TOR namespacing;
// internal logic preserved as-is. See TORTasks/feral-combat/architecture-
// dualwield-vendored.md for the big picture.

using System;
using System.Collections.Generic;

namespace TOR_Core.BattleMechanics.DualWield;

public static class DwRules
{
	public const float LeftStrikeSpeedUntrained = 0.6f;

	public const float LeftStrikeSkillForFullSpeed = 400f;

	public const float ThrustGripShift = 0.1f;

	public const float ThrustStart = 0.4f;

	public const float ChainNotBefore = 0.65f;

	public const float ChainAfterWindow = 0.05f;

	public const float HeightPitchDeg = 10f;

	public const float DeepPitchDeg = 35f;

	public const float SteepUpPitchDeg = 20f;

	public const float SweepStartDeg = 100f;

	public const float StraightConeDeg = 15f;

	public const float BodyRadius = 0.3f;

	public const float MountedLeanSweepStartDeg = 20f;

	public const int OffhandVanilla = -1;

	public const int OffhandNothing = 0;

	public const int OffhandShield = 1;

	public const int OffhandLeftWeapon = 2;

	public const int NoMountedStrike = 99;

	public const float MountedSideMin = 0.25f;

	public const float MountedSideAlong = 0.9f;

	public const float MountedAheadMin = 0.4f;

	public const float MountedAheadWide = 0.5f;

	public const int MountedThrust = 0;

	public const int MountedOverswing = 2;

	public const int MountedAheadEither = 3;

	public const float MountedShortMan = 1.6f;

	public const float MountedVeryShortMan = 1.4f;

	public const float MountedLeftSlashMin = 0.5f;

	public const float MountedLeftSlashMinShort = 0.9f;

	public const float MountedLeftSlashFromDeg = 27f;

	public const float MountedLeftSlashToDeg = 120f;

	public const float MountedRightSlashMin = 0.3f;

	public const float MountedRightSlashShortOfReach = 0.7f;

	public const float MountedRightSlashBehind = 0.3f;

	public const float MountedThrustLeft = 0.6f;

	public const float MountedThrustFrom = 0.9f;

	public const float MountedThrustFromShort = 1.2f;

	public const float MountedThrustBeyondReach = 0.1f;

	public const float MountedOverswingNear = 0.1f;

	public const float MountedOverswingFar = 0.9f;

	public const float MountedOverswingBehind = 0.4f;

	public const float MountedHorseHalfWidth = 0.6f;

	public const float MountedHorseAhead = 1.3f;

	public const float MountedHorseBehind = 1.2f;

	public const float MountedAheadRightOfLine = 0.05f;

	public const float MountedAheadThrustFrom = 1.2f;

	public const float MountedAheadThrustFromOnTheLeft = 1.6f;

	public const float MountedDiagonalFrom = 0.9f;

	public const float MountedDiagonalTo = 1.5f;

	public const float MountedDiagonalNear = 0.5f;

	public const float MountedDiagonalFar = 1f;

	public const float MountedFriendHorseHalf = 1f;

	public const int MissNobody = 0;

	public const int MissOutOfReach = 1;

	public const int MissAheadToTheSide = 2;

	public const int MissBehind = 3;

	public const int MissUnderTheHorse = 4;

	public const int MissNoAiTarget = 5;

	public const float MountedNearMargin = 1.5f;

	public const float MountedLeftSlashNear = 1f;

	public const float MountedLeftSlashMid = 1.4f;

	public const float MountedRightSlashNear = 1f;

	public static float LeftChance(float setting, float aiLevel)
	{
		return Math.Max(0f, Math.Min(1f, setting * 2f * aiLevel));
	}

	public static float LeftStrikeSpeed(int oneHanded, int vigorPlusControl, bool off)
	{
		if (off)
		{
			return 1f;
		}
		float num = Math.Max(0f, Math.Min(1f, ((float)oneHanded + 10f * (float)vigorPlusControl) / 400f));
		return 0.6f + 0.39999998f * num;
	}

	public static bool LeadsWithLeft(int agentIndex, float share)
	{
		if (share <= 0f)
		{
			return false;
		}
		return (float)((uint)(agentIndex * -1640531535 >>> 16) % 1000u) < share * 1000f;
	}

	public static bool OffHandHit(bool leftHit, bool leftHanded)
	{
		if (!leftHanded)
		{
			return leftHit;
		}
		return !leftHit;
	}

	public static string CombatParameterFor(string actionName)
	{
		string text = actionName ?? "";
		if (text.Contains("flail"))
		{
			if (text.Contains("overswing"))
			{
				return "1h_up_flail";
			}
			if (text.Contains("slashright"))
			{
				return "1h_right_flail_leftstance";
			}
			if (text.Contains("slashleft"))
			{
				return "1h_left_flail_leftstance";
			}
			return null;
		}
		if (text.Contains("horseback"))
		{
			if (text.Contains("overswing"))
			{
				return "1h_up_mounted";
			}
			if (text.Contains("thrust"))
			{
				return "onehanded_thrust_mounted";
			}
			if (text.Contains("slash_horseback_left"))
			{
				return "onehanded_left_mounted";
			}
			if (text.Contains("slash_horseback_right"))
			{
				return "onehanded_right_mounted";
			}
			return null;
		}
		if (text.Contains("backstab"))
		{
			return "onehanded_backstab";
		}
		if (text.Contains("thrust"))
		{
			return "onehanded_thrust";
		}
		if (text.Contains("overswing"))
		{
			return "1h_up";
		}
		bool flag = text.Contains("dagger");
		if (text.Contains("slashright"))
		{
			if (!flag)
			{
				return "onehanded_right_leftstance";
			}
			return "dagger_right_leftstance";
		}
		if (text.Contains("slashleft"))
		{
			if (!flag)
			{
				return "onehanded_left_leftstance";
			}
			return "dagger_left_leftstance";
		}
		return null;
	}

	public static void AdjustWindow(string actionName, ref float start, ref float end)
	{
		if ((actionName ?? "").Contains("thrust"))
		{
			end = Math.Min(1f, end + 0.1f);
			start = Math.Max(start, 0.4f);
		}
	}

	public static float ChainPoint(float windowEnd)
	{
		return Math.Min(0.95f, Math.Max(0.65f, windowEnd + 0.05f));
	}

	public static int StrikeHeight(float lookZ)
	{
		double num = Math.Asin(Math.Max(-1f, Math.Min(1f, lookZ))) * 180.0 / Math.PI;
		if (!(num <= -35.0))
		{
			if (!(num <= -10.0))
			{
				if (!(num >= 20.0))
				{
					return (num >= 10.0) ? 1 : 0;
				}
				return 2;
			}
			return -1;
		}
		return -2;
	}

	public static string AtHeight(string action, int height)
	{
		if (height != 0 && !string.IsNullOrEmpty(action))
		{
			string text = action;
			string text2 = ((height <= -2) ? "_lo2" : ((height < 0) ? "_lo" : ((height >= 2) ? "_hi2" : "_hi")));
			return text + text2;
		}
		return action;
	}

	public static string BlockedOf(string releaseName)
	{
		if (string.IsNullOrEmpty(releaseName))
		{
			return releaseName;
		}
		return AtHeight(WithoutHeight(releaseName).Replace("_quick_release_", "_quick_blocked_"), HeightOfAction(releaseName));
	}

	public static int LeftSlot(int main, int leftNow, bool[] oneHanded)
	{
		if (oneHanded == null || oneHanded.Length < 4)
		{
			return -1;
		}
		if (leftNow >= 0 && leftNow < 4 && leftNow != main && oneHanded[leftNow])
		{
			return leftNow;
		}
		int[] array = LeftRing(main, oneHanded);
		if (array.Length == 0)
		{
			return -1;
		}
		return array[0];
	}

	public static int[] LeftRing(int main, bool[] oneHanded)
	{
		if (oneHanded == null || oneHanded.Length < 4)
		{
			return new int[0];
		}
		List<int> list = new List<int>(3);
		if (main != 1 && oneHanded[1])
		{
			list.Add(1);
		}
		int num = ((main >= 0 && main < 4) ? main : 3);
		for (int i = 1; i <= 4; i++)
		{
			int num2 = (num + i) % 4;
			if (num2 != main && num2 != 1 && oneHanded[num2])
			{
				list.Add(num2);
			}
		}
		return list.ToArray();
	}

	public static int NextLeftOnWheel(int main, int leftInHand, bool[] oneHanded)
	{
		int[] array = LeftRing(main, oneHanded);
		if (array.Length == 0)
		{
			return -1;
		}
		if (leftInHand < 0)
		{
			return array[0];
		}
		int num = Array.IndexOf(array, leftInHand);
		if (num < 0 || num + 1 >= array.Length)
		{
			return -1;
		}
		return array[num + 1];
	}

	public static int WheelDownOffhand(bool paired, bool mainOneHanded, bool anotherLeftWeapon, bool anotherShield)
	{
		if (paired)
		{
			if (!anotherLeftWeapon)
			{
				return 0;
			}
			return 2;
		}
		if (!mainOneHanded || !anotherLeftWeapon)
		{
			return -1;
		}
		if (!anotherShield)
		{
			return 2;
		}
		return 1;
	}

	public static bool AllyInTheArc(int sweepFrom, float allyDeg, float allyDist, float enemyDeg, float enemyDist, float reach)
	{
		return AllyInTheArc(sweepFrom, allyDeg, allyDist, enemyDeg, enemyDist, reach, 100f);
	}

	public static bool AllyInTheArc(int sweepFrom, float allyDeg, float allyDist, float enemyDeg, float enemyDist, float reach, float sweepStartDeg)
	{
		if (allyDist > reach + 0.3f)
		{
			return false;
		}
		float num = (float)(Math.Atan2(0.30000001192092896, Math.Max(allyDist, 0.1f)) * 180.0 / Math.PI);
		if (allyDist > enemyDist && Math.Abs(allyDeg - enemyDeg) < num)
		{
			return false;
		}
		if (sweepFrom == 0)
		{
			if (allyDist < enemyDist)
			{
				return Math.Abs(allyDeg) - num < 15f;
			}
			return false;
		}
		float val = (float)sweepFrom * sweepStartDeg;
		float num2 = Math.Min(val, enemyDeg);
		float num3 = Math.Max(val, enemyDeg);
		if (allyDeg + num > num2)
		{
			return allyDeg - num < num3;
		}
		return false;
	}

	public static int MountedStrikeSide(float forward, float lateral, float reach)
	{
		float num = reach + 0.3f;
		if (forward * forward + lateral * lateral > num * num)
		{
			return 99;
		}
		if (Math.Abs(lateral) >= 0.25f && Math.Abs(forward) <= 0.9f)
		{
			if (!(lateral < 0f))
			{
				return 1;
			}
			return -1;
		}
		if (forward > 0.4f && Math.Abs(lateral) < 0.5f)
		{
			return 0;
		}
		return 99;
	}

	public static int MountedStrikeAtManOnFoot(float forward, float lateral, float reach, float manHeight)
	{
		float num = reach + 0.3f;
		float num2 = forward * forward + lateral * lateral;
		if (Math.Abs(lateral) < 0.6f && forward < 1.3f && forward > -1.2f)
		{
			return 99;
		}
		bool flag = manHeight < 1.6f;
		bool flag2 = manHeight < 1.4f;
		if (num2 <= num * num)
		{
			float num3 = (float)(Math.Atan2(lateral, forward) * 180.0 / Math.PI);
			if (!flag2 && lateral <= 0f - (flag ? 0.9f : 0.5f) && num3 <= -27f && num3 >= -120f)
			{
				return -1;
			}
			if (lateral >= 0.3f && lateral <= num - 0.7f && forward >= -0.3f)
			{
				return 1;
			}
		}
		bool flag3 = !flag2 && lateral >= -0.6f && lateral <= 0f && forward >= (flag ? 1.2f : 0.9f) && forward <= num + 0.1f;
		bool flag4 = lateral >= -0.9f && lateral <= -0.1f && forward >= -0.4f && num2 <= num * num;
		if (flag3 & flag4)
		{
			return 3;
		}
		if (flag3)
		{
			return 0;
		}
		if (!flag4)
		{
			return 99;
		}
		return 2;
	}

	public static int MountedAheadStrike(float forward, float lateral, bool canThrust)
	{
		if (lateral >= 0.05f)
		{
			return 1;
		}
		if (lateral <= -0.05f)
		{
			if (!canThrust || !(forward >= 1.6f))
			{
				return 2;
			}
			return 0;
		}
		if (!canThrust || !(forward >= 1.2f))
		{
			return 1;
		}
		return 0;
	}

	public static int MountedFrontDiagonal(float forward, float lateral, float reach)
	{
		float num = reach + 0.3f;
		float num2 = Math.Abs(lateral);
		if (forward <= 0.9f || forward > 1.5f || num2 < 0.5f || num2 > 1f)
		{
			return 99;
		}
		if (forward * forward + lateral * lateral > num * num)
		{
			return 99;
		}
		if (!(lateral < 0f))
		{
			return 1;
		}
		return -1;
	}

	public static bool AllyBeforeTheTarget(float allyDeg, float allyDist, float enemyDeg, float enemyDist)
	{
		if (allyDist >= enemyDist)
		{
			return false;
		}
		float num = (float)(Math.Atan2(0.30000001192092896, Math.Max(allyDist, 0.1f)) * 180.0 / Math.PI);
		return Math.Abs(allyDeg - enemyDeg) - num < 15f;
	}

	public static int MountedMissWhyOnFoot(float forward, float lateral, float reach)
	{
		float num = reach + 0.3f;
		float num2 = num + 1.5f;
		float num3 = forward * forward + lateral * lateral;
		if (num3 > num2 * num2)
		{
			return 0;
		}
		if (num3 > num * num)
		{
			return 1;
		}
		if (lateral > num - 0.7f && forward >= -0.3f && forward <= 0.9f)
		{
			return 1;
		}
		if (forward > 0.9f)
		{
			return 2;
		}
		if (forward < 0f)
		{
			return 3;
		}
		return 4;
	}

	public static int MountedMissWhy(float forward, float lateral, float reach)
	{
		float num = reach + 0.3f;
		float num2 = num + 1.5f;
		float num3 = forward * forward + lateral * lateral;
		if (num3 > num2 * num2)
		{
			return 0;
		}
		if (num3 > num * num || (MountedLeftSlashTooFar(lateral) && Math.Abs(forward) <= 0.9f))
		{
			return 1;
		}
		if (forward > 0.9f)
		{
			return 2;
		}
		if (forward < -0.9f)
		{
			return 3;
		}
		return 4;
	}

	public static bool MountedLeftSlashTooFar(float lateral)
	{
		return lateral <= -1.4f;
	}

	public static string MountedLeftSlashReach(float lateral)
	{
		float num = Math.Abs(lateral);
		if (!(num < 1f))
		{
			if (!(num < 1.4f))
			{
				return "_lo";
			}
			return "_lom";
		}
		return "_lon";
	}

	public static string MountedRightSlashReach(float lateral)
	{
		if (!(Math.Abs(lateral) < 1f))
		{
			return "_lom";
		}
		return "_lo";
	}

	public static int HeightOfAction(string action)
	{
		if (string.IsNullOrEmpty(action))
		{
			return 0;
		}
		if (action.EndsWith("_lo2", StringComparison.Ordinal))
		{
			return -2;
		}
		if (action.EndsWith("_hi2", StringComparison.Ordinal))
		{
			return 2;
		}
		if (action.EndsWith("_lo", StringComparison.Ordinal) || IsReachVariant(action))
		{
			return -1;
		}
		return action.EndsWith("_hi", StringComparison.Ordinal) ? 1 : 0;
	}

	private static bool IsReachVariant(string action)
	{
		if (!action.EndsWith("_lom", StringComparison.Ordinal))
		{
			return action.EndsWith("_lon", StringComparison.Ordinal);
		}
		return true;
	}

	public static string WithoutHeight(string action)
	{
		if (string.IsNullOrEmpty(action))
		{
			return action;
		}
		if (IsReachVariant(action) || action.EndsWith("_lo2", StringComparison.Ordinal) || action.EndsWith("_hi2", StringComparison.Ordinal))
		{
			return action.Substring(0, action.Length - 4);
		}
		if (!action.EndsWith("_lo", StringComparison.Ordinal) && !action.EndsWith("_hi", StringComparison.Ordinal))
		{
			return action;
		}
		return action.Substring(0, action.Length - 3);
	}

	public static bool ThrustBecomesOverswing(int thrustDamage)
	{
		return thrustDamage <= 0;
	}

	public static bool LeftTurnAfterPause(float now, float lastRightAt, float lastLeftAt, float pause = 1f)
	{
		return now - Math.Max(lastRightAt, lastLeftAt) > pause;
	}
}
