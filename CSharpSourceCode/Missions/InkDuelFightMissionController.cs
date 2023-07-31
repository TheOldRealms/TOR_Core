using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Objects;
using TaleWorlds.ObjectSystem;

namespace TOR_Core.Missions
{
	public class InkDuelFightMissionController : MissionLogic
	{
		private TORMissionAgentHandler _missionAgentSpawnLogic;
		private Action<bool> _onMissionEnd;
		private Agent _duelAgent;
		private bool _duelEnded;

		public InkDuelFightMissionController(Action<bool> onMissionEnd)
		{
			this._onMissionEnd = onMissionEnd;
		}

		public override void OnBehaviorInitialize()
		{
			base.OnBehaviorInitialize();
			_missionAgentSpawnLogic = Mission.GetMissionBehavior<TORMissionAgentHandler>();
		}

		public override void AfterStart()
		{
			Mission.SetMissionMode(MissionMode.Duel, true);
			Mission.IsInventoryAccessible = false;
			Mission.IsQuestScreenAccessible = false;
			Mission.DoesMissionRequireCivilianEquipment = false;
			_missionAgentSpawnLogic.SpawnPlayer(false, true, false, true, false, true);
			_duelAgent = _missionAgentSpawnLogic.SpawnEnemyDuelist();
			SpawnHorses();
			SpawnSpectators();
		}

        private void SpawnSpectators()
        {
            throw new NotImplementedException();
        }

        private void SpawnHorses()
		{
			foreach (GameEntity gameEntity in Mission.Current.Scene.FindEntitiesWithTag("sp_horse"))
			{
				MatrixFrame globalFrame = gameEntity.GetGlobalFrame();
				string objectName = gameEntity.Tags[1];
				ItemObject @object = MBObjectManager.Instance.GetObject<ItemObject>(objectName);
				ItemRosterElement itemRosterElement = new ItemRosterElement(@object, 1, null);
				ItemRosterElement itemRosterElement2 = default(ItemRosterElement);
				if (@object.HasHorseComponent)
				{
					globalFrame.rotation.OrthonormalizeAccordingToForwardAndKeepUpAsZAxis();
					Mission mission = Mission.Current;
					ItemRosterElement rosterElement = itemRosterElement;
					ItemRosterElement harnessRosterElement = itemRosterElement2;
					Vec2 asVec = globalFrame.rotation.f.AsVec2;
					Agent agent = mission.SpawnMonster(rosterElement, harnessRosterElement, globalFrame.origin, asVec, -1);
					AnimalSpawnSettings.CheckAndSetAnimalAgentFlags(gameEntity, agent);

					int num = 10 + MBRandom.RandomInt(90);
					for (int i = 0; i < num; i++)
					{
						agent.TickActionChannels(0.1f);
						Vec3 v = agent.ComputeAnimationDisplacement(0.1f);
						if (v.LengthSquared > 0f)
						{
							agent.TeleportToPosition(agent.Position + v);
						}
						agent.AgentVisuals.GetSkeleton().TickAnimations(0.1f, agent.AgentVisuals.GetGlobalFrame(), true);
					}
				}
			}
		}

		public override void OnAgentRemoved(Agent affectedAgent, Agent affectorAgent, AgentState agentState, KillingBlow blow)
		{
			if (affectedAgent == Agent.Main || affectedAgent == _duelAgent)
			{
				_duelEnded = true;
				GameTexts.SetVariable("leave_key", HyperlinkTexts.GetKeyHyperlinkText(HotKeyManager.GetHotKeyId("Generic", 4)));
				MBInformationManager.AddQuickInformation(GameTexts.FindText("str_duel_has_ended", null), 0, null, "");
				if (_onMissionEnd != null) _onMissionEnd(affectedAgent != Agent.Main);
			}
		}

		public override InquiryData OnEndMissionRequest(out bool canLeave)
		{
			canLeave = _duelEnded;
			if (!canLeave) MBInformationManager.AddQuickInformation(GameTexts.FindText("str_can_not_retreat_duel_ongoing", null), 0, null, "");
			return null;
		}
	}
}
