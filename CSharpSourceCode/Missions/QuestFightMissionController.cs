using SandBox.Conversation.MissionLogics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace TOR_Core.Missions
{
	public class QuestFightMissionController(PartyTemplateObject enemyPartyTemplate, int enemyCount, Action<bool> onMissionEnd = null, bool forceUsableMachineActivation = false) : MissionLogic
	{
		private TORMissionAgentHandler _missionAgentSpawnLogic;
		private MissionConversationLogic _missionConversationLogic;
		private readonly PartyTemplateObject _enemyPartyTemplate = enemyPartyTemplate;
		private readonly int _enemyCount = enemyCount;
		private readonly Action<bool> _onMissionEnd = onMissionEnd;
        private readonly bool _forceUsableMachineActivation = forceUsableMachineActivation;
        private bool _conversationFired;
		private bool _battleStarted;
        
        public bool PlayerCanLeave { get; set; } = false;

        public override void OnBehaviorInitialize()
		{
			base.OnBehaviorInitialize();
			_missionAgentSpawnLogic = Mission.GetMissionBehavior<TORMissionAgentHandler>();
			_missionConversationLogic = Mission.GetMissionBehavior<MissionConversationLogic>();
		}

        public override void AfterStart()
		{
			Mission.SetMissionMode(MissionMode.StartUp, true);
			Mission.IsInventoryAccessible = false;
			Mission.IsQuestScreenAccessible = true;
			Mission.DoesMissionRequireCivilianEquipment = false;
			_missionAgentSpawnLogic.SpawnPlayer(false, true, false, true, true);
			_missionAgentSpawnLogic.SpawnEnemies(_enemyPartyTemplate, _enemyCount);
			foreach (var agent in Mission.Agents)
			{
				if (agent != Agent.Main && agent.IsHuman)
				{
					agent.SetWatchState(Agent.WatchState.Patrolling);
				}
			}
			if(_forceUsableMachineActivation)
			{
				foreach(var missionObject in Mission.Current.MissionObjects)
				{
					if(missionObject is UsableMachine machine)
					{
						machine.Activate();
					}
				}
			}
		}

        public override void OnMissionTick(float dt)
        {
			if (!IsConversationMission() || _missionConversationLogic == null) return;
            if (!_conversationFired && !_battleStarted)
            {
				foreach (Agent agent in Mission.Agents.Where(x => x.IsHuman && x != Agent.Main))
				{
					if (CanAgentSeeAgent(agent, Agent.Main))
					{
						_missionConversationLogic.StartConversation(agent, false);
						_conversationFired = true;
						break;
					}
				}
			}
        }

		private bool IsConversationMission()
		{
			return Mission.SceneName != "TOR_cultist_lair_001" || Mission.SceneName != "TOR_nurgle_lair_001";

        }

		private void StartBattle()
        {
			_battleStarted = true;
			Mission.SetMissionMode(MissionMode.Battle, false);
			foreach (var agent in Mission.Agents)
			{
				if (agent.IsAIControlled && agent.IsHuman && agent.IsActive())
				{
					agent.SetWatchState(Agent.WatchState.Alarmed);
				}
			}
		}

		protected bool CanAgentSeeAgent(Agent agent, Agent targetAgent)
		{
			bool result = false;
			if ((agent.Position - targetAgent.Position).Length < 10f)
			{
				Vec3 eyeGlobalPosition = targetAgent.GetEyeGlobalPosition();
				Vec3 eyeGlobalPosition2 = agent.GetEyeGlobalPosition();
				if (MathF.Abs(Vec3.AngleBetweenTwoVectors(targetAgent.Position - agent.Position, agent.LookDirection)) < 1.5f)
				{
                    using (new TWSharedMutexReadLock(Scene.PhysicsAndRayCastLock))
					{
                        result = !Mission.Current.Scene.RayCastForClosestEntityOrTerrainMT(eyeGlobalPosition2, eyeGlobalPosition, out _, 0.01f, BodyFlags.CommonFocusRayCastExcludeFlags);
                    }
                }
			}
			return result;
		}

		public override InquiryData OnEndMissionRequest(out bool canLeave)
		{
			canLeave = (Mission.MissionResult != null && Mission.MissionResult.BattleResolved == true && Mission.MissionResult.PlayerVictory) || PlayerCanLeave;
            if (!canLeave) MBInformationManager.AddQuickInformation(new TextObject("{=tor_quest_fight_hint_not_leaving_str}You may not leave until finishing the quest scenario."));
			return null;
		}

		public override void OnAgentHit(Agent affectedAgent, Agent affectorAgent, in MissionWeapon affectorWeapon, in Blow blow, in AttackCollisionData attackCollisionData)
		{
			base.OnAgentHit(affectedAgent, affectorAgent, affectorWeapon, blow, attackCollisionData);
			if (!_battleStarted) StartBattle();
		}

        public override bool MissionEnded(ref MissionResult missionResult)
        {
            if(Agent.Main == null || !Agent.Main.IsActive())
            {
				missionResult = MissionResult.CreateDefeated(Mission);
                _onMissionEnd?.Invoke(missionResult.PlayerVictory);
                return true;
            }
			if (Mission.GetMemberCountOfSide(BattleSideEnum.Attacker) == 0)
			{
				missionResult = (Mission.PlayerTeam.Side == BattleSideEnum.Attacker) ? MissionResult.CreateDefeated(Mission) : MissionResult.CreateSuccessful(Mission, false);
                _onMissionEnd?.Invoke(missionResult.PlayerVictory);
                return true;
			}
			if (Mission.GetMemberCountOfSide(BattleSideEnum.Defender) == 0)
			{
				missionResult = (Mission.PlayerTeam.Side == BattleSideEnum.Attacker) ? MissionResult.CreateSuccessful(Mission, false) : MissionResult.CreateDefeated(Mission);
				_onMissionEnd?.Invoke(missionResult.PlayerVictory);
				return true;
			}
			return false;
		}
    }
}
