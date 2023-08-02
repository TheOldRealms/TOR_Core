using SandBox;
using SandBox.Tournaments;
using SandBox.Tournaments.MissionLogics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.AgentOrigins;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.TournamentGames;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View.MissionViews;
using TaleWorlds.ObjectSystem;
using TOR_Core.BattleMechanics.Jousting;

namespace TOR_Core.Missions
{
    public class JoustFightMissionController : MissionLogic, ITournamentGameBehavior
    {
		private TournamentMatch _match;
		private bool _isLastRound;
		private BasicMissionTimer _endTimer;
		private BasicMissionTimer _cheerTimer;
		private BasicMissionTimer _dismountNotificationTimer;
		private GameEntity _team0MountedSpawn;
		private GameEntity _team1MountedSpawn;
		private GameEntity _team0FootSpawn;
		private GameEntity _team1FootSpawn;
		private bool _isSimulated;
		private bool _forceEndMatch;
		private bool _cheerStarted;
		private CultureObject _culture;
		private List<TournamentParticipant> _aliveParticipants;
		private List<TournamentTeam> _aliveTeams;
		private List<Agent> _currentTournamentAgents;
		private List<Agent> _currentTournamentMountAgents;
		private const float XpShareForKill = 0.5f;
		private const float XpShareForDamage = 0.5f;
		private MissionCameraFadeView _cameraView;
		private JoustFightState _currentState = JoustFightState.MountedCombat;

		public JoustFightState CurrentState => _currentState;

		public JoustFightMissionController(CultureObject culture)
		{
			_match = null;
			_culture = culture;
			_cheerStarted = false;
			_currentTournamentAgents = new List<Agent>();
			_currentTournamentMountAgents = new List<Agent>();
		}

		public bool CanAgentRout(Agent agent) => false;

		public override void OnBehaviorInitialize()
		{
			base.OnBehaviorInitialize();
            Mission.CanAgentRout_AdditionalCondition += CanAgentRout;
		}

		public override void AfterStart()
		{
			_team0MountedSpawn = Mission.Scene.FindEntityWithTag("team0_mounted_spawn");
			_team1MountedSpawn = Mission.Scene.FindEntityWithTag("team1_mounted_spawn");
			_team0FootSpawn = Mission.Scene.FindEntityWithTag("team0_foot_spawn");
			_team1FootSpawn = Mission.Scene.FindEntityWithTag("team1_foot_spawn");
			_cameraView = Mission.GetMissionBehavior<MissionCameraFadeView>();
		}

        public override void OnMissionTick(float dt)
        {
            if(_currentState == JoustFightState.Transition)
            {
				if(IsThereAnyPlayerAgent() && GetPlayerAgent().HasMount)
                {
					if(_dismountNotificationTimer == null) _dismountNotificationTimer = new BasicMissionTimer();
					else if(_dismountNotificationTimer.ElapsedTime > 5)
                    {
						MBInformationManager.AddQuickInformation(new TextObject("You must dismount to continue with combat on foot."));
						_dismountNotificationTimer.Reset();
                    }
				}

				int num = 0;
				foreach(var agent in _currentTournamentAgents)
                {
					var action = agent.GetCurrentAction(0);
                    if (action.Name.Contains("act_dismount_"))
                    {
						var progress = agent.GetCurrentActionProgress(0);
						if(progress >= 0.95f)
                        {
							SandBoxHelpers.MissionHelper.FadeOutAgents(from x in _currentTournamentMountAgents
																	   where x.IsActive()
																	   select x, true, false);
						}
                    }

					var target = GetSpawnPointForTeam(agent.Team.TeamIndex, false);
					if (agent.IsPlayerControlled && !agent.HasMount) num++;
					else if(agent.Position.DistanceSquared(target.GlobalPosition) < 5 && !agent.HasMount)
                    {
						num++;
                    }
                }
				if(num == 2)
                {
					foreach(var agent in _currentTournamentAgents)
                    {
						if (!agent.IsPlayerControlled) agent.DisableScriptedMovement();
                    }
					_currentState = JoustFightState.FootCombat;

					foreach(var item in Mission.MountsWithoutRiders.ToList())
                    {
						item.Key.FadeOut(false, false);
                    }
				}
            }
        }

        public void PrepareForMatch()
		{
			List<Equipment> participantWeaponEquipmentList = GetParticipantWeaponEquipmentList();
			foreach (TournamentTeam tournamentTeam in _match.Teams)
			{
				int num = 0;
				foreach (TournamentParticipant tournamentParticipant in tournamentTeam.Participants)
				{
					tournamentParticipant.MatchEquipment = participantWeaponEquipmentList[num].Clone(false);
					AddRandomClothes(_culture, tournamentParticipant);
					num++;
				}
			}
		}

		public void StartMatch(TournamentMatch match, bool isLastRound)
		{
			_cheerStarted = false;
			_currentState = JoustFightState.MountedCombat;
			_match = match;
			_isLastRound = isLastRound;
			PrepareForMatch();
            Mission.SetMissionMode(MissionMode.Battle, true);

			if (_match.Teams.Count() != 2)
			{
				throw new ArgumentException("The number of teams in a jousting tournament match is other than 2.");
			}

			List<Team> list = new List<Team>();
			int num = 0;

			foreach (TournamentTeam tournamentTeam in _match.Teams)
			{
				BattleSideEnum side = tournamentTeam.IsPlayerTeam ? BattleSideEnum.Defender : BattleSideEnum.Attacker;
                Team team = Mission.Teams.Add(side, tournamentTeam.TeamColor, uint.MaxValue, tournamentTeam.TeamBanner, true, false, true);
				GameEntity spawnPoint = GetSpawnPointForTeam(num, true);
				foreach (TournamentParticipant tournamentParticipant in tournamentTeam.Participants)
				{
					SpawnTournamentParticipant(spawnPoint, tournamentParticipant, team);
				}
				num++;
				list.Add(team);
			}
			for (int i = 0; i < list.Count; i++)
			{
				for (int j = i + 1; j < list.Count; j++)
				{
					list[i].SetIsEnemyOf(list[j], true);
				}
			}
			_aliveParticipants = _match.Participants.ToList();
			_aliveTeams = _match.Teams.ToList();
		}

        private GameEntity GetSpawnPointForTeam(int teamIndex, bool isMounted)
        {
            if(teamIndex == 0)
            {
				if (isMounted) return _team0MountedSpawn;
				else return _team0FootSpawn;
            }
			if(teamIndex == 1)
            {
				if (isMounted) return _team1MountedSpawn;
				else return _team1FootSpawn;
			}
			return null;
        }

		public void RestartMatch()
        {
            if (!IsMatchEnded() && _endTimer == null)
            {
				if(_cameraView != null)
                {
					_cameraView.BeginFadeOutAndIn(0.1f, 0.1f, 0.5f);
					foreach (var agent in _currentTournamentAgents)
					{
						if (agent.Team.TeamIndex == 0)
						{
							agent.TeleportToPosition(_team0MountedSpawn.GlobalPosition);
							agent.LookDirection = _team0MountedSpawn.GetFrame().rotation.f;
						}
						else if (agent.Team.TeamIndex == 1)
						{
							agent.TeleportToPosition(_team1MountedSpawn.GlobalPosition);
							agent.LookDirection = _team1MountedSpawn.GetFrame().rotation.f;
						}
					}
				}
			}
		}

		protected override void OnEndMission()
		{
            Mission.CanAgentRout_AdditionalCondition -= CanAgentRout;
		}

		private void SpawnTournamentParticipant(GameEntity spawnPoint, TournamentParticipant participant, Team team)
		{
			MatrixFrame globalFrame = spawnPoint.GetGlobalFrame();
			globalFrame.rotation.OrthonormalizeAccordingToForwardAndKeepUpAsZAxis();
			SpawnParticipantAgent(participant, team, globalFrame);
		}

		private List<Equipment> GetParticipantWeaponEquipmentList()
		{
			List<Equipment> list = new List<Equipment>();
			CultureObject culture = PlayerEncounter.EncounterSettlement.Culture;
			CharacterObject characterObject = culture.TournamentTeamTemplatesForOneParticipant.FirstOrDefault();
			foreach (Equipment sourceEquipment in characterObject.BattleEquipments)
			{
				Equipment equipment = new Equipment();
				equipment.FillFrom(sourceEquipment, true);
				list.Add(equipment);
			}
			return list;
		}

		public void SkipMatch(TournamentMatch match)
		{
			_match = match;
			PrepareForMatch();
			Simulate();
		}

		public bool IsMatchEnded()
		{
			if (_isSimulated || _match == null)
			{
				return true;
			}
			if ((_endTimer != null && _endTimer.ElapsedTime > 6f) || _forceEndMatch)
			{
				_forceEndMatch = false;
				_endTimer = null;
				return true;
			}
			if (_cheerTimer != null && !_cheerStarted && _cheerTimer.ElapsedTime > 1f)
			{
				OnMatchResultsReady();
				_cheerTimer = null;
				_cheerStarted = true;
                AgentVictoryLogic missionBehavior = Mission.GetMissionBehavior<AgentVictoryLogic>();
				foreach (Agent agent in _currentTournamentAgents)
				{
					if (agent.IsAIControlled)
					{
						missionBehavior.SetTimersOfVictoryReactionsOnTournamentVictoryForAgent(agent, 1f, 3f);
					}
				}
				return false;
			}
            if (_endTimer == null && !CheckIfIsThereAnyEnemies())
			{
				_endTimer = new BasicMissionTimer();
                if (!_cheerStarted)
				{
					_cheerTimer = new BasicMissionTimer();
				}
			}
			return false;
		}

		public void OnMatchResultsReady()
		{
			if (!_match.IsPlayerParticipating())
			{
				MBInformationManager.AddQuickInformation(new TextObject("{=UBd0dEPp}Match is over", null), 0, null, "");
				return;
			}
			if (_match.IsPlayerWinner())
			{
				if (_isLastRound)
				{
					MBInformationManager.AddQuickInformation(new TextObject("{=Jn0k20c3}Round is over, you survived the final round of the tournament.", null), 0, null, "");
					return;
				}
				else
				{
					MBInformationManager.AddQuickInformation(new TextObject("{=uytwdSVH}Round is over, you are qualified for the next stage of the tournament.", null), 0, null, "");
					return;
				}
			}
			else
			{
				MBInformationManager.AddQuickInformation(new TextObject("{=lcVauEKV}Round is over, you are disqualified from the tournament.", null), 0, null, "");
				return;
			}
		}

		public void OnMatchEnded()
		{
			SandBoxHelpers.MissionHelper.FadeOutAgents(from x in _currentTournamentAgents
													   where x.IsActive()
													   select x, true, false);
			SandBoxHelpers.MissionHelper.FadeOutAgents(from x in _currentTournamentMountAgents
													   where x.IsActive()
													   select x, true, false);
            Mission.ClearCorpses(false);
            Mission.Teams.Clear();
            Mission.RemoveSpawnedItemsAndMissiles();
			_match = null;
			_endTimer = null;
			_cheerTimer = null;
			_dismountNotificationTimer = null;
			_isSimulated = false;
			_currentTournamentAgents.Clear();
			_currentTournamentMountAgents.Clear();
		}

		private void SpawnParticipantAgent(TournamentParticipant participant, Team team, MatrixFrame frame)
		{
			CharacterObject character = participant.Character;
			AgentBuildData agentBuildData = new AgentBuildData(new SimpleAgentOrigin(character, -1, null, participant.Descriptor)).Team(team).InitialPosition(frame.origin);
			Vec2 vec = frame.rotation.f.AsVec2;
			vec = vec.Normalized();
			AgentBuildData agentBuildData2 = agentBuildData.InitialDirection(vec).Equipment(participant.MatchEquipment).ClothingColor1(team.Color).Banner(team.Banner).Controller(character.IsPlayerCharacter ? Agent.ControllerType.Player : Agent.ControllerType.AI);
			Agent agent = Mission.SpawnAgent(agentBuildData2, false);
			if (character.IsPlayerCharacter)
			{
				agent.Health = character.HeroObject.HitPoints;
                Mission.PlayerTeam = team;
			}
			else
			{
				agent.SetWatchState(Agent.WatchState.Alarmed);
				agent.Team.MasterOrderController?.SetOrder(OrderType.Charge);
			}
			agent.WieldInitialWeapons(Agent.WeaponWieldActionType.InstantAfterPickUp, Equipment.InitialWeaponEquipPreference.Any);
			_currentTournamentAgents.Add(agent);
			if (agent.HasMount)
			{
				_currentTournamentMountAgents.Add(agent.MountAgent);
			}
		}

		private void AddRandomClothes(CultureObject culture, TournamentParticipant participant)
		{
			Equipment participantArmor = Campaign.Current.Models.TournamentModel.GetParticipantArmor(participant.Character);
			for (int i = 5; i < 10; i++)
			{
				EquipmentElement equipmentFromSlot = participantArmor.GetEquipmentFromSlot((EquipmentIndex)i);
				if (equipmentFromSlot.Item != null)
				{
					participant.MatchEquipment.AddEquipmentToSlotWithoutAgent((EquipmentIndex)i, equipmentFromSlot);
				}
			}
		}

		private bool CheckIfTeamIsDead(TournamentTeam affectedParticipantTeam)
		{
			bool result = true;
			using (List<TournamentParticipant>.Enumerator enumerator = _aliveParticipants.GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					if (enumerator.Current.Team == affectedParticipantTeam)
					{
						result = false;
						break;
					}
				}
			}
			return result;
		}

		private void AddScoreToRemainingTeams()
		{
			foreach (TournamentTeam tournamentTeam in _aliveTeams)
			{
				foreach (TournamentParticipant tournamentParticipant in tournamentTeam.Participants)
				{
					tournamentParticipant.AddScore(1);
				}
			}
		}

		public override void OnAgentRemoved(Agent affectedAgent, Agent affectorAgent, AgentState agentState, KillingBlow killingBlow)
		{
			if (!IsMatchEnded() && affectorAgent != null && affectedAgent != affectorAgent && affectedAgent.IsHuman && affectorAgent.IsHuman)
			{
				TournamentParticipant participant = _match.GetParticipant(affectedAgent.Origin.UniqueSeed);
				_aliveParticipants.Remove(participant);
				_currentTournamentAgents.Remove(affectedAgent);
				if (CheckIfTeamIsDead(participant.Team))
				{
					_aliveTeams.Remove(participant.Team);
					AddScoreToRemainingTeams();
				}
			}
			else if(!IsMatchEnded() && affectorAgent != null && affectedAgent != affectorAgent && affectedAgent.IsMount && affectorAgent.IsHuman && _currentState == JoustFightState.MountedCombat)
            {
				_currentState = JoustFightState.Transition;
				foreach(var agent in _currentTournamentAgents)
                {
					agent.DropItem(EquipmentIndex.Weapon0);
                    if (!agent.IsPlayerControlled)
                    {
						var spawnpoint = GetSpawnPointForTeam(agent.Team.TeamIndex, false);
						WorldPosition pos = new WorldPosition(Mission.Scene, spawnpoint.GlobalPosition);
						agent.SetScriptedPosition(ref pos, true, Agent.AIScriptedFrameFlags.GoWithoutMount | Agent.AIScriptedFrameFlags.NoAttack);
                    }
                }
            }
		}

		public override void OnScoreHit(Agent affectedAgent, Agent affectorAgent, WeaponComponentData attackerWeapon, bool isBlocked, bool isSiegeEngineHit, in Blow blow, in AttackCollisionData collisionData, float damagedHp, float hitDistance, float shotDifficulty)
		{
			if (affectorAgent == null)
			{
				return;
			}
			if (affectorAgent.IsMount && affectorAgent.RiderAgent != null)
			{
				affectorAgent = affectorAgent.RiderAgent;
			}
			if (affectorAgent.Character == null || affectedAgent.Character == null)
			{
				return;
			}
			float num = blow.InflictedDamage;
			if (num > affectedAgent.HealthLimit)
			{
				num = affectedAgent.HealthLimit;
			}
			float num2 = num / affectedAgent.HealthLimit;
			EnemyHitReward(affectedAgent, affectorAgent, blow.MovementSpeedDamageModifier, shotDifficulty, attackerWeapon, blow.AttackType, 0.5f * num2, num);
		}

		private void EnemyHitReward(Agent affectedAgent, Agent affectorAgent, float lastSpeedBonus, float lastShotDifficulty, WeaponComponentData lastAttackerWeapon, AgentAttackType attackType, float hitpointRatio, float damageAmount)
		{
			CharacterObject affectedCharacter = (CharacterObject)affectedAgent.Character;
			CharacterObject affectorCharacter = (CharacterObject)affectorAgent.Character;
			if (affectedAgent.Origin != null && affectorAgent != null && affectorAgent.Origin != null)
			{
				bool isHorseCharge = affectorAgent.MountAgent != null && attackType == AgentAttackType.Collision;
				SkillLevelingManager.OnCombatHit(affectorCharacter, affectedCharacter, null, null, lastSpeedBonus, lastShotDifficulty, lastAttackerWeapon, hitpointRatio, CombatXpModel.MissionTypeEnum.Tournament, affectorAgent.MountAgent != null, affectorAgent.Team == affectedAgent.Team, false, damageAmount, affectedAgent.Health < 1f, false, isHorseCharge);
			}
		}

		public bool CheckIfIsThereAnyEnemies()
		{
			Team team = null;
			foreach (Agent agent in _currentTournamentAgents)
			{
				if (agent.IsHuman && agent.IsActive() && agent.Team != null)
				{
					if (team == null)
					{
						team = agent.Team;
					}
					else if (team != agent.Team)
					{
						return true;
					}
				}
			}
			return false;
		}

		private void Simulate()
		{
			_isSimulated = false;
			if (_currentTournamentAgents.Count == 0)
			{
				_aliveParticipants = _match.Participants.ToList();
				_aliveTeams = _match.Teams.ToList();
			}
            TournamentParticipant tournamentParticipant = _aliveParticipants.FirstOrDefault((TournamentParticipant x) => x.Character == CharacterObject.PlayerCharacter);
			if (tournamentParticipant != null)
			{
				TournamentTeam team = tournamentParticipant.Team;
				foreach (TournamentParticipant tournamentParticipant2 in team.Participants)
				{
					tournamentParticipant2.ResetScore();
					_aliveParticipants.Remove(tournamentParticipant2);
				}
				_aliveTeams.Remove(team);
				AddScoreToRemainingTeams();
			}
			Dictionary<TournamentParticipant, Tuple<float, float>> dictionary = new Dictionary<TournamentParticipant, Tuple<float, float>>();
            foreach (TournamentParticipant tournamentParticipant3 in _aliveParticipants)
			{
				float item;
				float item2;
				tournamentParticipant3.Character.GetSimulationAttackPower(out item, out item2, tournamentParticipant3.MatchEquipment);
				dictionary.Add(tournamentParticipant3, new Tuple<float, float>(item, item2));
			}
			int num = 0;
			while (_aliveParticipants.Count > 1 && _aliveTeams.Count > 1)
			{
				num++;
				num %= _aliveParticipants.Count;
				TournamentParticipant tournamentParticipant4 = _aliveParticipants[num];
				int num2;
				TournamentParticipant tournamentParticipant5;
				do
				{
					num2 = MBRandom.RandomInt(_aliveParticipants.Count);
					tournamentParticipant5 = _aliveParticipants[num2];
				}
				while (tournamentParticipant4 == tournamentParticipant5 || tournamentParticipant4.Team == tournamentParticipant5.Team);
				if (dictionary[tournamentParticipant5].Item2 - dictionary[tournamentParticipant4].Item1 > 0f)
				{
					dictionary[tournamentParticipant5] = new Tuple<float, float>(dictionary[tournamentParticipant5].Item1, dictionary[tournamentParticipant5].Item2 - dictionary[tournamentParticipant4].Item1);
				}
				else
				{
					dictionary.Remove(tournamentParticipant5);
					_aliveParticipants.Remove(tournamentParticipant5);
					if (CheckIfTeamIsDead(tournamentParticipant5.Team))
					{
						_aliveTeams.Remove(tournamentParticipant5.Team);
						AddScoreToRemainingTeams();
					}
					if (num2 < num)
					{
						num--;
					}
				}
			}
			_isSimulated = true;
		}

		private bool IsThereAnyPlayerAgent()
		{
			if (Mission.MainAgent != null && Mission.MainAgent.IsActive())
			{
				return true;
			}
			return _currentTournamentAgents.Any((Agent agent) => agent.IsPlayerControlled);
		}

		private Agent GetPlayerAgent()
        {
			if (Mission.MainAgent != null && Mission.MainAgent.IsActive())
			{
				return Mission.MainAgent;
			}
			return _currentTournamentAgents.FirstOrDefault((Agent agent) => agent.IsPlayerControlled);
		}

		private void SkipMatch()
		{
			Mission.Current.GetMissionBehavior<JoustTournamentBehavior>().SkipMatch(false);
		}

		public override InquiryData OnEndMissionRequest(out bool canPlayerLeave)
		{
			InquiryData result = null;
			canPlayerLeave = true;
			if (_match != null)
			{
				if (_match.IsPlayerParticipating())
				{
					MBTextManager.SetTextVariable("SETTLEMENT_NAME", Hero.MainHero.CurrentSettlement.EncyclopediaLinkWithName, false);
					if (IsThereAnyPlayerAgent())
					{
						if (Mission.IsPlayerCloseToAnEnemy(5f))
						{
							canPlayerLeave = false;
							MBInformationManager.AddQuickInformation(GameTexts.FindText("str_can_not_retreat", null), 0, null, "");
						}
						else if (CheckIfIsThereAnyEnemies())
						{
							result = new InquiryData(GameTexts.FindText("str_tournament", null).ToString(), GameTexts.FindText("str_tournament_forfeit_game", null).ToString(), true, true, GameTexts.FindText("str_yes", null).ToString(), GameTexts.FindText("str_no", null).ToString(), new Action(this.SkipMatch), null, "", 0f, null, null, null);
						}
						else
						{
							_forceEndMatch = true;
							canPlayerLeave = false;
						}
					}
					else if (CheckIfIsThereAnyEnemies())
					{
						result = new InquiryData(GameTexts.FindText("str_tournament", null).ToString(), GameTexts.FindText("str_tournament_skip", null).ToString(), true, true, GameTexts.FindText("str_yes", null).ToString(), GameTexts.FindText("str_no", null).ToString(), new Action(this.SkipMatch), null, "", 0f, null, null, null);
					}
					else
					{
						_forceEndMatch = true;
						canPlayerLeave = false;
					}
				}
				else if (CheckIfIsThereAnyEnemies())
				{
					result = new InquiryData(GameTexts.FindText("str_tournament", null).ToString(), GameTexts.FindText("str_tournament_skip", null).ToString(), true, true, GameTexts.FindText("str_yes", null).ToString(), GameTexts.FindText("str_no", null).ToString(), new Action(this.SkipMatch), null, "", 0f, null, null, null);
				}
				else
				{
					_forceEndMatch = true;
					canPlayerLeave = false;
				}
			}
			return result;
		}

		public enum JoustFightState
		{
			MountedCombat,
			Transition,
			FootCombat
		}
	}
}
