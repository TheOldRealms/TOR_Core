// Vendored from Yotthani's DualWield-v0.10.9 mod (permission granted
// 2026-11-24). Full MissionBehavior port, ilspycmd decompile artifacts
// hand-fixed. Replaces the Phase 1 DualWieldMissionLogic stub.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using TOR_Core.BattleMechanics.DualWield;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.DotNet;
using TaleWorlds.Engine;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TOR_Core.CharacterDevelopment;
using TOR_Core.Extensions;
using TOR_Core.HarmonyPatches.DualWield;
using Path = System.IO.Path;
using TaleWorlds.ObjectSystem;

namespace TOR_Core.BattleMechanics.DualWield;

public class DualWieldMissionBehavior : MissionBehavior
{
	private readonly HashSet<int> _agentsWithAttachment = new HashSet<int>();

	internal readonly NpcRangedHybrids RangedHybrids = new NpcRangedHybrids();

	private readonly Dictionary<int, EquipmentIndex> _offHandSlots = new Dictionary<int, EquipmentIndex>();

	private bool _indicatorShown;

	private int _followUpCount;

	private static readonly ActionIndexCache _idleReady1h = ActionIndexCache.Create("act_idle_1h_without_shield_1");

	private static readonly ActionIndexCache _actDwSlashRight = ActionIndexCache.Create("act_dw_cu_slashright");

	private static readonly ActionIndexCache _actDwSlashLeft = ActionIndexCache.Create("act_dw_cu_slashleft");

	private static readonly ActionIndexCache _mirUp = ActionIndexCache.Create("act_tor_dwc_qck_release_overswing_1h_left_stance");

	private static readonly ActionIndexCache _mirDown = ActionIndexCache.Create("act_tor_dwc_qck_release_thrust_1h_left_stance");

	private static readonly ActionIndexCache _mirRight = ActionIndexCache.Create("act_tor_dwc_qck_release_slashright_1h_left_stance");

	private static readonly ActionIndexCache _mirLeft = ActionIndexCache.Create("act_tor_dwc_qck_release_slashleft_1h_left_stance");

	private static readonly ActionIndexCache _mirBit = ActionIndexCache.Create("act_dwmirbit");

	private static readonly ActionIndexCache _selfEnc = ActionIndexCache.Create("act_dwselfenc");

	private static readonly string[] _dwcNames = BuildDwcNames();

	private static readonly ActionIndexCache[] _dwcActs = BuildDwcActions("act_tor_dwc_");

	private static readonly ActionIndexCache[] _dwvActs = BuildDwcActions("act_dwv_");

	private int _dwcIdx = -1;

	private float _dwcReadyAt;

	private float _dwcIdleAt;

	private bool _dwcWasRaw;

	private bool _dwcWasDown;

	private static readonly ActionIndexCache _dagUp = ActionIndexCache.Create("act_tor_dwc_qck_release_backstab_1h_left_stance");

	private static readonly ActionIndexCache _dagRight = ActionIndexCache.Create("act_tor_dwc_qck_release_slashright_dagger_leftstance");

	private static readonly ActionIndexCache _dagLeft = ActionIndexCache.Create("act_tor_dwc_qck_release_slashleft_dagger_leftstance");

	private static readonly ActionIndexCache _flailUp = ActionIndexCache.Create("act_tor_dwc_qck_release_overswing_flail_left_stance");

	private static readonly ActionIndexCache _flailRight = ActionIndexCache.Create("act_tor_dwc_qck_release_slashright_flail_left_stance");

	private static readonly ActionIndexCache _flailLeft = ActionIndexCache.Create("act_tor_dwc_qck_release_slashleft_flail_left_stance");

	private static readonly ActionIndexCache _rdyFlailUp = ActionIndexCache.Create("act_tor_dwc_ready_overswing_flail_left_stance");

	private static readonly ActionIndexCache _rdyFlailRight = ActionIndexCache.Create("act_tor_dwc_ready_slashright_flail_left_stance");

	private static readonly ActionIndexCache _rdyFlailLeft = ActionIndexCache.Create("act_tor_dwc_ready_slashleft_flail_left_stance");

	private static readonly ActionIndexCache _rdyUp = ActionIndexCache.Create("act_tor_dwc_ready_overswing_1h_left_stance");

	private static readonly ActionIndexCache _rdyDown = ActionIndexCache.Create("act_tor_dwc_ready_thrust_1h_left_stance");

	private static readonly ActionIndexCache _rdyRight = ActionIndexCache.Create("act_tor_dwc_ready_slashright_1h_left_stance");

	private static readonly ActionIndexCache _rdyLeft = ActionIndexCache.Create("act_tor_dwc_ready_slashleft_1h_left_stance");

	private static readonly ActionIndexCache _rdyDagUp = ActionIndexCache.Create("act_tor_dwc_ready_backstab_1h_left_stance");

	private static readonly ActionIndexCache _rdyDagRight = ActionIndexCache.Create("act_tor_dwc_ready_slashright_dagger_leftstance");

	private static readonly ActionIndexCache _rdyDagLeft = ActionIndexCache.Create("act_tor_dwc_ready_slashleft_dagger_leftstance");

	private bool _toggleWasDown;

	private static readonly ActionIndexCache _turnToBack = ActionIndexCache.Create("act_tor_dwc_switch_alternate_dagger_left_stance");

	private static readonly ActionIndexCache _turnToBackEnd = ActionIndexCache.Create("act_tor_dwc_switch_alternate_dagger_left_stance_cont");

	private static readonly ActionIndexCache _turnToFront = ActionIndexCache.Create("act_tor_dwc_switch_back_alternate_dagger_left_stance");

	private static readonly ActionIndexCache _turnToFrontEnd = ActionIndexCache.Create("act_tor_dwc_switch_back_alternate_dagger_left_stance_cont");

	private static readonly ActionIndexCache _vanillaTurnToBackEnd = ActionIndexCache.Create("act_switch_alternate_dagger_continue_left_stance");

	private EquipmentIndex _turnSlot = (EquipmentIndex)(-1);

	private int _turnNext;

	private bool _turnToBackNow;

	private bool _tickLoggedOnce;

	private int _tickCount;

	private bool _animProbeDone;

	private static StreamWriter _capW;

	private static StreamWriter _capWW;

	private const string CaptureDir = "D:/Work/Sources/github/bannerlord/HoN/DualWield/tools/capture";

	private static float _capUntil;

	private static string _capTag = "";

	private const int CAP_BONES = 28;

	private float _locoNext;

	private int _locoLines;

	private const int LOCO_MAX_LINES = 600;

	private float _riderProbeNext;

	private int _riderProbeLines;

	private bool _rmbWasDown;

	private bool _windingUp;

	private ActionIndexCache _windupAct;

	private bool _wasMounted;

	public const float LEFT_STRIKE_SPEED = 0.6f;

	public static bool PlayerOffHandStriking;

	public static bool PlayerMainLeftStriking;

	private bool _strikeSlowOn;

	private float _speedLeftSum;

	private float _speedRightSum;

	private int _speedLeftN;

	private int _speedRightN;

	private ActionIndexCache _sepStrikeAct = ActionIndexCache.act_none;

	private float _sepStrikeAt = -10f;

	private bool _sepQueued;

	private const float SEP_MIN_WINDUP = 0.15f;

	private float _sepWindupAt = -10f;

	private Agent.UsageDirection _windupDir = (Agent.UsageDirection)3;

	private int _windupHeight;

	private Agent.UsageDirection _sepStrikeDir = (Agent.UsageDirection)(-1);

	public static Agent.UsageDirection PlayerLeftArrow = (Agent.UsageDirection)(-1);

	private bool _sepReleasePending;

	private bool _nextIsLeft;

	private string _pendingDiag;

	private int _pendingDiagTicks;

	private int _dbgChanWatch;

	private bool _vanillaSwingWasActive;

	private Agent.UsageDirection _lastRightDir = (Agent.UsageDirection)(-1);

	private Agent.UsageDirection _leftTurnAimAtStart = (Agent.UsageDirection)(-1);

	private float _lastRightAt = -10f;

	private bool _padHintShown;

	private bool _padAlternating;

	private int _bendAction = -1;

	private float _bendPitch;

	private float _bendYaw;

	private bool _bendSampled;

	private int _bendLogs;

	private int _oursAction = -1;

	private bool _oursSampled;

	private float _oursPitch;

	private float _oursYaw;

	private bool _gWasDown;

	private bool _nWasDown;

	private bool _pWasDown;

	private bool _oWasDown;

	private bool _kp6WasDown;

	private bool _kp4WasDown;

	private bool _kp9WasDown;

	private int _clipPlayTicks;

	private int _savedCh0AnimIdx = -1;

	private int _dwCombatMode;

	private int _settingsModeSeen = -1;

	private object _settingsObjectSeen;

	private int _settingsSwapLogs;

	private readonly LeftBladeStrike _leftBlade = new LeftBladeStrike();

	private float _lastTickDt;

	private const float NPC_LEFT_COOLDOWN = 2.5f;

	private readonly Dictionary<int, LeftBladeStrike> _npcStrikes = new Dictionary<int, LeftBladeStrike>();

	private readonly List<LeftBladeStrike> _npcStrikesDue = new List<LeftBladeStrike>();

	private readonly FluidStrikeCap _strikeCap = new FluidStrikeCap();

	private int _npcStrikeCap = 48;

	private int _npcStrikesRunning;

	private readonly Dictionary<int, float> _npcLeftCooldown = new Dictionary<int, float>();

	private readonly List<Agent> _npcCounterQueue = new List<Agent>();

	private readonly Random _npcRng = new Random();

	private int _npcLeftLogs;

	private int _guardSamplePart;

	private const float NPC_ALLY_RADIUS = 1.6f;

	private readonly MBList<Agent> _npcNearby = new MBList<Agent>();

	private readonly List<Agent.UsageDirection> _npcDirs = new List<Agent.UsageDirection>();

	private readonly HashSet<int> _npcReleasing = new HashSet<int>();

	private readonly HashSet<int> _npcFollowUps = new HashSet<int>();

	private const int NPC_WATCH_STRIDE = 3;

	private const float NPC_LEAD_CHANCE = 0.8f;

	private const float NPC_LEAD_COOLDOWN = 1.2f;

	private readonly HashSet<int> _npcReadying = new HashSet<int>();

	private readonly HashSet<int> _npcLeads = new HashSet<int>();

	private int _npcWatchPhase;

	private readonly MBList<Agent> _npcReach = new MBList<Agent>();

	private readonly Dictionary<int, AgentFlag> _npcHeldFlags = new Dictionary<int, AgentFlag>();

	private int _npcHoldLogs;

	private const float MOUNTED_LEAD_SIDE = 0.5f;

	private const float MOUNTED_LEAD_AHEAD = 0.4f;

	private const float MOUNTED_SEARCH_MARGIN = 6.5f;

	private const float MOUNTED_MIN_TARGET_HEIGHT = 1f;

	private const float EYES_TO_HEAD = 0.1f;

	private bool _mountedTargetAhead;

	private bool _mountedTookAiTarget;

	private int _npcMountedLogs;

	private int _npcNoTargetLogs;

	public static bool IsActive { get; private set; }

	public override MissionBehaviorType BehaviorType => (MissionBehaviorType)1;

	private static bool CaptureEnabled
	{
		get
		{
			if (DualWieldSettings.Get().DebugMode)
			{
				return Directory.Exists(Path.GetDirectoryName("D:/Work/Sources/github/bannerlord/HoN/DualWield/tools/capture"));
			}
			return false;
		}
	}

	private static bool NpcFootChannel1
	{
		get
		{
			try
			{
				return DualWieldSettings.Get().NpcFootLeftChannel1;
			}
			catch
			{
				return false;
			}
		}
	}

	private static bool MountedLeftOn => MountedChannelSetting() >= 0;

	internal static bool LeftHanded
	{
		get
		{
			try
			{
				return DualWieldSettings.Get().TestLeftHanded;
			}
			catch
			{
				return false;
			}
		}
	}

	private static bool RiderLeftTakesAiTarget
	{
		get
		{
			try
			{
				return DualWieldSettings.Get().TestRiderAiTarget;
			}
			catch
			{
				return true;
			}
		}
	}

	private static bool MeasuredRiderZones
	{
		get
		{
			try
			{
				return DualWieldSettings.Get().TestRiderZones;
			}
			catch
			{
				return false;
			}
		}
	}

	public DualWieldMissionBehavior()
	{
		IsActive = true;
	}

	private static string[] BuildDwcNames()
	{
		List<string> list = new List<string>();
		string[] array = new string[2] { "_1h_left_stance", "_1h" };
		foreach (string text in array)
		{
			string[] array2 = new string[2] { "quick_release_", "release_" };
			foreach (string text2 in array2)
			{
				string[] array3 = new string[4] { "slashright", "slashleft", "overswing", "thrust" };
				foreach (string text3 in array3)
				{
					list.Add(text2 + text3 + text);
				}
			}
		}
		return list.ToArray();
	}

	private static ActionIndexCache[] BuildDwcActions(string prefix)
	{
		ActionIndexCache[] array = new ActionIndexCache[_dwcNames.Length];
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = ActionIndexCache.Create(prefix + _dwcNames[i]);
		}
		return array;
	}

	private Agent.UsageDirection LeftUsable(Agent agent, Agent.UsageDirection dir)
	{
		if ((int)dir != 1)
		{
			return dir;
		}
		try
		{
			if (!_offHandSlots.TryGetValue(agent.Index, out var value))
			{
				return dir;
			}
			MissionWeapon val = agent.Equipment[value];
			if (val.IsEmpty || val.CurrentUsageItem == null)
			{
				return dir;
			}
			return (Agent.UsageDirection)((!DwRules.ThrustBecomesOverswing(val.CurrentUsageItem.ThrustDamage)) ? ((int)dir) : 0);
		}
		catch
		{
			return dir;
		}
	}

	private bool OffhandIsDagger(Agent agent)
	{
		try
		{
			if (!_offHandSlots.TryGetValue(agent.Index, out var value))
			{
				return false;
			}
			MissionWeapon val = agent.Equipment[value];
			return !val.IsEmpty && val.CurrentUsageItem != null && (int)val.CurrentUsageItem.WeaponClass == 1 && (val.CurrentUsageItem.WeaponFlags & (WeaponFlags)0x200000000L) > 0L;
		}
		catch
		{
			return false;
		}
	}

	private bool OffhandIsFlail(Agent agent)
	{
		try
		{
			if (!_offHandSlots.TryGetValue(agent.Index, out var value))
			{
				return false;
			}
			MissionWeapon val = agent.Equipment[value];
			if (val.IsEmpty)
			{
				return false;
			}
			string text = ((val.Item != null) ? val.Item.SkeletonName : null);
			if (!string.IsNullOrEmpty(text) && text.IndexOf("flail", StringComparison.OrdinalIgnoreCase) >= 0)
			{
				return true;
			}
			string text2 = ((val.CurrentUsageItem != null) ? val.CurrentUsageItem.ItemUsage : null);
			return !string.IsNullOrEmpty(text2) && text2.IndexOf("flail", StringComparison.OrdinalIgnoreCase) >= 0;
		}
		catch
		{
			return false;
		}
	}

	private void ProcessOffhandUsageToggle(Agent agent)
	{
		bool flag = false;
		try
		{
			flag = GameKeyDown(17, (InputKey)45);
		}
		catch
		{
		}
		bool flag2 = flag && !_toggleWasDown;
		_toggleWasDown = flag;
		try
		{
			if ((int)_turnSlot != -1)
			{
				TickLeftTurn(agent);
			}
			else
			{
				if (!_offHandSlots.TryGetValue(agent.Index, out var value))
				{
					return;
				}
				MissionWeapon val = agent.Equipment[value];
				if (val.IsEmpty || val.Item == null || val.Item.Weapons == null || ((List<WeaponComponentData>)(object)val.Item.Weapons).Count < 2)
				{
					return;
				}
				EquipmentIndex primaryWieldedItemIndex = agent.GetPrimaryWieldedItemIndex();
				MissionWeapon val2 = (((int)primaryWieldedItemIndex != -1) ? agent.Equipment[primaryWieldedItemIndex] : MissionWeapon.Invalid);
				bool flag3 = !val2.IsEmpty && val2.Item != null && val2.Item.Weapons != null && ((List<WeaponComponentData>)(object)val2.Item.Weapons).Count >= 2;
				int num;
				if (flag3)
				{
					bool flag4 = val2.CurrentUsageItem != null && (val2.CurrentUsageItem.WeaponFlags & (WeaponFlags)0x200000000L) > 0L;
					bool flag5 = val.CurrentUsageItem != null && (val.CurrentUsageItem.WeaponFlags & (WeaponFlags)0x200000000L) > 0L;
					if (flag4 == flag5)
					{
						return;
					}
					num = -1;
					for (int i = 0; i < ((List<WeaponComponentData>)(object)val.Item.Weapons).Count; i++)
					{
						if ((((List<WeaponComponentData>)(object)val.Item.Weapons)[i].WeaponFlags & (WeaponFlags)0x200000000L) > 0L == flag4)
						{
							num = i;
							break;
						}
					}
					if (num < 0)
					{
						return;
					}
				}
				else
				{
					if (!flag2)
					{
						return;
					}
					num = (val.CurrentUsageIndex + 1) % ((List<WeaponComponentData>)(object)val.Item.Weapons).Count;
					if (StartLeftTurn(agent, value, val, num))
					{
						return;
					}
				}
				ApplyLeftUsage(agent, value, val, num, flag3 ? " - follows main hand" : " - key");
			}
		}
		catch (Exception ex)
		{
			DualWieldLog.Log("[OffhandUsage] failed: " + ex.Message);
		}
	}

	private void ApplyLeftUsage(Agent agent, EquipmentIndex slot, MissionWeapon w, int next, string how)
	{
		int currentUsageIndex = w.CurrentUsageIndex;
		agent.SetUsageIndexOfWeaponInSlotAsClient(slot, next);
		bool flag = (((List<WeaponComponentData>)(object)w.Item.Weapons)[next].WeaponFlags & (WeaponFlags)0x200000000L) > 0L;
		DualWieldLog.Log(string.Format("[OffhandUsage] '{0}' usage {1} -> {2} (now reads {3}), {4}{5}", new object[6]
		{
			((MBObjectBase)w.Item).StringId,
			currentUsageIndex,
			next,
			agent.Equipment[slot].CurrentUsageIndex,
			flag ? "held backwards" : "forward grip",
			how
		}));
		InformationManager.DisplayMessage(new InformationMessage("Left weapon: " + (flag ? "reverse grip" : "normal grip"), Colors.Cyan));
	}

	private bool StartLeftTurn(Agent agent, EquipmentIndex slot, MissionWeapon w, int next)
	{
		try
		{
			if (agent.HasMount || _leftBlade.Active)
			{
				return false;
			}
			WeaponComponentData currentUsageItem = w.CurrentUsageItem;
			if (currentUsageItem == null || (int)currentUsageItem.WeaponClass != 1)
			{
				return false;
			}
			bool flag = (((List<WeaponComponentData>)(object)w.Item.Weapons)[next].WeaponFlags & (WeaponFlags)0x200000000L) > 0L;
			if (flag == (currentUsageItem.WeaponFlags & (WeaponFlags)0x200000000L) > 0L)
			{
				return false;
			}
			ActionIndexCache val = (flag ? _turnToBack : _turnToFront);
			if (val.Index >= 0)
			{
				ActionIndexCache val2 = (flag ? _turnToBackEnd : _turnToFrontEnd);
				if (val2.Index >= 0)
				{
					val2 = agent.GetCurrentAction(1);
					if (val2.Index != ActionIndexCache.act_none.Index)
					{
						return false;
					}
					if (!agent.SetActionChannel(1, ref val, true, (AnimFlags)0, 0f, 1f, -0.2f, 0.4f, 0f, false, -0.2f, 0, true))
					{
						return false;
					}
					_turnSlot = slot;
					_turnNext = next;
					_turnToBackNow = flag;
					return true;
				}
			}
			return false;
		}
		catch
		{
			return false;
		}
	}

	private void TickLeftTurn(Agent agent)
	{
		EquipmentIndex turnSlot = _turnSlot;
		try
		{
			ActionIndexCache val = (_turnToBackNow ? _turnToBack : _turnToFront);
			ActionIndexCache val2 = (_turnToBackNow ? _turnToBackEnd : _turnToFrontEnd);
			ActionIndexCache currentAction = agent.GetCurrentAction(1);
			int index = currentAction.Index;
			bool flag = index == val.Index;
			if (flag && agent.GetCurrentActionProgress(1) < 0.92f)
			{
				return;
			}
			_turnSlot = (EquipmentIndex)(-1);
			MissionWeapon w = agent.Equipment[turnSlot];
			if (!w.IsEmpty && w.Item != null && w.Item.Weapons != null && _turnNext < ((List<WeaponComponentData>)(object)w.Item.Weapons).Count)
			{
				ApplyLeftUsage(agent, turnSlot, w, _turnNext, flag ? " - key, turned in the hand" : " - key, the turn was cut short");
				if (flag || (_vanillaTurnToBackEnd.Index >= 0 && index == _vanillaTurnToBackEnd.Index))
				{
					agent.SetActionChannel(1, ref val2, true, (AnimFlags)0, 0f, 1f, -0.2f, 0.4f, 0f, false, -0.2f, 0, true);
				}
			}
		}
		catch (Exception ex)
		{
			_turnSlot = (EquipmentIndex)(-1);
			DualWieldLog.Log("[OffhandUsage] turn failed: " + ex.Message);
		}
	}

	private ActionIndexCache MirrorForDirection(Agent agent)
	{
		Agent.UsageDirection dir = (Agent.UsageDirection)3;
		try
		{
			dir = agent.PlayerAttackDirection();
		}
		catch
		{
		}
		return MirrorForDirection(agent, dir);
	}

	private ActionIndexCache MirrorForDirection(Agent agent, Agent.UsageDirection dir)
	{
		if (agent != null && agent.HasMount)
		{
			return MountedAction(agent, dir, ready: false);
		}
		dir = ArrivalSide(LeftUsable(agent, dir));
		try
		{
			if (OffhandIsFlail(agent))
			{
				if ((int)dir != 2)
				{
					if ((int)dir == 3)
					{
						if (_flailRight.Index >= 0)
						{
							return _flailRight;
						}
					}
					else if (_flailUp.Index >= 0)
					{
						return _flailUp;
					}
				}
				else if (_flailLeft.Index >= 0)
				{
					return _flailLeft;
				}
			}
			if (OffhandIsDagger(agent))
			{
				if ((int)dir != 2)
				{
					if ((int)dir == 3)
					{
						if (_dagRight.Index >= 0)
						{
							return _dagRight;
						}
					}
					else if (_dagUp.Index >= 0)
					{
						return _dagUp;
					}
				}
				else if (_dagLeft.Index >= 0)
				{
					return _dagLeft;
				}
			}
			switch ((int)dir)
			{
			case 0:
				return _mirUp;
			case 1:
				return _mirDown;
			case 2:
				return _mirLeft;
			case 3:
				return _mirRight;
			}
		}
		catch
		{
		}
		return _mirRight;
	}

	private static int HeightOf(Agent agent)
	{
		try
		{
			return (!agent.HasMount && !FootChannel1(agent)) ? DwRules.StrikeHeight(agent.LookDirection.z) : 0;
		}
		catch
		{
			return 0;
		}
	}

	private static ActionIndexCache AtHeight(ActionIndexCache act, int height)
	{
		if (height == 0 || act.Index < 0)
		{
			return act;
		}
		ActionIndexCache result = ActionCache.Get(DwRules.AtHeight(ActionCache.NameOf(act), height));
		if (result.Index < 0 && (height < -1 || height > 1))
		{
			result = ActionCache.Get(DwRules.AtHeight(ActionCache.NameOf(act), Math.Sign(height)));
		}
		if (result.Index < 0)
		{
			return act;
		}
		return result;
	}

	private ActionIndexCache ReadyForDirection(Agent agent)
	{
		return ReadyForDirection(agent, SafeAttackDirection(agent));
	}

	private ActionIndexCache ReadyForDirection(Agent agent, Agent.UsageDirection dir)
	{
		if (agent != null && agent.HasMount)
		{
			return MountedAction(agent, dir, ready: true);
		}
		dir = ArrivalSide(LeftUsable(agent, dir));
		try
		{
			if (OffhandIsFlail(agent))
			{
				if ((int)dir != 2)
				{
					if ((int)dir == 3)
					{
						if (_rdyFlailRight.Index >= 0)
						{
							return _rdyFlailRight;
						}
					}
					else if (_rdyFlailUp.Index >= 0)
					{
						return _rdyFlailUp;
					}
				}
				else if (_rdyFlailLeft.Index >= 0)
				{
					return _rdyFlailLeft;
				}
			}
			if (OffhandIsDagger(agent))
			{
				if ((int)dir != 2)
				{
					if ((int)dir == 3)
					{
						if (_rdyDagRight.Index >= 0)
						{
							return _rdyDagRight;
						}
					}
					else if (_rdyDagUp.Index >= 0)
					{
						return _rdyDagUp;
					}
				}
				else if (_rdyDagLeft.Index >= 0)
				{
					return _rdyDagLeft;
				}
			}
			switch ((int)dir)
			{
			case 0:
				return _rdyUp;
			case 1:
				return _rdyDown;
			case 2:
				return _rdyLeft;
			case 3:
				return _rdyRight;
			}
		}
		catch
		{
		}
		return _rdyRight;
	}

	public void AttachOffHandWeapon(Agent agent, EquipmentIndex offHandSlot)
	{
		if (agent == null || !agent.IsActive())
		{
			return;
		}
		RemoveOffHandAttachment(agent);
		if (!DualWieldStateManager.IsDualWielding(agent))
		{
			DualWieldStateManager.Register(agent);
			if (DualWieldLog.Unlimited || agent.IsMainAgent)
			{
				DualWieldLog.Log("AttachOffHandWeapon: Registered agent " + agent.Name + " in StateManager");
			}
		}
		MissionWeapon val = agent.Equipment[offHandSlot];
		if (val.IsEmpty)
		{
			DualWieldLog.Log($"AttachOffHandWeapon: slot {(int)offHandSlot} is EMPTY â€” aborting");
			return;
		}
		if (DualWieldLog.Unlimited || agent.IsMainAgent)
		{
			object arg = (int)offHandSlot;
			ItemObject item = val.Item;
			string arg2 = ((item != null) ? ((MBObjectBase)item).StringId : null) ?? "NULL";
			ItemObject item2 = val.Item;
			DualWieldLog.Log($"AttachOffHandWeapon: slot {arg}, item={arg2}, flags={((item2 != null) ? (object)item2.ItemFlags : (object)null)}");
		}
		try
		{
			int num = 0;
			EquipmentIndex primaryWieldedItemIndex = agent.GetPrimaryWieldedItemIndex();
			if ((int)primaryWieldedItemIndex != -1)
			{
				MissionWeapon val2 = agent.Equipment[primaryWieldedItemIndex];
				if (!val2.IsEmpty)
				{
					num = val2.CurrentUsageIndex;
				}
			}
			agent.SetWieldedItemIndexAsClient((Agent.HandIndex)1, offHandSlot, true, true, num);
			if (DualWieldLog.Unlimited || agent.IsMainAgent)
			{
				DualWieldLog.Log($"SetWieldedItemIndexAsClient: offhand slot {(int)offHandSlot} for {agent.Name}");
			}
		}
		catch (Exception ex)
		{
			DualWieldLog.Log("SetWieldedItemIndexAsClient failed: " + ex.Message);
		}
		_agentsWithAttachment.Add(agent.Index);
		_offHandSlots[agent.Index] = offHandSlot;
		DualWieldStateManager.SetPaired(agent, paired: true);
		try
		{
			agent.UpdateAgentProperties();
		}
		catch (Exception ex2)
		{
			DualWieldLog.Log("AttachOffHandWeapon: stat refresh failed: " + ex2.Message);
		}
	}

	public void RemoveOffHandAttachment(Agent agent, bool keepInHand = false)
	{
		if (agent == null || !_agentsWithAttachment.Contains(agent.Index))
		{
			return;
		}
		// Claws (GovernedBy=Berserk) are part of the body and must stay in
		// hand for the agent's lifetime. Covers both single-hand-claw and
		// dual-wielded-claw agents; the engine must not sheath the claw
		// mid-combat.
		if (agent.IsActive())
		{
			var offhandIdx = agent.GetOffhandWieldedItemIndex();
			if ((int)offhandIdx != -1 && agent.Equipment[offhandIdx].Item?.IsDualWieldItem() == true)
			{
				return;
			}
		}
		if (agent == Agent.Main)
		{
			ReleasePlayerStrikeState(agent, "dual wield off");
		}
		bool flag = true;
		if (agent.IsActive())
		{
			EquipmentIndex offhandWieldedItemIndex = agent.GetOffhandWieldedItemIndex();
			flag = (int)offhandWieldedItemIndex != -1 && (!_offHandSlots.TryGetValue(agent.Index, out var value) || offhandWieldedItemIndex == value) && DualWieldWieldingPatches.IsOneHandedMeleeWeapon(agent.Equipment[offhandWieldedItemIndex]);
			if (!flag && (int)offhandWieldedItemIndex != -1)
			{
				string name = agent.Name;
				object arg = (int)offhandWieldedItemIndex;
				MissionWeapon val = agent.Equipment[offhandWieldedItemIndex];
				ItemObject item = val.Item;
				DualWieldLog.Log($"[RemoveOH] {name}: left hand holds slot {arg} '{((item != null) ? ((MBObjectBase)item).StringId : null)}' - not ours, left as it is");
			}
		}
		if ((agent.IsActive() & flag) && !keepInHand)
		{
			try
			{
				agent.SetWieldedItemIndexAsClient((Agent.HandIndex)1, (EquipmentIndex)(-1), true, false, 0);
				DualWieldLog.Log("[RemoveOH] Unwielded offhand for " + agent.Name);
			}
			catch (Exception ex)
			{
				DualWieldLog.Log("Unwield offhand failed: " + ex.Message);
			}
		}
		_agentsWithAttachment.Remove(agent.Index);
		_offHandSlots.Remove(agent.Index);
		if (DualWieldStateManager.IsPaired(agent))
		{
			DualWieldStateManager.SetPaired(agent, paired: false);
			if (agent.IsActive())
			{
				try
				{
					agent.UpdateAgentProperties();
				}
				catch (Exception ex2)
				{
					DualWieldLog.Log("[RemoveOH] stat refresh failed: " + ex2.Message);
				}
			}
		}
		DualWieldLog.Log("OffHand removed for agent " + agent.Name);
	}

	private static void DebugHud(InformationMessage message)
	{
		if (DualWieldSettings.Get().DebugMode)
		{
			InformationManager.DisplayMessage(message);
		}
	}

	private void UpdateLeftArrow(Agent agent)
	{
		Agent.UsageDirection playerLeftArrow = (Agent.UsageDirection)(-1);
		try
		{
			if (agent != null && agent.IsActive() && _agentsWithAttachment.Contains(agent.Index))
			{
				if (_windingUp)
				{
					playerLeftArrow = LeftUsable(agent, _windupDir);
				}
				else if ((int)_sepStrikeDir != -1 && SepStrikeRunning(agent))
				{
					playerLeftArrow = _sepStrikeDir;
				}
			}
		}
		catch
		{
		}
		PlayerLeftArrow = playerLeftArrow;
	}

	private static bool MainHandOneHanded(Agent agent)
	{
		try
		{
			EquipmentIndex primaryWieldedItemIndex = agent.GetPrimaryWieldedItemIndex();
			return (int)primaryWieldedItemIndex != -1 && DualWieldWieldingPatches.IsOneHandedMeleeWeapon(agent.Equipment[primaryWieldedItemIndex]);
		}
		catch
		{
			return true;
		}
	}

	private bool HoldsOurOffhand(Agent agent)
	{
		try
		{
			if (!_offHandSlots.TryGetValue(agent.Index, out var value))
			{
				return false;
			}
			EquipmentIndex offhandWieldedItemIndex = agent.GetOffhandWieldedItemIndex();
			return offhandWieldedItemIndex == value && DualWieldWieldingPatches.IsOneHandedMeleeWeapon(agent.Equipment[offhandWieldedItemIndex]);
		}
		catch
		{
			return true;
		}
	}

	private void ProbeAnimRegistration()
	{
		try
		{
			int animationIndexWithName = MBAnimation.GetAnimationIndexWithName("anim_dw_lh_test");
			int animationIndexWithName2 = MBAnimation.GetAnimationIndexWithName("dw_lh_test");
			int numAnimations = MBAnimation.GetNumAnimations();
			string[] array = new string[4] { "dw_rig|anim_dw_verbatim_test", "anim_dw_verbatim_test", "dw_rig", "dw_verbatim_test" };
			foreach (string text in array)
			{
				int num = -999;
				try
				{
					num = MBAnimation.GetAnimationIndexWithName(text);
				}
				catch
				{
				}
				DualWieldLog.Log($"[AnimProbe-verbatim] '{text}' idx={num}");
				if (num >= 0)
				{
					DebugHud(new InformationMessage($"[DW verbatim] '{text}' REGISTERED idx={num}", Colors.Green));
				}
			}
			MBActionSet actionSet = MBActionSet.GetActionSet("as_human_warrior");
			array = new string[4] { "act_dw_cu_slashright", "act_dw_cu_slashleft", "act_dw_rel_slashright_l", "act_dw_rel_slashleft_l" };
			foreach (string text2 in array)
			{
				ActionIndexCache val = ActionIndexCache.Create(text2);
				string text3 = "?";
				bool flag = false;
				try
				{
					text3 = MBActionSet.GetActionAnimationName(actionSet, ref val);
				}
				catch (Exception ex)
				{
					text3 = "EXC:" + ex.Message;
				}
				try
				{
					flag = MBActionSet.CheckActionAnimationClipExists(actionSet, ref val);
				}
				catch
				{
				}
				DualWieldLog.Log(string.Format("[DWactions] {0} code={1} clip='{2}' clipExists={3}", new object[4] { text2, val.Index, text3, flag }));
			}
			array = new string[7] { "anim_dw_v1", "anim_dw_v2", "dw_v3", "dw_v4", "anim_dw_pt_verbatim", "dw_slashright_l", "dw_slashright_r" };
			foreach (string text4 in array)
			{
				int num2 = -999;
				try
				{
					num2 = MBAnimation.GetAnimationIndexWithName(text4);
				}
				catch
				{
				}
				DualWieldLog.Log("[VariantProbe] anim '" + text4 + "' idx=" + num2 + ((num2 >= 0) ? "  ACCEPTED" : "  rejected"));
			}
			array = new string[4] { "act_tor_dwc_qck_release_slashright_1h_left_stance", "act_tor_dwc_qck_release_slashleft_1h_left_stance", "act_tor_dwc_qck_release_overswing_1h_left_stance", "act_tor_dwc_qck_release_thrust_1h_left_stance" };
			foreach (string text5 in array)
			{
				ActionIndexCache val2 = ActionIndexCache.Create(text5);
				string text6 = "?";
				try
				{
					text6 = MBActionSet.GetActionAnimationName(MBActionSet.GetActionSet("as_human_warrior"), ref val2);
				}
				catch
				{
				}
				DualWieldLog.Log("[ActProbe] " + text5 + " index=" + val2.Index + " clip='" + text6 + "'" + ((val2.Index >= 0) ? "" : "  *** NOT REGISTERED ***"));
			}
			array = new string[5] { "dwc_quick_release_slashright_1h_left_stance", "dwc_quick_release_slashleft_1h_left_stance", "dwc_quick_release_overswing_1h_left_stance", "dwc_quick_release_thrust_1h_left_stance", "dw_cu_slashleft" };
			foreach (string text7 in array)
			{
				int num3 = -999;
				try
				{
					num3 = MBAnimation.GetAnimationIndexWithName(text7);
				}
				catch
				{
				}
				float num4 = -1f;
				float num5 = -1f;
				try
				{
					num4 = MBAnimation.GetAnimationDuration(text7);
				}
				catch
				{
				}
				try
				{
					num5 = MBAnimation.GetAnimationBlendInPeriod(text7);
				}
				catch
				{
				}
				DualWieldLog.Log("[VariantProbe] clip '" + text7 + "' idx=" + num3 + " duration=" + num4 + " blendIn=" + num5 + ((num4 > 0f) ? "  ANIM OK" : "  *** ANIM BAD ***"));
			}
			MBActionSet actionSet2 = MBActionSet.GetActionSet("as_human_warrior");
			ActionIndexCache val3 = ActionIndexCache.Create("act_quick_release_slashright_1h_left_stance");
			string text8 = "?";
			bool flag2 = false;
			try
			{
				text8 = MBActionSet.GetActionAnimationName(actionSet2, ref val3);
			}
			catch
			{
			}
			try
			{
				flag2 = MBActionSet.CheckActionAnimationClipExists(actionSet2, ref val3);
			}
			catch
			{
			}
			DualWieldLog.Log($"[AnimProbe] anim_dw_lh_test idx={animationIndexWithName}  dw_lh_test idx={animationIndexWithName2}  totalAnims={numAnimations}");
			DualWieldLog.Log($"[AnimProbe] act_quick_release_slashright_1h_left_stance â†’ clip='{text8}' clipExists={flag2} (XSLT worked if clip=='dw_lh_test')");
			DebugHud(new InformationMessage($"[DW probe] anim_dw_lh_test idx={animationIndexWithName}, actionâ†’'{text8}'", Colors.Cyan));
			string text9 = "?";
			array = new string[4] { "act_quick_release_thrust_1h_left_stance", "act_quick_release_thrust_1h", "act_release_thrust_1h_left_stance", "act_quick_release_slashright_1h_left_stance" };
			foreach (string text10 in array)
			{
				ActionIndexCache val4 = ActionIndexCache.Create(text10);
				string text11 = "?";
				try
				{
					text11 = MBActionSet.GetActionAnimationName(actionSet2, ref val4);
				}
				catch (Exception ex2)
				{
					text11 = "EXC:" + ex2.Message;
				}
				if (text10 == "act_quick_release_thrust_1h_left_stance")
				{
					text9 = text11;
				}
				DualWieldLog.Log($"[AnimProbe-xslt] {text10} (code={val4.Index}) â†’ clip='{text11}'");
			}
			DebugHud(new InformationMessage("[DW probe] thrust_1h_leftâ†’'" + text9 + "' (XSLT applied if =='dw_verbatim_test')", (text9 == "dw_verbatim_test") ? Colors.Green : Colors.Red));
			try
			{
				Agent main = Agent.Main;
				if (main == null || !main.IsActive())
				{
					return;
				}
				EquipmentIndex primaryWieldedItemIndex = main.GetPrimaryWieldedItemIndex();
				string text12;
				if ((int)primaryWieldedItemIndex != -1)
				{
					MissionWeapon val5 = main.Equipment[primaryWieldedItemIndex];
					if (!val5.IsEmpty)
					{
						val5 = main.Equipment[primaryWieldedItemIndex];
						if (val5.CurrentUsageItem != null)
						{
							val5 = main.Equipment[primaryWieldedItemIndex];
							text12 = val5.CurrentUsageItem.ItemUsage;
							goto IL_0603;
						}
					}
				}
				text12 = "none";
				goto IL_0603;
				IL_0603:
				string text13 = text12;
				DualWieldLog.Log("[AnimProbe-player] main weapon usage='" + text13 + "'");
				DebugHud(new InformationMessage("[DW probe] player main usage='" + text13 + "'", Colors.Cyan));
			}
			catch (Exception ex3)
			{
				DualWieldLog.Log("[AnimProbe-player] ERR " + ex3.Message);
			}
		}
		catch (Exception ex4)
		{
			DualWieldLog.Log("[AnimProbe] ERR " + ex4.Message);
		}
	}

	private static void CloseCapture()
	{
		try
		{
			_capW?.Dispose();
		}
		catch
		{
		}
		try
		{
			_capWW?.Dispose();
		}
		catch
		{
		}
		_capW = null;
		_capWW = null;
		_capUntil = 0f;
	}

	private static StreamWriter OpenCapture(string name, string header)
	{
		string text = "D:/Work/Sources/github/bannerlord/HoN/DualWield/tools/capture";
		Directory.CreateDirectory(text);
		string text2 = Path.Combine(text, name + ".csv");
		FileInfo fileInfo = new FileInfo(text2);
		if (fileInfo.Exists && fileInfo.Length > 0)
		{
			fileInfo.MoveTo(Path.Combine(text, name + "_" + fileInfo.LastWriteTime.ToString("yyyyMMdd-HHmmss") + ".csv"));
		}
		StreamWriter streamWriter = new StreamWriter(text2, append: false);
		streamWriter.WriteLine(header);
		return streamWriter;
	}

	private void CaptureArm(string tag)
	{
		if (!CaptureEnabled)
		{
			return;
		}
		try
		{
			if (_capW == null)
			{
				_capW = OpenCapture("poses", "clip,t,ch0,ch1,p0,p1,s0,s1,bone,c0x,c0y,c0z,c1x,c1y,c1z,fx,fy,fz");
				_capWW = OpenCapture("weapons", "clip,t,ch0,p0,which,ox,oy,oz,sx,sy,sz,fx,fy,fz,ux,uy,uz");
			}
			_capTag = tag ?? "";
			try
			{
				Agent mainAgent = Mission.Current.MainAgent;
				string text = "-";
				string text2 = "-";
				if (mainAgent != null)
				{
					MissionWeapon wieldedWeapon = mainAgent.WieldedWeapon;
					if (!wieldedWeapon.IsEmpty && wieldedWeapon.Item != null)
					{
						text = ((MBObjectBase)wieldedWeapon.Item).StringId;
					}
					MissionWeapon wieldedOffhandWeapon = mainAgent.WieldedOffhandWeapon;
					if (!wieldedOffhandWeapon.IsEmpty && wieldedOffhandWeapon.Item != null)
					{
						text2 = ((MBObjectBase)wieldedOffhandWeapon.Item).StringId;
					}
				}
				File.AppendAllText(Path.Combine("D:/Work/Sources/github/bannerlord/HoN/DualWield/tools/capture", "equip.txt"), string.Format("{0:0.000}\t{1}\tmain={2}\toffhand={3}\n", new object[4]
				{
					Mission.Current.CurrentTime,
					_capTag,
					text,
					text2
				}));
			}
			catch (Exception ex)
			{
				DualWieldLog.Log("[CAP] equip failed: " + ex.Message);
			}
			_capUntil = Mission.Current.CurrentTime + 12f;
		}
		catch (Exception ex2)
		{
			DualWieldLog.Log("[CAP] arm failed: " + ex2.Message);
		}
	}

	private void CaptureTick()
	{
		if (_capW == null || Mission.Current == null)
		{
			return;
		}
		float t = Mission.Current.CurrentTime;
		if (t > _capUntil)
		{
			return;
		}
		Agent mainAgent = Mission.Current.MainAgent;
		object obj;
		if (mainAgent == null)
		{
			obj = null;
		}
		else
		{
			MBAgentVisuals agentVisuals = mainAgent.AgentVisuals;
			obj = ((agentVisuals != null) ? agentVisuals.GetSkeleton() : null);
		}
		Skeleton val = (Skeleton)obj;
		if ((NativeObject)(object)val == (NativeObject)null)
		{
			return;
		}
		CultureInfo ci = CultureInfo.InvariantCulture;
		string a0 = "";
		string text = "";
		float p0 = 0f;
		float num = 0f;
		float num2 = 0f;
		float num3 = 0f;
		try
		{
			a0 = val.GetAnimationAtChannel(0) ?? "";
			text = val.GetAnimationAtChannel(1) ?? "";
			p0 = val.GetAnimationParameterAtChannel(0);
			num = val.GetAnimationParameterAtChannel(1);
			num2 = val.GetAnimationSpeedAtChannel(0);
			num3 = val.GetAnimationSpeedAtChannel(1);
		}
		catch
		{
		}
		try
		{
			for (sbyte b = 0; b < 28; b++)
			{
				Vec3 origin = val.GetBoneEntitialFrameAtChannel(0, b).origin;
				Vec3 origin2 = val.GetBoneEntitialFrameAtChannel(1, b).origin;
				Vec3 origin3 = val.GetBoneEntitialFrame(b).origin;
				_capW.WriteLine(string.Join(",", new string[18]
				{
					_capTag,
					t.ToString("0.000", ci),
					a0,
					text,
					p0.ToString("0.000", ci),
					num.ToString("0.000", ci),
					num2.ToString("0.000", ci),
					num3.ToString("0.000", ci),
					b.ToString(),
					origin.x.ToString("0.0000", ci),
					origin.y.ToString("0.0000", ci),
					origin.z.ToString("0.0000", ci),
					origin2.x.ToString("0.0000", ci),
					origin2.y.ToString("0.0000", ci),
					origin2.z.ToString("0.0000", ci),
					origin3.x.ToString("0.0000", ci),
					origin3.y.ToString("0.0000", ci),
					origin3.z.ToString("0.0000", ci)
				}));
			}
			_capW.Flush();
		}
		catch (Exception ex)
		{
			DualWieldLog.Log("[CAP] tick failed: " + ex.Message);
			_capW = null;
		}
		if (_capWW == null)
		{
			return;
		}
		try
		{
			Func<float, string> f4 = (float v) => v.ToString("0.0000", ci);
			Action<string, MatrixFrame> action = (string which, MatrixFrame m) =>
			{
				_capWW.WriteLine(string.Join(",", new string[17]
				{
					_capTag,
					t.ToString("0.000", ci),
					a0,
					p0.ToString("0.000", ci),
					which,
					f4(m.origin.x),
					f4(m.origin.y),
					f4(m.origin.z),
					f4(m.rotation.s.x),
					f4(m.rotation.s.y),
					f4(m.rotation.s.z),
					f4(m.rotation.f.x),
					f4(m.rotation.f.y),
					f4(m.rotation.f.z),
					f4(m.rotation.u.x),
					f4(m.rotation.u.y),
					f4(m.rotation.u.z)
				}));
			};
			EquipmentIndex primaryWieldedItemIndex = mainAgent.GetPrimaryWieldedItemIndex();
			EquipmentIndex offhandWieldedItemIndex = mainAgent.GetOffhandWieldedItemIndex();
			if ((int)primaryWieldedItemIndex != -1)
			{
				WeakGameEntity weaponEntityFromEquipmentSlot = mainAgent.GetWeaponEntityFromEquipmentSlot(primaryWieldedItemIndex);
				if (weaponEntityFromEquipmentSlot.IsValid)
				{
					action("main_local", weaponEntityFromEquipmentSlot.GetFrame());
					action("main_global", weaponEntityFromEquipmentSlot.GetGlobalFrame());
				}
			}
			if ((int)offhandWieldedItemIndex != -1)
			{
				WeakGameEntity weaponEntityFromEquipmentSlot2 = mainAgent.GetWeaponEntityFromEquipmentSlot(offhandWieldedItemIndex);
				if (weaponEntityFromEquipmentSlot2.IsValid)
				{
					action("off_local", weaponEntityFromEquipmentSlot2.GetFrame());
					action("off_global", weaponEntityFromEquipmentSlot2.GetGlobalFrame());
				}
			}
			action("visuals_global", mainAgent.AgentVisuals.GetGlobalFrame());
			action("bone20", val.GetBoneEntitialFrame((sbyte)20));
			action("bone27", val.GetBoneEntitialFrame((sbyte)27));
			_capWW.Flush();
		}
		catch (Exception ex2)
		{
			DualWieldLog.Log("[CAP] weapon tick failed: " + ex2.Message);
			_capWW = null;
		}
	}

	private void RiderPoseProbe()
	{
		if (_riderProbeLines >= 90 || Mission.Current == null || !DualWieldSettings.Get().DebugMode)
		{
			return;
		}
		Agent mainAgent = Mission.Current.MainAgent;
		if (mainAgent == null || !mainAgent.IsActive() || !mainAgent.HasMount || !_agentsWithAttachment.Contains(mainAgent.Index))
		{
			return;
		}
		float currentTime = Mission.Current.CurrentTime;
		if (currentTime < _riderProbeNext)
		{
			return;
		}
		_riderProbeNext = currentTime + 1f;
		try
		{
			MBAgentVisuals agentVisuals = mainAgent.AgentVisuals;
			Skeleton val = ((agentVisuals != null) ? agentVisuals.GetSkeleton() : null);
			if ((NativeObject)(object)val == (NativeObject)null)
			{
				return;
			}
			Vec3 origin = val.GetBoneEntitialFrame((sbyte)0).origin;
			Vec3 val2 = val.GetBoneEntitialFrame((sbyte)19).origin - origin;
			Vec3 val3 = val.GetBoneEntitialFrame((sbyte)26).origin - origin;
			string text = "?";
			string text2 = "?";
			string text3 = "?";
			string text4 = "?";
			string text5 = "?";
			try
			{
				text = ActionCache.NameOf(mainAgent.GetCurrentAction(1));
			}
			catch
			{
			}
			try
			{
				text2 = ActionCache.NameOf(mainAgent.GetCurrentAction(0));
			}
			catch
			{
			}
			MBActionSet actionSet;
			try
			{
				actionSet = mainAgent.ActionSet;
				text3 = actionSet.GetName();
			}
			catch
			{
			}
			try
			{
				ActionIndexCache currentAction = mainAgent.MountAgent.GetCurrentAction(0);
				text4 = ActionCache.NameOf(currentAction);
				string text6;
				if (currentAction.Index < 0)
				{
					text6 = "";
				}
				else
				{
					actionSet = mainAgent.MountAgent.ActionSet;
					text6 = actionSet.GetAnimationName(ref currentAction) ?? "";
				}
				text5 = text6;
			}
			catch
			{
			}
			string text7 = "?";
			try
			{
				Vec3 origin2 = val.GetBoneEntitialFrameAtChannel(0, (sbyte)0).origin;
				Vec3 val4 = val.GetBoneEntitialFrameAtChannel(0, (sbyte)19).origin - origin2;
				Vec3 val5 = val.GetBoneEntitialFrameAtChannel(0, (sbyte)26).origin - origin2;
				text7 = string.Format(CultureInfo.InvariantCulture, "left higher by {0:+0.00;-0.00}, forward by {1:+0.00;-0.00}", val4.z - val5.z, val4.y - val5.y);
			}
			catch
			{
			}
			_riderProbeLines++;
			CultureInfo invariantCulture = CultureInfo.InvariantCulture;
			string format = "[Rider] channel 0 alone: " + text7 + " || v {0:0.0} | ch0 '{1}' #{12} act0 '{13}' | ch1 '{2}' act1 '{3}' | set '{14}' | horse act '{15}' -> horse's set plays '{16}' | left hand {4:+0.00;-0.00} {5:+0.00;-0.00} {6:+0.00;-0.00} | right hand {7:+0.00;-0.00} {8:+0.00;-0.00} {9:+0.00;-0.00} | left higher by {10:+0.00;-0.00} m, further forward by {11:+0.00;-0.00} m";
			object[] array = new object[17];
			Vec3 velocity = (mainAgent.MountAgent ?? mainAgent).Velocity;
			array[0] = velocity.Length;
			array[1] = val.GetAnimationAtChannel(0) ?? "";
			array[2] = val.GetAnimationAtChannel(1) ?? "";
			array[3] = text;
			array[4] = val2.x;
			array[5] = val2.y;
			array[6] = val2.z;
			array[7] = val3.x;
			array[8] = val3.y;
			array[9] = val3.z;
			array[10] = val2.z - val3.z;
			array[11] = val2.y - val3.y;
			array[12] = val.GetAnimationIndexAtChannel(0);
			array[13] = text2;
			array[14] = text3;
			array[15] = text4;
			array[16] = text5;
			DualWieldLog.Log(string.Format(invariantCulture, format, array));
		}
		catch (Exception ex)
		{
			_riderProbeLines = 90;
			DualWieldLog.Log("[Rider] probe failed: " + ex.Message);
		}
	}

	private void LocoProbe(float dt)
	{
		if (_locoLines >= 600 || Mission.Current == null || !DualWieldSettings.Get().DebugMode)
		{
			return;
		}
		Agent mainAgent = Mission.Current.MainAgent;
		if (mainAgent == null || !mainAgent.IsActive())
		{
			return;
		}
		float currentTime = Mission.Current.CurrentTime;
		if (currentTime < _locoNext)
		{
			return;
		}
		_locoNext = currentTime + 0.25f;
		try
		{
			Vec2 movementVelocity = mainAgent.MovementVelocity;
			float length = movementVelocity.Length;
			if (length < 0.5f)
			{
				return;
			}
			MBAgentVisuals agentVisuals = mainAgent.AgentVisuals;
			Skeleton val = ((agentVisuals != null) ? agentVisuals.GetSkeleton() : null);
			if (!((NativeObject)(object)val == (NativeObject)null))
			{
				float z = val.GetBoneEntitialFrame((sbyte)0).origin.z;
				float num = val.GetBoneEntitialFrame((sbyte)19).origin.z - z;
				float num2 = val.GetBoneEntitialFrame((sbyte)26).origin.z - z;
				float num3 = val.GetBoneEntitialFrameAtChannel(1, (sbyte)19).origin.z - val.GetBoneEntitialFrameAtChannel(1, (sbyte)0).origin.z;
				string text = val.GetAnimationAtChannel(0) ?? "";
				string text2 = val.GetAnimationAtChannel(1) ?? "";
				string text3 = "?";
				try
				{
					ActionIndexCache currentAction = mainAgent.GetCurrentAction(1);
					text3 = currentAction.GetName();
				}
				catch
				{
				}
				_locoLines++;
				DualWieldLog.Log(string.Format(CultureInfo.InvariantCulture, "[Loco] t {0:0.00} v {1:0.00} | ch0 '{2}' p {3:0.00} | ch1 '{4}' p {5:0.00} act1 {6} | hand above pelvis L {7:+0.00;-0.00} R {8:+0.00;-0.00} (ch1 alone L {9:+0.00;-0.00})", new object[10]
				{
					currentTime,
					length,
					text,
					val.GetAnimationParameterAtChannel(0),
					text2,
					val.GetAnimationParameterAtChannel(1),
					text3,
					num,
					num2,
					num3
				}));
			}
		}
		catch (Exception ex)
		{
			_locoLines = 600;
			DualWieldLog.Log("[Loco] failed: " + ex.Message);
		}
	}

	public override void OnMissionTick(float dt)
	{
		base.OnMissionTick(dt);
		long started = DwPerf.Start();
		try
		{
			MissionTick(dt);
		}
		finally
		{
			DwPerf.Stop(0, started);
		}
	}

	private void MissionTick(float dt)
	{
		CaptureTick();
		LocoProbe(dt);
		RiderPoseProbe();
		_lastTickDt = dt;
		_leftBlade.Tick(dt);
		TickNpcLeft(dt);
		if (DualWieldSettings.Get().EnableDualWield)
		{
			try
			{
				RangedHybrids.Tick(dt, this);
			}
			catch (Exception ex)
			{
				DualWieldLog.Log("[Ranged] tick failed: " + ex.Message);
			}
		}
		if (!DualWieldSettings.Get().EnableDualWield)
		{
			return;
		}
		if (!_animProbeDone)
		{
			_animProbeDone = true;
			ProbeAnimRegistration();
		}
		if (!_tickLoggedOnce)
		{
			_tickLoggedOnce = true;
			DualWieldLog.Log("OnMissionTick: v10.30 â€” Mode system + usage diagnostics");
			_dwCombatMode = Math.Max(1, Math.Min(2, DualWieldSettings.Get().AttackMode));
			string[] array = new string[3] { "Mirror", "Alternating (LMB)", "Separate (LMB right, own key left)" };
			DualWieldLog.Log($"[DW] Initial mode from settings: {_dwCombatMode} = {array[_dwCombatMode]}");
			try
			{
				DualWieldSettings dualWieldSettings = DualWieldSettings.Get();
				DualWieldLog.Log("[DW] switches: player's left on foot channel " + (dualWieldSettings.FootLeftChannel1 ? 1 : 0) + ", NPC left on foot channel " + (dualWieldSettings.NpcFootLeftChannel1 ? 1 : 0) + ", left on horseback channel " + dualWieldSettings.MountedLeftChannel + ", rider's left takes the AI's target " + (dualWieldSettings.TestRiderAiTarget ? "on" : "OFF") + ", measured rider zones " + (dualWieldSettings.TestRiderZones ? "ON" : "off") + ", free rein hand " + (dualWieldSettings.FreeReinHand ? "on" : "OFF") + ", log look bend " + (dualWieldSettings.TestLogLookBend ? "ON" : "off") + ", everyone dual wields " + (dualWieldSettings.TestNpcDualWield ? "ON" : "off") + string.Format(CultureInfo.InvariantCulture, ", NPC own left strikes {0:0.00}, share leading left {1:0.00}", dualWieldSettings.NpcOffhandFollowUpChance, dualWieldSettings.NpcLeftLeadShare));
			}
			catch
			{
			}
			try
			{
				_settingsModeSeen = DualWieldSettings.Get().AttackMode;
			}
			catch
			{
			}
			try
			{
				int itemUsageIndex = MBItem.GetItemUsageIndex("tor_dw_offhand");
				int itemUsageIndex2 = MBItem.GetItemUsageIndex("tor_dw_mainhand_swing_thrust");
				int itemUsageIndex3 = MBItem.GetItemUsageIndex("hand_shield");
				int itemUsageIndex4 = MBItem.GetItemUsageIndex("onehanded_swing_thrust");
				DualWieldLog.Log($"[UsageIdx] dw_offhand={itemUsageIndex}, dw_mainhand_swing_thrust={itemUsageIndex2}");
				DualWieldLog.Log($"[UsageIdx] hand_shield={itemUsageIndex3}, onehanded_swing_thrust={itemUsageIndex4}");
				DebugHud(new InformationMessage($"[DW] UsageIdx: dw_off={itemUsageIndex} dw_main={itemUsageIndex2} shield={itemUsageIndex3}", (itemUsageIndex >= 0) ? Colors.Green : Colors.Red));
				Agent main = Agent.Main;
				if (main != null && main.IsActive())
				{
					EquipmentIndex primaryWieldedItemIndex = main.GetPrimaryWieldedItemIndex();
					EquipmentIndex offhandWieldedItemIndex = main.GetOffhandWieldedItemIndex();
					string text = "none";
					string text2 = "none";
					int num = -1;
					int num2 = -1;
					if ((int)primaryWieldedItemIndex != -1)
					{
						MissionWeapon val = main.Equipment[primaryWieldedItemIndex];
						if (!val.IsEmpty && val.CurrentUsageItem != null)
						{
							text = val.CurrentUsageItem.ItemUsage ?? "null";
							num = (string.IsNullOrEmpty(text) ? (-1) : MBItem.GetItemUsageIndex(text));
						}
					}
					if ((int)offhandWieldedItemIndex != -1)
					{
						MissionWeapon val2 = main.Equipment[offhandWieldedItemIndex];
						if (!val2.IsEmpty && val2.CurrentUsageItem != null)
						{
							text2 = val2.CurrentUsageItem.ItemUsage ?? "null";
							num2 = (string.IsNullOrEmpty(text2) ? (-1) : MBItem.GetItemUsageIndex(text2));
						}
					}
					DualWieldLog.Log($"[Player] mainSlot={primaryWieldedItemIndex} usage='{text}' nativeIdx={num}");
					DualWieldLog.Log($"[Player] offSlot={offhandWieldedItemIndex} usage='{text2}' nativeIdx={num2}");
					DebugHud(new InformationMessage(string.Format("[DW] Main: '{0}'={1} Off: '{2}'={3}", new object[4] { text, num, text2, num2 }), (num2 >= 0) ? Colors.Green : Colors.Yellow));
				}
			}
			catch (Exception ex2)
			{
				DualWieldLog.Log("[UsageIdx] Error: " + ex2.Message);
			}
		}
		_tickCount++;
		if (_pendingDiagTicks > 0 && --_pendingDiagTicks == 0 && _pendingDiag != null)
		{
			AnimDiagnostics.Dump(Agent.Main, _pendingDiag);
		}
		Agent main2 = Agent.Main;
		UpdateSeparateKeyOverride(main2);
		UpdatePlayerStrikeSlow(main2);
		UpdateLeftArrow(main2);
		if (main2 != null && main2.IsActive())
		{
			try
			{
				DualWieldWieldingPatches.TickLeftGestures(main2, this);
			}
			catch (Exception ex3)
			{
				DualWieldLog.Log("[Wield] left gesture tick failed: " + ex3.Message);
			}
		}
		if (_tickCount % 15 == 0 && main2 != null && main2.IsActive() && !DualWieldStateManager.IsDualWielding(main2) && DualWieldDetector.PlayerCanDualWield(main2.Equipment))
		{
			try
			{
				DualWieldSpawnPatches.RegisterPlayerLate(main2);
			}
			catch (Exception ex4)
			{
				DualWieldLog.Log("[Spawn] late registration failed: " + ex4.Message);
			}
		}
		if (_tickCount % 15 == 0 && main2 != null && main2.IsActive() && !_agentsWithAttachment.Contains(main2.Index) && DualWieldStateManager.IsDualWielding(main2) && (int)main2.GetOffhandWieldedItemIndex() != -1)
		{
			try
			{
				DualWieldWieldingPatches.AdoptHeldOffhand(main2, this);
			}
			catch (Exception ex5)
			{
				DualWieldLog.Log("[Wield] adopt failed: " + ex5.Message);
			}
		}
		if (_agentsWithAttachment.Count == 0)
		{
			return;
		}
		if (main2 != null && main2.IsActive() && _agentsWithAttachment.Contains(main2.Index) && (!HoldsOurOffhand(main2) || !MainHandOneHanded(main2)))
		{
			EquipmentIndex offhandWieldedItemIndex2 = main2.GetOffhandWieldedItemIndex();
			EquipmentIndex primaryWieldedItemIndex2 = main2.GetPrimaryWieldedItemIndex();
			string[] array2 = new string[7] { "[DW] ", main2.Name, ": left hand holds ", null, null, null, null };
			MissionWeapon val3;
			string text3;
			if ((int)offhandWieldedItemIndex2 != -1)
			{
				val3 = main2.Equipment[offhandWieldedItemIndex2];
				ItemObject item = val3.Item;
				text3 = "'" + ((item != null) ? ((MBObjectBase)item).StringId : null) + "'";
			}
			else
			{
				text3 = "nothing";
			}
			array2[3] = text3;
			array2[4] = ", right hand ";
			string text4;
			if ((int)primaryWieldedItemIndex2 != -1)
			{
				val3 = main2.Equipment[primaryWieldedItemIndex2];
				ItemObject item2 = val3.Item;
				text4 = "'" + ((item2 != null) ? ((MBObjectBase)item2).StringId : null) + "'";
			}
			else
			{
				text4 = "nothing";
			}
			array2[5] = text4;
			array2[6] = " - dual wield off, keys back to vanilla";
			DualWieldLog.Log(string.Concat(array2));
			RemoveOffHandAttachment(main2);
		}
		if (main2 != null && main2.IsActive() && _agentsWithAttachment.Contains(main2.Index))
		{
			ProcessOffhandUsageToggle(main2);
			FollowSettingsMode(main2);
			if (main2.HasMount != _wasMounted)
			{
				OnMountChanged(main2);
			}
			if (!main2.HasMount || MountedLeftOn)
			{
				if (_dwCombatMode == 2)
				{
					if (SeparateOnGamepad(main2))
					{
						ProcessAlternatingMode(main2);
					}
					else
					{
						ProcessSeparatedMode(main2);
					}
				}
				else
				{
					ProcessAlternatingMode(main2);
				}
			}
			TickLookBendControl(main2);
			ProcessManualTest(main2);
		}
		if (!_indicatorShown && _tickCount > 120 && main2 != null && main2.IsActive() && _agentsWithAttachment.Contains(main2.Index))
		{
			_indicatorShown = true;
			string[] array3 = new string[3] { "Mirror", "Alternating (every second strike is the left one)", "Separate (left hand on its own key)" };
			InformationManager.DisplayMessage(new InformationMessage("Dual wielding - mode: " + array3[Math.Max(0, Math.Min(2, _dwCombatMode))] + ". N switches the mode.", Colors.Green));
		}
		if (_tickCount % 300 == 0)
		{
			DualWieldLog.Log($"[Tick] heartbeat #{_tickCount}, tracking {_agentsWithAttachment.Count}, followUps={_followUpCount}");
			DualWieldLog.Log(DwPerf.Report());
		}
		if (_tickCount % 60 != 0)
		{
			return;
		}
		long started = DwPerf.Start();
		int[] array4 = _agentsWithAttachment.ToArray();
		foreach (int num3 in array4)
		{
			Mission current = Mission.Current;
			Agent val4 = ((current != null) ? current.FindAgentWithIndex(num3) : null);
			if (val4 == null || !val4.IsActive())
			{
				_agentsWithAttachment.Remove(num3);
				_offHandSlots.Remove(num3);
			}
		}
		DwPerf.Stop(3, started);
	}

	public static float LeftStrikeSpeed(Agent agent)
	{
		try
		{
			if (DualWieldSettings.Get().LeftStrikeSlowdownOff)
			{
				return 1f;
			}
			int oneHanded = 0;
			try
			{
				BasicCharacterObject character = agent.Character;
				oneHanded = ((character != null) ? character.GetSkillValue(DefaultSkills.OneHanded) : 0);
			}
			catch
			{
			}
			return DwRules.LeftStrikeSpeed(oneHanded, HeroAttributes(agent), off: false);
		}
		catch
		{
			return 0.6f;
		}
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static int HeroAttributes(Agent agent)
	{
		try
		{
			BasicCharacterObject character = agent.Character;
			BasicCharacterObject obj = ((character is CharacterObject) ? character : null);
			Hero val = ((obj != null) ? ((CharacterObject)obj).HeroObject : null);
			if (val == null || DefaultCharacterAttributes.Vigor == null)
			{
				return 0;
			}
			return val.GetAttributeValue(DefaultCharacterAttributes.Vigor) + val.GetAttributeValue(DefaultCharacterAttributes.Control);
		}
		catch
		{
			return 0;
		}
	}

	private void UpdatePlayerStrikeSlow(Agent agent)
	{
		bool flag = agent != null && agent.IsActive();
		bool flag2 = flag && !agent.HasMount && _agentsWithAttachment.Contains(agent.Index) && (_dwCombatMode == 1 || _dwCombatMode == 2) && (_windingUp || SepStrikeRunning(agent));
		bool flag3 = false;
		if (flag)
		{
			Vec3 velocity = agent.Velocity;
			Vec2 asVec = velocity.AsVec2;
			float length = asVec.Length;
			bool flag4 = false;
			try
			{
				Agent.ActionCodeType currentActionType = agent.GetCurrentActionType(1);
				flag4 = (int)currentActionType == 19 || (int)currentActionType == 20;
			}
			catch
			{
			}
			flag3 = flag4 && !flag2;
			if (flag2)
			{
				_speedLeftSum += length;
				_speedLeftN++;
			}
			else if (_speedLeftN > 0)
			{
				DualWieldLog.Log($"[Speed] left strike: mean {_speedLeftSum / (float)_speedLeftN:0.00} m/s over {_speedLeftN} ticks (speed x{LeftStrikeSpeed(agent):0.00})");
				_speedLeftSum = 0f;
				_speedLeftN = 0;
			}
			if (flag4 && !flag2 && _agentsWithAttachment.Contains(agent.Index))
			{
				_speedRightSum += length;
				_speedRightN++;
			}
			else if (_speedRightN > 0)
			{
				DualWieldLog.Log($"[Speed] right swing: mean {_speedRightSum / (float)_speedRightN:0.00} m/s over {_speedRightN} ticks (vanilla)");
				_speedRightSum = 0f;
				_speedRightN = 0;
			}
		}
		int num;
		bool flag5;
		if (flag && LeftHanded && !agent.HasMount)
		{
			num = (_agentsWithAttachment.Contains(agent.Index) ? 1 : 0);
			if (num != 0)
			{
				flag5 = flag3;
				goto IL_01cb;
			}
		}
		else
		{
			num = 0;
		}
		flag5 = flag2;
		goto IL_01cb;
		IL_01cb:
		bool flag6 = flag5;
		bool flag7 = (byte)((uint)num & (flag2 ? 1u : 0u)) != 0;
		if (flag6 == PlayerOffHandStriking && flag7 == PlayerMainLeftStriking)
		{
			return;
		}
		_strikeSlowOn = flag6 | flag7;
		PlayerOffHandStriking = flag6;
		PlayerMainLeftStriking = flag7;
		if (!flag)
		{
			return;
		}
		try
		{
			agent.UpdateAgentProperties();
		}
		catch (Exception ex)
		{
			DualWieldLog.Log("[Speed] stat refresh failed: " + ex.Message);
		}
	}

	private void UpdateSeparateKeyOverride(Agent agent)
	{
		if (_dwCombatMode != 2 || agent == null || !agent.IsActive() || (agent.HasMount && !MountedLeftOn) || !_agentsWithAttachment.Contains(agent.Index) || !HoldsOurOffhand(agent) || !MainHandOneHanded(agent))
		{
			SeparateKeyOverride.Restore();
			return;
		}
		InputKey leftHandKey = (InputKey)225;
		try
		{
			leftHandKey = DualWieldSettings.Get().LeftHandKey;
		}
		catch
		{
		}
		SeparateKeyOverride.Apply(leftHandKey, LeftHanded);
	}

	private void ReleasePlayerStrikeState(Agent agent, string why)
	{
		try
		{
			if (agent != null && agent.IsActive())
			{
				AgentFlag agentFlags = agent.GetAgentFlags();
				if ((agentFlags & (AgentFlag)(8)) == 0)
				{
					agent.SetAgentFlags((AgentFlag)(agentFlags | (AgentFlag)(8)));
				}
			}
		}
		catch
		{
		}
		_windingUp = false;
		_nextIsLeft = false;
		_sepQueued = false;
		_sepStrikeAt = -10f;
		_sepReleasePending = false;
		_vanillaSwingWasActive = false;
		if (_leftBlade.Active)
		{
			_leftBlade.Stop(why);
		}
	}

	private void OnMountChanged(Agent agent)
	{
		_wasMounted = agent.HasMount;
		try
		{
			AgentFlag agentFlags = agent.GetAgentFlags();
			if ((agentFlags & (AgentFlag)(8)) == 0)
			{
				agent.SetAgentFlags((AgentFlag)(agentFlags | (AgentFlag)(8)));
			}
		}
		catch
		{
		}
		_windingUp = false;
		_nextIsLeft = false;
		_sepQueued = false;
		_sepStrikeAt = -10f;
		_sepReleasePending = false;
		_leftBlade.Stop(_wasMounted ? "mounted" : "dismounted");
		string? text;
		if (_wasMounted)
		{
			text = (MountedLeftOn ? ("mounted - left strikes from horseback, channel " + LeftChannel(agent)) : "mounted - left strikes off (MCM test switch), the left weapon guards");
		}
		else
		{
			text = "on foot - left strikes on channel " + LeftChannel(agent);
		}
		DualWieldLog.Log("[DW] " + text);
	}

	private static int LeftChannel(Agent agent)
	{
		if (agent == null || !agent.HasMount)
		{
			return FootChannel1(agent) ? 1 : 0;
		}
		return Math.Max(0, MountedChannelSetting());
	}

	private static bool FootChannel1(Agent agent)
	{
		if (agent == null || !agent.IsMainAgent || agent.HasMount)
		{
			return false;
		}
		try
		{
			return DualWieldSettings.Get().FootLeftChannel1;
		}
		catch
		{
			return false;
		}
	}

	private static int MountedChannelSetting()
	{
		try
		{
			return DualWieldSettings.Get().MountedLeftChannel;
		}
		catch
		{
			return -1;
		}
	}

	private ActionIndexCache MountedAction(Agent agent, Agent.UsageDirection dir, bool ready)
	{
		dir = LeftUsable(agent, dir);
		string text;
		if ((int)dir == 2)
		{
			text = "slash_horseback_right";
		}
		else if ((int)dir == 3)
		{
			text = "slash_horseback_left";
		}
		else
		{
			text = (((int)dir == 1) ? "thrust_1h_horseback" : "overswing_horseback");
		}
		return ActionCache.Get("act_tor_dwc_" + (ready ? "ready_" : "quick_release_") + text);
	}

	private static ulong LeftFlags(ulong baseFlags)
	{
		try
		{
			if (DualWieldSettings.Get().TestLeftLookSlope)
			{
				return baseFlags | 0x100000;
			}
		}
		catch
		{
		}
		return baseFlags;
	}

	private bool SepStrikeRunning(Agent agent, float endAt = 0.9f)
	{
		if (_leftBlade.Stunned)
		{
			return true;
		}
		float num = Mission.Current.CurrentTime - _sepStrikeAt;
		if (num < 0.12f)
		{
			return true;
		}
		if (num > 1.5f)
		{
			return false;
		}
		try
		{
			int num2 = LeftChannel(agent);
			ActionIndexCache currentAction = agent.GetCurrentAction(num2);
			if (currentAction.Index != _sepStrikeAct.Index)
			{
				return false;
			}
			if (endAt < 0.9f && agent.GetCurrentActionProgress(num2) >= endAt)
			{
				return false;
			}
			MBAgentVisuals agentVisuals = agent.AgentVisuals;
			Skeleton val = ((agentVisuals != null) ? agentVisuals.GetSkeleton() : null);
			return (NativeObject)(object)val != (NativeObject)null && val.GetAnimationParameterAtChannel(num2) < 0.9f;
		}
		catch
		{
			return num < 0.66f;
		}
	}

	private void ProcessSeparatedMode(Agent agent)
	{
		InputKey val = (InputKey)225;
		try
		{
			val = DualWieldSettings.Get().LeftHandKey;
		}
		catch
		{
		}
		if (LeftHanded && (int)val != 224)
		{
			val = (InputKey)224;
		}
		LeftStrikeInput(agent, Input.IsKeyDown(val), otherSwingRunning: false, null, "sep");
	}

	private bool LeftStrikeInput(Agent agent, bool keyDown, bool otherSwingRunning, Func<Agent.UsageDirection> aim, string tag)
	{
		bool flag = otherSwingRunning || SepStrikeRunning(agent, _sepQueued ? DwRules.ChainPoint(_leftBlade.WindowEnd) : 0.9f);
		float currentTime = Mission.Current.CurrentTime;
		Func<Agent.UsageDirection> func = aim ?? ((Func<Agent.UsageDirection>)(() =>
		{
			return SafeAttackDirection(agent);
		}));
		if (_windingUp && _windupAct.Index >= 0 && currentTime - _sepWindupAt > 0.1f)
		{
			int index = _windupAct.Index;
			try
			{
				ActionIndexCache currentAction = agent.GetCurrentAction(LeftChannel(agent));
				index = currentAction.Index;
			}
			catch
			{
			}
			if (index != _windupAct.Index)
			{
				DualWieldLog.Log("[DW " + tag + "] wind-up interrupted ('" + ActionCache.NameOf(agent.GetCurrentAction(LeftChannel(agent))) + "')");
				_windingUp = false;
				_sepReleasePending = false;
				_sepQueued = false;
			}
		}
		if ((keyDown && !_rmbWasDown) & flag)
		{
			if (!_sepQueued)
			{
				DualWieldLog.Log("[DW " + tag + "] press buffered, strike still running");
			}
			_sepQueued = true;
		}
		if (!keyDown && !flag && _sepQueued && !_windingUp)
		{
			Agent.UsageDirection val = func();
			_windupHeight = HeightOf(agent);
			ActionIndexCache windupAct = AtHeight(ReadyForDirection(agent, val), _windupHeight);
			if (windupAct.Index >= 0)
			{
				agent.SetActionChannel(LeftChannel(agent), ref windupAct, true, (AnimFlags)LeftFlags(33uL), 0f, 1f, -0.2f, 0.4f, 0f, false, -0.2f, 0, true);
				_windupAct = windupAct;
				_windupDir = val;
				_windingUp = true;
				_sepWindupAt = currentTime;
				DualWieldLog.Log("[DW " + tag + "] buffered strike -> wind-up " + windupAct.GetName());
			}
			_sepQueued = false;
			_sepReleasePending = true;
		}
		if (keyDown && !flag && !_windingUp && (!_rmbWasDown || _sepQueued))
		{
			if (!_windingUp)
			{
				_sepWindupAt = currentTime;
			}
			_sepQueued = false;
			Agent.UsageDirection val2 = func();
			_windupHeight = HeightOf(agent);
			ActionIndexCache windupAct2 = AtHeight(ReadyForDirection(agent, val2), _windupHeight);
			if (windupAct2.Index >= 0 && (!_windingUp || windupAct2.Index != _windupAct.Index))
			{
				agent.SetActionChannel(LeftChannel(agent), ref windupAct2, true, (AnimFlags)LeftFlags(33uL), 0f, 1f, -0.2f, 0.4f, 0f, false, -0.2f, 0, true);
				_windupAct = windupAct2;
				_windupDir = val2;
				if (!_windingUp)
				{
					DualWieldLog.Log("[DW " + tag + "] wind-up -> " + windupAct2.GetName());
				}
				_windingUp = true;
			}
		}
		bool result = false;
		if (!keyDown && _rmbWasDown && _windingUp)
		{
			_sepReleasePending = true;
		}
		if (!flag && !keyDown && (_sepReleasePending || _sepQueued) && (!_windingUp || currentTime - _sepWindupAt >= 0.15f))
		{
			bool windingUp = _windingUp;
			_windingUp = false;
			_sepQueued = false;
			_sepReleasePending = false;
			Agent.UsageDirection val3 = (windingUp ? _windupDir : func());
			_sepStrikeDir = LeftUsable(agent, val3);
			ActionIndexCache val4 = AtHeight(MirrorForDirection(agent, val3), windingUp ? _windupHeight : HeightOf(agent));
			if (val4.Index < 0)
			{
				val4 = _actDwSlashLeft;
			}
			bool flag2 = agent.SetActionChannel(LeftChannel(agent), ref val4, true, (AnimFlags)LeftFlags(33uL), 0f, 1f, -0.2f, 0.4f, 0f, false, -0.2f, 0, true);
			StartLeftDamage(agent, ActionCache.NameOf(val4), LeftUsable(agent, val3));
			_sepStrikeAct = val4;
			_sepStrikeAt = currentTime;
			_followUpCount++;
			DualWieldLog.Log(string.Format("[DW {0}] release -> clip {1} ({2}) on channel {3} set={4}", new object[5]
			{
				tag,
				val4.GetName(),
				val3,
				LeftChannel(agent),
				flag2
			}));
			if (flag2)
			{
				DualWieldBalance.PlayerLeftFired(agent.HasMount);
			}
			CaptureArm(tag + " " + ActionCache.NameOf(val4));
			DebugHud(new InformationMessage("[DW] LINKS", Colors.Green));
			result = true;
		}
		_rmbWasDown = keyDown;
		return result;
	}

	private static Agent.UsageDirection ArrivalSide(Agent.UsageDirection dir)
	{
		if ((int)dir != 2)
		{
			if ((int)dir == 3)
			{
				return (Agent.UsageDirection)2;
			}
			return dir;
		}
		return (Agent.UsageDirection)3;
	}

	private static Agent.UsageDirection SafeAttackDirection(Agent agent)
	{
		try
		{
			return agent.PlayerAttackDirection();
		}
		catch
		{
			return (Agent.UsageDirection)3;
		}
	}

	private string DumpChannels(Agent agent)
	{
		string text = "";
		for (int i = 0; i < 4; i++)
		{
			try
			{
				string text2 = text;
				object arg = i;
				ActionIndexCache currentAction = agent.GetCurrentAction(i);
				text = text2 + $"ch{arg}='{currentAction.GetName()}' ";
			}
			catch
			{
				text += $"ch{i}=? ";
			}
		}
		return text;
	}

	private static bool AttackKeyDown()
	{
		return GameKeyDown(9, (InputKey)224);
	}

	private static bool GameKeyDown(int id, InputKey fallback)
	{
		try
		{
			GameKeyContext category = HotKeyManager.GetCategory("CombatHotKeyCategory");
			GameKey val = ((category != null) ? category.GetGameKey(id) : null);
			if (val == null)
			{
				return Input.IsKeyDown(fallback);
			}
			Key keyboardKey = val.KeyboardKey;
			if (keyboardKey != (Key)null && (int)keyboardKey.InputKey != -1 && Input.IsKeyDown(keyboardKey.InputKey))
			{
				return true;
			}
			Key controllerKey = val.ControllerKey;
			return controllerKey != (Key)null && (int)controllerKey.InputKey != -1 && Input.IsKeyDown(controllerKey.InputKey);
		}
		catch
		{
			return Input.IsKeyDown(fallback);
		}
	}

	private bool SeparateOnGamepad(Agent agent)
	{
		bool flag = false;
		try
		{
			flag = Input.IsGamepadActive;
		}
		catch
		{
		}
		if (flag != _padAlternating && !_windingUp && !_leftBlade.Active && !SepStrikeRunning(agent))
		{
			ReleasePlayerStrikeState(agent, "input device changed");
			_padAlternating = flag;
		}
		if (!_padAlternating)
		{
			return false;
		}
		if (!_padHintShown)
		{
			_padHintShown = true;
			DualWieldLog.Log("[DW] Separate mode with a gamepad active: fighting Alternating (the attack trigger strikes right and left in turn)");
			InformationManager.DisplayMessage(new InformationMessage("Dual Wield: on a gamepad the attack trigger strikes right and left in turn (Separate mode needs mouse and keyboard).", Colors.Cyan));
		}
		return true;
	}

	private void ProcessAlternatingMode(Agent agent)
	{
		bool flag = false;
		try
		{
			for (int i = 1; i < 4; i++)
			{
				if (flag)
				{
					break;
				}
				string text = ActionCache.NameOf(agent.GetCurrentAction(i));
				if (!string.IsNullOrEmpty(text) && text.IndexOf("release", StringComparison.OrdinalIgnoreCase) >= 0 && !text.StartsWith("act_tor_dwc_", StringComparison.Ordinal))
				{
					flag = true;
				}
			}
		}
		catch
		{
		}
		bool flag2 = flag && !_vanillaSwingWasActive;
		_vanillaSwingWasActive = flag;
		bool flag3 = AttackKeyDown();
		float currentTime = Mission.Current.CurrentTime;
		if (flag2)
		{
			_lastRightAt = currentTime;
		}
		if (LeftHanded && !_nextIsLeft && !flag && !_windingUp && DwRules.LeftTurnAfterPause(currentTime, _lastRightAt, _sepStrikeAt))
		{
			_nextIsLeft = true;
			_lastRightDir = (Agent.UsageDirection)(-1);
			_leftTurnAimAtStart = SafeAttackDirection(agent);
		}
		if (!_nextIsLeft & flag2)
		{
			_nextIsLeft = true;
			_lastRightDir = SafeAttackDirection(agent);
			_leftTurnAimAtStart = _lastRightDir;
			DebugHud(new InformationMessage("[DW] right", Colors.Cyan));
		}
		bool flag4 = _nextIsLeft || _windingUp || SepStrikeRunning(agent);
		try
		{
			AgentFlag agentFlags = agent.GetAgentFlags();
			bool flag5 = (agentFlags & (AgentFlag)(8)) > 0;
			if (flag4 & flag5)
			{
				agent.SetAgentFlags((AgentFlag)(agentFlags & ~(AgentFlag)8));
			}
			else if (!flag4 && !flag5)
			{
				agent.SetAgentFlags((AgentFlag)(agentFlags | (AgentFlag)(8)));
			}
		}
		catch (Exception ex)
		{
			DualWieldLog.Log("[DW alt] SetAgentFlags failed: " + ex.Message);
		}
		if (!_nextIsLeft)
		{
			_rmbWasDown = flag3;
		}
		else if (LeftStrikeInput(agent, flag3, flag, () =>
		{
			return AltLeftDirection(agent);
		}, "alt"))
		{
			_nextIsLeft = false;
		}
	}

	private void TickLookBendControl(Agent agent)
	{
		try
		{
			bool flag = false;
			try
			{
				flag = DualWieldSettings.Get().TestLogLookBend;
			}
			catch
			{
			}
			if (!flag || agent == null)
			{
				return;
			}
			TickLookBendOurs(agent);
			if ((int)agent.GetCurrentActionType(1) != 20)
			{
				_bendAction = -1;
				return;
			}
			ActionIndexCache currentAction = agent.GetCurrentAction(1);
			int index = currentAction.Index;
			if (index != _bendAction)
			{
				_bendAction = index;
				_bendSampled = false;
				Vec3 lookDirection = agent.LookDirection;
				_bendPitch = (float)(Math.Asin(MathF.Clamp(lookDirection.z, -1f, 1f)) * 180.0 / Math.PI);
				Mat3 rotation = agent.Frame.rotation;
				float num = lookDirection.x * rotation.f.x + lookDirection.y * rotation.f.y;
				float num2 = lookDirection.x * rotation.s.x + lookDirection.y * rotation.s.y;
				_bendYaw = (float)(Math.Atan2(num2, num) * 180.0 / Math.PI);
			}
			if (!_bendSampled && !(agent.GetCurrentActionProgress(1) < 0.45f))
			{
				_bendSampled = true;
				MBAgentVisuals agentVisuals = agent.AgentVisuals;
				Skeleton val = ((agentVisuals != null) ? agentVisuals.GetSkeleton() : null);
				if (!((NativeObject)(object)val == (NativeObject)null) && _bendLogs++ < 150)
				{
					Vec3 origin = val.GetBoneEntitialFrame((sbyte)13).origin;
					Vec3 origin2 = val.GetBoneEntitialFrame((sbyte)26).origin;
					Vec3 origin3 = val.GetBoneEntitialFrame((sbyte)0).origin;
					DualWieldLog.Log(string.Format(CultureInfo.InvariantCulture, "[LookBend] {0} {1}: look pitch {2:+0;-0} yaw {3:+0;-0} deg | head {4:0.00} {5:0.00} {6:0.00} | right hand {7:0.00} {8:0.00} {9:0.00} | pelvis {10:0.00} {11:0.00} {12:0.00} (model space x y z, progress 0.45)", new object[13]
					{
						agent.HasMount ? "mounted" : "on foot",
						ActionCache.NameOf(agent.GetCurrentAction(1)),
						_bendPitch,
						_bendYaw,
						origin.x,
						origin.y,
						origin.z,
						origin2.x,
						origin2.y,
						origin2.z,
						origin3.x,
						origin3.y,
						origin3.z
					}));
				}
			}
		}
		catch
		{
		}
	}

	private void TickLookBendOurs(Agent agent)
	{
		int num = -1;
		string text = null;
		for (int i = 0; i < 2; i++)
		{
			if (num >= 0)
			{
				break;
			}
			string text2 = ActionCache.NameOf(agent.GetCurrentAction(i));
			if (text2 != null && text2.StartsWith("act_tor_dwc_qck_release_", StringComparison.Ordinal))
			{
				num = i;
				text = text2;
			}
		}
		if (num < 0)
		{
			_oursAction = -1;
			return;
		}
		ActionIndexCache currentAction = agent.GetCurrentAction(num);
		int index = currentAction.Index;
		if (index != _oursAction)
		{
			_oursAction = index;
			_oursSampled = false;
			Vec3 lookDirection = agent.LookDirection;
			_oursPitch = (float)(Math.Asin(MathF.Clamp(lookDirection.z, -1f, 1f)) * 180.0 / Math.PI);
			Mat3 rotation = agent.Frame.rotation;
			_oursYaw = (float)(Math.Atan2(lookDirection.x * rotation.s.x + lookDirection.y * rotation.s.y, lookDirection.x * rotation.f.x + lookDirection.y * rotation.f.y) * 180.0 / Math.PI);
		}
		if (!_oursSampled && !(agent.GetCurrentActionProgress(num) < 0.45f))
		{
			_oursSampled = true;
			MBAgentVisuals agentVisuals = agent.AgentVisuals;
			Skeleton val = ((agentVisuals != null) ? agentVisuals.GetSkeleton() : null);
			if (!((NativeObject)(object)val == (NativeObject)null) && _bendLogs++ < 150)
			{
				Vec3 origin = val.GetBoneEntitialFrame((sbyte)13).origin;
				Vec3 origin2 = val.GetBoneEntitialFrame((sbyte)19).origin;
				Vec3 origin3 = val.GetBoneEntitialFrame((sbyte)0).origin;
				DualWieldLog.Log(string.Format(CultureInfo.InvariantCulture, "[LookBend] ours {0} {1}: look pitch {2:+0;-0} yaw {3:+0;-0} deg | head {4:0.00} {5:0.00} {6:0.00} | left hand {7:0.00} {8:0.00} {9:0.00} | pelvis {10:0.00} {11:0.00} {12:0.00} (model space x y z, progress 0.45)", new object[13]
				{
					agent.HasMount ? "mounted" : "on foot",
					text,
					_oursPitch,
					_oursYaw,
					origin.x,
					origin.y,
					origin.z,
					origin2.x,
					origin2.y,
					origin2.z,
					origin3.x,
					origin3.y,
					origin3.z
				}));
			}
		}
	}

	private Agent.UsageDirection AltLeftDirection(Agent agent)
	{
		Agent.UsageDirection val = SafeAttackDirection(agent);
		bool flag = false;
		try
		{
			flag = DualWieldSettings.Get().AlternatingCrossCombo;
		}
		catch
		{
		}
		if (!flag)
		{
			return val;
		}
		if (val != _leftTurnAimAtStart)
		{
			return val;
		}
		Agent.UsageDirection lastRightDir = _lastRightDir;
		return (int)lastRightDir switch
		{
			2 => (Agent.UsageDirection)3, 
			3 => (Agent.UsageDirection)2, 
			0 => (Agent.UsageDirection)1, 
			1 => (Agent.UsageDirection)0, 
			_ => val, 
		};
	}

	private void ProcessManualTest(Agent agent)
	{
		bool flag = Input.IsKeyDown((InputKey)49);
		if (flag && !_nWasDown)
		{
			CycleDWMode(agent);
		}
		_nWasDown = flag;
		if (!DualWieldSettings.Get().DebugMode)
		{
			return;
		}
		if (_dbgChanWatch > 0 && agent != null && agent.IsActive())
		{
			DualWieldLog.Log($"[DW watch {_dbgChanWatch}] {DumpChannels(agent)}");
			_dbgChanWatch--;
		}
		if (_clipPlayTicks > 0)
		{
			_clipPlayTicks--;
			if (_clipPlayTicks == 0 && agent != null && agent.IsActive())
			{
				try
				{
					MBAgentVisuals agentVisuals = agent.AgentVisuals;
					Skeleton val = ((agentVisuals != null) ? agentVisuals.GetSkeleton() : null);
					if ((NativeObject)(object)val != (NativeObject)null)
					{
						ActionIndexCache actionAtChannel = MBSkeletonExtensions.GetActionAtChannel(val, 0);
						string text = "?";
						try
						{
							text = actionAtChannel.GetName();
						}
						catch
						{
						}
						string text2 = "?";
						try
						{
							text2 = val.GetAnimationAtChannel(0);
						}
						catch
						{
						}
						DualWieldLog.Log($"[DW clip] restore-state action='{text}' anim='{text2}' savedAnimIdx={_savedCh0AnimIdx}");
						DebugHud(new InformationMessage("[DW clip] act='" + text + "' anim='" + text2 + "'", Colors.Cyan));
						{ ActionIndexCache _tmpAct = _idleReady1h; agent.SetActionChannel(0, ref _tmpAct, true, (AnimFlags)0, 0f, 1f, 0.25f, 0f, 0f, false, -0.2f, 0, true); }
					}
				}
				catch (Exception ex)
				{
					DualWieldLog.Log("[DW clip] restore ERR " + ex.Message);
				}
			}
		}
		bool flag2 = Input.IsKeyDown((InputKey)25);
		if (flag2 && !_pWasDown)
		{
			FireClipAction(agent, _mirBit, "compact segment, 12 bones re-encoded mirrored");
		}
		_pWasDown = flag2;
		bool flag3 = Input.IsKeyDown((InputKey)24);
		if (flag3 && !_oWasDown)
		{
			AnimDiagnostics.Dump(agent, "BEFORE-editor");
			FireClipAction(agent, _actDwSlashLeft, "act_dw_cu_slashleft (editor)");
			_pendingDiag = "AFTER-editor";
			_pendingDiagTicks = 8;
		}
		_oWasDown = flag3;
		bool flag4 = Input.IsKeyDown((InputKey)34);
		if (flag4 && !_gWasDown)
		{
			AnimDiagnostics.Dump(agent, "BEFORE-self");
			_pendingDiag = "AFTER-self";
			_pendingDiagTicks = 3;
			FireClipAction(agent, _selfEnc, "control: source curves through my own encoder");
		}
		_gWasDown = flag4;
		bool flag5 = Input.IsKeyDown((InputKey)77);
		if (flag5 && !_kp6WasDown)
		{
			FireClipAction(agent, _actDwSlashRight, "act_dw_cu_slashright");
		}
		_kp6WasDown = flag5;
		bool flag6 = Input.IsKeyDown((InputKey)75);
		if (flag6 && !_kp4WasDown)
		{
			FireClipAction(agent, _actDwSlashLeft, "act_dw_cu_slashleft");
		}
		_kp4WasDown = flag6;
		bool flag7 = Input.IsKeyDown((InputKey)73);
		if (flag7 && !_kp9WasDown)
		{
			PlayClipDirect(agent, "dw_cu_pt", "anim_dw_pt_verbatim");
		}
		_kp9WasDown = flag7;
		float num = ((Mission.Current != null) ? Mission.Current.CurrentTime : 0f);
		if (_dwcIdleAt > 0f && num >= _dwcIdleAt)
		{
			_dwcIdleAt = 0f;
			if (agent != null && agent.IsActive())
			{
				{ ActionIndexCache _tmpAct = _idleReady1h; agent.SetActionChannel(0, ref _tmpAct, true, (AnimFlags)0, 0f, 1f, 0.25f, 0f, 0f, false, -0.2f, 0, true); }
			}
		}
		bool flag8 = Input.IsKeyDown((InputKey)35);
		if (flag8 && !_dwcWasRaw && num < _dwcReadyAt)
		{
			DebugHud(new InformationMessage("[DW] wait " + (_dwcReadyAt - num).ToString("0.0") + "s (clip still playing)", Colors.Yellow));
		}
		_dwcWasRaw = flag8;
		bool flag9 = flag8 && num >= _dwcReadyAt;
		if (flag9 && !_dwcWasDown)
		{
			_dwcReadyAt = num + 1.5f;
			_dwcIdleAt = num + 1.2f;
			bool flag10 = Input.IsKeyDown((InputKey)42) || Input.IsKeyDown((InputKey)54);
			bool flag11 = Input.IsKeyDown((InputKey)29) || Input.IsKeyDown((InputKey)157);
			int num2 = _dwcActs.Length;
			if (!flag11)
			{
				_dwcIdx = ((_dwcIdx + ((!flag10) ? 1 : (-1))) % num2 + num2) % num2;
			}
			if (_dwcIdx < 0)
			{
				_dwcIdx = 0;
			}
			ActionIndexCache act = (flag11 ? _dwvActs[_dwcIdx] : _dwcActs[_dwcIdx]);
			string text3 = "?";
			try
			{
				text3 = MBActionSet.GetActionAnimationName(MBActionSet.GetActionSet("as_human_warrior"), ref act);
			}
			catch
			{
			}
			DebugHud(new InformationMessage("[DW] " + (_dwcIdx + 1) + "/" + num2 + (flag11 ? " VANILLA  " : " mirror   ") + _dwcNames[_dwcIdx] + "  -> plays '" + text3 + "'", flag11 ? Colors.Yellow : Colors.Cyan));
			FireClipAction(agent, act, (flag11 ? "vanilla " : "mirrored ") + _dwcNames[_dwcIdx]);
		}
		_dwcWasDown = flag9;
	}

	private void FireClipAction(Agent agent, ActionIndexCache act, string name)
	{
		try
		{
			if (agent == null || !agent.IsActive())
			{
				DebugHud(new InformationMessage("[DW act] no active agent", Colors.Red));
				return;
			}
			MBActionSet actionSet = MBActionSet.GetActionSet("as_human_warrior");
			string text = "?";
			try
			{
				text = MBActionSet.GetActionAnimationName(actionSet, ref act);
			}
			catch (Exception ex)
			{
				text = "EXC:" + ex.Message;
			}
			bool flag = agent.SetActionChannel(0, ref act, true, (AnimFlags)33, 0f, 1f, -0.2f, 0.4f, 0f, false, -0.2f, 0, true);
			CaptureArm(name);
			bool flag2 = name.IndexOf("self", StringComparison.OrdinalIgnoreCase) >= 0;
			ulong num = 0uL;
			ulong num2 = 0uL;
			try
			{
				num = (ulong)(long)agent.GetCurrentAnimationFlag(0);
				num2 = (ulong)(long)agent.GetCurrentAnimationFlag(1);
			}
			catch (Exception ex2)
			{
				DualWieldLog.Log("[DWflags] read threw: " + ex2.Message);
			}
			DualWieldLog.Log("[DWflags] " + name + " selfWritten=" + flag2 + " ch0=0x" + num.ToString("X") + " ch1=0x" + num2.ToString("X"));
			DebugHud(new InformationMessage("[flags] ch0=0x" + num.ToString("X"), Colors.Yellow));
			string text2 = "?";
			try
			{
				MBAgentVisuals agentVisuals = agent.AgentVisuals;
				object obj;
				if (agentVisuals == null)
				{
					obj = null;
				}
				else
				{
					Skeleton skeleton = agentVisuals.GetSkeleton();
					obj = ((skeleton != null) ? skeleton.GetAnimationAtChannel(0) : null);
				}
				if (obj == null)
				{
					obj = "null";
				}
				text2 = (string)obj;
			}
			catch
			{
			}
			DualWieldLog.Log(string.Format("[DW act] {0} code={1} mappedClip='{2}' set={3} ch0anim='{4}'", new object[5] { name, act.Index, text, flag, text2 }));
			bool flag3 = flag && text2 != null && text2.Contains("dw_cu");
			DebugHud(new InformationMessage($"[DW act] set={flag} map='{text}' ch0='{text2}'", flag3 ? Colors.Green : Colors.Yellow));
		}
		catch (Exception ex3)
		{
			DualWieldLog.Log("[DW act] ERR " + ex3.Message);
			DebugHud(new InformationMessage("[DW act] ERR " + ex3.Message, Colors.Red));
		}
	}

	private void PlayClipDirect(Agent agent, string clipId, string animName)
	{
		try
		{
			if (agent == null || !agent.IsActive())
			{
				DebugHud(new InformationMessage("[DW clip] no active agent", Colors.Red));
				return;
			}
			int animationIndexWithName = MBAnimation.GetAnimationIndexWithName(clipId);
			int animationIndexWithName2 = MBAnimation.GetAnimationIndexWithName(animName);
			int animationIndexWithName3 = MBAnimation.GetAnimationIndexWithName("van_slashright");
			MBAgentVisuals agentVisuals = agent.AgentVisuals;
			Skeleton val = ((agentVisuals != null) ? agentVisuals.GetSkeleton() : null);
			if ((NativeObject)(object)val == (NativeObject)null)
			{
				DebugHud(new InformationMessage("[DW clip] skeleton NULL", Colors.Red));
				return;
			}
			string text;
			if (animationIndexWithName >= 0)
			{
				text = clipId;
			}
			else
			{
				text = ((animationIndexWithName2 >= 0) ? animName : null);
			}
			if (text != null)
			{
				_savedCh0AnimIdx = val.GetAnimationIndexAtChannel(0);
				MBSkeletonExtensions.SetAnimationAtChannel(val, text, 0, 1f, 0.15f, 0f);
				_clipPlayTicks = 70;
				DualWieldLog.Log($"[DW clip] savedCh0AnimIdx={_savedCh0AnimIdx}");
			}
			DualWieldLog.Log(string.Format("[DW clip] clip '{0}'={1}  anim '{2}'={3}  van_slashright={4}  chose={5} on {6}", new object[7]
			{
				clipId,
				animationIndexWithName,
				animName,
				animationIndexWithName2,
				animationIndexWithName3,
				text ?? "NONE",
				agent.Name
			}));
			DebugHud(new InformationMessage(string.Format("[DW clip] clip={0} anim={1} van={2} play={3}", new object[4]
			{
				animationIndexWithName,
				animationIndexWithName2,
				animationIndexWithName3,
				text ?? "NONE"
			}), (text != null) ? Colors.Green : Colors.Red));
		}
		catch (Exception ex)
		{
			DualWieldLog.Log("[DW clip] ERR " + ex.Message);
			DebugHud(new InformationMessage("[DW clip] ERR " + ex.Message, Colors.Red));
		}
	}

	private void CycleDWMode(Agent agent)
	{
		SetDWMode(agent, (_dwCombatMode != 1) ? 1 : 2, persist: true);
	}

	private void FollowSettingsMode(Agent agent)
	{
		int attackMode;
		try
		{
			DualWieldSettings dualWieldSettings = DualWieldSettings.Get();
			if (dualWieldSettings != _settingsObjectSeen)
			{
				if (_settingsObjectSeen != null && _settingsSwapLogs++ < 5)
				{
					DualWieldLog.Log(string.Format("[DW] MCM settings object replaced - mode there {0}, left key {1}, instance {2}", dualWieldSettings.AttackMode, dualWieldSettings.LeftHandKey, (DualWieldSettings.Get() == null) ? "NULL" : "set"));
				}
				_settingsObjectSeen = dualWieldSettings;
			}
			attackMode = dualWieldSettings.AttackMode;
		}
		catch
		{
			return;
		}
		if (attackMode != _settingsModeSeen)
		{
			DualWieldLog.Log($"[DW] MCM combat mode changed to {attackMode} (was {_settingsModeSeen}), mission mode {_dwCombatMode}");
			_settingsModeSeen = attackMode;
			if (attackMode >= 1 && attackMode <= 2 && attackMode != _dwCombatMode)
			{
				SetDWMode(agent, attackMode, persist: false);
			}
		}
	}

	private void SetDWMode(Agent agent, int mode, bool persist)
	{
		_dwCombatMode = mode;
		try
		{
			AgentFlag agentFlags = agent.GetAgentFlags();
			if ((agentFlags & (AgentFlag)(8)) == 0)
			{
				agent.SetAgentFlags((AgentFlag)(agentFlags | (AgentFlag)(8)));
			}
		}
		catch
		{
		}
		_windingUp = false;
		_sepQueued = false;
		_sepStrikeAt = -10f;
		_sepReleasePending = false;
		string[] array = new string[3] { "Mirror", "Alternating (LMB)", "Separate (LMB right, own key left)" };
		InformationManager.DisplayMessage(new InformationMessage("Dual wield mode: " + array[_dwCombatMode], new Color(1f, 0.6f, 0f, 1f)));
		DualWieldLog.Log($"[DW] Mode â†’ {_dwCombatMode}: {array[_dwCombatMode]}");
		if (persist)
		{
			try
			{
				// AttackMode is read-only in TOR (no MCM UI to change it). Yotthani persisted it here.
			}
			catch
			{
			}
			_settingsModeSeen = _dwCombatMode;
		}
		_nextIsLeft = false;
	}

	public override void OnAgentHit(Agent affectedAgent, Agent affectorAgent, in MissionWeapon affectorWeapon, in Blow blow, in AttackCollisionData attackCollisionData)
	{
		base.OnAgentHit(affectedAgent, affectorAgent, in affectorWeapon, in blow, in attackCollisionData);
		(Campaign.Current?.Models?.CombatXpModel as TOR_Core.Models.TORCombatXpModel)
			?.GrantBerserkXpForHit(affectorAgent, affectedAgent, blow);
		try
		{
			Blow val;
			if (affectorAgent != null && affectedAgent != null)
			{
				val = blow;
				if (!val.IsMissile && affectorAgent.IsMainAgent && _offHandSlots.ContainsKey(affectorAgent.Index))
				{
					DualWieldBalance.PlayerHit(LeftBladeStrike.ApplyingLeftHit, blow.InflictedDamage, affectedAgent.Health <= 0f);
				}
			}
			if (affectorAgent == null || affectedAgent == null)
			{
				return;
			}
			val = blow;
			if (!val.IsMissile && DualWieldBalance.IsNpc(affectorAgent))
			{
				bool flag = _offHandSlots.ContainsKey(affectorAgent.Index);
				if (flag)
				{
					DualWieldBalance.Hit(LeftBladeStrike.ApplyingLeftHit, blow.InflictedDamage, affectedAgent.Health <= 0f);
				}
				if (flag && LeftBladeStrike.ApplyingLeftHit && affectorAgent.HasMount && blow.InflictedDamage > 0)
				{
					DualWieldBalance.Rider(10);
				}
				if (blow.InflictedDamage > 0 && affectedAgent.IsHuman && Mission.Current != null)
				{
					DualWieldBalance.ChainHit(affectorAgent, affectedAgent, flag, Mission.Current.CurrentTime);
				}
			}
		}
		catch
		{
		}
	}

	private void AdjustNpcStrikeCap(float dt, float strikeSeconds)
	{
		_npcStrikeCap = _strikeCap.Update(dt, strikeSeconds);
		DwPerf.StrikeCap = _npcStrikeCap;
	}

	public override void OnMeleeHit(Agent attacker, Agent victim, bool isCanceled, AttackCollisionData collisionData)
	{
		base.OnMeleeHit(attacker, victim, isCanceled, collisionData);
		try
		{
			if (LeftBladeStrike.ApplyingLeftHit || attacker == null || victim == null || !attacker.IsActive() || !DualWieldBalance.IsNpc(attacker))
			{
				return;
			}
			bool flag = _offHandSlots.ContainsKey(attacker.Index);
			if (victim.IsHuman && attacker.IsFriendOf(victim))
			{
				DualWieldBalance.FriendCollision(flag);
			}
			else
			{
				if (!flag)
				{
					return;
				}
				CombatCollisionResult collisionResult = collisionData.CollisionResult;
				if ((int)collisionResult != 3 && (int)collisionResult != 4 && (int)collisionResult != 5)
				{
					return;
				}
				DualWieldBalance.RightBlocked();
				if ((!_npcLeftCooldown.TryGetValue(attacker.Index, out var value) || !(value > 0f)) && !(_npcRng.NextDouble() >= (double)NpcLeftChance(attacker, DualWieldSettings.Get().NpcOffhandChance)) && !_npcCounterQueue.Contains(attacker))
				{
					_npcCounterQueue.Add(attacker);
					if (attacker.HasMount && MountedLeftOn)
					{
						DualWieldBalance.Rider(5);
					}
				}
			}
		}
		catch (Exception ex)
		{
			DualWieldLog.Log("[NPC-LH] melee hit failed: " + ex.Message);
		}
	}

	private void TickNpcLeft(float dt)
	{
		long started = DwPerf.Start();
		_npcStrikesDue.Clear();
		if (_npcStrikes.Count > 0)
		{
			foreach (LeftBladeStrike value in _npcStrikes.Values)
			{
				if (value.Active)
				{
					_npcStrikesDue.Add(value);
				}
			}
			foreach (LeftBladeStrike item in _npcStrikesDue)
			{
				item.Tick(dt);
			}
		}
		_npcStrikesRunning = _npcStrikesDue.Count;
		AdjustNpcStrikeCap(dt, DwPerf.Seconds(started));
		DwPerf.Concurrent(_npcStrikesRunning + (_leftBlade.Active ? 1 : 0));
		DwPerf.Stop(2, started);
		if (_npcLeftCooldown.Count > 0)
		{
			foreach (int item2 in new List<int>(_npcLeftCooldown.Keys))
			{
				float num = _npcLeftCooldown[item2] - dt;
				if (num <= 0f)
				{
					_npcLeftCooldown.Remove(item2);
				}
				else
				{
					_npcLeftCooldown[item2] = num;
				}
			}
		}
		ReleaseHeldNpcs();
		started = DwPerf.Start();
		WatchNpcRightSwings();
		DwPerf.Stop(1, started);
		if (_npcCounterQueue.Count > 0)
		{
			List<Agent> list = new List<Agent>(_npcCounterQueue);
			_npcCounterQueue.Clear();
			foreach (Agent item3 in list)
			{
				DualWieldMissionBehavior dualWieldMissionBehavior = this;
				Agent agent = item3;
				string why;
				if (_npcFollowUps.Remove(item3.Index))
				{
					why = "own follow-up";
				}
				else
				{
					why = (_npcLeads.Remove(item3.Index) ? "lead" : "counter");
				}
				dualWieldMissionBehavior.FireNpcLeft(agent, why);
			}
		}
		if (!DualWieldBalance.Tick(dt))
		{
			return;
		}
		started = DwPerf.Start();
		int num2 = (_guardSamplePart = (_guardSamplePart + 1) % 8);
		foreach (int key in _offHandSlots.Keys)
		{
			if (key % 8 != num2)
			{
				continue;
			}
			Mission current5 = Mission.Current;
			Agent val = ((current5 != null) ? current5.FindAgentWithIndex(key) : null);
			if (val != null && val.IsActive() && DualWieldBalance.IsNpc(val))
			{
				try
				{
					DualWieldBalance.SampleGuard(ActionCache.NameOf(val.GetCurrentAction(1)));
				}
				catch
				{
				}
			}
		}
		DwPerf.Stop(3, started);
	}

	private List<Agent.UsageDirection> SafeLeftDirections(Agent agent, out string allies)
	{
		bool flag = false;
		bool flag2 = false;
		bool flag3 = false;
		bool flag4 = false;
		bool flag5 = false;
		try
		{
			Mat3 rotation = agent.Frame.rotation;
			Vec2 asVec = rotation.f.AsVec2;
			Vec2 asVec2 = rotation.s.AsVec2;
			asVec.Normalize();
			asVec2.Normalize();
			Vec3 position = agent.Position;
			Vec2 asVec3 = position.AsVec2;
			float num = LeftReach(agent);
			bool flag6 = NearestEnemyAhead(agent, asVec3, asVec, asVec2, num, out var deg, out var dist);
			((List<Agent>)(object)_npcNearby).Clear();
			Mission.Current.GetNearbyAgents(asVec3, Math.Max(1.6f, num + 0.3f), _npcNearby);
			foreach (Agent item in (List<Agent>)(object)_npcNearby)
			{
				if (item == agent || !item.IsActive() || !item.IsHuman || !agent.IsFriendOf(item))
				{
					continue;
				}
				position = item.Position;
				Vec2 val = position.AsVec2 - asVec3;
				float num2 = val.X * asVec.X + val.Y * asVec.Y;
				float num3 = val.X * asVec2.X + val.Y * asVec2.Y;
				float length = val.Length;
				if (num2 >= -0.3f && num2 <= 0.25f && length <= 1.6f)
				{
					if (num3 > 0.2f)
					{
						flag2 = true;
					}
					else if (num3 < -0.2f)
					{
						flag = true;
					}
					else
					{
						flag = true;
						flag2 = true;
					}
				}
				if (flag6)
				{
					float allyDeg = (float)(Math.Atan2(num3, num2) * 180.0 / Math.PI);
					if (DwRules.AllyInTheArc(-1, allyDeg, length, deg, dist, num))
					{
						flag3 = true;
					}
					if (DwRules.AllyInTheArc(1, allyDeg, length, deg, dist, num))
					{
						flag4 = true;
					}
					if (DwRules.AllyInTheArc(0, allyDeg, length, deg, dist, num))
					{
						flag5 = true;
					}
				}
			}
		}
		catch
		{
		}
		_npcDirs.Clear();
		if (!flag5)
		{
			_npcDirs.Add((Agent.UsageDirection)0);
			_npcDirs.Add((Agent.UsageDirection)1);
		}
		if (!flag2 && !flag3)
		{
			_npcDirs.Add((Agent.UsageDirection)2);
		}
		if (!flag && !flag4)
		{
			_npcDirs.Add((Agent.UsageDirection)3);
		}
		allies = (flag ? "L" : "-") + (flag2 ? "R" : "-") + " arc " + (flag3 ? "l" : "-") + (flag5 ? "s" : "-") + (flag4 ? "r" : "-");
		return _npcDirs;
	}

	private float LeftReach(Agent agent)
	{
		try
		{
			if (_offHandSlots.TryGetValue(agent.Index, out var value))
			{
				MissionWeapon weapon = agent.Equipment[value];
				if (!weapon.IsEmpty && weapon.CurrentUsageItem != null)
				{
					return Math.Min(2.2f, 0.55f + LeftBladeStrike.WeaponReach(weapon, weapon.CurrentUsageItem));
				}
			}
		}
		catch
		{
		}
		return 1.4f;
	}

	private bool NearestEnemyAhead(Agent agent, Vec2 me, Vec2 fwd, Vec2 side, float reach, out float deg, out float dist)
	{
		deg = 0f;
		dist = float.MaxValue;
		((List<Agent>)(object)_npcReach).Clear();
		Mission.Current.GetNearbyAgents(me, reach + 0.3f + 0.5f, _npcReach);
		foreach (Agent item in (List<Agent>)(object)_npcReach)
		{
			if (item != agent && item.IsActive() && item.IsHuman && agent.IsEnemyOf(item))
			{
				Vec3 position = item.Position;
				Vec2 val = position.AsVec2 - me;
				float length = val.Length;
				float num = val.X * fwd.X + val.Y * fwd.Y;
				if (!(length < 0.001f) && !(num < 0.5f * length) && !(length >= dist))
				{
					dist = length;
					deg = (float)(Math.Atan2(val.X * side.X + val.Y * side.Y, num) * 180.0 / Math.PI);
				}
			}
		}
		return dist < float.MaxValue;
	}

	private static bool LeftLeads(Agent agent)
	{
		float share = 0.2f;
		try
		{
			share = DualWieldSettings.Get().NpcLeftLeadShare;
		}
		catch
		{
		}
		return DwRules.LeadsWithLeft(agent.Index, share);
	}

	private void TryLeadLeft(Agent a)
	{
		bool hasMount = a.HasMount;
		if ((hasMount && !MountedLeftOn) || _npcCounterQueue.Contains(a) || _npcLeftCooldown.ContainsKey(a.Index) || (_npcStrikes.TryGetValue(a.Index, out var value) && value.Active))
		{
			return;
		}
		bool flag = (hasMount ? MountedTarget(a, out var _, out var _, out var _, out var _) : EnemyInReach(a));
		if (!(_npcRng.NextDouble() >= (double)NpcLeftChance(a, 0.8f)) && flag)
		{
			_npcLeads.Add(a.Index);
			_npcCounterQueue.Add(a);
			if (hasMount)
			{
				DualWieldBalance.Rider(12);
			}
		}
	}

	private void WatchNpcRightSwings()
	{
		if (_offHandSlots.Count == 0 || Mission.Current == null)
		{
			return;
		}
		Agent main = Agent.Main;
		if (_offHandSlots.Count == 1 && main != null && _offHandSlots.ContainsKey(main.Index))
		{
			return;
		}
		float num = 0.3f;
		try
		{
			num = DualWieldSettings.Get().NpcOffhandFollowUpChance;
		}
		catch
		{
		}
		float num2 = 0.2f;
		try
		{
			num2 = DualWieldSettings.Get().NpcLeftLeadShare;
		}
		catch
		{
		}
		if (num <= 0f && num2 <= 0f)
		{
			_npcReleasing.Clear();
			_npcReadying.Clear();
			return;
		}
		int side = (_npcWatchPhase = (_npcWatchPhase + 1) % 3);
		int num3 = side;
		foreach (Agent item in (List<Agent>)(object)Mission.Current.Agents)
		{
			if (item == null || item.Index % 3 != num3 || !_offHandSlots.ContainsKey(item.Index) || !item.IsActive() || !DualWieldBalance.IsNpc(item))
			{
				continue;
			}
			Agent.ActionCodeType currentActionType;
			try
			{
				currentActionType = item.GetCurrentActionType(1);
			}
			catch
			{
				continue;
			}
			if ((int)currentActionType == 19)
			{
				if (_npcReadying.Add(item.Index) && LeftLeads(item))
				{
					TryLeadLeft(item);
				}
			}
			else
			{
				_npcReadying.Remove(item.Index);
			}
			bool flag = (int)currentActionType == 20;
			bool flag2 = _npcReleasing.Contains(item.Index);
			if (flag)
			{
				if (!flag2)
				{
					_npcReleasing.Add(item.Index);
				}
			}
			else
			{
				if (!flag2)
				{
					continue;
				}
				_npcReleasing.Remove(item.Index);
				if (num <= 0f)
				{
					continue;
				}
				bool flag3 = item.HasMount && MountedLeftOn;
				if (flag3)
				{
					DualWieldBalance.Rider(0);
				}
				if (_npcCounterQueue.Contains(item) || _npcLeftCooldown.ContainsKey(item.Index) || (_npcStrikes.TryGetValue(item.Index, out var value) && value.Active))
				{
					if (flag3)
					{
						DualWieldBalance.Rider(1);
					}
					continue;
				}
				bool flag4 = ((!item.HasMount) ? EnemyInReach(item) : (MountedLeftOn && MountedTarget(item, out side, out var _, out var _, out var _)));
				if (flag3 && !flag4)
				{
					DualWieldBalance.Rider(2);
					DualWieldBalance.RiderNoTargetWhy(MountedNoTargetWhy(item));
				}
				if (_npcRng.NextDouble() >= (double)NpcLeftChance(item, num) || !flag4)
				{
					if (flag3 & flag4)
					{
						DualWieldBalance.Rider(3);
					}
					continue;
				}
				if (flag3)
				{
					DualWieldBalance.Rider(4);
				}
				_npcFollowUps.Add(item.Index);
				_npcCounterQueue.Add(item);
			}
		}
	}

	private bool EnemyInReach(Agent agent)
	{
		try
		{
			Vec3 position = agent.Position;
			Vec2 asVec = position.AsVec2;
			MatrixFrame frame = agent.Frame;
			Vec2 asVec2 = frame.rotation.f.AsVec2;
			asVec2.Normalize();
			((List<Agent>)(object)_npcReach).Clear();
			Mission.Current.GetNearbyAgents(asVec, 2.3f, _npcReach);
			foreach (Agent item in (List<Agent>)(object)_npcReach)
			{
				if (item != agent && item.IsActive() && item.IsHuman && agent.IsEnemyOf(item))
				{
					position = item.Position;
					Vec2 val = position.AsVec2 - asVec;
					if (!(val.Normalize() < 0.001f) && val.X * asVec2.X + val.Y * asVec2.Y > 0.5f)
					{
						return true;
					}
				}
			}
		}
		catch
		{
		}
		return false;
	}

	private void HoldNpcForStrike(Agent agent)
	{
		try
		{
			string text = "?";
			if (_npcHoldLogs < 15)
			{
				try
				{
					text = ActionCache.NameOf(agent.GetCurrentAction(1));
				}
				catch
				{
				}
			}
			if (!_npcHeldFlags.ContainsKey(agent.Index))
			{
				_npcHeldFlags[agent.Index] = agent.GetAgentFlags();
			}
			agent.SetAgentFlags((AgentFlag)(_npcHeldFlags[agent.Index] & ~(AgentFlag)24));
			{ ActionIndexCache _tmpAct = ActionIndexCache.act_none; agent.SetActionChannel(1, ref _tmpAct, true, (AnimFlags)0, 0f, 1f, 0.1f, 0.4f, 0f, false, -0.2f, 0, true); }
			if (_npcHoldLogs++ < 15)
			{
				DualWieldLog.Log("[NPC-LH] " + agent.Name + ": channel 1 held '" + text + "' when the left strike started - cleared");
			}
		}
		catch (Exception ex)
		{
			DualWieldLog.Log("[NPC-LH] hold failed: " + ex.Message);
		}
	}

	private void ReleaseHeldNpcs()
	{
		if (_npcHeldFlags.Count == 0)
		{
			return;
		}
		foreach (int item in new List<int>(_npcHeldFlags.Keys))
		{
			if (_npcStrikes.TryGetValue(item, out var value) && (value.Active || value.Stunned))
			{
				continue;
			}
			Mission current2 = Mission.Current;
			Agent val = ((current2 != null) ? current2.FindAgentWithIndex(item) : null);
			if (val != null && val.IsActive())
			{
				try
				{
					val.SetAgentFlags(_npcHeldFlags[item]);
				}
				catch
				{
				}
			}
			_npcHeldFlags.Remove(item);
		}
	}

	private static float NpcLeftChance(Agent agent, float setting)
	{
		float aiLevel = 0.5f;
		try
		{
			aiLevel = agent.AgentDrivenProperties.AIParryOnAttackAbility;
		}
		catch
		{
		}
		return DwRules.LeftChance(setting, aiLevel);
	}

	private void FireNpcLeft(Agent agent)
	{
		FireNpcLeft(agent, "counter");
	}

	private void FireNpcLeft(Agent agent, string why)
	{
		try
		{
			if (agent == null || !agent.IsActive() || !_offHandSlots.TryGetValue(agent.Index, out var value))
			{
				return;
			}
			if (!HoldsOurOffhand(agent))
			{
				bool flag = !DualWieldWieldingPatches.IsOneHandedMeleeWeapon(agent.Equipment[value]);
				if (_npcLeftLogs++ < 40)
				{
					object[] array = new object[4]
					{
						agent.Name,
						(int)value,
						null,
						null
					};
					MissionWeapon val = agent.Equipment[value];
					ItemObject item = val.Item;
					array[2] = ((item != null) ? ((MBObjectBase)item).StringId : null);
					array[3] = (flag ? "pairing dropped" : "no counter this time");
					DualWieldLog.Log(string.Format("[NPC-LH] {0}: left hand does not hold our weapon (slot {1} = '{2}') - {3}", array));
				}
				if (flag)
				{
					RemoveOffHandAttachment(agent);
				}
				return;
			}
			if (_npcStrikesRunning >= _npcStrikeCap)
			{
				DualWieldBalance.LeftSkipped();
				if (agent.HasMount && MountedLeftOn)
				{
					DualWieldBalance.Rider(8);
				}
				return;
			}
			if (agent.HasMount)
			{
				if (MountedLeftOn)
				{
					FireNpcLeftMounted(agent, value, why);
				}
				return;
			}
			List<Agent.UsageDirection> list = SafeLeftDirections(agent, out var allies);
			if (list.Count == 0)
			{
				DualWieldBalance.LeftHeldForAlly();
				if (_npcLeftLogs++ < 20)
				{
					DualWieldLog.Log("[NPC-LH] " + agent.Name + ": " + why + " -> no left strike, ally " + allies);
				}
				return;
			}
			Agent.UsageDirection val2 = LeftUsable(agent, list[_npcRng.Next(list.Count)]);
			bool npcFootChannel = NpcFootChannel1;
			int num = (npcFootChannel ? 1 : 0);
			ActionIndexCache act = AtHeight(MirrorForDirection(agent, val2), (!npcFootChannel) ? HeightOf(agent) : 0);
			if (act.Index < 0)
			{
				return;
			}
			if (npcFootChannel)
			{
				HoldNpcForStrike(agent);
			}
			bool flag2 = agent.SetActionChannel(num, ref act, true, (AnimFlags)LeftFlags(33uL), 0f, 1f, -0.2f, 0.4f, 0f, false, -0.2f, 0, true);
			if (_npcLeftLogs++ < 20)
			{
				DualWieldLog.Log(string.Format("[NPC-LH] {0}: {1} -> left strike {2} on channel {3} ({4}, allies {5}) set={6}", new object[7]
				{
					agent.Name,
					why,
					act.GetName(),
					num,
					val2,
					allies,
					flag2
				}));
			}
			if (flag2)
			{
				if (!npcFootChannel)
				{
					HoldNpcForStrike(agent);
				}
				if (!_npcStrikes.TryGetValue(agent.Index, out var value2))
				{
					value2 = (_npcStrikes[agent.Index] = new LeftBladeStrike());
				}
				value2.Stop("next strike");
				value2.Start(agent, value, ActionCache.NameOf(act), val2, num);
				DualWieldBalance.NpcFootStrike(num);
				_npcStrikesRunning++;
				_npcLeftCooldown[agent.Index] = (LeftLeads(agent) ? 1.2f : 2.5f);
				DualWieldBalance.LeftFired(why == "own follow-up", why == "lead");
			}
		}
		catch (Exception ex)
		{
			DualWieldLog.Log("[NPC-LH] counter failed: " + ex.Message);
		}
	}

	private static Agent AiTarget(Agent agent)
	{
		Agent val = null;
		try
		{
			val = agent.ImmediateEnemy;
		}
		catch
		{
		}
		if (val == null || !val.IsActive() || !val.IsHuman || !agent.IsEnemyOf(val))
		{
			val = null;
			try
			{
				val = agent.GetTargetAgent();
			}
			catch
			{
			}
			if (val != null && (!val.IsActive() || !val.IsHuman || !agent.IsEnemyOf(val)))
			{
				val = null;
			}
		}
		return val;
	}

	private static Vec2 PlanarVelocity(Agent a)
	{
		try
		{
			Vec3 velocity = (a.MountAgent ?? a).Velocity;
			return velocity.AsVec2;
		}
		catch
		{
			return Vec2.Zero;
		}
	}

	private void FireNpcLeftMounted(Agent agent, EquipmentIndex slot, string why)
	{
		if (!MountedTarget(agent, out var side, out var deg, out var dist, out var onFoot))
		{
			DualWieldBalance.Rider(6);
			return;
		}
		Agent.UsageDirection val = (Agent.UsageDirection)(side switch
		{
			2 => 0, 
			0 => 1, 
			1 => 2, 
			-1 => 3, 
			_ => (_npcRng.Next(2) == 0) ? 1 : 0, 
		});
		if (side == 0 && LeftUsable(agent, val) != val)
		{
			DualWieldBalance.Rider(6);
			return;
		}
		val = LeftUsable(agent, val);
		if (MountedAllyInTheWay(agent, side, deg, dist, onFoot, _mountedTargetAhead))
		{
			DualWieldBalance.LeftHeldForAlly();
			DualWieldBalance.Rider(7);
			return;
		}
		ActionIndexCache act = AtHeight(MountedAction(agent, val, ready: false), onFoot ? (-1) : 0);
		if (((side == -1 || side == 1) & onFoot) && act.Index >= 0)
		{
			float lateral = dist * (float)Math.Sin((double)deg * Math.PI / 180.0);
			string text = ((side == -1) ? DwRules.MountedLeftSlashReach(lateral) : DwRules.MountedRightSlashReach(lateral));
			if (text != "_lo")
			{
				ActionIndexCache val2 = ActionCache.Get(DwRules.WithoutHeight(ActionCache.NameOf(act)) + text);
				if (val2.Index >= 0)
				{
					act = val2;
				}
			}
		}
		if (act.Index < 0)
		{
			return;
		}
		HoldNpcForStrike(agent);
		int num = LeftChannel(agent);
		bool flag = agent.SetActionChannel(num, ref act, true, (AnimFlags)LeftFlags(33uL), 0f, 1f, -0.2f, 0.4f, 0f, false, -0.2f, 0, true);
		if (_npcMountedLogs++ < 20)
		{
			DualWieldLog.Log(string.Format("[NPC-LH] {0}: mounted {1} -> {2} on channel {3} (enemy will be at {4:0} deg, {5:0.0} m; the AI's right arm: {6}) set={7}", new object[8]
			{
				agent.Name,
				why,
				ActionCache.NameOf(act),
				num,
				deg,
				dist,
				SafeAttackDirection(agent),
				flag
			}));
		}
		if (flag)
		{
			if (!_npcStrikes.TryGetValue(agent.Index, out var value))
			{
				value = (_npcStrikes[agent.Index] = new LeftBladeStrike());
			}
			value.Stop("next strike");
			value.Start(agent, slot, ActionCache.NameOf(act), val, num);
			_npcStrikesRunning++;
			_npcLeftCooldown[agent.Index] = (LeftLeads(agent) ? 1.2f : 2.5f);
			DualWieldBalance.LeftFired(why == "own follow-up", why == "lead");
			DualWieldBalance.LeftMounted();
			DualWieldBalance.Rider(9);
			DualWieldBalance.RiderTarget(agent.Index, deg, dist);
			if (_mountedTookAiTarget)
			{
				DualWieldBalance.Rider(13);
			}
		}
	}

	private int MountedNoTargetWhy(Agent agent)
	{
		try
		{
			Mat3 rotation = agent.Frame.rotation;
			Vec2 asVec = rotation.f.AsVec2;
			Vec2 asVec2 = rotation.s.AsVec2;
			asVec.Normalize();
			asVec2.Normalize();
			float num = LeftReach(agent);
			Vec3 position = agent.Position;
			Vec2 val = position.AsVec2 + PlanarVelocity(agent) * 0.5f;
			float num2 = float.MaxValue;
			float num3 = 0f;
			float num4 = 0f;
			bool flag = true;
			((List<Agent>)(object)_npcReach).Clear();
			Mission.Current.GetNearbyAgents(val, num + 6.5f, _npcReach);
			foreach (Agent item in (List<Agent>)(object)_npcReach)
			{
				if (item != agent && item.IsActive() && item.IsHuman && agent.IsEnemyOf(item) && (item.HasMount || !(item.GetEyeGlobalPosition().z - item.Position.z < 1f)))
				{
					position = item.Position;
					Vec2 val2 = position.AsVec2 + PlanarVelocity(item) * 0.5f - val;
					float length = val2.Length;
					if (!(length >= num2))
					{
						num2 = length;
						num3 = val2.X * asVec.X + val2.Y * asVec.Y;
						num4 = val2.X * asVec2.X + val2.Y * asVec2.Y;
						flag = !item.HasMount;
					}
				}
			}
			if (num2 == float.MaxValue)
			{
				return 0;
			}
			int num5 = ((flag && MeasuredRiderZones) ? DwRules.MountedMissWhyOnFoot(num3, num4, num) : DwRules.MountedMissWhy(num3, num4, num));
			if (_npcNoTargetLogs++ < 40)
			{
				CultureInfo invariantCulture = CultureInfo.InvariantCulture;
				object[] array = new object[8]
				{
					flag ? "man on foot" : "rider",
					num3,
					num4,
					num2,
					num,
					null,
					null,
					null
				};
				Vec2 val3 = PlanarVelocity(agent);
				array[5] = val3.Length;
				array[6] = num5 switch
				{
					4 => (object)"under the horse", 
					3 => "behind", 
					2 => "ahead to the side", 
					1 => "out of reach", 
					_ => "nobody near", 
				};
				array[7] = "nearest";
				DualWieldLog.Log(string.Format(invariantCulture, "[Mounted] no target: {7} {0} at forward {1:+0.0;-0.0} lateral {2:+0.0;-0.0} m ({3:0.0} m), reach {4:0.00} m, rider {5:0.0} m/s - {6}", array));
			}
			return num5;
		}
		catch
		{
			return -1;
		}
	}

	private bool MountedTarget(Agent agent, out int side, out float deg, out float dist, out bool onFoot)
	{
		side = 0;
		deg = 0f;
		dist = float.MaxValue;
		onFoot = true;
		_mountedTargetAhead = false;
		_mountedTookAiTarget = false;
		try
		{
			Mat3 rotation = agent.Frame.rotation;
			Vec2 asVec = rotation.f.AsVec2;
			Vec2 asVec2 = rotation.s.AsVec2;
			asVec.Normalize();
			asVec2.Normalize();
			float num = LeftReach(agent);
			Vec3 position = agent.Position;
			Vec2 asVec3 = position.AsVec2;
			Vec2 val = PlanarVelocity(agent);
			((List<Agent>)(object)_npcReach).Clear();
			Agent val2 = (RiderLeftTakesAiTarget ? AiTarget(agent) : null);
			for (int i = ((val2 == null) ? 1 : 0); i < 2; i++)
			{
				if (dist != float.MaxValue)
				{
					break;
				}
				((List<Agent>)(object)_npcReach).Clear();
				if (i == 0)
				{
					((List<Agent>)(object)_npcReach).Add(val2);
				}
				else
				{
					Mission.Current.GetNearbyAgents(asVec3 + val * 0.5f, num + 6.5f, _npcReach);
				}
				foreach (Agent item in (List<Agent>)(object)_npcReach)
				{
					if (item == agent || !item.IsActive() || !item.IsHuman || !agent.IsEnemyOf(item) || (!item.HasMount && item.GetEyeGlobalPosition().z - item.Position.z < 1f))
					{
						continue;
					}
					position = item.Position;
					Vec2 asVec4 = position.AsVec2;
					Vec2 val3 = PlanarVelocity(item);
					for (int j = 0; j < 2; j++)
					{
						float num2 = ((j == 0) ? 0.5f : 0.4f);
						Vec2 val4 = asVec4 + val3 * num2 - (asVec3 + val * num2);
						float num3 = val4.X * asVec.X + val4.Y * asVec.Y;
						float num4 = val4.X * asVec2.X + val4.Y * asVec2.Y;
						bool flag = MeasuredRiderZones && !item.HasMount;
						int num5 = (flag ? DwRules.MountedStrikeAtManOnFoot(num3, num4, num, item.GetEyeGlobalPosition().z - item.Position.z + 0.1f) : DwRules.MountedStrikeSide(num3, num4, num));
						if (num5 == 99 && !flag && !item.HasMount)
						{
							num5 = DwRules.MountedFrontDiagonal(num3, num4, num);
						}
						if (num5 == -1 && !flag && !item.HasMount && DwRules.MountedLeftSlashTooFar(num4))
						{
							continue;
						}
						bool flag2 = num5 == -1 || num5 == 1;
						if (num5 != 99 && j == 0 == flag2)
						{
							if (num5 == 0 && !flag)
							{
								num5 = (item.HasMount ? 3 : DwRules.MountedAheadStrike(num3, num4, (int)LeftUsable(agent, (Agent.UsageDirection)1) == 1));
							}
							float length = val4.Length;
							if (!(length >= dist))
							{
								dist = length;
								side = num5;
								onFoot = !item.HasMount;
								_mountedTargetAhead = j == 1;
								deg = (float)(Math.Atan2(num4, num3) * 180.0 / Math.PI);
							}
						}
					}
				}
				if (i == 0 && dist < float.MaxValue)
				{
					_mountedTookAiTarget = true;
				}
			}
		}
		catch
		{
		}
		return dist < float.MaxValue;
	}

	private bool MountedAllyInTheWay(Agent agent, int side, float targetDeg, float targetDist, bool leaning, bool ahead)
	{
		try
		{
			Mat3 rotation = agent.Frame.rotation;
			Vec2 asVec = rotation.f.AsVec2;
			Vec2 asVec2 = rotation.s.AsVec2;
			asVec.Normalize();
			asVec2.Normalize();
			float num = LeftReach(agent);
			bool flag = side == -1 || side == 1;
			float num2 = ((flag && !ahead) ? 0.5f : 0.4f);
			Vec3 position = agent.Position;
			Vec2 val = position.AsVec2 + PlanarVelocity(agent) * num2;
			int sweepFrom = (flag ? (-side) : 0);
			((List<Agent>)(object)_npcNearby).Clear();
			Mission.Current.GetNearbyAgents(val, num + 6.5f, _npcNearby);
			foreach (Agent item in (List<Agent>)(object)_npcNearby)
			{
				if (item == agent || !item.IsActive() || !item.IsHuman || !agent.IsFriendOf(item))
				{
					continue;
				}
				position = item.Position;
				Vec2 val2 = position.AsVec2 + PlanarVelocity(item) * num2;
				Vec2 val3 = Vec2.Zero;
				if (item.HasMount)
				{
					MatrixFrame frame = item.Frame;
					val3 = frame.rotation.f.AsVec2;
					val3.Normalize();
					val3 *= 1f;
				}
				for (int i = (item.HasMount ? (-1) : 0); i <= (item.HasMount ? 1 : 0); i++)
				{
					Vec2 val4 = val2 + val3 * (float)i - val;
					float num3 = val4.X * asVec.X + val4.Y * asVec.Y;
					float allyDeg = (float)(Math.Atan2(val4.X * asVec2.X + val4.Y * asVec2.Y, num3) * 180.0 / Math.PI);
					if ((!flag & leaning) && MeasuredRiderZones)
					{
						if (DwRules.AllyBeforeTheTarget(allyDeg, val4.Length, targetDeg, targetDist))
						{
							return true;
						}
						continue;
					}
					float sweepStartDeg = ((leaning & flag) ? 20f : 100f);
					if (DwRules.AllyInTheArc(sweepFrom, allyDeg, val4.Length, targetDeg, targetDist, num, sweepStartDeg))
					{
						return true;
					}
				}
			}
		}
		catch
		{
		}
		return false;
	}

	private void StartLeftDamage(Agent agent, string clipName, Agent.UsageDirection aim)
	{
		if (_offHandSlots.TryGetValue(agent.Index, out var value))
		{
			_leftBlade.Stop("next strike");
			_leftBlade.Start(agent, value, clipName, aim, LeftChannel(agent));
		}
	}

	public override void OnAgentRemoved(Agent affectedAgent, Agent affectorAgent, AgentState agentState, KillingBlow blow)
	{
		base.OnAgentRemoved(affectedAgent, affectorAgent, agentState, blow);
		if (affectedAgent != null)
		{
			_agentsWithAttachment.Remove(affectedAgent.Index);
			_offHandSlots.Remove(affectedAgent.Index);
			if (_npcStrikes.TryGetValue(affectedAgent.Index, out var value))
			{
				value.Stop("attacker gone");
				_npcStrikes.Remove(affectedAgent.Index);
			}
			_npcLeftCooldown.Remove(affectedAgent.Index);
			_npcReleasing.Remove(affectedAgent.Index);
			_npcReadying.Remove(affectedAgent.Index);
			_npcLeads.Remove(affectedAgent.Index);
			_npcFollowUps.Remove(affectedAgent.Index);
			_npcHeldFlags.Remove(affectedAgent.Index);
			RangedHybrids.Remove(affectedAgent.Index);
		}
		DualWieldStateManager.Unregister(affectedAgent);
	}

	protected override void OnEndMission()
	{
		base.OnEndMission();
		PlayerLeftArrow = (Agent.UsageDirection)(-1);
		DualWieldBalance.Report("mission end");
		try
		{
			string text = OneAgentLimbRay.TakeSummary();
			if (text != null)
			{
				DualWieldLog.Log(text);
			}
		}
		catch
		{
		}
		try
		{
			string text2 = LeftBladeStrike.TakeStaleSummary();
			if (text2 != null)
			{
				DualWieldLog.Log(text2);
			}
		}
		catch
		{
		}
		DualWieldBalance.Reset();
		_npcStrikes.Clear();
		RangedHybrids.Clear();
		_npcLeftCooldown.Clear();
		_npcCounterQueue.Clear();
		_npcLeftLogs = 0;
		_npcNoTargetLogs = 0;
		_wasMounted = false;
		_strikeSlowOn = false;
		PlayerOffHandStriking = false;
		PlayerMainLeftStriking = false;
		DualWieldLog.Flush();
		_npcReleasing.Clear();
		_npcReadying.Clear();
		_npcLeads.Clear();
		_npcFollowUps.Clear();
		_npcHeldFlags.Clear();
		SeparateKeyOverride.Reset();
		CloseCapture();
		IsActive = false;
		_agentsWithAttachment.Clear();
		_offHandSlots.Clear();
		_indicatorShown = false;
		_followUpCount = 0;
		_nextIsLeft = false;
		_rmbWasDown = false;
		_windingUp = false;
		_gWasDown = false;
		_nWasDown = false;
		_dwCombatMode = 0;
		DualWieldWieldingPatches.ClearTrackingState();
		DualWieldStateManager.Clear();
	}

	static DualWieldMissionBehavior()
	{
	}
}
