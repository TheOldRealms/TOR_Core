using SandBox;
using SandBox.Tournaments;
using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.AgentOrigins;
using TaleWorlds.CampaignSystem.TournamentGames;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TOR_Core.BattleMechanics.CustomArenaModes;

namespace TOR_Core.Missions
{
    public class ArcheryContestMissionController(CultureObject culture) : MissionLogic, ITournamentGameBehavior
    {
        private readonly CultureObject _culture = culture;
        private const float EXPECTED_TIME_TO_AIM = 1.9f;
        private TournamentMatch _match;
        private BasicMissionTimer _endTimer;
        private bool _isSimulated;
        private bool _isLastRound;
        private List<DestructableComponent> _leftSideTargets;
        private List<DestructableComponent> _rightSideTargets;
        private GameEntity _leftParticipantSpawn;
        private GameEntity _rightParticipantSpawn;
        private Agent _leftParticipantAgent;
        private Agent _rightParticipantAgent;
        private BasicMissionTimer _leftTimer;
        private BasicMissionTimer _rightTimer;

        public override void AfterStart()
        {
            _leftTimer = new();
            _rightTimer = new();
            _leftParticipantSpawn = Mission.Scene.FindEntityWithTag("sp_tp_left");
            _rightParticipantSpawn = Mission.Scene.FindEntityWithTag("sp_tp_right");
            _leftSideTargets = (from x in Mission.ActiveMissionObjects.FindAllWithType<DestructableComponent>()
                                where x.GameEntity.HasTag("archery_target_left")
                                select x).ToList();
            _rightSideTargets = (from x in Mission.ActiveMissionObjects.FindAllWithType<DestructableComponent>()
                                where x.GameEntity.HasTag("archery_target_right")
                                select x).ToList();
            Mission.SetMissionMode(MissionMode.Battle, true);
            foreach (DestructableComponent destructableComponent in _leftSideTargets)
            {
                destructableComponent.OnDestroyed += new DestructableComponent.OnHitTakenAndDestroyedDelegate(OnLeftTargetDestroyed);
            }
            foreach (DestructableComponent destructableComponent in _rightSideTargets)
            {
                destructableComponent.OnDestroyed += new DestructableComponent.OnHitTakenAndDestroyedDelegate(OnRightTargetDestroyed);
            }
        }

        private void OnRightTargetDestroyed(DestructableComponent target, Agent attackerAgent, in MissionWeapon weapon, ScriptComponentBehavior attackerScriptComponentBehavior, int inflictedDamage)
        {
            var controller = _rightParticipantAgent.GetController<ArcheryContestAgentController>();
            controller.OnTargetHit(_rightParticipantAgent, target);
            _match.GetParticipant(_rightParticipantAgent.Origin.UniqueSeed).AddScore(CalculateScore(target, _rightParticipantAgent, _rightTimer.ElapsedTime));
            _rightTimer.Reset();
            if (attackerAgent.IsPlayerControlled)
            {
                Hero.MainHero.AddSkillXp(DefaultSkills.Bow, 50f);
            }
        }

        private void OnLeftTargetDestroyed(DestructableComponent target, Agent attackerAgent, in MissionWeapon weapon, ScriptComponentBehavior attackerScriptComponentBehavior, int inflictedDamage)
        {
            var controller = _leftParticipantAgent.GetController<ArcheryContestAgentController>();
            controller.OnTargetHit(_leftParticipantAgent, target);
            _match.GetParticipant(_leftParticipantAgent.Origin.UniqueSeed).AddScore(CalculateScore(target, _leftParticipantAgent, _leftTimer.ElapsedTime));
            _leftTimer.Reset();
            if (attackerAgent.IsPlayerControlled)
            {
                Hero.MainHero.AddSkillXp(DefaultSkills.Bow, 50f);
            }
        }

        private int CalculateScore(DestructableComponent target, Agent agent, float elapsedTime)
        {
            var timeSinceLastHit = elapsedTime < 0.2f ? EXPECTED_TIME_TO_AIM : elapsedTime;
            var distance = target.GameEntity.GlobalPosition.Distance(agent.GetEyeGlobalPosition());
            var timeBonus = EXPECTED_TIME_TO_AIM / timeSinceLastHit;
            return (int)(distance * timeBonus);
        }

        public void StartMatch(TournamentMatch match, bool isLastRound)
        {
            _match = match;
            _isLastRound = isLastRound;
            _leftParticipantAgent = null;
            _rightParticipantAgent = null;
            _leftTimer.Reset();
            _rightTimer.Reset();
            ResetTargets();
            PrepareForMatch();

            if (_match.Teams.Count() != 2)
            {
                throw new ArgumentException("The number of teams in an archery tournament match is other than 2.");
            }

            var teamIndex = 0;
            foreach(var team in _match.Teams)
            {
                var missionTeam = Mission.Teams.Add(BattleSideEnum.None, team.TeamColor, uint.MaxValue, team.TeamBanner, true, false, true);
                GameEntity spawnPoint = teamIndex == 0 ? _leftParticipantSpawn : _rightParticipantSpawn;
                var side = teamIndex == 0 ? ArcherySide.Left : ArcherySide.Right;
                foreach (var participant in team.Participants)
                {
                    SpawnTournamentParticipant(spawnPoint, participant, missionTeam, side);
                }
                teamIndex++;
            }
        }

        private void SpawnTournamentParticipant(GameEntity spawnPoint, TournamentParticipant participant, Team team, ArcherySide side)
        {
            MatrixFrame globalFrame = spawnPoint.GetGlobalFrame();
            globalFrame.rotation.OrthonormalizeAccordingToForwardAndKeepUpAsZAxis();
            SpawnParticipantAgent(participant, team, globalFrame, side);
        }

        private void SpawnParticipantAgent(TournamentParticipant participant, Team team, MatrixFrame frame, ArcherySide side)
        {
            CharacterObject character = participant.Character;
            AgentBuildData agentBuildData = new AgentBuildData(new SimpleAgentOrigin(character, -1, null, participant.Descriptor)).Team(team).InitialPosition(frame.origin);
            Vec2 vec = frame.rotation.f.AsVec2;
            vec = vec.Normalized();
            AgentBuildData agentBuildData2 = agentBuildData.InitialDirection(vec).Equipment(participant.MatchEquipment).ClothingColor1(team.Color).Banner(team.Banner).Controller(character.IsPlayerCharacter ? Agent.ControllerType.Player : Agent.ControllerType.AI);
            Agent agent = Mission.SpawnAgent(agentBuildData2, false);
            ArcheryContestAgentController archeryContestAgentController = agent.AddController(typeof(ArcheryContestAgentController)) as ArcheryContestAgentController;
            archeryContestAgentController.SetTargets(team.TeamIndex == 0 ? _leftSideTargets : _rightSideTargets);
            if (character.IsPlayerCharacter)
            {
                agent.Health = character.HeroObject.HitPoints;
                Mission.PlayerTeam = team;
            }
            agent.WieldInitialWeapons(Agent.WeaponWieldActionType.InstantAfterPickUp, Equipment.InitialWeaponEquipPreference.RangedForMainHand);
            agent.SetWatchState(Agent.WatchState.Alarmed);
            agent.ToggleInvulnerable();
            if(side == ArcherySide.Left)
            {
                _leftParticipantAgent = agent;
            }
            else
            {
                _rightParticipantAgent = agent;
            }
        }

        public void SkipMatch(TournamentMatch match)
        {
            _match = match;
            PrepareForMatch();
            Simulate();
        }

        private void PrepareForMatch()
        {
            List<Equipment> participantWeaponEquipmentList = GetParticipantWeaponEquipmentList();
            foreach (var team in _match.Teams)
            {
                int num = 0;
                foreach (var participant in team.Participants)
                {
                    participant.MatchEquipment = participantWeaponEquipmentList[num].Clone(false);
                    AddParticipantArmor(participant);
                    num++;
                }
            }
        }

        private void AddParticipantArmor(TournamentParticipant participant)
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

        private List<Equipment> GetParticipantWeaponEquipmentList()
        {
            List<Equipment> list = [];
            CharacterObject characterObject = _culture.TournamentTeamTemplatesForOneParticipant.FirstOrDefault();
            foreach (Equipment sourceEquipment in characterObject.BattleEquipments)
            {
                Equipment equipment = new();
                equipment.FillFrom(sourceEquipment, true);
                list.Add(equipment);
            }
            return list;
        }

        private void Simulate()
        {
            _isSimulated = false;
            List<TournamentParticipant> list = _match.Participants.ToList();
            int i = _leftSideTargets.Count;
            while (i > 0)
            {
                foreach (TournamentParticipant tournamentParticipant in list)
                {
                    if (i == 0)
                    {
                        break;
                    }
                    if (MBRandom.RandomFloat < GetDeadliness(tournamentParticipant))
                    {
                        tournamentParticipant.AddScore(MBRandom.RandomInt(10,30));
                        i--;
                    }
                }
            }
            _isSimulated = true;
        }

        private float GetDeadliness(TournamentParticipant tournamentParticipant)
        {
            return 0.01f + (tournamentParticipant.Character.GetSkillValue(DefaultSkills.Bow) / 300f * 0.19f);
        }

        public bool IsMatchEnded()
        {
            if (_isSimulated || _match == null)
            {
                return true;
            }
            if (_endTimer != null && _endTimer.ElapsedTime > 3f)
            {
                _endTimer = null;
                return true;
            }
            if (_endTimer == null && IsLeftSideFinished() && IsRightSideFinished())
            {
                _endTimer = new BasicMissionTimer();
            }
            return false;
        }

        private bool IsLeftSideFinished()
        {
            return _leftSideTargets.All(x => x.IsDestroyed) || _leftParticipantAgent.Equipment.GetAmmoAmount(EquipmentIndex.WeaponItemBeginSlot) <= 0;
        }

        private bool IsRightSideFinished()
        {
            return _rightSideTargets.All(x => x.IsDestroyed) || _rightParticipantAgent.Equipment.GetAmmoAmount(EquipmentIndex.WeaponItemBeginSlot) <= 0;
        }

        private bool IsThereAnyTargetLeft()
        {
            return _leftSideTargets.Any(x => !x.IsDestroyed) || _rightSideTargets.Any(x => !x.IsDestroyed);
        }

        private bool IsThereAnyArrowLeft()
        {
            return Mission.Agents.Any((Agent agent) => agent.Equipment.GetAmmoAmount(EquipmentIndex.WeaponItemBeginSlot) > 0);
        }

        public void OnMatchEnded()
        {
            if(_leftParticipantAgent != null)
            {
                SandBoxHelpers.MissionHelper.FadeOutAgents([_leftParticipantAgent], false, false);
            }
            if (_rightParticipantAgent != null)
            {
                SandBoxHelpers.MissionHelper.FadeOutAgents([_rightParticipantAgent], false, false);
            }
            Mission.ClearCorpses(false);
            Mission.Teams.Clear();
            Mission.RemoveSpawnedItemsAndMissiles();
            _match = null;
            _endTimer = null;
            _isSimulated = false;
        }

        public override void OnMissionTick(float dt)
        {
            if (!IsMatchEnded())
            {
                foreach (var agent in Mission.Agents)
                {
                    var controller = agent.GetController<ArcheryContestAgentController>();
                    controller?.OnTick();
                }
            }
        }

        private void ResetTargets()
        {
            foreach (DestructableComponent destructableComponent in _leftSideTargets)
            {
                destructableComponent.Reset();
            }
            foreach (DestructableComponent destructableComponent in _rightSideTargets)
            {
                destructableComponent.Reset();
            }
        }

        private enum ArcherySide
        {
            Left,
            Right
        }
    }
}
