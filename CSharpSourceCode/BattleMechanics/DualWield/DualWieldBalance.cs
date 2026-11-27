// Vendored from Yotthani's DualWield-v0.10.9 mod (permission granted
// 2026-11-24). Class name prefixed DualWield for TOR namespacing;
// internal logic preserved as-is. See TORTasks/feral-combat/architecture-
// dualwield-vendored.md for the big picture.

using System;
using System.Collections.Generic;
using System.Globalization;
using TaleWorlds.MountAndBlade;

namespace TOR_Core.BattleMechanics.DualWield;

public static class DualWieldBalance
{
	private const float REPORT_EVERY = 60f;

	private static int _rightHits;

	private static int _rightKills;

	private static int _rightBlocked;

	private static float _rightDamage;

	private static int _leftFired;

	private static int _leftHits;

	private static int _leftKills;

	private static int _leftParried;

	private static int _leftOnFriend;

	private static int _leftSkipped;

	private static int _leftHeldForAlly;

	private static int _leftMounted;

	private static int _leftBlocked;

	private static float _leftDamage;

	private static readonly int[] _guards = new int[4];

	private const float CHAIN_WINDOW = 2f;

	private const int CHAIN_MIN = 3;

	private static readonly Dictionary<long, List<float>> _recent = new Dictionary<long, List<float>>();

	private static int _chainsDw;

	private static int _chainsOther;

	private static int _maxChainDw;

	private static int _maxChainOther;

	private static int _otherHits;

	private static int _rightOnFriend;

	private static int _otherOnFriend;

	private static int _leftFollowUps;

	private static int _leftLeads;

	private static float _sinceReport;

	private static float _sinceSample;

	private static readonly int[,] _thrownBack = new int[2, 5];

	private static int _pLeftFired;

	private static int _pLeftMounted;

	private static int _pLeftHits;

	private static int _pLeftKills;

	private static int _pLeftParried;

	private static int _pLeftOnFriend;

	private static int _pRightHits;

	private static int _pLeftBlocked;

	private static float _pLeftDamage;

	private static float _pRightDamage;

	private static int _thrownBackUpright;

	private static readonly int[] _npcFootChannel = new int[2];

	public const int RiderSwing = 0;

	public const int RiderBusy = 1;

	public const int RiderNoTarget = 2;

	public const int RiderNoRoll = 3;

	public const int RiderQueued = 4;

	public const int RiderCounterQueued = 5;

	public const int RiderTargetGone = 6;

	public const int RiderAlly = 7;

	public const int RiderCap = 8;

	public const int RiderFired = 9;

	public const int RiderHit = 10;

	public const int RiderOnFriend = 11;

	public const int RiderLeadQueued = 12;

	public const int RiderAtAiTarget = 13;

	private static readonly int[] _rider = new int[14];

	private static readonly int[] _riderNoTargetWhy = new int[6];

	private static readonly int[,] _riderKind = new int[4, 5];

	private static readonly int[,] _riderLeftReach = new int[3, 5];

	private static readonly int[,] _riderRightReach = new int[2, 5];

	private static readonly int[] _riderFriend = new int[4];

	private static int _riderFriendLogs;

	private static readonly Dictionary<int, string> _riderTarget = new Dictionary<int, string>();

	public static void Reset()
	{
		_rightHits = (_rightKills = (_rightBlocked = 0));
		_rightDamage = 0f;
		_leftFired = (_leftHits = (_leftKills = (_leftParried = (_leftOnFriend = (_leftSkipped = (_leftHeldForAlly = (_leftMounted = (_leftBlocked = 0))))))));
		_leftDamage = 0f;
		for (int i = 0; i < _guards.Length; i++)
		{
			_guards[i] = 0;
		}
		_recent.Clear();
		_chainsDw = (_chainsOther = (_maxChainDw = (_maxChainOther = (_otherHits = 0))));
		_rightOnFriend = (_otherOnFriend = (_leftFollowUps = (_leftLeads = 0)));
		Array.Clear(_thrownBack, 0, _thrownBack.Length);
		_thrownBackUpright = 0;
		Array.Clear(_npcFootChannel, 0, _npcFootChannel.Length);
		_pLeftFired = (_pLeftMounted = (_pLeftHits = (_pLeftKills = (_pLeftParried = (_pLeftOnFriend = (_pRightHits = (_pLeftBlocked = 0)))))));
		_pLeftDamage = (_pRightDamage = 0f);
		Array.Clear(_rider, 0, _rider.Length);
		Array.Clear(_riderNoTargetWhy, 0, _riderNoTargetWhy.Length);
		Array.Clear(_riderKind, 0, _riderKind.Length);
		Array.Clear(_riderLeftReach, 0, _riderLeftReach.Length);
		Array.Clear(_riderFriend, 0, _riderFriend.Length);
		_riderFriendLogs = 0;
		_riderTarget.Clear();
		Array.Clear(_riderRightReach, 0, _riderRightReach.Length);
		_sinceReport = (_sinceSample = 0f);
	}

	public static bool IsNpc(Agent a)
	{
		if (a != null && !a.IsPlayerControlled)
		{
			return (int)a.Controller != 2;
		}
		return false;
	}

	public static void RightBlocked()
	{
		_rightBlocked++;
	}

	public static void LeftFired(bool followUp, bool lead = false)
	{
		_leftFired++;
		if (followUp)
		{
			_leftFollowUps++;
		}
		if (lead)
		{
			_leftLeads++;
		}
	}

	public static void LeftParried(Agent attacker)
	{
		if (IsNpc(attacker))
		{
			_leftParried++;
		}
		else if (attacker != null)
		{
			_pLeftParried++;
		}
	}

	public static void LeftOnFriend(Agent attacker)
	{
		if (IsNpc(attacker))
		{
			_leftOnFriend++;
			if (attacker.HasMount)
			{
				_rider[11]++;
			}
		}
		else if (attacker != null)
		{
			_pLeftOnFriend++;
		}
	}

	public static void LeftBlocked(Agent attacker)
	{
		if (IsNpc(attacker))
		{
			_leftBlocked++;
		}
		else if (attacker != null)
		{
			_pLeftBlocked++;
		}
	}

	public static void PlayerLeftFired(bool mounted)
	{
		_pLeftFired++;
		if (mounted)
		{
			_pLeftMounted++;
		}
	}

	public static void PlayerHit(bool left, int damage, bool killed)
	{
		if (damage <= 0)
		{
			return;
		}
		if (left)
		{
			_pLeftHits++;
			_pLeftDamage += damage;
			if (killed)
			{
				_pLeftKills++;
			}
		}
		else
		{
			_pRightHits++;
			_pRightDamage += damage;
		}
	}

	public static void LeftSkipped()
	{
		_leftSkipped++;
	}

	public static void LeftHeldForAlly()
	{
		_leftHeldForAlly++;
	}

	public static void LeftMounted()
	{
		_leftMounted++;
	}

	public static void NpcFootStrike(int channel)
	{
		if (channel == 0 || channel == 1)
		{
			_npcFootChannel[channel]++;
		}
	}

	public static void Rider(int gate)
	{
		_rider[gate]++;
	}

	public static void RiderNoTargetWhy(int why)
	{
		if (why >= 0 && why < _riderNoTargetWhy.Length)
		{
			_riderNoTargetWhy[why]++;
		}
	}

	public static void RiderTarget(int agentIndex, float deg, float dist)
	{
		_riderTarget[agentIndex] = string.Format(CultureInfo.InvariantCulture, "{0:0} deg {1:0.0} m", deg, dist);
	}

	public static void RiderStrikeEnded(string action, bool metABody, bool wounded, bool blocked, int friend, string friendWhere, int attackerIndex = -1)
	{
		if (string.IsNullOrEmpty(action))
		{
			return;
		}
		int num = ((!action.Contains("slash_horseback_left")) ? (action.Contains("slash_horseback_right") ? 1 : (action.Contains("thrust") ? 2 : (action.Contains("overswing") ? 3 : (-1)))) : 0);
		if (num < 0)
		{
			return;
		}
		Tally(_riderKind, num, metABody, wounded, blocked, friend);
		if (num == 0)
		{
			Tally(_riderLeftReach, (!action.EndsWith("_lon", StringComparison.Ordinal)) ? (action.EndsWith("_lom", StringComparison.Ordinal) ? 1 : 2) : 0, metABody, wounded, blocked, friend);
		}
		if (num == 1)
		{
			Tally(_riderRightReach, action.EndsWith("_lom", StringComparison.Ordinal) ? 1 : 0, metABody, wounded, blocked, friend);
		}
		if (friend > 0 && friend < _riderFriend.Length)
		{
			_riderFriend[friend]++;
			if (_riderFriendLogs++ < 120)
			{
				DualWieldLog.Log("[Mounted] friend met by " + action.Replace("act_tor_dwc_qck_release_", "") + ": " + friend switch
				{
					2 => "a rider", 
					1 => "a man on foot", 
					_ => "a mount", 
				} + " at " + (friendWhere ?? "?") + " | the strike's target was at " + (_riderTarget.TryGetValue(attackerIndex, out var value) ? value : "?"));
			}
		}
	}

	private static void Tally(int[,] t, int row, bool metABody, bool wounded, bool blocked, int friend)
	{
		t[row, 0]++;
		if (metABody)
		{
			t[row, 1]++;
		}
		if (wounded)
		{
			t[row, 2]++;
		}
		if (blocked)
		{
			t[row, 3]++;
		}
		if (friend > 0)
		{
			t[row, 4]++;
		}
	}

	private static string Outcome(int[,] t, int row)
	{
		return string.Format("{0} of {1} met a body - {2} wounded an enemy, {3} blocked, {4} on a friend", new object[5]
		{
			t[row, 1],
			t[row, 0],
			t[row, 2],
			t[row, 3],
			t[row, 4]
		});
	}

	public static void ThrownBack(Agent attacker, int height, bool upright)
	{
		_thrownBack[(!IsNpc(attacker)) ? 1u : 0u, Math.Max(-2, Math.Min(2, height)) + 2]++;
		if (upright)
		{
			_thrownBackUpright++;
		}
	}

	public static void FriendCollision(bool dualWielder)
	{
		if (dualWielder)
		{
			_rightOnFriend++;
		}
		else
		{
			_otherOnFriend++;
		}
	}

	public static void Hit(bool left, int damage, bool killed)
	{
		if (damage <= 0)
		{
			return;
		}
		if (left)
		{
			_leftHits++;
			_leftDamage += damage;
			if (killed)
			{
				_leftKills++;
			}
		}
		else
		{
			_rightHits++;
			_rightDamage += damage;
			if (killed)
			{
				_rightKills++;
			}
		}
	}

	public static void ChainHit(Agent attacker, Agent victim, bool dualWielder, float now)
	{
		if (attacker == null || victim == null)
		{
			return;
		}
		if (!dualWielder)
		{
			_otherHits++;
		}
		long key = ((long)attacker.Index << 32) | (uint)victim.Index;
		if (!_recent.TryGetValue(key, out var value))
		{
			value = (_recent[key] = new List<float>());
		}
		value.Add(now);
		value.RemoveAll((float t) => t < now - 2f);
		if (dualWielder)
		{
			if (value.Count == 3)
			{
				_chainsDw++;
			}
			if (value.Count > _maxChainDw)
			{
				_maxChainDw = value.Count;
			}
		}
		else
		{
			if (value.Count == 3)
			{
				_chainsOther++;
			}
			if (value.Count > _maxChainOther)
			{
				_maxChainOther = value.Count;
			}
		}
		if (_recent.Count <= 4000)
		{
			return;
		}
		List<long> list2 = new List<long>();
		foreach (KeyValuePair<long, List<float>> item in _recent)
		{
			if (item.Value.Count == 0 || item.Value[item.Value.Count - 1] < now - 2f)
			{
				list2.Add(item.Key);
			}
		}
		foreach (long item2 in list2)
		{
			_recent.Remove(item2);
		}
	}

	public static bool Tick(float dt)
	{
		_sinceReport += dt;
		if (_sinceReport >= 60f)
		{
			_sinceReport = 0f;
			Report("interim");
		}
		_sinceSample += dt;
		if (_sinceSample < 1f)
		{
			return false;
		}
		_sinceSample = 0f;
		return true;
	}

	public static void SampleGuard(string action)
	{
		if (!string.IsNullOrEmpty(action) && action.Contains("guard_"))
		{
			if (action.Contains("guard_up"))
			{
				_guards[0]++;
			}
			else if (action.Contains("guard_bottom"))
			{
				_guards[1]++;
			}
			else if (action.Contains("guard_left"))
			{
				_guards[2]++;
			}
			else if (action.Contains("guard_right"))
			{
				_guards[3]++;
			}
		}
	}

	public static void Report(string when)
	{
		int num = 0;
		int[,] thrownBack = _thrownBack;
		foreach (int num2 in thrownBack)
		{
			num += num2;
		}
		if (_rightHits + _rightBlocked + _leftFired + _otherHits + _rightOnFriend + _otherOnFriend + _guards[0] + _guards[1] + _guards[2] + _guards[3] + num + _pLeftFired + _pRightHits != 0)
		{
			float num3 = 0f;
			float num4 = 1f;
			try
			{
				num3 = DualWieldSettings.Get().NpcOffhandChance;
				num4 = DualWieldSettings.Get().OffHandDamageMultiplier;
			}
			catch
			{
			}
			float whole = _rightDamage + _leftDamage;
			int num5 = Math.Max(0, _leftFired - _leftHits - _leftParried - _leftBlocked - _leftOnFriend);
			DualWieldLog.Log(string.Format("[Balance] {0}: NPC dual wielders | right: {1} hits, {2:0} dmg, {3} kills, {4} blocked", new object[5] { when, _rightHits, _rightDamage, _rightKills, _rightBlocked }) + string.Format(" | left: {0} strikes ({1} counters = {2} of blocked, setting {3:0%}; {4} own follow-ups; {5} leading strikes; {6} over the cap of strikes at once; {7} held back for an ally in every arc; {8} from horseback; on foot {9} on channel 0, {10} on channel 1), {11} hits, {12:0} dmg, {13} kills, {14} parried, {15} blocked, {16} stopped on a friend, {17} missed", new object[18]
			{
				_leftFired,
				_leftFired - _leftFollowUps - _leftLeads,
				Pct(_leftFired - _leftFollowUps - _leftLeads, _rightBlocked),
				num3,
				_leftFollowUps,
				_leftLeads,
				_leftSkipped,
				_leftHeldForAlly,
				_leftMounted,
				_npcFootChannel[0],
				_npcFootChannel[1],
				_leftHits,
				_leftDamage,
				_leftKills,
				_leftParried,
				_leftBlocked,
				_leftOnFriend,
				num5
			}) + $" | left share of damage {Pct(_leftDamage, whole)}, malus {num4:0%}" + string.Format(" | blows on friends (share of blows that met a body): right {0} ({1}), left {2} ({3}), other NPCs {4} ({5})", new object[6]
			{
				_rightOnFriend,
				Pct(_rightOnFriend, _rightOnFriend + _rightHits),
				_leftOnFriend,
				Pct(_leftOnFriend, _leftOnFriend + _leftHits),
				_otherOnFriend,
				Pct(_otherOnFriend, _otherOnFriend + _otherHits)
			}) + string.Format(" | stun chains (>={0} hits on one victim in {1:0} s): dual wielders {2} per 100 hits (longest {3}), other NPCs {4} (longest {5}, {6} hits)", new object[7]
			{
				3,
				2f,
				Per100(_chainsDw, _rightHits + _leftHits),
				_maxChainDw,
				Per100(_chainsOther, _otherHits),
				_maxChainOther,
				_otherHits
			}) + string.Format(" | guards held (s): up {0}, bottom {1}, left {2}, right {3} - left blade guards {4}", new object[5]
			{
				_guards[0],
				_guards[1],
				_guards[2],
				_guards[3],
				Pct(_guards[0] + _guards[1] + _guards[2], _guards[0] + _guards[1] + _guards[2] + _guards[3])
			}) + string.Format(" | left strikes thrown back by height (deep/low/level/high/steep): NPCs {0}/{1}/{2}/{3}/{4}, player {5}/{6}/{7}/{8}/{9}, upright for want of a variant {10}", new object[11]
			{
				_thrownBack[0, 0],
				_thrownBack[0, 1],
				_thrownBack[0, 2],
				_thrownBack[0, 3],
				_thrownBack[0, 4],
				_thrownBack[1, 0],
				_thrownBack[1, 1],
				_thrownBack[1, 2],
				_thrownBack[1, 3],
				_thrownBack[1, 4],
				_thrownBackUpright
			}) + string.Format(" | player: left {0} strikes ({1} from horseback), {2} hits, {3:0} dmg, {4} kills, {5} parried, {6} blocked, {7} stopped on a friend; right {8} hits, {9:0} dmg", new object[10] { _pLeftFired, _pLeftMounted, _pLeftHits, _pLeftDamage, _pLeftKills, _pLeftParried, _pLeftBlocked, _pLeftOnFriend, _pRightHits, _pRightDamage }) + ((_rider[0] + _rider[5] + _rider[12] == 0) ? "" : ($" | riders' left: {_rider[0]} right swings ended - {_rider[1]} busy (cooldown or a strike running), {_rider[2]} no enemy where the blade lands" + string.Format(" (nobody near {0}, out of reach {1}, ahead to the side {2}, behind {3}, under the horse {4}, the AI fights nobody {5}),", new object[6]
			{
				_riderNoTargetWhy[0],
				_riderNoTargetWhy[1],
				_riderNoTargetWhy[2],
				_riderNoTargetWhy[3],
				_riderNoTargetWhy[4],
				_riderNoTargetWhy[5]
			}) + string.Format(" {0} lost the roll, {1} queued, plus {2} counters and {3} leads queued; when due: {4} target gone, {5} held back for an ally, {6} over the cap, {7} struck ({8} at the man the AI fights) - {9} hits, {10} stopped on a friend", new object[11]
			{
				_rider[3],
				_rider[4],
				_rider[5],
				_rider[12],
				_rider[6],
				_rider[7],
				_rider[8],
				_rider[9],
				_rider[13],
				_rider[10],
				_rider[11]
			}) + "; by strike: slash to the left " + Outcome(_riderKind, 0) + " [close " + Outcome(_riderLeftReach, 0) + "; middle " + Outcome(_riderLeftReach, 1) + "; far " + Outcome(_riderLeftReach, 2) + "]; slash to the right " + Outcome(_riderKind, 1) + " [close " + Outcome(_riderRightReach, 0) + "; further out " + Outcome(_riderRightReach, 1) + "]; thrust " + Outcome(_riderKind, 2) + "; overswing " + Outcome(_riderKind, 3) + $"; the friends met: {_riderFriend[1]} men on foot, {_riderFriend[2]} riders, {_riderFriend[3]} mounts")));
		}
	}

	private static string Pct(float part, float whole)
	{
		if (!(whole > 0f))
		{
			return "-";
		}
		return (part / whole).ToString("0%");
	}

	private static string Per100(int count, int hits)
	{
		if (hits <= 0)
		{
			return "-";
		}
		return (100f * (float)count / (float)hits).ToString("0.0");
	}
}
