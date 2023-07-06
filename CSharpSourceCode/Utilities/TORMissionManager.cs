using SandBox;
using SandBox.Conversation.MissionLogics;
using SandBox.Missions.MissionLogics;
using SandBox.View;
using SandBox.View.Missions;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.TroopSuppliers;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Missions.Handlers;
using TaleWorlds.MountAndBlade.Source.Missions;
using TaleWorlds.MountAndBlade.Source.Missions.Handlers;
using TaleWorlds.MountAndBlade.Source.Missions.Handlers.Logic;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.View.MissionViews;
using static TaleWorlds.MountAndBlade.Mission;

namespace TOR_Core.Utilities
{
    [MissionManager]
	public static class TorMissionManager
	{
		[MissionMethod]
		public static Mission OpenQuestMission(string scene, PartyTemplateObject enemyPartyTemplate, int enemyCount = 8)
		{
			return MissionState.OpenNew("QuestFight", SandBoxMissions.CreateSandBoxMissionInitializerRecord(scene, "", false, DecalAtlasGroup.All), (Mission mission) => new MissionBehavior[]
			{
				new MissionOptionsComponent(),
				new CampaignMissionComponent(),
				new MissionBasicTeamLogic(),
				new MissionAgentLookHandler(),
				new BasicLeaveMissionLogic(true),
				new LeaveMissionLogic(),
				new AgentHumanAILogic(),
				new MissionConversationLogic(),
				new QuestFightMissionController(enemyPartyTemplate, enemyCount),
				new TORMissionAgentHandler(),
				new HeroSkillHandler(),
				new MissionFightHandler(),
				new MissionFacialAnimationHandler(),
				new MissionHardBorderPlacer(),
				new MissionBoundaryPlacer(),
				new MissionBoundaryCrossingHandler(),
				new EquipmentControllerLeaveLogic()
			}, true, true);
		}


		[MissionMethod]
		public static Mission OpenGraveyardMission()
		{
			var rec = SandBoxMissions.CreateSandBoxMissionInitializerRecord("TOR_graveyard_01_forceatmo");
			return MissionState.OpenNew("Battle", rec, delegate (Mission mission)
			{
				IMissionTroopSupplier[] suppliers = new IMissionTroopSupplier[]
				{
					new PartyGroupTroopSupplier(MapEvent.PlayerMapEvent, BattleSideEnum.Defender),
					new PartyGroupTroopSupplier(MapEvent.PlayerMapEvent, BattleSideEnum.Attacker)
				};
				List<MissionBehavior> list = new List<MissionBehavior>();
				list.Add(new MissionAgentSpawnLogic(suppliers, BattleSideEnum.Defender, BattleSizeType.Battle)); //OK
				list.Add(new BattlePowerCalculationLogic()); //OK
				list.Add(new BattleSpawnLogic("battle_set")); //OK
				list.Add(new GraveyardFightMissionController()); //OK
				list.Add(new CampaignMissionComponent()); //OK
				list.Add(new BattleAgentLogic()); //OK
				list.Add(new MountAgentLogic()); //OK
				list.Add(new MissionOptionsComponent()); //OK
				list.Add(new BattleEndLogic()); //OK
				list.Add(new MissionCombatantsLogic(MobileParty.MainParty.MapEvent.InvolvedParties, PartyBase.MainParty, MobileParty.MainParty.MapEvent.GetLeaderParty(BattleSideEnum.Defender), MobileParty.MainParty.MapEvent.GetLeaderParty(BattleSideEnum.Attacker), Mission.MissionTeamAITypeEnum.FieldBattle, false));
				list.Add(new BattleObserverMissionLogic()); //OK
				list.Add(new AgentHumanAILogic());
				list.Add(new AgentVictoryLogic());
				list.Add(new MissionAgentPanicHandler());
				list.Add(new BattleMissionAgentInteractionLogic());
				list.Add(new AgentMoraleInteractionLogic());
				list.Add(new AssignPlayerRoleInTeamMissionController(true, false, false, null, FormationClass.General));
				Hero attackerHero = MapEvent.PlayerMapEvent.AttackerSide.LeaderParty.LeaderHero;
				TextObject attackerGeneralName = (attackerHero != null) ? attackerHero.Name : null;
				Hero defenderHero = MapEvent.PlayerMapEvent.DefenderSide.LeaderParty.LeaderHero;
				list.Add(new SandboxGeneralsAndCaptainsAssignmentLogic(attackerGeneralName, (defenderHero != null) ? defenderHero.Name : null, null, null, false));
				list.Add(new EquipmentControllerLeaveLogic());
				list.Add(new MissionHardBorderPlacer());
				list.Add(new MissionBoundaryPlacer());
				list.Add(new MissionBoundaryCrossingHandler());
				list.Add(new HighlightsController());
				list.Add(new BattleHighlightsController());
				list.Add(new DeploymentMissionController(false));
				list.Add(new BattleDeploymentHandler(false));
				return list.ToArray();
			}, true, true);
		}
	}

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
			foreach(var agent in Mission.Agents)
            {
				if(agent != Agent.Main && agent.IsHuman)
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

	[ViewCreatorModule]
	public class QuestFightViewCreatorModule
	{
		[ViewMethod("QuestFight")]
		public static MissionView[] OpenQuestFightMission(Mission mission)
		{
			return new List<MissionView>
			{
				new MissionCampaignView(),
				new MissionConversationCameraView(),
				SandBoxViewCreator.CreateMissionConversationView(mission),
				ViewCreator.CreateMissionSingleplayerEscapeMenu(CampaignOptions.IsIronmanMode),
				ViewCreator.CreateOptionsUIHandler(),
				ViewCreator.CreateMissionMainAgentEquipDropView(mission),
				new MissionSingleplayerViewHandler(),
				ViewCreator.CreateMissionAgentStatusUIHandler(mission),
				ViewCreator.CreateMissionMainAgentEquipmentController(mission),
				ViewCreator.CreateMissionAgentLockVisualizerView(mission),
				ViewCreator.CreateMissionBoundaryCrossingView(),
				SandBoxViewCreator.CreateMissionBarterView(),
				ViewCreator.CreateMissionLeaveView(),
				new MissionBoundaryWallView(),
				SandBoxViewCreator.CreateMissionNameMarkerUIHandler(mission),
				new MissionItemContourControllerView(),
				new MissionAgentContourControllerView(),
				new MissionCampaignBattleSpectatorView(),
				ViewCreator.CreatePhotoModeView(),
				ViewCreator.CreateSingleplayerMissionKillNotificationUIHandler()
			}.ToArray();
		}
	}
}
