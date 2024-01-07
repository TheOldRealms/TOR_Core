using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.View.MissionViews;
using TaleWorlds.ScreenSystem;
using TOR_Core.AbilitySystem;
using TOR_Core.AbilitySystem.Crosshairs;
using TOR_Core.AbilitySystem.CrossHairs;
using TOR_Core.BattleMechanics.Crosshairs;
using TOR_Core.Extensions;

namespace TOR_Core.Battle.CrosshairMissionBehavior
{
    [OverrideView(typeof(MissionCrosshair))]
    public class CustomCrosshairMissionBehavior : MissionView
    {
        private bool _areCrosshairsInitialized;
        private ICrosshair _currentCrosshair;
        private Crosshair _weaponCrosshair;
        private SniperScope _sniperScope;
        private AbilityCrosshair _abilityCrosshair;
        private AbilityComponent _abilityComponent;
        private AbilityManagerMissionLogic _missionLogic;
        private CareerAbility _careerAbility;

        public ICrosshair CurrentCrosshair { get => _currentCrosshair; }

        public override void OnMissionScreenTick(float dt)
        {
            if (!_areCrosshairsInitialized)
            {
                if (Agent.Main != null && MissionScreen != null)
                    InitializeCrosshairs();
                else
                    return;
            }
            if (CanUseCrosshair())
            {
                if (CanUseAbilityCrosshair())
                {
                    if (_currentCrosshair == _weaponCrosshair)
                        _weaponCrosshair.DisableTargetGadgetOpacities();

                    if (_currentCrosshair != _abilityCrosshair)
                        ChangeCrosshair(_abilityCrosshair);
                }
                else if (!Agent.Main.WieldedWeapon.IsEmpty)
                {

                    if (CanUseSniperScope() && Agent.Main.WieldedWeapon.CurrentUsageItem.IsRangedWeapon)
                    {
                        if (_currentCrosshair != _sniperScope)
                            ChangeCrosshair(_sniperScope);
                    }
                    else
                    {
                        if (_currentCrosshair != _weaponCrosshair)
                            ChangeCrosshair(_weaponCrosshair);
                    }
                }
                else
                {
                    ChangeCrosshair(null);
                }
                if (_currentCrosshair != null) _currentCrosshair.Tick();

            }
            else if (_currentCrosshair != null)
                ChangeCrosshair(null);
        }

        private void ChangeCrosshair(ICrosshair crosshair)
        {
            _currentCrosshair?.Hide();
            _currentCrosshair = crosshair;
            if (_currentCrosshair != null) _currentCrosshair.Show();
        }

        private bool CanUseCrosshair()
        {
            var careerAbilityPreventCrosshairCondition = _careerAbility != null && _careerAbility.RequiresDisabledCrosshairDuringAbility && _careerAbility.IsActive;        //vampire bat swarm shouldnt have crosshair

            return Agent.Main != null &&
                   Agent.Main.State == AgentState.Active &&
                    !careerAbilityPreventCrosshairCondition &&
                     Mission.Mode != MissionMode.Conversation &&
                     Mission.Mode != MissionMode.Deployment &&
                     Mission.Mode != MissionMode.CutScene &&
                     MissionScreen != null &&
                     MissionScreen.CustomCamera == null &&
                     (MissionScreen.OrderFlag == null || !MissionScreen.OrderFlag.IsVisible) &&
                     !MissionScreen.IsViewingCharacter() &&
                     !MissionScreen.IsPhotoModeEnabled &&
                     !MBEditor.EditModeEnabled &&
                     BannerlordConfig.DisplayTargetingReticule &&
                     !ScreenManager.GetMouseVisibility();
        }

        private bool CanUseAbilityCrosshair()
        {
            return !Mission.IsFriendlyMission &&
                   _missionLogic != null &&
                   _missionLogic.CurrentState == AbilityModeState.Targeting &&
                   !_abilityComponent.CurrentAbility.IsDisabled(Agent.Main, out _);
        }

        private void InitializeCrosshairs()
        {
            _weaponCrosshair = new Crosshair();
            _weaponCrosshair.InitializeCrosshair();
            _sniperScope = new SniperScope();

            if (Agent.Main.IsAbilityUser() && (_abilityComponent = Agent.Main.GetComponent<AbilityComponent>()) != null)
            {
                _missionLogic = Mission.Current.GetMissionBehavior<AbilityManagerMissionLogic>();
                _abilityComponent.CurrentAbilityChanged += ChangeAbilityCrosshair;
                _abilityComponent.InitializeCrosshairs();
                _abilityCrosshair = _abilityComponent.CurrentAbility?.Crosshair;
                if (Game.Current.GameType is Campaign)
                {
                    _careerAbility = _abilityComponent?.CareerAbility;
                }

            }
            _areCrosshairsInitialized = true;

        }

        public override void OnMissionScreenFinalize()
        {
            if (!_areCrosshairsInitialized)
            {
                return;
            }
            _weaponCrosshair.FinalizeCrosshair();
            _abilityComponent = null;
            _abilityCrosshair = null;
            _sniperScope.FinalizeCrosshair();
            _sniperScope = null;
            _areCrosshairsInitialized = false;
            if (_abilityComponent != null)
            {
                _abilityComponent.CurrentAbilityChanged -= ChangeAbilityCrosshair;
            }
        }

        private bool CanUseSniperScope()
        {
            return Mission.CameraIsFirstPerson &&
                   Input.IsKeyDown(InputKey.LeftShift) &&
                   Input.IsKeyDown(InputKey.LeftMouseButton) &&
                   Agent.Main.GetCurrentActionType(1) == Agent.ActionCodeType.ReadyRanged &&
                   Agent.Main.WieldedWeapon.Item.StringId.Contains("longrifle") &&
                   IsRightAngleToShoot();
        }

        private void ChangeAbilityCrosshair(AbilityCrosshair crosshair)
        {
            _abilityCrosshair?.Hide();
            _abilityCrosshair = crosshair;
        }

        private bool IsRightAngleToShoot()
        {
            float numberToCheck = MBMath.WrapAngle(Agent.Main.LookDirection.AsVec2.RotationInRadians - Agent.Main.GetMovementDirection().RotationInRadians);
            Vec2 bodyRotationConstraint = Agent.Main.GetBodyRotationConstraint(1);
            return !(Mission.Current.MainAgent.MountAgent != null && !MBMath.IsBetween(numberToCheck, bodyRotationConstraint.x, bodyRotationConstraint.y) && (bodyRotationConstraint.x < -0.1f || bodyRotationConstraint.y > 0.1f));
        }
    }
}