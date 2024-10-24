using SandBox;
using SandBox.Missions.AgentBehaviors;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.AgentOrigins;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;
using TOR_Core.BattleMechanics.AI.CivilianMissionAI;

namespace TOR_Core.Missions
{
    public class TORMissionAgentHandler(string playerSpecialSpawnTag = null) : MissionLogic
	{
		private string _playerSpecialSpawnTag = playerSpecialSpawnTag;
        private Dictionary<string, List<UsableMachine>> _usablePoints = [];

        public override void EarlyStart()
        {
			CollectUsablePoints();
        }

        private void CollectUsablePoints()
        {
			_usablePoints.Clear();
			foreach (UsableMachine usableMachine in Mission.MissionObjects.FindAllWithType<UsableMachine>())
			{
				if (usableMachine.IsDeactivated) continue;
				foreach (string key in usableMachine.GameEntity.Tags)
				{
					if (!_usablePoints.ContainsKey(key))
					{
						_usablePoints.Add(key, []);
					}
					_usablePoints[key].Add(usableMachine);
				}
			}
		}

		public void SpawnPlayer(bool civilianEquipment = false, bool noHorses = false, bool noWeapon = false, bool wieldInitialWeapons = false, bool isStealth = false, bool isDuelMode = false, string spawnTag = "")
		{
			if (Campaign.Current.GameMode != CampaignGameMode.Campaign)
			{
				civilianEquipment = false;
			}
			MatrixFrame matrixFrame = MatrixFrame.Identity;
			GameEntity gameEntity = Mission.Scene.FindEntityWithTag("spawnpoint_player");
			if (gameEntity != null)
			{
				matrixFrame = gameEntity.GetGlobalFrame();
				matrixFrame.rotation.OrthonormalizeAccordingToForwardAndKeepUpAsZAxis();
			}
			if (_playerSpecialSpawnTag != null)
			{
				GameEntity gameEntity2 = null;
				UsableMachine usableMachine = GetAllUsablePointsWithTag(_playerSpecialSpawnTag).FirstOrDefault();
				if (usableMachine != null)
				{
					StandingPoint standingPoint = usableMachine.StandingPoints.FirstOrDefault();
					gameEntity2 = ((standingPoint != null) ? standingPoint.GameEntity : null);
				}
				if (gameEntity2 == null)
				{
					gameEntity2 = Mission.Scene.FindEntityWithTag(_playerSpecialSpawnTag);
				}
				if (gameEntity2 != null)
				{
					matrixFrame = gameEntity2.GetGlobalFrame();
					matrixFrame.rotation.OrthonormalizeAccordingToForwardAndKeepUpAsZAxis();
				}
			}
			
			CharacterObject playerCharacter = GetPlayerCharacter();
			AgentBuildData agentBuildData = new AgentBuildData(playerCharacter).Team(Mission.PlayerTeam).InitialPosition(matrixFrame.origin);
			Vec2 vec = matrixFrame.rotation.f.AsVec2;
			vec = vec.Normalized();
			AgentBuildData agentBuildData2 = agentBuildData.InitialDirection(vec)
				.CivilianEquipment(civilianEquipment)
				.NoHorses(noHorses)
				.NoWeapons(noWeapon)
				.ClothingColor1(Mission.PlayerTeam.Color)
				.ClothingColor2(Mission.PlayerTeam.Color2)
				.TroopOrigin(new PartyAgentOrigin(PartyBase.MainParty, GetPlayerCharacter(), -1, default, false))
				.MountKey(MountCreationKey.GetRandomMountKeyString(playerCharacter.Equipment[EquipmentIndex.ArmorItemEndSlot].Item, playerCharacter.GetMountKeySeed()))
				.Controller(Agent.ControllerType.Player);

            if (isDuelMode)
            {
				agentBuildData2.Equipment(GetRapierReplacement(playerCharacter));
            }

			Hero heroObject = playerCharacter.HeroObject;
			if (((heroObject != null) ? heroObject.ClanBanner : null) != null)
			{
				agentBuildData2.Banner(playerCharacter.HeroObject.ClanBanner);
			}
			
			Agent agent = Mission.SpawnAgent(agentBuildData2, false);
			if (wieldInitialWeapons)
			{
				agent.WieldInitialWeapons(Agent.WeaponWieldActionType.InstantAfterPickUp);
			}
			
			for (int i = 0; i < 3; i++)
			{
				Agent.Main.AgentVisuals.GetSkeleton().TickAnimations(0.1f, Agent.Main.AgentVisuals.GetGlobalFrame(), true);
			}
		}

        private Equipment GetRapierReplacement(CharacterObject playerCharacter)
        {
            Equipment equipment = new Equipment();
			for (EquipmentIndex i = EquipmentIndex.ArmorItemBeginSlot; i <= EquipmentIndex.ArmorItemEndSlot; i++)
			{
				EquipmentElement equipmentFromSlot = playerCharacter.Equipment.GetEquipmentFromSlot(i);
				if (equipmentFromSlot.Item != null)
				{
					equipment.AddEquipmentToSlotWithoutAgent(i, new EquipmentElement(equipmentFromSlot.Item, null, null, false));
				}
			}
			var rapier = Game.Current.ObjectManager.GetObject<ItemObject>("tor_empire_weapon_rapier_001");
			if(rapier != null)
            {
				equipment.AddEquipmentToSlotWithoutAgent(EquipmentIndex.Weapon0, new EquipmentElement(rapier));
            }
			return equipment;
		}

        public void SpawnEnemies(PartyTemplateObject template, int enemyCount)
        {
			int count = 0;
			if (template == null) return;
			int maxCount = GetAllUsablePointsWithTag("npc_common").Count;
			if (maxCount < enemyCount) enemyCount = maxCount;
			List<CharacterObject> enemies = new List<CharacterObject>();
			foreach(var stack in template.Stacks)
            {
				enemies.Add(stack.Character);
            }
			while(count < enemyCount)
            {
				SpawnEnemy(enemies.GetRandomElementInefficiently());
				count++;
			}
        }

		public Agent SpawnEnemyDuelist()
        {
			var character = MBObjectManager.Instance.GetObject<CharacterObject>("tor_ti_vittorio");
			if(character != null)
            {
				var agent = SpawnEnemy(character, "npc_duel_enemy", true);
				agent.Defensiveness = 3;
				return agent;
            }
			return null;
        }

		private Agent SpawnEnemy(CharacterObject character, string specialTag = null, bool turnHostile = false)
        {
			MatrixFrame matrixFrame = MatrixFrame.Identity;
			List<UsableMachine> allUsablePointsWithTag = GetAllUsablePointsWithTag("npc_common");
			if(!string.IsNullOrWhiteSpace(specialTag)) allUsablePointsWithTag = GetAllUsablePointsWithTag(specialTag);
			if (allUsablePointsWithTag.Count > 0)
			{
				var point = allUsablePointsWithTag.GetRandomElementInefficiently();
				if (point != null) GetSpawnFrameFromUsableMachine(point, out matrixFrame);
				if (matrixFrame != MatrixFrame.Identity)
				{
					matrixFrame.rotation.f.z = 0f;
					matrixFrame.rotation.f.Normalize();
					matrixFrame.rotation.u = Vec3.Up;
					matrixFrame.rotation.s = Vec3.CrossProduct(matrixFrame.rotation.f, matrixFrame.rotation.u);
					matrixFrame.rotation.OrthonormalizeAccordingToForwardAndKeepUpAsZAxis();

                    using (new TWSharedMutexReadLock(Scene.PhysicsAndRayCastLock))
					{
                        matrixFrame.origin.z = Mission.Scene.GetGroundHeightAtPositionMT(matrixFrame.origin, BodyFlags.CommonCollisionExcludeFlags);
                    }

					var agentData = GetAgentBuildData(character, matrixFrame);

					var agent = Mission.SpawnAgent(agentData);
					AnimationSystemData animationSystemData = agentData.AgentMonster.FillAnimationSystemData(MBGlobals.GetActionSetWithSuffix(agentData.AgentMonster, agentData.AgentIsFemale, ActionSetCode.HideoutBanditActionSetSuffix), character.GetStepSize(), false);
					agent.SetActionSet(ref animationSystemData);
					var agentNavigator = agent.GetComponent<CampaignAgentComponent>().CreateAgentNavigator();
					var daily = agentNavigator.AddBehaviorGroup<TORDailyBehaviorGroup>();
					daily.AddBehavior<TORWalkingBehavior>().SetIndoorWandering(false);
					agentNavigator.AddBehaviorGroup<InterruptingBehaviorGroup>();
					var alarmed = agentNavigator.AddBehaviorGroup<TORAlarmedBehaviorGroup>();
					alarmed.AddBehavior<TORFightBehavior>();
                    if (turnHostile)
                    {
						agent.SetWatchState(Agent.WatchState.Alarmed);
                    }
					return agent;
				}
			}
			return null;
		}

		public IEnumerable<string> GetAllSpawnTags()
		{
			return _usablePoints.Keys.ToList();
		}

		public UsableMachine FindUnusedPointWithTagForAgent(Agent agent, string tag)
		{
			return FindUnusedPointForAgent(agent, _usablePoints, tag);
		}

		private UsableMachine FindUnusedPointForAgent(Agent agent, Dictionary<string, List<UsableMachine>> usableMachinesList, string primaryTag)
		{
			List<UsableMachine> list;
			if (usableMachinesList.TryGetValue(primaryTag, out list) && list.Count > 0)
			{
				return list.GetRandomElementWithPredicate(x => !x.IsDisabled && !x.IsDestroyed && x.IsStandingPointAvailableForAgent(agent));
			}
			return null;
		}

		private AgentBuildData GetAgentBuildData(CharacterObject character, MatrixFrame spawnFrame)
		{
			BasicCharacterObject troopCharacter = character as BasicCharacterObject;

			IAgentOriginBase troopOrigin = new SimpleAgentOrigin(troopCharacter);

			AgentBuildData buildData = new AgentBuildData(troopCharacter).
				Team(Mission.PlayerEnemyTeam).
				Equipment(troopCharacter.GetFirstEquipment(false)).
				TroopOrigin(troopOrigin).
				InitialPosition(spawnFrame.origin).
				InitialDirection(spawnFrame.rotation.f.AsVec2.Normalized()).
				NoHorses(true).
				CivilianEquipment(false);
			return buildData;
		}

		public List<UsableMachine> GetAllUsablePointsWithTag(string tag)
		{
			List<UsableMachine> list = new List<UsableMachine>();
			List<UsableMachine> collection = new List<UsableMachine>();
			if (_usablePoints.TryGetValue(tag, out collection))
			{
				list.AddRange(collection);
			}
			return list;
		}

		private CharacterObject GetPlayerCharacter()
		{
			CharacterObject characterObject = CharacterObject.PlayerCharacter;
			if (characterObject == null)
			{
				characterObject = Game.Current.ObjectManager.GetObject<CharacterObject>("main_hero_for_perf");
			}
			return characterObject;
		}

		private bool GetSpawnFrameFromUsableMachine(UsableMachine usableMachine, out MatrixFrame frame)
		{
			frame = MatrixFrame.Identity;
			StandingPoint randomElementWithPredicate = usableMachine.StandingPoints.GetRandomElementWithPredicate((StandingPoint x) => !x.IsDeactivated && !x.IsDisabled);
			if (randomElementWithPredicate != null)
			{
				frame = randomElementWithPredicate.GameEntity.GetGlobalFrame();
				return true;
			}
			return false;
		}
	}
}
