using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace TOR_Core.Missions
{
	public class QuestFightMissionController : MissionLogic
	{
		private TORMissionAgentHandler _missionAgentSpawnLogic;
		private readonly PartyTemplateObject _enemyPartyTemplate;
		private readonly int _enemyCount;

		public QuestFightMissionController(PartyTemplateObject enemyPartyTemplate, int enemyCount)
		{
			_enemyPartyTemplate = enemyPartyTemplate;
			_enemyCount = enemyCount;
		}

		public override void OnBehaviorInitialize()
		{
			base.OnBehaviorInitialize();
			_missionAgentSpawnLogic = Mission.GetMissionBehavior<TORMissionAgentHandler>();
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

		public override void OnAgentHit(Agent affectedAgent, Agent affectorAgent, in MissionWeapon affectorWeapon, in Blow blow, in AttackCollisionData attackCollisionData)
		{
			base.OnAgentHit(affectedAgent, affectorAgent, affectorWeapon, blow, attackCollisionData);
			foreach (var agent in Mission.Agents)
			{
				if (agent != Agent.Main && agent.IsHuman && agent.IsActive())
				{
					agent.SetWatchState(Agent.WatchState.Alarmed);
				}
			}
		}
	}
}
