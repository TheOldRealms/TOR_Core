using SandBox;
using SandBox.Missions.AgentBehaviors;
using SandBox.Objects.AnimationPoints;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TOR_Core.Missions;
using TOR_Core.Utilities;

namespace TOR_Core.BattleMechanics.AI.CivilianMissionAI
{
    public class TORWalkingBehavior : SandBox.Missions.AgentBehaviors.AgentBehavior
    {
		private readonly bool _isIndoor;
		private UsableMachine _wanderTarget;
		private Timer _waitTimer;
		private bool _indoorWanderingIsActive;
		private bool _outdoorWanderingIsActive;
		private bool _wasSimulation;
		private TORMissionAgentHandler _agentHandler;

		private bool CanWander
		{
			get
			{
				return (_isIndoor && _indoorWanderingIsActive) || (!_isIndoor && _outdoorWanderingIsActive);
			}
		}

		public TORWalkingBehavior(AgentBehaviorGroup behaviorGroup) : base(behaviorGroup)
		{
			_wanderTarget = null;
			_isIndoor = Mission.Current.Scene.IsAtmosphereIndoor;
			_indoorWanderingIsActive = true;
			_outdoorWanderingIsActive = true;
			_wasSimulation = false;
			_agentHandler = Mission.GetMissionBehavior<TORMissionAgentHandler>();
		}

		public void SetIndoorWandering(bool isActive)
		{
			_indoorWanderingIsActive = isActive;
		}

		public void SetOutdoorWandering(bool isActive)
		{
			_outdoorWanderingIsActive = isActive;
		}

		public override void Tick(float dt, bool isSimulation)
		{
            if (_wanderTarget == null || Navigator.TargetUsableMachine == null || _wanderTarget.IsDisabled || !_wanderTarget.IsStandingPointAvailableForAgent(OwnerAgent))
			{
				_wanderTarget = FindTarget();
			}
            else if (Navigator.GetDistanceToTarget(_wanderTarget) < 5f)
			{
                bool flag = _wasSimulation && !isSimulation && _wanderTarget != null && _waitTimer != null;
				if (_waitTimer == null)
				{
					AnimationPoint animationPoint = OwnerAgent.CurrentlyUsedGameObject as AnimationPoint;
					float num = (animationPoint != null) ? animationPoint.GetRandomWaitInSeconds() : 5f;
					_waitTimer = new Timer(Mission.CurrentTime, (num < 0f) ? 5f : num, true);
				}
				else if (_waitTimer.Check(Mission.CurrentTime) || flag)
				{
					if (CanWander)
					{
						_waitTimer = null;
                        UsableMachine usableMachine = FindTarget();
                        if (usableMachine == null || IsChildrenOfSameParent(usableMachine, _wanderTarget))
						{
							AnimationPoint animationPoint2 = OwnerAgent.CurrentlyUsedGameObject as AnimationPoint;
							float duration = (animationPoint2 != null) ? animationPoint2.GetRandomWaitInSeconds() : 5f;
							_waitTimer = new Timer(Mission.CurrentTime, (duration < 0f) ? 5f : duration, true);
						}
						else
						{
							_wanderTarget = usableMachine;
						}
					}
					else
					{
						_waitTimer.Reset(100f);
					}
				}
			}
            Navigator.SetTarget(_wanderTarget, false);
			_wasSimulation = isSimulation;
		}

		private bool IsChildrenOfSameParent(UsableMachine machine, UsableMachine otherMachine)
		{
			GameEntity gameEntity = machine.GameEntity;
			while (gameEntity.Parent != null)
			{
				gameEntity = gameEntity.Parent;
			}
			GameEntity gameEntity2 = otherMachine.GameEntity;
			while (gameEntity2.Parent != null)
			{
				gameEntity2 = gameEntity2.Parent;
			}
			return gameEntity == gameEntity2;
		}

		public override void ConversationTick()
		{
			if (_waitTimer != null)
			{
				_waitTimer.Reset(Mission.CurrentTime);
			}
		}

		public override float GetAvailability(bool isSimulation)
		{
            if (FindTarget() == null)
			{
				return 0f;
			}
			return 1f;
		}

		public override void SetCustomWanderTarget(UsableMachine customUsableMachine)
		{
			_wanderTarget = customUsableMachine;
			if (_waitTimer != null)
			{
				_waitTimer = null;
			}
		}

		private UsableMachine FindRandomWalkingTarget(bool forWaiting)
		{
			if (forWaiting && (_wanderTarget ?? Navigator.TargetUsableMachine) != null)
			{
				return null;
			}
			return _agentHandler.FindUnusedPointWithTagForAgent(OwnerAgent, "npc_common");
		}

		private UsableMachine FindTarget()
		{
			return FindRandomWalkingTarget(_isIndoor && !_indoorWanderingIsActive);
		}

		private float GetTargetScore(UsableMachine usableMachine)
		{
            if (OwnerAgent.GetComponent<CampaignAgentComponent>().AgentNavigator.SpecialTargetTag != null && !usableMachine.GameEntity.HasTag(OwnerAgent.GetComponent<CampaignAgentComponent>().AgentNavigator.SpecialTargetTag))
			{
				return 0f;
			}
			StandingPoint vacantStandingPointForAI = usableMachine.GetVacantStandingPointForAI(OwnerAgent);
			if (vacantStandingPointForAI == null || vacantStandingPointForAI.IsDisabledForAgent(OwnerAgent))
			{
				return 0f;
			}
			float num = 1f;
			Vec3 vec = vacantStandingPointForAI.GetUserFrameForAgent(OwnerAgent).Origin.GetGroundVec3MT() - OwnerAgent.Position;
			if (vec.Length < 2f)
			{
				num *= vec.Length / 2f;
			}
			return num * (0.8f + MBRandom.RandomFloat * 0.2f);
		}

		public override void OnSpecialTargetChanged()
		{
			if (_wanderTarget == null)
			{
				return;
			}
            if (!Navigator.SpecialTargetTag.IsEmpty() && !_wanderTarget.GameEntity.HasTag(Navigator.SpecialTargetTag))
			{
				_wanderTarget = null;
                Navigator.SetTarget(_wanderTarget, false);
				return;
			}
            if (Navigator.SpecialTargetTag.IsEmpty() && !_wanderTarget.GameEntity.HasTag("npc_common"))
			{
				_wanderTarget = null;
                Navigator.SetTarget(_wanderTarget, false);
			}
		}

		public override string GetDebugInfo()
		{
			string text = "Walk ";
			if (_waitTimer != null)
			{
				text = string.Concat(new object[]
				{
					text,
					"(Wait ",
					(int)_waitTimer.ElapsedTime(),
					"/",
					_waitTimer.Duration,
					")"
				});
			}
			else if (_wanderTarget == null)
			{
				text += "(search for target!)";
			}
			return text;
		}

		protected override void OnDeactivate()
		{
            Navigator.ClearTarget();
			_wanderTarget = null;
			_waitTimer = null;
		}
	}
}
