using SandBox;
using SandBox.Missions.AgentBehaviors;
using SandBox.Missions.MissionLogics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TOR_Core.Utilities;

namespace TOR_Core.BattleMechanics.AI.CivilianMissionAI
{
    public class TORAlarmedBehaviorGroup : AgentBehaviorGroup
    {
		private static readonly ActionIndexCache act_scared_to_normal_1 = ActionIndexCache.Create("act_scared_to_normal_1");
		public const float SafetyDistance = 15f;
		public const float SafetyDistanceSquared = 225f;
		private readonly MissionFightHandler _missionFightHandler;
		public bool DisableCalmDown;
		private readonly BasicMissionTimer _alarmedTimer;
		private readonly BasicMissionTimer _checkCalmDownTimer;
		private bool _isCalmingDown;

		public TORAlarmedBehaviorGroup(AgentNavigator navigator, Mission mission) : base(navigator, mission)
		{
			_alarmedTimer = new BasicMissionTimer();
			_checkCalmDownTimer = new BasicMissionTimer();
			_missionFightHandler = Mission.GetMissionBehavior<MissionFightHandler>();
		}

		public override void Tick(float dt, bool isSimulation)
		{
			if (ScriptedBehavior != null)
			{
				if (!ScriptedBehavior.IsActive)
				{
                    DisableAllBehaviors();
                    ScriptedBehavior.IsActive = true;
				}
			}
			else
			{
				float num = 0f;
				int num2 = -1;
				for (int i = 0; i < Behaviors.Count; i++)
				{
					float availability = Behaviors[i].GetAvailability(isSimulation);
					if (availability > num)
					{
						num = availability;
						num2 = i;
					}
				}
				if (num > 0f && num2 != -1 && !Behaviors[num2].IsActive)
				{
                    DisableAllBehaviors();
					Behaviors[num2].IsActive = true;
				}
			}
			TickActiveBehaviors(dt, isSimulation);
		}

		private void TickActiveBehaviors(float dt, bool isSimulation)
		{
			foreach (var agentBehavior in Behaviors)
			{
				if (agentBehavior.IsActive)
				{
					agentBehavior.Tick(dt, isSimulation);
				}
			}
		}

		public override float GetScore(bool isSimulation)
		{
			if (OwnerAgent.CurrentWatchState == Agent.WatchState.Alarmed)
			{
				if (!DisableCalmDown && _alarmedTimer.ElapsedTime > 10f && _checkCalmDownTimer.ElapsedTime > 1f)
				{
					if (!_isCalmingDown)
					{
						_checkCalmDownTimer.Reset();
						if (!IsNearDanger())
						{
							_isCalmingDown = true;
                            OwnerAgent.DisableScriptedMovement();
                            OwnerAgent.SetActionChannel(0, act_scared_to_normal_1, false, 0UL, 0f, 1f, -0.2f, 0.4f, MBRandom.RandomFloat, false, -0.2f, 0, true);
						}
					}
					else if (!OwnerAgent.ActionSet.AreActionsAlternatives(OwnerAgent.GetCurrentActionValue(0), act_scared_to_normal_1))
					{
						_isCalmingDown = false;
						return 0f;
					}
				}
				return 1f;
			}
			if (IsNearDanger())
			{
				AlarmedBehaviorGroup.AlarmAgent(base.OwnerAgent);
				return 1f;
			}
			return 0f;
		}

		private bool IsNearDanger()
		{
			float num;
			Agent closestAlarmSource = GetClosestAlarmSource(out num);
			return closestAlarmSource != null && (num < 225f || Navigator.CanSeeAgent(closestAlarmSource));
		}

		public Agent GetClosestAlarmSource(out float distanceSquared)
		{
			distanceSquared = float.MaxValue;
			if (_missionFightHandler == null || !_missionFightHandler.IsThereActiveFight())
			{
				return null;
			}
			Agent result = null;
			foreach (Agent agent in _missionFightHandler.GetDangerSources(OwnerAgent))
			{
				float num = agent.Position.DistanceSquared(OwnerAgent.Position);
				if (num < distanceSquared)
				{
					distanceSquared = num;
					result = agent;
				}
			}
			return result;
		}

		public static void AlarmAgent(Agent agent)
		{
			agent.SetWatchState(Agent.WatchState.Alarmed);
		}

		protected override void OnActivate()
		{
			TextObject textObject = new TextObject("{=!}{p0} {p1} activate alarmed behavior group.", null);
			textObject.SetTextVariable("p0", OwnerAgent.Name);
			textObject.SetTextVariable("p1", OwnerAgent.Index);
			_isCalmingDown = false;
			_alarmedTimer.Reset();
			_checkCalmDownTimer.Reset();
            OwnerAgent.DisableScriptedMovement();
            OwnerAgent.ClearTargetFrame();
			Navigator.SetItemsVisibility(false);

			if (Navigator.MemberOfAlley != null || MissionFightHandler.IsAgentAggressive(OwnerAgent))
			{
				DisableCalmDown = true;
			}
		}

		protected override void OnDeactivate()
		{
			base.OnDeactivate();
			_isCalmingDown = false;
			if (OwnerAgent.IsActive())
			{
                OwnerAgent.TryToSheathWeaponInHand(Agent.HandIndex.OffHand, Agent.WeaponWieldActionType.WithAnimationUninterruptible);
                OwnerAgent.TryToSheathWeaponInHand(Agent.HandIndex.MainHand, Agent.WeaponWieldActionType.WithAnimationUninterruptible);
				if (OwnerAgent.Team.IsValid && OwnerAgent.Team == Mission.PlayerEnemyTeam)
				{
                    OwnerAgent.SetTeam(new Team(MBTeam.InvalidTeam, BattleSideEnum.None, null, uint.MaxValue, uint.MaxValue, null), true);
				}
                OwnerAgent.SetWatchState(Agent.WatchState.Patrolling);
                OwnerAgent.ResetLookAgent();
                OwnerAgent.SetActionChannel(0, ActionIndexCache.act_none, true, 0UL, 0f, 1f, -0.2f, 0.4f, 0f, false, -0.2f, 0, true);
                OwnerAgent.SetActionChannel(1, ActionIndexCache.act_none, true, 0UL, 0f, 1f, -0.2f, 0.4f, 0f, false, -0.2f, 0, true);
			}
		}
	}
}
