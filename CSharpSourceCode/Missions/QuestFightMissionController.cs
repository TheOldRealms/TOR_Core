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
	public class QuestFightMissionController : MissionLogic
	{
		private TORMissionAgentHandler _missionAgentSpawnLogic;
		private MissionConversationLogic _missionConversationLogic;
		private readonly PartyTemplateObject _enemyPartyTemplate;
		private readonly int _enemyCount;
		private Action<bool> _onMissionEnd;
		private bool _conversationFired;
		private bool _battleStarted;

		public QuestFightMissionController(PartyTemplateObject enemyPartyTemplate, int enemyCount, Action<bool> onMissionEnd = null)
		{
			_enemyPartyTemplate = enemyPartyTemplate;
			_enemyCount = enemyCount;
			_onMissionEnd = onMissionEnd;
		}

		public override void OnBehaviorInitialize()
		{
			base.OnBehaviorInitialize();
			_missionAgentSpawnLogic = Mission.GetMissionBehavior<TORMissionAgentHandler>();
			_missionConversationLogic = Mission.GetMissionBehavior<MissionConversationLogic>();
			if(_missionConversationLogic != null)
            {
				Campaign.Current.ConversationManager.ConversationEnd += ConversationManager_ConversationEnd;
			}
		}

        private void ConversationManager_ConversationEnd()
        {
			StartBattle();
			Campaign.Current.ConversationManager.ConversationEnd -= ConversationManager_ConversationEnd;
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
		}

        public override void OnMissionTick(float dt)
        {
			if (Mission.SceneName != "TOR_cultist_lair_001" || _missionConversationLogic == null) return;
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

		private void StartBattle()
        {
			Mission.SetMissionMode(MissionMode.Battle, false);
			foreach (var agent in Mission.Agents)
			{
				if (agent != Agent.Main && agent.IsHuman && agent.IsActive())
				{
					agent.SetWatchState(Agent.WatchState.Alarmed);
				}
			}
		}

		protected bool CanAgentSeeAgent(Agent agent, Agent targetAgent)
		{
			if ((agent.Position - targetAgent.Position).Length < 10f)
			{
				Vec3 eyeGlobalPosition = targetAgent.GetEyeGlobalPosition();
				Vec3 eyeGlobalPosition2 = agent.GetEyeGlobalPosition();
				if (MathF.Abs(Vec3.AngleBetweenTwoVectors(targetAgent.Position - agent.Position, agent.LookDirection)) < 1.5f)
				{
					float num;
					return !Mission.Current.Scene.RayCastForClosestEntityOrTerrain(eyeGlobalPosition2, eyeGlobalPosition, out num, 0.01f, BodyFlags.CommonFocusRayCastExcludeFlags);
				}
			}
			return false;
		}

		public override InquiryData OnEndMissionRequest(out bool canLeave)
		{
			canLeave = Mission.MissionResult != null && Mission.MissionResult.BattleResolved == true && Mission.MissionResult.PlayerVictory;
            if (!canLeave) MBInformationManager.AddQuickInformation(new TextObject("You may not leave until finishing the quest scenario."));
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
				if (_onMissionEnd != null) _onMissionEnd(missionResult.PlayerVictory);
				return true;
            }
			if (Mission.GetMemberCountOfSide(BattleSideEnum.Attacker) == 0)
			{
				missionResult = (Mission.PlayerTeam.Side == BattleSideEnum.Attacker) ? MissionResult.CreateDefeated(Mission) : MissionResult.CreateSuccessful(Mission, false);
				if (_onMissionEnd != null) _onMissionEnd(missionResult.PlayerVictory);
				return true;
			}
			if (Mission.GetMemberCountOfSide(BattleSideEnum.Defender) == 0)
			{
				missionResult = (Mission.PlayerTeam.Side == BattleSideEnum.Attacker) ? MissionResult.CreateSuccessful(Mission, false) : MissionResult.CreateDefeated(Mission);
				if (_onMissionEnd != null) _onMissionEnd(missionResult.PlayerVictory);
				return true;
			}
			return false;
		}
    }
}
