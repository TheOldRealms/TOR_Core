using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace TOR_Core.Missions
{
	public class GraveyardFightMissionController : MissionLogic
	{
		public override void OnBehaviorInitialize()
		{
			base.OnBehaviorInitialize();
			_missionAgentSpawnLogic = Mission.GetMissionBehavior<MissionAgentSpawnLogic>();
			_mapEvent = MapEvent.PlayerMapEvent;
		}

		public override void AfterStart()
		{
			int numDefender = MathF.Min(_mapEvent.GetNumberOfInvolvedMen(BattleSideEnum.Defender), 4);
			int numAttacker = _mapEvent.GetNumberOfInvolvedMen(BattleSideEnum.Attacker);
			int defenderInitialSpawn = numDefender;
			int attackerInitialSpawn = numAttacker;
			Mission.DoesMissionRequireCivilianEquipment = false;
			_missionAgentSpawnLogic.SetSpawnHorses(BattleSideEnum.Defender, false);
			_missionAgentSpawnLogic.SetSpawnHorses(BattleSideEnum.Attacker, false);
			_missionAgentSpawnLogic.InitWithSinglePhase(numDefender, numAttacker, defenderInitialSpawn, attackerInitialSpawn, true, true, MissionSpawnSettings.CreateDefaultSpawnSettings());
		}

		private MissionAgentSpawnLogic _missionAgentSpawnLogic;
		private MapEvent _mapEvent;
	}
}
