using System;
using SandBox.Missions.MissionLogics;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View.MissionViews;
using TOR_Core.CharacterDevelopment;
using TOR_Core.Extensions;
using TOR_Core.GameManagers;
using TOR_Core.HarmonyPatches;
using TOR_Core.Utilities;

namespace TOR_Core.AbilitySystem.Scripts
{
    public class SummonChampionScript : CareerAbilityScript
    {
        private MissionCameraFadeView _cameraView;
        private Agent _champion;
        private bool _championIsActive;
        private bool _isDisabled;
        private bool _isHideOutMission;
        private bool _summoned;
        private GameKey _specialMoveKey;
        private string _summonedChampionId;
        private Vec3 _targetPosition;

        protected override void OnInit()
        {
            var effects = GetEffectsToTrigger();

            foreach (var effect in effects)
                if (effect.SummonedTroopId != "none")
                {
                    _summonedChampionId = effect.SummonedTroopId;
                    if (_summonedChampionId.Contains("_plate") && _summonedChampionId.Contains("two_handed")) _summonedChampionId = "tor_vc_harbinger_champion_plate_two_handed";
                    break;
                }

            var hideoutMissionController = Mission.Current.GetMissionBehavior<HideoutMissionController>();
            if (hideoutMissionController != null)
            {
                var abilityManagerLogic = Mission.Current.GetMissionBehavior<AbilityManagerMissionLogic>();
                if (abilityManagerLogic != null)
                {
                    abilityManagerLogic.OnInitHideOutBossFight += OnHideOutMissionStateChanged;
                }
                _isHideOutMission = true;
            }

            Mission.Current.OnBeforeAgentRemoved += AgentRemoved;

            _cameraView = Mission.Current.GetMissionBehavior<MissionCameraFadeView>();

            _specialMoveKey = HotKeyManager.GetCategory(nameof(TORGameKeyContext)).GetGameKey("CareerAbilityCast");

            _targetPosition = GameEntity.GlobalPosition;
        }

        private void OnHideOutMissionStateChanged()
        {
            if (_champion != null && _champion.IsActive())
            {
                KillChampion();
                CasterAgent.GetComponent<AbilityComponent>().CareerAbility.AddCharge(TORCareers.Necromancer.MaxCharge);
                Stop();
            }
        }

        private void AgentRemoved(Agent affectedagent, Agent affectoragent, AgentState agentstate, KillingBlow killingblow)
        {
            if (affectedagent == _champion) Stop();
        }

        protected override void OnBeforeTick(float dt)
        {
            if (!_summoned) InitialShiftToChampion();

            if (!CasterAgent.IsActive() && !_isDisabled)
            {
                KillChampion();
                Stop();
            }
            if (_isDisabled)
            {
                Stop();
            }

            if ((Input.IsKeyPressed(_specialMoveKey.KeyboardKey.InputKey) ||
                  Input.IsKeyPressed(_specialMoveKey.ControllerKey.InputKey))
                && Hero.MainHero.HasCareerChoice("DeArcanisKadonKeystone"))
                SwitchBetweenAgents();

            if (_champion != null && _champion.Health <= 1)
            {
                KillChampion();
            }
        }

        protected override void OnBeforeRemoved(int removeReason)
        {
            Mission.Current.OnBeforeAgentRemoved -= AgentRemoved;
            if (_championIsActive) ShiftControllerToCaster();
            CasterAgent.RemoveStatusEffect("greater_harbinger_ward_protection");

            var abilityManagerLogic = Mission.Current.GetMissionBehavior<AbilityManagerMissionLogic>();
            if (abilityManagerLogic != null)
            {
                abilityManagerLogic.OnInitHideOutBossFight -= OnHideOutMissionStateChanged;
            }
        }

        private void InitialShiftToChampion()
        {
            var data = TORSummonHelper.GetAgentBuildData(CasterAgent, _summonedChampionId);
            _champion = TORSummonHelper.SpawnAgent(data, _targetPosition);

            _champion.ApplyStatusEffect("greater_harbinger_debuff", null, 9999f);

            if (_isHideOutMission)
            {
                _champion.Formation = null;
                CasterAgent.Team.PlayerOrderController.SelectAllFormations();
                CasterAgent.Team.PlayerOrderController.SetOrder(OrderType.Charge);
            }

            ShiftControllerToChampion();

            _summoned = true;
        }

        private void ShiftControllerToChampion()
        {
            if (_champion.Health > 0)
            {
                CasterAgent.Controller = Agent.ControllerType.None;
                _champion.Controller = Agent.ControllerType.Player;
                CasterAgent.SetTargetPosition(CasterAgent.Position.AsVec2);

                if (Hero.MainHero.HasCareerChoice("CodexMortificaKeystone")) CasterAgent.ApplyStatusEffect("greater_harbinger_ward_protection", null, 9999f);
                _championIsActive = true;
            }

            _champion.WieldInitialWeapons();

            _cameraView.BeginFadeOutAndIn(0.1f, 0.1f, 0.5f);
        }

        private void ShiftControllerToCaster()
        {
            CasterAgent.ClearTargetFrame();
            if (_isHideOutMission)
            {
                CasterAgent.Team.PlayerOrderController.SelectAllFormations();
                CasterAgent.Team.PlayerOrderController.SetOrder(OrderType.Charge);
            }

            if (CasterAgent.Health > 0)
            {
                CasterAgent.Controller = Agent.ControllerType.Player;
                _champion.Controller = Agent.ControllerType.AI;
                CasterAgent.RemoveStatusEffect("greater_harbinger_ward_protection");
            }

            _cameraView.BeginFadeOutAndIn(0.1f, 0.1f, 0.5f);
            CasterAgent.WieldInitialWeapons();
            //TODO there is an initial weird hit that is done by caster and champion. Have to investigate this. 
            _championIsActive = false;
        }

        private void KillChampion()
        {
            if (_champion != null)
            {
                var blow = new Blow();
                blow.OwnerId = CasterAgent.Index;
                _champion.Die(blow);
                _isDisabled = true;
            }
        }

        private void SwitchBetweenAgents()
        {
            if (!_championIsActive && _summoned)
                ShiftControllerToChampion();
            else
                ShiftControllerToCaster();
        }
    }

}