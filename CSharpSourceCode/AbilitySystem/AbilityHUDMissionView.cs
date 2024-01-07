using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.View.MissionViews;
using TaleWorlds.ScreenSystem;

namespace TOR_Core.AbilitySystem
{
    [DefaultView]
    class AbilityHUDMissionView : MissionView
    {
        private int _countOfAbilities;
        private bool _hasCareerAbility;
        private bool _isInitialized;
        private AbilityHUD_VM _abilityHUD_VM;
        private CareerAbilityHUD_VM _careerAbilityHUD_VM;
        private AbilityRadialSelection_VM _abilityRadialSelection_VM;
        private GauntletLayer _abilityLayer;
        private GauntletLayer _careerAbilityLayer;
        private GauntletLayer _radialMenuLayer;

        public override void OnBehaviorInitialize()
        {
            base.OnBehaviorInitialize();
            Mission.Current.OnMainAgentChanged += (o, s) => CheckMainAgent();

            _abilityHUD_VM = new AbilityHUD_VM();
            _abilityLayer = new GauntletLayer(100);
            _abilityLayer.LoadMovie("AbilityHUD", _abilityHUD_VM);
            MissionScreen.AddLayer(_abilityLayer);

            _careerAbilityHUD_VM = new CareerAbilityHUD_VM();
            _careerAbilityLayer = new GauntletLayer(99);
            _careerAbilityLayer.LoadMovie("SpecialMoveHUD", _careerAbilityHUD_VM);
            MissionScreen.AddLayer(_careerAbilityLayer);

            _abilityRadialSelection_VM = new AbilityRadialSelection_VM();
            _radialMenuLayer = new GauntletLayer(98);
            _radialMenuLayer.LoadMovie("AbilityRadialSelection", _abilityRadialSelection_VM);
            MissionScreen.AddLayer(_radialMenuLayer);

            _isInitialized = true;
        }

        private void CheckMainAgent()
        {
            if (Agent.Main != null)
            {
                var component = Agent.Main.GetComponent<AbilityComponent>();
                if (component != null)
                {
                    _countOfAbilities = component.KnownAbilitySystem.Count;
                    var careerAbility = component.CareerAbility;
                    if (careerAbility != null)
                    {
                        _careerAbilityHUD_VM.CareerAbility = careerAbility;
                        _hasCareerAbility = true;
                    }
                    if (_abilityRadialSelection_VM != null) _abilityRadialSelection_VM.FillAbilities(Agent.Main);
                }
            }
        }

        public void DisplayErrorMessage(string message)
        {
            if(_abilityRadialSelection_VM != null) _abilityRadialSelection_VM.DisplayErrorMessage(message);
        }

        public override void OnMissionTick(float dt)
        {
            if (_isInitialized)
            {
                bool canHudBeVisible = Agent.Main != null &&
                                       Agent.Main.State == AgentState.Active &&
                                       (Mission.Current.Mode == MissionMode.Battle || 
                                       Mission.Current.Mode == MissionMode.Stealth) &&
                                       MissionScreen.CustomCamera == null &&
                                       !MissionScreen.IsViewingCharacter() &&
                                       !MissionScreen.IsPhotoModeEnabled &&
                                       !ScreenManager.GetMouseVisibility();
                if (canHudBeVisible)
                {
                    if (_countOfAbilities > 0)
                    {
                        _abilityHUD_VM.RefreshValues();
                        _abilityRadialSelection_VM.RefreshValues();

                    }
                    if (_hasCareerAbility)
                    {
                        _careerAbilityHUD_VM.RefreshValues();
                    }
                    return;
                }
                _abilityHUD_VM.IsVisible = false;
                _careerAbilityHUD_VM.IsVisible = false;
                _abilityRadialSelection_VM.IsVisible = false;
            }
        }
    }
}
