// Vendored from Yotthani's DualWield-v0.10.9 mod (permission granted
// 2026-11-24). Full port, ilspycmd decompile artifacts hand-fixed.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using TaleWorlds.Core;
using TaleWorlds.DotNet;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;

namespace TOR_Core.BattleMechanics.DualWield;

public sealed class LeftBladeStrike
{
	private struct GuardLine
	{
		public Vec3 Base;

		public Vec3 Tip;

		public sbyte Bone;
	}

	public const sbyte LH_WEAPON_BONE = 20;

	private const float MIN_TIP_SPEED = 3.5f;

	private const float MAX_DURATION = 1.4f;

	private Agent _attacker;

	private EquipmentIndex _slot;

	private bool _thrust;

	private string _actionName;

	private Agent.UsageDirection _direction;

	private float _elapsed;

	private bool _hasPrev;

	private Vec3 _prevBase;

	private Vec3 _prevTip;

	private float _maxTipSpeed;

	private MatrixFrame _prevEntitial;

	private bool _hasPrevEntitial;

	private int _staleTicks;

	private int _staleFixed;

	private static int _staleTicksAll;

	private static int _staleFixedAll;

	private static int _staleStrikesAll;

	private static int _strikesAll;

	private float _prevProgress;

	private CombatWindows.Window _window;

	private bool _probed;

	private readonly HashSet<int> _hitThisStrike = new HashSet<int>();

	private bool _woundedEnemy;

	private bool _wasBlocked;

	private bool _riderStrike;

	private int _friendMet;

	private string _friendWhere;

	private bool _spent;

	private float _lastMomentum;

	private float _stunUntil = -1f;

	private const float FRIEND_STUN = 0.4f;

	private static readonly Dictionary<string, float> _reachByItem = new Dictionary<string, float>();

	private const float GUARD_CONTACT = 0.1f;

	private const float GUARD_RADIUS = 3f;

	private const float LIMB_RADIUS = 4f;

	private const sbyte RH_WEAPON_BONE = 27;

	private readonly MBList<Agent> _nearby = new MBList<Agent>();

	private readonly List<Agent> _guards = new List<Agent>();

	private readonly List<Vec3> _guardBase = new List<Vec3>();

	private readonly List<Vec3> _guardTip = new List<Vec3>();

	private readonly List<sbyte> _guardBone = new List<sbyte>();

	private static readonly GuardLine[] NoGuard = new GuardLine[0];

	private static readonly Dictionary<int, GuardLine[]> _guardCache = new Dictionary<int, GuardLine[]>();

	private static float _guardCacheTime = -1f;

	private static readonly List<GuardLine> _lineBuf = new List<GuardLine>(2);

	private const float PRE_WINDOW = 0.08f;

	private const int SUB_STEPS = 8;

	private const int LAST_RAY = 17;

	private const int NPC_SUB_STEPS = 2;

	private int _steps = 8;

	private int _lastRay = 17;

	private bool _sweptLast;

	private static int _diagBudget = 120;

	private static readonly MBList<Agent> _diagNear = new MBList<Agent>();

	private bool _diag;

	private int _diagWindowTicks;

	private int _diagSweeps;

	private float _diagBest;

	private float _diagProgress;

	private float _diagBaseZ;

	private float _diagTipZ;

	private float _diagFeetZ;

	private float _diagEyesZ;

	private int _limbMode;

	private int _actIndex = -1;

	private int _channel;

	private const float SCENE_HOLD_TIME = 0.06f;

	private float _sceneHoldLeft;

	private float _sceneHoldProgress;

	private bool _holdChecked;

	private static readonly Dictionary<string, int> _soundIds = new Dictionary<string, int>();

	private const float CANDIDATE_MAN = 2f;

	private const float CANDIDATE_MOUNT = 3f;

	private const float HIT_STOP_SPEED = 0.35f;

	private const float HIT_STOP_TIME = 0.07f;

	private float _hitStopLeft;

	private static readonly Random _reactRng = new Random();

	private static int _bloodEnter = -2;

	private static int _sweatEnter = -2;

	private static MethodInfo _meleeHitCallback;

	private static MethodInfo _defendResults;

	private static bool _defendResultsMissing;

	private static int _defendResultsErrors;

	public float WindowEnd => _window.End;

	public bool Active => _attacker != null;

	public bool Stunned
	{
		get
		{
			Mission current = Mission.Current;
			if (current != null)
			{
				return current.CurrentTime < _stunUntil;
			}
			return false;
		}
	}

	private bool Chatty
	{
		get
		{
			if (!DualWieldLog.Unlimited)
			{
				if (_attacker != null)
				{
					return _attacker.IsMainAgent;
				}
				return false;
			}
			return true;
		}
	}

	public static bool ApplyingLeftHit { get; private set; }

	public void Start(Agent attacker, EquipmentIndex offSlot, string clipName, Agent.UsageDirection aimDirection, int channel = 0)
	{
		_attacker = attacker;
		_slot = offSlot;
		bool flag = !attacker.IsMainAgent;
		_steps = (flag ? 2 : 8);
		_lastRay = (flag ? 3 : 17);
		string text = (_actionName = clipName ?? "");
		ActionIndexCache val = ActionCache.Get(text);
		_actIndex = val.Index;
		_thrust = text.Contains("thrust") || text.Contains("backstab");
		_direction = aimDirection;
		_spent = false;
		if (_attacker != null && _sceneHoldLeft > 0f)
		{
			try
			{
				_attacker.SetCurrentActionSpeed(_channel, 1f);
			}
			catch
			{
			}
		}
		_sceneHoldLeft = 0f;
		_channel = channel;
		try
		{
			_limbMode = DualWieldSettings.Get().LimbRayMode;
		}
		catch
		{
			_limbMode = 0;
		}
		_diag = attacker.HasMount && _diagBudget > 0;
		if (_diag)
		{
			_diagBudget--;
			_diagBest = 99f;
			_diagWindowTicks = 0;
			_diagSweeps = 0;
			_diagProgress = 0f;
		}
		_hitStopLeft = 0f;
		_elapsed = 0f;
		_hasPrev = false;
		_sweptLast = false;
		_maxTipSpeed = 0f;
		_hasPrevEntitial = false;
		_staleTicks = 0;
		_staleFixed = 0;
		_prevProgress = 0f;
		_window = CombatWindows.For(text);
		_probed = false;
		_hitThisStrike.Clear();
		_woundedEnemy = (_wasBlocked = false);
		_friendMet = 0;
		_friendWhere = null;
		_riderStrike = attacker.HasMount && !attacker.IsMainAgent;
	}

	public void Stop(string why)
	{
		if (_attacker != null && (_hitStopLeft > 0f || _sceneHoldLeft > 0f))
		{
			_hitStopLeft = 0f;
			_sceneHoldLeft = 0f;
			try
			{
				_attacker.SetCurrentActionSpeed(_channel, 1f);
			}
			catch
			{
			}
		}
		if (_attacker != null && DualWieldLog.Unlimited)
		{
			DualWieldLog.Log($"[LH-Blade] strike end ({why}): max tip speed {_maxTipSpeed:0.0} m/s, hits {_hitThisStrike.Count}");
		}
		if (_attacker != null && _diag)
		{
			_diag = false;
			DualWieldLog.Log(string.Format(CultureInfo.InvariantCulture, "[Mounted] {0} {1} end ({2}) after {3:0.00} s at progress {4:0.00} (window {5:0.00}-{6:0.00}): contacts {7}, window ticks {8} (swept {9}), tip max {10:0.0} m/s, stale ticks {17} (forced update moved {18}) | closest enemy to the blade {11} at progress {12:0.00}: blade z {13:0.00}..{14:0.00}, enemy feet {15:0.00} eyes {16:0.00}", new object[19]
			{
				_attacker.IsMainAgent ? "player" : "npc",
				_actionName.Replace("act_dwc_quick_release_", ""),
				why,
				_elapsed,
				_prevProgress,
				_window.Start,
				_window.End,
				_hitThisStrike.Count,
				_diagWindowTicks,
				_diagSweeps,
				_maxTipSpeed,
				(_diagBest < 90f) ? (_diagBest.ToString("0.00", CultureInfo.InvariantCulture) + " m") : "none within 3 m",
				_diagProgress,
				_diagBaseZ,
				_diagTipZ,
				_diagFeetZ,
				_diagEyesZ,
				_staleTicks,
				_staleFixed
			}));
		}
		if (_attacker != null)
		{
			_strikesAll++;
			if (_staleTicks > 0)
			{
				_staleStrikesAll++;
			}
			_staleTicksAll += _staleTicks;
			_staleFixedAll += _staleFixed;
		}
		if (_attacker != null && _riderStrike)
		{
			DualWieldBalance.RiderStrikeEnded(_actionName, _hitThisStrike.Count > 0, _woundedEnemy, _wasBlocked || why == "parried", _friendMet, _friendWhere, _attacker.Index);
		}
		_attacker = null;
	}

	public void Tick(float dt)
	{
		if (_attacker == null)
		{
			return;
		}
		if (!_attacker.IsActive() || Mission.Current == null)
		{
			Stop("attacker gone");
			return;
		}
		_elapsed += dt;
		if (_sceneHoldLeft > 0f)
		{
			_sceneHoldLeft -= dt;
			if (_sceneHoldLeft > 0f)
			{
				if (!_holdChecked && _sceneHoldLeft < 0.03f)
				{
					_holdChecked = true;
					DualWieldLog.Log($"[LH-Blade] hold check: contact {_sceneHoldProgress:0.000}, now {CurrentProgress():0.000} mid-hold");
				}
				return;
			}
			try
			{
				_attacker.SetCurrentActionSpeed(_channel, 1f);
			}
			catch
			{
			}
			PlayBlockedReaction("scene", _sceneHoldProgress, 0.1f);
			Stop("scene");
			return;
		}
		if (_hitStopLeft > 0f)
		{
			_hitStopLeft -= dt;
			if (_hitStopLeft <= 0f)
			{
				try
				{
					_attacker.SetCurrentActionSpeed(_channel, 1f);
				}
				catch
				{
				}
			}
		}
		if (_spent)
		{
			if (_hitStopLeft <= 0f)
			{
				Stop("hit");
			}
			return;
		}
		if (_actIndex >= 0 && _elapsed > 0.1f)
		{
			int num = _actIndex;
			try
			{
				ActionIndexCache currentAction = _attacker.GetCurrentAction(_channel);
				num = currentAction.Index;
			}
			catch
			{
			}
			if (num != _actIndex)
			{
				Stop("interrupted");
				return;
			}
		}
		if (_elapsed > 1.4f)
		{
			Stop("time");
			return;
		}
		float num2 = CurrentProgress();
		if (num2 < _window.Start - 0.08f || (num2 > _window.End && _prevProgress > _window.End))
		{
			_hasPrev = false;
			_sweptLast = false;
			_prevProgress = num2;
			return;
		}
		long started = DwPerf.Start();
		MissionWeapon weapon = _attacker.Equipment[_slot];
		WeaponComponentData currentUsageItem = weapon.CurrentUsageItem;
		MBAgentVisuals agentVisuals = _attacker.AgentVisuals;
		Skeleton val = ((agentVisuals != null) ? agentVisuals.GetSkeleton() : null);
		if (weapon.IsEmpty || currentUsageItem == null || (NativeObject)(object)val == (NativeObject)null)
		{
			DwPerf.Stop(8, started);
			Stop("no weapon");
			return;
		}
		MatrixFrame a = val.GetBoneEntitialFrame((sbyte)20);
		bool flag = _hitStopLeft > 0f || _sceneHoldLeft > 0f;
		if (flag)
		{
			_hasPrevEntitial = false;
		}
		else if (_hasPrevEntitial && SameFrame(in a, in _prevEntitial))
		{
			_staleTicks++;
			try
			{
				val.ForceUpdateBoneFrames();
				MatrixFrame a2 = val.GetBoneEntitialFrame((sbyte)20);
				if (!SameFrame(in a2, in a))
				{
					_staleFixed++;
					a = a2;
				}
			}
			catch
			{
			}
		}
		if (!flag)
		{
			_prevEntitial = a;
			_hasPrevEntitial = true;
		}
		MatrixFrame globalFrame = agentVisuals.GetGlobalFrame();
		MatrixFrame val2 = globalFrame.TransformToParent(ref a);
		Vec3 val3 = BladeAxis(val2, currentUsageItem);
		val3.Normalize();
		float num3 = WeaponReach(weapon, currentUsageItem);
		Vec3 origin = val2.origin;
		Vec3 val4 = origin + val3 * num3;
		DwPerf.Stop(8, started);
		if (!_hasPrev || dt <= 0f)
		{
			_prevBase = origin;
			_prevTip = val4;
			_hasPrev = true;
			_prevProgress = num2;
			return;
		}
		Vec3 val5 = val4 - _prevTip;
		float num4 = val5.Length / dt;
		if (num4 > _maxTipSpeed)
		{
			_maxTipSpeed = num4;
		}
		if (!_probed && num4 > 8f)
		{
			_probed = true;
			LogProgressSources("fast tick");
		}
		float num5 = num2;
		bool flag2 = _window.Contains(num5) || _window.Contains(_prevProgress);
		if (_diag & flag2)
		{
			DiagClosestEnemy(origin, val4, num5, num4 >= 3.5f);
		}
		if (flag2 && num4 >= 3.5f)
		{
			long started2 = DwPerf.Start();
			Sweep(weapon, currentUsageItem, origin, val4, num4, num3);
			DwPerf.Stop(10, started2);
			_sweptLast = _attacker != null;
		}
		else
		{
			_sweptLast = false;
		}
		_prevBase = origin;
		_prevTip = val4;
		_prevProgress = num5;
	}

	private static bool SameFrame(in MatrixFrame a, in MatrixFrame b)
	{
		if (a.origin.x == b.origin.x && a.origin.y == b.origin.y && a.origin.z == b.origin.z && a.rotation.u.x == b.rotation.u.x && a.rotation.u.y == b.rotation.u.y && a.rotation.u.z == b.rotation.u.z && a.rotation.s.x == b.rotation.s.x)
		{
			return a.rotation.s.y == b.rotation.s.y;
		}
		return false;
	}

	public static string TakeStaleSummary()
	{
		if (_strikesAll == 0)
		{
			return null;
		}
		string result = $"[LH-Blade] stale bone poses (skeleton not updated, off the screen): {_staleStrikesAll} of {_strikesAll} left strikes, " + $"{_staleTicksAll} ticks - the forced update moved the bone in {_staleFixedAll}";
		_staleTicksAll = (_staleFixedAll = (_staleStrikesAll = (_strikesAll = 0)));
		return result;
	}

	internal static float WeaponReach(MissionWeapon weapon, WeaponComponentData usage)
	{
		float num = Math.Max(0.2f, usage.GetRealWeaponLength());
		ItemObject item = weapon.Item;
		if (item == null || string.IsNullOrEmpty(item.SkeletonName) || string.IsNullOrEmpty(item.MultiMeshName))
		{
			return num;
		}
		if (_reachByItem.TryGetValue(((MBObjectBase)item).StringId, out var value))
		{
			return value;
		}
		value = num;
		try
		{
			MetaMesh copy = MetaMesh.GetCopy(item.MultiMeshName, false, true);
			if ((NativeObject)(object)copy != (NativeObject)null && copy.IsValid)
			{
				BoundingBox boundingBox = copy.GetBoundingBox();
				Vec3 val = boundingBox.max - boundingBox.min;
				float num2 = MathF.Max(val.x, MathF.Max(val.y, val.z));
				if (num2 > num)
				{
					value = num2;
				}
				DualWieldLog.Log($"[LH-Reach] '{((MBObjectBase)item).StringId}' hangs on '{item.SkeletonName}': declared {num:0.00} m, " + string.Format("mesh '{0}' box {1:0.00} x {2:0.00} x {3:0.00} -> reach {4:0.00} m", new object[5] { item.MultiMeshName, val.x, val.y, val.z, value }));
			}
			else
			{
				DualWieldLog.Log($"[LH-Reach] '{((MBObjectBase)item).StringId}': mesh '{item.MultiMeshName}' not loadable, keeping {num:0.00} m");
			}
		}
		catch (Exception ex)
		{
			DualWieldLog.Log("[LH-Reach] mesh measurement failed for '" + ((MBObjectBase)item).StringId + "': " + ex.Message);
		}
		_reachByItem[((MBObjectBase)item).StringId] = value;
		return value;
	}

	private void CollectGuards()
	{
		long started = DwPerf.Start();
		_guards.Clear();
		_guardBase.Clear();
		_guardTip.Clear();
		_guardBone.Clear();
		try
		{
			((List<Agent>)(object)_nearby).Clear();
			Mission current = Mission.Current;
			Vec3 position = _attacker.Position;
			current.GetNearbyAgents(position.AsVec2, (_limbMode != 0) ? 4f : 3f, _nearby);
			foreach (Agent item in (List<Agent>)(object)_nearby)
			{
				if (item != null && item.IsHuman && IsTarget(item))
				{
					GuardLine[] array = GuardLinesOf(item);
					for (int i = 0; i < array.Length; i++)
					{
						GuardLine guardLine = array[i];
						_guards.Add(item);
						_guardBase.Add(guardLine.Base);
						_guardTip.Add(guardLine.Tip);
						_guardBone.Add(guardLine.Bone);
					}
				}
			}
		}
		catch (Exception ex)
		{
			_guards.Clear();
			DualWieldLog.Log("[LH-Blade] guard collect failed: " + ex.Message);
		}
		DwPerf.Stop(9, started);
	}

	private static GuardLine[] GuardLinesOf(Agent a)
	{
		float currentTime = Mission.Current.CurrentTime;
		if (currentTime != _guardCacheTime)
		{
			_guardCache.Clear();
			_guardCacheTime = currentTime;
		}
		if (_guardCache.TryGetValue(a.Index, out var value))
		{
			return value;
		}
		GuardLine[] array = NoGuard;
		Agent.ActionCodeType currentActionType = a.GetCurrentActionType(1);
		if ((int)currentActionType >= 1 && (int)currentActionType <= 14 && (int)currentActionType != 2)
		{
			MissionWeapon wieldedWeapon = a.WieldedWeapon;
			MBAgentVisuals agentVisuals = a.AgentVisuals;
			Skeleton val = ((agentVisuals != null) ? agentVisuals.GetSkeleton() : null);
			if (!wieldedWeapon.IsEmpty && wieldedWeapon.CurrentUsageItem != null && (NativeObject)(object)val != (NativeObject)null)
			{
				_lineBuf.Clear();
				AddGuardLine(a, val, 27, wieldedWeapon);
				EquipmentIndex offhandWieldedItemIndex = a.GetOffhandWieldedItemIndex();
				if ((int)offhandWieldedItemIndex != -1)
				{
					MissionWeapon held = a.Equipment[offhandWieldedItemIndex];
					if (!held.IsEmpty && held.CurrentUsageItem != null && held.CurrentUsageItem.IsMeleeWeapon && !held.CurrentUsageItem.IsShield)
					{
						AddGuardLine(a, val, 20, held);
					}
				}
				if (_lineBuf.Count > 0)
				{
					array = _lineBuf.ToArray();
				}
			}
		}
		_guardCache[a.Index] = array;
		return array;
	}

	private static Vec3 BladeAxis(MatrixFrame weaponBone, WeaponComponentData usage)
	{
		if (usage == null || (int)(usage.WeaponFlags & (WeaponFlags)0x200000000L) == 0)
		{
			return weaponBone.rotation.u;
		}
		return -weaponBone.rotation.u;
	}

	private static void AddGuardLine(Agent a, Skeleton sk, sbyte bone, MissionWeapon held)
	{
		MatrixFrame globalFrame = a.AgentVisuals.GetGlobalFrame();
		MatrixFrame boneEntitialFrame = sk.GetBoneEntitialFrame(bone);
		MatrixFrame val = globalFrame.TransformToParent(ref boneEntitialFrame);
		Vec3 val2 = BladeAxis(val, held.CurrentUsageItem);
		if (!(val2.Normalize() < 0.0001f))
		{
			_lineBuf.Add(new GuardLine
			{
				Base = val.origin,
				Tip = val.origin + val2 * WeaponReach(held, held.CurrentUsageItem),
				Bone = bone
			});
		}
	}

	private static float SegmentDistance(Vec3 p0, Vec3 p1, Vec3 q0, Vec3 q1, out Vec3 onP)
	{
		Vec3 val = p1 - p0;
		Vec3 val2 = q1 - q0;
		Vec3 val3 = p0 - q0;
		float num = Vec3.DotProduct(val, val);
		float num2 = Vec3.DotProduct(val2, val2);
		float num3 = Vec3.DotProduct(val2, val3);
		if (num < 1E-08f && num2 < 1E-08f)
		{
			onP = p0;
			return val3.Length;
		}
		float num4;
		float num5;
		if (num < 1E-08f)
		{
			num4 = 0f;
			num5 = MathF.Clamp(num3 / num2, 0f, 1f);
		}
		else
		{
			float num6 = Vec3.DotProduct(val, val3);
			if (num2 < 1E-08f)
			{
				num5 = 0f;
				num4 = MathF.Clamp((0f - num6) / num, 0f, 1f);
			}
			else
			{
				float num7 = Vec3.DotProduct(val, val2);
				float num8 = num * num2 - num7 * num7;
				num4 = ((num8 > 1E-08f) ? MathF.Clamp((num7 * num3 - num6 * num2) / num8, 0f, 1f) : 0f);
				num5 = (num7 * num4 + num3) / num2;
				if (num5 < 0f)
				{
					num5 = 0f;
					num4 = MathF.Clamp((0f - num6) / num, 0f, 1f);
				}
				else if (num5 > 1f)
				{
					num5 = 1f;
					num4 = MathF.Clamp((num7 - num6) / num, 0f, 1f);
				}
			}
		}
		onP = p0 + val * num4;
		Vec3 val4 = q0 + val2 * num5;
		Vec3 val5 = onP - val4;
		return val5.Length;
	}

	private void Sweep(MissionWeapon weapon, WeaponComponentData usage, Vec3 bas, Vec3 tip, float tipSpeed, float length)
	{
		Mission current = Mission.Current;
		CollectGuards();
		float num4 = default;
		sbyte b = default;
		float num6 = default;
		Vec3 point2 = default;
		WeakGameEntity entity = default;
		for (int i = (_sweptLast ? 1 : 0); i <= _lastRay; i++)
		{
			bool flag = i > _steps + 1;
			float num;
			if (flag)
			{
				num = 1f + (float)(i - _steps - 1) / (float)_steps;
			}
			else
			{
				num = ((i <= _steps) ? ((float)i / (float)_steps) : 1f);
			}
			Vec3 val;
			Vec3 val2;
			if ((i <= _steps) | flag)
			{
				val = Vec3.Lerp(_prevBase, bas, num);
				val2 = Vec3.Lerp(_prevTip, tip, num);
				if (flag)
				{
					Vec3 val3 = val2 - val;
					if (val3.Normalize() > 0.0001f)
					{
						val2 = val + val3 * length;
					}
				}
			}
			else
			{
				val = _prevTip;
				val2 = tip;
			}
			Vec3 val4 = val2 - val;
			float length2 = val4.Length;
			if (length2 < 0.001f)
			{
				continue;
			}
			Vec3 val6;
			if (!flag && i <= _steps && _guards.Count > 0)
			{
				for (int j = 0; j < _guards.Count; j++)
				{
					Agent val5 = _guards[j];
					if (_hitThisStrike.Contains(val5.Index))
					{
						continue;
					}
					float num2 = SegmentDistance(val, val2, _guardBase[j], _guardTip[j], out var onP);
					if (!(num2 > 0.1f))
					{
						_hitThisStrike.Add(val5.Index);
						val6 = onP - val;
						float along = MathF.Min(val6.Length, length);
						if (Chatty)
						{
							DualWieldLog.Log(string.Format("[LH-Blade] PARRIED by {0}'s {1} weapon ({2}), gap {3:0.00} m, tip {4:0.0} m/s", new object[5]
							{
								val5.Name,
								(_guardBone[j] == 20) ? "LEFT" : "right",
								val5.GetCurrentActionType(1),
								num2,
								tipSpeed
							}));
						}
						DualWieldBalance.LeftParried(_attacker);
						ApplyHit(val5, weapon, usage, _guardBone[j], onP, along, tipSpeed, tip - _prevTip, blocked: true, withShield: false, out var progress);
						PlayBlockedReaction("parried", progress);
						Stop("parried");
						return;
					}
				}
			}
			float dist = 0f;
			sbyte bone = -1;
			Agent val7 = null;
			if (!flag)
			{
				long started = DwPerf.Start();
				if (_limbMode == 2 && OneAgentLimbRay.Available)
				{
					val7 = CastAtCandidates(val, val2, length2, out dist, out bone);
				}
				else
				{
					val7 = current.RayCastForClosestAgentsLimbs(val, val2, _attacker.Index, 0.03f, out dist, out bone);
					if (val7 != null && val7 == _attacker.MountAgent && dist < length2)
					{
						float num3 = dist + 0.02f;
						val7 = current.RayCastForClosestAgentsLimbs(val + val4 * (num3 / length2), val2, val7.Index, 0.03f, out num4, out b);
						dist = num3 + num4;
						bone = b;
					}
					if (_limbMode == 1 && OneAgentLimbRay.Available)
					{
						Agent val8 = CastAtCandidates(val, val2, length2, out var dist2, out var bone2);
						bool flag2 = val7 != null && dist <= length2 + 0.03f && bone >= 0;
						bool engineWasCandidate = flag2 && (((List<Agent>)(object)_nearby).Contains(val7) || (!val7.IsHuman && val7.RiderAgent != null && ((List<Agent>)(object)_nearby).Contains(val7.RiderAgent)));
						float num5;
						if (!flag2)
						{
							num5 = 0f;
						}
						else
						{
							val6 = val7.Position - _attacker.Position;
							num5 = val6.Length;
						}
						float engineOrigin = num5;
						OneAgentLimbRay.Compare(flag2 ? val7 : null, dist, bone, flag2 ? LimbKind(val7) : (-1), val8, dist2, bone2, LimbKind(val8), length2, engineWasCandidate, engineOrigin);
					}
				}
				DwPerf.Stop(13, started);
			}
			if (val7 != null && dist <= length2 + 0.03f && bone >= 0 && IsTarget(val7))
			{
				_hitThisStrike.Add(val7.Index);
				Vec3 point = val + val4 * (MathF.Min(dist, length2) / length2);
				float along2 = ((i <= _steps) ? MathF.Min(dist, length) : length);
				long started2 = DwPerf.Start();
				bool flag3 = IsBlocked(val7, out var withShield);
				if (!flag3)
				{
					flag3 = AiReactionBlock(val7, out withShield);
				}
				DwPerf.Stop(15, started2);
				if (flag3)
				{
					DualWieldBalance.LeftBlocked(_attacker);
					if (Chatty)
					{
						DualWieldLog.Log(string.Format("[LH-Blade] BLOCKED by {0} ({1}{2}), tip {3:0.0} m/s", new object[4]
						{
							val7.Name,
							val7.GetCurrentActionType(1),
							withShield ? ", shield" : "",
							tipSpeed
						}));
					}
					if (DualWieldSettings.Get().DebugMode)
					{
						InformationManager.DisplayMessage(new InformationMessage("[DW] left blocked", Colors.Yellow));
					}
				}
				MeleeCollisionReaction val9 = ApplyHit(val7, weapon, usage, bone, point, along2, tipSpeed, tip - _prevTip, flag3, withShield, out var progress2);
				if (flag3 || (int)val9 == 3 || (int)val9 == 2)
				{
					started2 = DwPerf.Start();
					PlayBlockedReaction(flag3 ? "blocked" : ((object)val9/*cast due to constrained. prefix*/).ToString(), progress2);
					Stop(flag3 ? "blocked" : ("weapon " + ((object)val9/*cast due to constrained. prefix*/).ToString()));
					DwPerf.Stop(15, started2);
					break;
				}
				started2 = DwPerf.Start();
				StartHitStop();
				DwPerf.Stop(15, started2);
				if ((int)val9 != 0 || _lastMomentum <= 0f)
				{
					_spent = true;
					break;
				}
			}
			else if (_steps != 2 || i == _steps + 1)
			{
				long started3 = DwPerf.Start();
				bool flag4 = current.Scene.RayCastForClosestEntityOrTerrain(val, val2, out num6, out point2, out entity, 0.02f, (BodyFlags)540127625);
				DwPerf.Stop(14, started3);
				if (flag4 && num6 <= length2 && !PassesThrough(entity))
				{
					float contactTime;
					if (flag)
					{
						contactTime = 1f;
					}
					else
					{
						contactTime = ((i <= _steps) ? num : MathF.Min(num6 / length2, 1f));
					}
					LogProgressSources("scene contact");
					StartSceneHold(point2, val4, entity, num6, length2, i, tipSpeed, contactTime);
					break;
				}
			}
		}
	}

	private void DiagClosestEnemy(Vec3 bas, Vec3 tip, float progress, bool swept)
	{
		try
		{
			_diagWindowTicks++;
			if (swept)
			{
				_diagSweeps++;
			}
			Vec3 val = (bas + tip) * 0.5f;
			((List<Agent>)(object)_diagNear).Clear();
			Mission.Current.GetNearbyAgents(val.AsVec2, 3f, _diagNear);
			foreach (Agent item in (List<Agent>)(object)_diagNear)
			{
				if (item != _attacker && item.IsActive() && item.IsHuman && _attacker.IsEnemyOf(item))
				{
					Vec3 position = item.Position;
					Vec2 val2 = position.AsVec2 - val.AsVec2;
					float length = val2.Length;
					if (!(length >= _diagBest))
					{
						_diagBest = length;
						_diagProgress = progress;
						_diagBaseZ = bas.z;
						_diagTipZ = tip.z;
						_diagFeetZ = item.Position.z;
						_diagEyesZ = item.GetEyeGlobalPosition().z;
					}
				}
			}
		}
		catch
		{
		}
	}

	private static bool PassesThrough(WeakGameEntity entity)
	{
		try
		{
			if (!entity.IsValid)
			{
				return false;
			}
			PhysicsMaterial physicsMaterial = entity.GetPhysicsMaterial();
			return physicsMaterial.IsValid && (physicsMaterial.GetFlags() & (PhysicsMaterialFlags)8) > 0;
		}
		catch
		{
			return false;
		}
	}

	private void StartSceneHold(Vec3 point, Vec3 ray, WeakGameEntity entity, float dist, float rayLen, int ri, float tipSpeed, float contactTime)
	{
		Mission current = Mission.Current;
		_sceneHoldProgress = MathF.Clamp(_prevProgress + (CurrentProgress() - _prevProgress) * contactTime, _window.Start, _window.End);
		string text = (entity.IsValid ? (entity.Name ?? "") : "");
		string text2 = MaterialAt(point, entity, text);
		DualWieldLog.Log(string.Format("[LH-Blade] SCENE hit {0} [{1}] at {2:0.00}/{3:0.00} m (ray {4}/{5}{6}), tip {7:0.0} m/s, now {8:0.000} -> freeze {9:0.000}", new object[10]
		{
			(text.Length > 0) ? text : "(no entity)",
			text2,
			dist,
			rayLen,
			ri,
			_lastRay,
			(ri > _steps + 1) ? " look-ahead" : "",
			tipSpeed,
			CurrentProgress(),
			_sceneHoldProgress
		}));
		try
		{
			MissionWeapon val = _attacker.Equipment[_slot];
			int num = ImpactSound(val.CurrentUsageItem, text2);
			if (num >= 0)
			{
				current.MakeSound(num, point, true, false, -1, -1);
			}
			else
			{
				DualWieldLog.Log("[LH-Blade] no impact sound for surface '" + text2 + "'");
			}
			string text3;
			if (text2.Contains("wood"))
			{
				text3 = "psys_game_metal_wood_coll";
			}
			else if (text2.Contains("stone") || text2.Contains("rock"))
			{
				text3 = "psys_game_metal_stone_coll";
			}
			else
			{
				text3 = (text2.Contains("metal") ? "psys_game_metal_metal_coll" : null);
			}
			if (text3 != null)
			{
				Vec3 f = -ray;
				if (f.Normalize() < 0.0001f)
				{
					f = Vec3.Up;
				}
				Mat3 identity = Mat3.Identity;
				identity.f = f;
				identity.Orthonormalize();
				current.AddParticleSystemBurstByName(text3, new MatrixFrame(ref identity, ref point), false);
			}
			if (!string.IsNullOrEmpty(_actionName) && CurrentProgress() - _sceneHoldProgress > 0.002f)
			{
				ActionIndexCache val2 = ActionCache.Get(_actionName);
				if (val2.Index >= 0)
				{
					_attacker.SetActionChannel(_channel, ref val2, true, (AnimFlags)33, 0f, 1f, 0f, 0.4f, _sceneHoldProgress, false, -0.2f, 0, true);
				}
			}
			_attacker.SetCurrentActionSpeed(_channel, 0f);
		}
		catch (Exception ex)
		{
			DualWieldLog.Log("[LH-Blade] scene impact failed: " + ex.Message);
		}
		_hitStopLeft = 0f;
		_holdChecked = false;
		_sceneHoldLeft = 0.06f;
	}

	private string MaterialAt(Vec3 point, WeakGameEntity entity, string name)
	{
		try
		{
			if (entity.IsValid)
			{
				PhysicsMaterial physicsMaterial = entity.GetPhysicsMaterial();
				if (physicsMaterial.IsValid && !string.IsNullOrEmpty(physicsMaterial.Name))
				{
					return physicsMaterial.Name.ToLowerInvariant();
				}
			}
		}
		catch
		{
		}
		try
		{
			if (point.z < Mission.Current.Scene.GetTerrainHeight(point.AsVec2, true) + 0.1f)
			{
				return "soil";
			}
		}
		catch
		{
		}
		string text = (name ?? "").ToLowerInvariant();
		if (text.Contains("stone") || text.Contains("rock") || text.Contains("wall") || text.Contains("brick"))
		{
			return "stone";
		}
		return "wood";
	}

	private static int EventId(string path)
	{
		if (_soundIds.TryGetValue(path, out var value))
		{
			return value;
		}
		try
		{
			value = SoundEvent.GetEventIdFromString(path);
		}
		catch
		{
			value = -1;
		}
		_soundIds[path] = value;
		return value;
	}

	private static int ImpactSound(WeaponComponentData usage, string surface)
	{
		string text = ((usage != null && !string.IsNullOrEmpty(usage.PhysicsMaterial)) ? usage.PhysicsMaterial.ToLowerInvariant() : "metal_weapon");
		int num = EventId("event:/mission/combat/impact/" + text + "/" + surface);
		if (num < 0)
		{
			num = EventId("event:/mission/combat/impact/" + text + "/default");
		}
		if (num < 0)
		{
			num = EventId("event:/mission/combat/impact/metal_weapon/" + surface);
		}
		if (num < 0)
		{
			num = EventId("event:/mission/combat/impact/metal_weapon/default");
		}
		return num;
	}

	private Agent CastAtCandidates(Vec3 from, Vec3 to, float rayLen, out float dist, out sbyte bone)
	{
		dist = float.MaxValue;
		bone = -1;
		Agent best = null;
		Agent mountAgent = _attacker.MountAgent;
		for (int i = 0; i < ((List<Agent>)(object)_nearby).Count; i++)
		{
			Agent val = ((List<Agent>)(object)_nearby)[i];
			if (val != null && (int)val.State != 5)
			{
				if (val != _attacker)
				{
					CastAtCandidate(val, 2f, from, to, rayLen, ref best, ref dist, ref bone);
				}
				Agent mountAgent2 = val.MountAgent;
				if (mountAgent2 != null && mountAgent2 != mountAgent && (int)mountAgent2.State != 5)
				{
					CastAtCandidate(mountAgent2, 3f, from, to, rayLen, ref best, ref dist, ref bone);
				}
			}
		}
		return best;
	}

	private void CastAtCandidate(Agent a, float within, Vec3 from, Vec3 to, float rayLen, ref Agent best, ref float dist, ref sbyte bone)
	{
		float num = OriginToRay(a.Position, from, to);
		if (!(num > within))
		{
			MBAgentVisuals agentVisuals = a.AgentVisuals;
			if (!((NativeObject)(object)agentVisuals == (NativeObject)null) && agentVisuals.IsValid() && OneAgentLimbRay.Cast(a, from, to, 0.03f, out var distance, out var bone2) && !(distance > rayLen + 0.03f) && !(distance >= dist) && bone2 >= 0 && bone2 < agentVisuals.GetSkeleton().GetBoneCount())
			{
				OneAgentLimbRay.NoteHitOrigin(a.IsHuman, num);
				best = a;
				dist = distance;
				bone = bone2;
			}
		}
	}

	private int LimbKind(Agent a)
	{
		if (a == null)
		{
			return -1;
		}
		if (!a.IsHuman)
		{
			return 4;
		}
		if (!a.IsActive())
		{
			return 3;
		}
		if (a == _attacker || !_attacker.IsEnemyOf(a))
		{
			return 2;
		}
		return _hitThisStrike.Contains(a.Index) ? 1 : 0;
	}

	private static float OriginToRay(Vec3 origin, Vec3 from, Vec3 to)
	{
		float num = to.x - from.x;
		float num2 = to.y - from.y;
		float num3 = origin.x - from.x;
		float num4 = origin.y - from.y;
		float num5 = num * num + num2 * num2;
		float num6 = ((num5 > 1E-06f) ? MathF.Clamp((num3 * num + num4 * num2) / num5, 0f, 1f) : 0f);
		float num7 = num3 - num * num6;
		float num8 = num4 - num2 * num6;
		return (float)Math.Sqrt(num7 * num7 + num8 * num8);
	}

	private bool IsTarget(Agent victim)
	{
		if (victim == _attacker || !victim.IsActive() || _hitThisStrike.Contains(victim.Index))
		{
			return false;
		}
		if (victim == _attacker.MountAgent)
		{
			return false;
		}
		return true;
	}

	private float CurrentProgress()
	{
		try
		{
			return MathF.Clamp(_attacker.GetCurrentActionProgress(_channel), 0f, 1f);
		}
		catch
		{
			return 0.5f;
		}
	}

	private void LogProgressSources(string tag)
	{
		try
		{
			float animationParameterAtChannel = _attacker.AgentVisuals.GetSkeleton().GetAnimationParameterAtChannel(_channel);
			ActionIndexCache currentAction = _attacker.GetCurrentAction(_channel);
			if (DualWieldLog.Unlimited)
			{
				DualWieldLog.Log($"[LH-Blade] {tag}: actionProgress {_attacker.GetCurrentActionProgress(_channel):0.000}, animParam {animationParameterAtChannel:0.000}, " + string.Format("channel action '{0}', ours '{1}', window {2:0.00}-{3:0.00}", new object[4]
				{
					currentAction.GetName(),
					_actionName,
					_window.Start,
					_window.End
				}));
			}
		}
		catch (Exception ex)
		{
			DualWieldLog.Log("[LH-Blade] progress probe failed: " + ex.Message);
		}
	}

	private void StartHitStop()
	{
		try
		{
			_attacker.SetCurrentActionSpeed(_channel, 0.35f);
			_hitStopLeft = 0.07f;
		}
		catch
		{
		}
	}

	private void PlayBlockedReaction(string why, float releaseProgress, float blendIn = 0.05f)
	{
		try
		{
			if (string.IsNullOrEmpty(_actionName) || !_actionName.Contains("_quick_release_"))
			{
				return;
			}
			string text = ActionCache.BlockedOf(_actionName);
			ActionIndexCache val = ActionCache.Get(text);
			bool upright = false;
			if (val.Index < 0 && text != DwRules.WithoutHeight(text))
			{
				val = ActionCache.Get(text = DwRules.WithoutHeight(text));
				upright = true;
			}
			if (val.Index < 0)
			{
				DualWieldLog.Log("[LH-Blade] no blocked action " + text);
				return;
			}
			float num = MathF.Clamp(1f - releaseProgress, 0f, 0.95f);
			bool flag = _attacker.SetActionChannel(_channel, ref val, true, (AnimFlags)33, 0f, 1f, blendIn, 0.4f, num, false, -0.2f, 0, true);
			if (Chatty)
			{
				DualWieldLog.Log(string.Format("[LH-Blade] {0} -> {1} from {2:0.00} set={3}", new object[4] { why, text, num, flag }));
			}
			if (flag)
			{
				DualWieldBalance.ThrownBack(_attacker, DwRules.HeightOfAction(text), upright);
			}
		}
		catch (Exception ex)
		{
			DualWieldLog.Log("[LH-Blade] blocked reaction failed: " + ex.Message);
		}
	}

	private bool AiReactionBlock(Agent victim, out bool withShield)
	{
		withShield = false;
		try
		{
			if (victim == null || !victim.IsHuman || victim.IsPlayerControlled || victim.IsMainAgent)
			{
				return false;
			}
			Agent.ActionCodeType currentActionType = victim.GetCurrentActionType(1);
			bool flag = (int)currentActionType == 19;
			if (!(((int)currentActionType == 0 || (int)currentActionType == 35 || (int)currentActionType == 36 || ((int)currentActionType >= 1 && (int)currentActionType <= 14)) | flag))
			{
				return false;
			}
			Vec3 val = _attacker.Position - victim.Position;
			val.z = 0f;
			Vec3 lookDirection = victim.LookDirection;
			lookDirection.z = 0f;
			if (val.Normalize() < 0.001f || lookDirection.Normalize() < 0.001f || Vec3.DotProduct(val, lookDirection) < 0.35f)
			{
				return false;
			}
			MissionWeapon wieldedOffhandWeapon = victim.WieldedOffhandWeapon;
			bool flag2 = !wieldedOffhandWeapon.IsEmpty && wieldedOffhandWeapon.CurrentUsageItem != null && wieldedOffhandWeapon.CurrentUsageItem.IsShield;
			MissionWeapon wieldedWeapon = victim.WieldedWeapon;
			bool flag3 = !wieldedWeapon.IsEmpty && wieldedWeapon.CurrentUsageItem != null && wieldedWeapon.CurrentUsageItem.IsMeleeWeapon;
			if (!flag2 && !flag3)
			{
				return false;
			}
			int num = 0;
			SkillObject val2 = (flag3 ? wieldedWeapon.CurrentUsageItem.RelevantSkill : null);
			if (val2 != null && victim.Character != null)
			{
				num = victim.Character.GetSkillValue(val2);
			}
			float num2 = 0.25f + 0.45f * MathF.Min(1f, (float)num / 250f) + (flag2 ? 0.1f : 0f);
			if (flag)
			{
				num2 *= 0.5f;
			}
			if (_reactRng.NextDouble() >= (double)MathF.Min(0.8f, num2))
			{
				return false;
			}
			withShield = flag2;
			PlayDefend(victim, flag2);
			if (Chatty)
			{
				DualWieldLog.Log(string.Format("[LH-Blade] AI reaction block by {0} (skill {1}, chance {2:0.00}{3}, was {4})", new object[5]
				{
					victim.Name,
					num,
					MathF.Min(0.8f, num2),
					flag2 ? ", shield" : "",
					currentActionType
				}));
			}
			return true;
		}
		catch (Exception ex)
		{
			DualWieldLog.Log("[LH-Blade] reaction block failed: " + ex.Message);
			return false;
		}
	}

	private void PlayDefend(Agent victim, bool shield)
	{
		try
		{
			bool flag = false;
			try
			{
				flag = victim.GetIsLeftStance();
			}
			catch
			{
			}
			string text;
			if (shield)
			{
				text = (((int)_direction == 0) ? "act_defend_shield_active_up" : "act_defend_shield_active_down");
			}
			else
			{
				string text2;
				if ((int)_direction != 0)
				{
					if ((int)_direction == 1)
					{
						text2 = "forward";
					}
					else
					{
						text2 = (((int)_direction == 2) ? "left" : "right");
					}
				}
				else
				{
					text2 = "up";
				}
				text = "act_defend_" + text2 + "_1h_parry_light";
			}
			if (flag)
			{
				text += "_left_stance";
			}
			ActionIndexCache val = ActionCache.Get(text);
			if (val.Index >= 0)
			{
				victim.SetActionChannel(1, ref val, true, (AnimFlags)0, 0f, 1f, 0.05f, 0.4f, 0f, false, -0.2f, 0, true);
			}
		}
		catch (Exception ex)
		{
			DualWieldLog.Log("[LH-Blade] defend animation failed: " + ex.Message);
		}
	}

	private bool IsBlocked(Agent victim, out bool withShield)
	{
		withShield = false;
		Agent.ActionCodeType currentActionType = victim.GetCurrentActionType(1);
		if ((int)currentActionType < 1 || (int)currentActionType > 14)
		{
			return false;
		}
		Vec3 val = _attacker.Position - victim.Position;
		val.z = 0f;
		Vec3 lookDirection = victim.LookDirection;
		lookDirection.z = 0f;
		if (val.Normalize() < 0.001f || lookDirection.Normalize() < 0.001f)
		{
			return false;
		}
		if (Vec3.DotProduct(val, lookDirection) < 0.2f)
		{
			return false;
		}
		if ((int)currentActionType == 2)
		{
			withShield = true;
			return true;
		}
		Agent.UsageDirection direction = _direction;
		switch ((int)direction)
		{
		case 0:
			if ((int)currentActionType != 8 && (int)currentActionType != 4)
			{
				return (int)currentActionType == 12;
			}
			return true;
		case 1:
			if ((int)currentActionType != 7 && (int)currentActionType != 3)
			{
				return (int)currentActionType == 11;
			}
			return true;
		case 2:
			if ((int)currentActionType != 10 && (int)currentActionType != 6)
			{
				return (int)currentActionType == 14;
			}
			return true;
		case 3:
			if ((int)currentActionType != 9 && (int)currentActionType != 5)
			{
				return (int)currentActionType == 13;
			}
			return true;
		default:
			return false;
		}
	}

	private string DefenderSurface(Agent victim, bool withShield)
	{
		try
		{
			MissionWeapon val = (withShield ? victim.WieldedOffhandWeapon : victim.WieldedWeapon);
			if (val.IsEmpty & withShield)
			{
				for (EquipmentIndex val2 = (EquipmentIndex)0; (int)val2 < 5; val2 = (EquipmentIndex)(val2 + 1))
				{
					MissionWeapon val3 = victim.Equipment[val2];
					if (!val3.IsEmpty && val3.CurrentUsageItem != null && val3.CurrentUsageItem.IsShield)
					{
						val = val3;
						break;
					}
				}
			}
			if (!val.IsEmpty && val.CurrentUsageItem != null && !string.IsNullOrEmpty(val.CurrentUsageItem.PhysicsMaterial))
			{
				return val.CurrentUsageItem.PhysicsMaterial.ToLowerInvariant();
			}
		}
		catch
		{
		}
		if (!withShield)
		{
			return "metal_weapon";
		}
		return "wood_shield";
	}

	private void PlayImpact(Agent victim, Vec3 point, Vec3 dir, AttackCollisionData result, HitParticleResultData particles, bool blocked, bool withShield, DamageTypes damageType)
	{
		try
		{
			Mission current = Mission.Current;
			MissionWeapon val;
			if (blocked)
			{
				val = _attacker.Equipment[_slot];
				int num = ImpactSound(val.CurrentUsageItem, DefenderSurface(victim, withShield));
				if (num >= 0)
				{
					current.MakeSound(num, point, true, false, -1, -1);
				}
				return;
			}
			if (result.InflictedDamage <= 0 && victim.Health > 0f)
			{
				val = _attacker.Equipment[_slot];
				int num2 = ImpactSound(val.CurrentUsageItem, "flesh");
				if (num2 >= 0)
				{
					current.MakeSound(num2, point, true, false, -1, -1);
				}
				return;
			}
			if (_bloodEnter == -2)
			{
				_bloodEnter = ParticleSystemManager.GetRuntimeIdByName("psys_game_blood_sword_enter");
				_sweatEnter = ParticleSystemManager.GetRuntimeIdByName("psys_game_sweat_sword_enter");
			}
			string text = null;
			if (particles.StartHitParticleIndex >= 0 && particles.StartHitParticleIndex == _bloodEnter)
			{
				text = "psys_game_blood_sword_enter";
			}
			else if (particles.StartHitParticleIndex >= 0 && particles.StartHitParticleIndex == _sweatEnter)
			{
				text = "psys_game_sweat_sword_enter";
			}
			if (text != null)
			{
				Vec3 f = dir;
				if (f.Normalize() < 0.0001f)
				{
					f = Vec3.Forward;
				}
				Mat3 identity = Mat3.Identity;
				identity.f = f;
				identity.Orthonormalize();
				current.AddParticleSystemBurstByName(text, new MatrixFrame(ref identity, ref point), false);
			}
			int inflictedDamage = result.InflictedDamage;
			int num3;
			if ((int)damageType == 1)
			{
				if (inflictedDamage < 15)
				{
					num3 = CombatSoundContainer.SoundCodeMissionCombatPierceLow;
				}
				else
				{
					num3 = ((inflictedDamage < 35) ? CombatSoundContainer.SoundCodeMissionCombatPierceMed : CombatSoundContainer.SoundCodeMissionCombatPierceHigh);
				}
			}
			else if ((int)damageType != 2)
			{
				if (inflictedDamage < 15)
				{
					num3 = CombatSoundContainer.SoundCodeMissionCombatCutLow;
				}
				else
				{
					num3 = ((inflictedDamage < 35) ? CombatSoundContainer.SoundCodeMissionCombatCutMed : CombatSoundContainer.SoundCodeMissionCombatCutHigh);
				}
			}
			else if (inflictedDamage < 15)
			{
				num3 = CombatSoundContainer.SoundCodeMissionCombatBluntLow;
			}
			else
			{
				num3 = ((inflictedDamage < 35) ? CombatSoundContainer.SoundCodeMissionCombatBluntMed : CombatSoundContainer.SoundCodeMissionCombatBluntHigh);
			}
			current.MakeSound(num3, point, false, true, _attacker.Index, victim.Index);
		}
		catch (Exception ex)
		{
			DualWieldLog.Log("[LH-Blade] impact fx failed: " + ex.Message);
		}
	}

	private KeyValuePair<float, float> DefendResults(Agent victim, bool blocked, bool withShield, StrikeType strike, float along, float progress)
	{
		if (_defendResultsMissing)
		{
			return new KeyValuePair<float, float>(0f, 0f);
		}
		if (victim == null || !victim.IsHuman)
		{
			return new KeyValuePair<float, float>(0f, 0f);
		}
		try
		{
			if (_defendResults == null)
			{
				_defendResults = typeof(MissionCombatMechanicsHelper).GetMethod("GetDefendCollisionResults", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
				if (_defendResults == null)
				{
					_defendResultsMissing = true;
					DualWieldLog.Log("[LH-Blade] GetDefendCollisionResults not found - no stun periods");
					return new KeyValuePair<float, float>(0f, 0f);
				}
			}
			object[] array = new object[16]
			{
				_attacker,
				victim,
				(object)(CombatCollisionResult)((!blocked) ? 1 : 3),
				(int)_slot,
				false,
				strike,
				_direction,
				along,
				progress,
				blocked && !withShield,
				false,
				false,
				0f,
				0f,
				false,
				false
			};
			_defendResults.Invoke(null, array);
			float value = (float)array[12];
			float key = (float)array[13];
			if (((bool)array[14] || (bool)array[15]) && Chatty)
			{
				DualWieldLog.Log($"[LH-Blade] defend results: crushedThrough {array[14]}, chamber {array[15]}");
			}
			return new KeyValuePair<float, float>(key, value);
		}
		catch (TargetInvocationException ex)
		{
			if (_defendResultsErrors++ < 5)
			{
				DualWieldLog.Log("[LH-Blade] stun periods failed for this blow: " + (ex.InnerException?.ToString() ?? ex.Message));
			}
			return new KeyValuePair<float, float>(0f, 0f);
		}
		catch (Exception ex2)
		{
			_defendResultsMissing = true;
			DualWieldLog.Log("[LH-Blade] stun periods failed: " + ex2.Message);
			return new KeyValuePair<float, float>(0f, 0f);
		}
	}

	private MeleeCollisionReaction ApplyHit(Agent victim, MissionWeapon weapon, WeaponComponentData usage, sbyte bone, Vec3 point, float along, float tipSpeed, Vec3 motion, bool blocked, bool withShield, out float progress)
	{
		long started = DwPerf.Start();
		try
		{
			return ApplyHitCore(victim, weapon, usage, bone, point, along, tipSpeed, motion, blocked, withShield, out progress);
		}
		finally
		{
			DwPerf.Stop(11, started);
		}
	}

	private MeleeCollisionReaction ApplyHitCore(Agent victim, MissionWeapon weapon, WeaponComponentData usage, sbyte bone, Vec3 point, float along, float tipSpeed, Vec3 motion, bool blocked, bool withShield, out float progress)
	{
		progress = 0.5f;
		try
		{
			BoneBodyPartType bodyPartType = victim.AgentVisuals.GetBoneTypeData(bone).BodyPartType;
			StrikeType val = (StrikeType)(_thrust ? 1 : 0);
			DamageTypes val2 = (_thrust ? usage.ThrustDamageType : usage.SwingDamageType);
			Vec3 val3 = motion;
			if (val3.Normalize() < 0.0001f)
			{
				val3 = victim.Position - _attacker.Position;
			}
			progress = CurrentProgress();
			sbyte b = (sbyte)((weapon.Item != null) ? _attacker.Monster.GetBoneToAttachForItemFlags(weapon.Item.ItemFlags) : 20);
			KeyValuePair<float, float> keyValuePair = DefendResults(victim, blocked, withShield, val, along, progress);
			AttackCollisionData attackCollisionDataForDebugPurpose = AttackCollisionData.GetAttackCollisionDataForDebugPurpose(blocked & withShield, blocked & withShield, false, true, false, false, false, false, false, _thrust, false, false, (CombatCollisionResult)((!blocked) ? 1 : 3), (int)_slot, (int)val, (int)val2, bone, bodyPartType, b, _direction, 0, (CombatHitResultFlags)0, progress, along, keyValuePair.Key, keyValuePair.Value, 0f, 0f, 0f, 0f, Vec3.Up, val3, point, Vec3.Zero, Vec3.Zero, victim.Velocity, Vec3.Up);
			if (_meleeHitCallback == null)
			{
				_meleeHitCallback = typeof(Mission).GetMethod("MeleeHitCallback", BindingFlags.Instance | BindingFlags.NonPublic);
			}
			if (_meleeHitCallback == null)
			{
				DualWieldLog.Log("[LH-Blade] Mission.MeleeHitCallback not found");
				return (MeleeCollisionReaction)(-1);
			}
			object[] array = new object[11]
			{
				attackCollisionDataForDebugPurpose,
				_attacker,
				victim,
				null,
				1f,
				(object)(MeleeCollisionReaction)0,
				(object)(CrushThroughState)0,
				val3,
				val3,
				(object)default(HitParticleResultData),
				false
			};
			float health = victim.Health;
			Agent.ActionCodeType currentActionType = victim.GetCurrentActionType(1);
			try
			{
				_ = _attacker.IsDoingPassiveAttack;
			}
			catch
			{
			}
			bool flag = false;
			try
			{
				flag = _attacker.IsFriendOf(victim);
			}
			catch
			{
			}
			ApplyingLeftHit = true;
			try
			{
				_meleeHitCallback.Invoke(Mission.Current, array);
			}
			finally
			{
				ApplyingLeftHit = false;
			}
			AttackCollisionData val4 = (AttackCollisionData)array[0];
			MeleeCollisionReaction val5 = (MeleeCollisionReaction)array[5];
			_lastMomentum = (float)array[4];
			PlayImpact(victim, point, val3, val4, (HitParticleResultData)array[9], blocked, withShield, val2);
			if (Chatty)
			{
				DualWieldLog.Log(string.Format("[LH-Blade] {0} {1} {2} bone {3}: {4} dmg (armor absorbed {5}, magnitude {6:0.0}), health {7:0} -> {8:0}, reaction {9}, tip {10:0.0} m/s at {11:0.00} m, progress {12:0.00}, victim before {13} vs strike {14}, friend {15}", new object[16]
				{
					blocked ? "BLOCK" : "HIT",
					victim.Name,
					bodyPartType,
					bone,
					val4.InflictedDamage,
					val4.AbsorbedByArmor,
					val4.BaseMagnitude,
					health,
					victim.Health,
					val5,
					tipSpeed,
					along,
					progress,
					currentActionType,
					_direction,
					flag
				}));
			}
			if (blocked)
			{
				_wasBlocked = true;
			}
			else if (!flag && val4.InflictedDamage > 0)
			{
				_woundedEnemy = true;
			}
			Vec3 val6;
			if (flag && !blocked && _friendMet == 0 && _attacker.HasMount)
			{
				_friendMet = ((!victim.IsHuman) ? 3 : ((!victim.HasMount) ? 1 : 2));
				try
				{
					Mat3 rotation = _attacker.Frame.rotation;
					val6 = victim.Position;
					Vec2 asVec = val6.AsVec2;
					val6 = _attacker.Position;
					Vec2 val7 = asVec - val6.AsVec2;
					Vec2 asVec2 = rotation.f.AsVec2;
					Vec2 asVec3 = rotation.s.AsVec2;
					asVec2.Normalize();
					asVec3.Normalize();
					_friendWhere = string.Format(CultureInfo.InvariantCulture, "{0:0} deg {1:0.0} m, progress {2:0.00}", Math.Atan2(val7.X * asVec3.X + val7.Y * asVec3.Y, val7.X * asVec2.X + val7.Y * asVec2.Y) * 180.0 / Math.PI, val7.Length, progress);
				}
				catch
				{
				}
			}
			if (flag && !blocked)
			{
				DualWieldBalance.LeftOnFriend(_attacker);
				float num = ((val4.AttackerStunPeriod > 0f) ? val4.AttackerStunPeriod : 0.4f);
				_stunUntil = Mission.Current.CurrentTime + num;
				if (DualWieldLog.Unlimited)
				{
					DualWieldLog.Log($"[LH-Blade] {_attacker.Name}: blow on a friend - stunned {num:0.00} s");
				}
			}
			try
			{
				AttackCollisionData val8 = attackCollisionDataForDebugPurpose;
				bool flag2 = MissionCombatMechanicsHelper.HitWithAnotherBone(ref val8, _attacker, ref weapon);
				if (DualWieldLog.Unlimited)
				{
					string[] array2 = new string[5];
					object arg = flag2;
					object arg2 = b;
					ItemObject item = weapon.Item;
					array2[0] = $"[LH-Blade] dmg inputs: otherBone={arg} (attackBone {arg2}, item flags {((item != null) ? (object)item.ItemFlags : (object)null)}), ";
					array2[1] = $"swingSpeed {weapon.GetModifiedSwingSpeedForCurrentUsage()} thrustSpeed {weapon.GetModifiedThrustSpeedForCurrentUsage()}, ";
					object[] array3 = new object[4];
					ItemObject item2 = weapon.Item;
					array3[0] = ((item2 != null) ? new float?(item2.Weight) : ((float?)null));
					array3[1] = usage.GetRealWeaponLength();
					array3[2] = usage.TotalInertia;
					array3[3] = usage.CenterOfMass;
					array2[2] = string.Format("weight {0:0.00} len {1:0.00} inertia {2:0.000} com {3:0.00}, ", array3);
					array2[3] = $"factor swing {usage.SwingDamageFactor:0.00} thrust {usage.ThrustDamageFactor:0.00}, momentum after {(float)array[4]:0.00}, ";
					object arg3 = val;
					object arg4 = _direction;
					val6 = _attacker.Velocity;
					array2[4] = $"strike {arg3} dir {arg4}, attacker vel {val6.Length:0.0}";
					DualWieldLog.Log(string.Concat(array2));
				}
			}
			catch (Exception ex)
			{
				DualWieldLog.Log("[LH-Blade] dmg inputs failed: " + ex.Message);
			}
			return val5;
		}
		catch (Exception ex2)
		{
			DualWieldLog.Log("[LH-Blade] hit failed: " + (ex2.InnerException ?? ex2).Message);
			return (MeleeCollisionReaction)(-1);
		}
	}
}
