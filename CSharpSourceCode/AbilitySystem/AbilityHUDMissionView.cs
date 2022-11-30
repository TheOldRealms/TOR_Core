using TaleWorlds.CampaignSystem;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.View.MissionViews;
using TaleWorlds.ScreenSystem;
using TOR_Core.AbilitySystem.Spells;
using TOR_Core.Extensions;

namespace TOR_Core.AbilitySystem
{
    [DefaultView]
    class AbilityHUDMissionView : MissionView
    {
        private bool _hasCareerAbility;
        private bool _hasAbility;
        private bool _hasSpecialMove;
        private bool _isInitialized;
        private AbilityHUD_VM _abilityHUD_VM;
        private CareerAbilityHUD_VM _careerabilityHUD_VM;
        private GauntletLayer _abilityLayer;
        private GauntletLayer _specialMoveLayer;
        private GauntletLayer _careerAbilityLayer;

        public override void OnBehaviorInitialize()
        {
            base.OnBehaviorInitialize();
            Mission.Current.OnMainAgentChanged += (o, s) => CheckMainAgent();

            if (Campaign.Current != null)
            {
                if (Campaign.Current.MainParty.LeaderHero.HasCareerAbility())//Campaign.Current.MainParty.LeaderHero.HasCareerAbility())
                {
                    _abilityHUD_VM = new AbilityHUD_VM(true);
                    _abilityLayer = new GauntletLayer(100);
                    _abilityLayer.LoadMovie("AbilityHUD", _abilityHUD_VM);
                    MissionScreen.AddLayer(_abilityLayer);
                    
                    _careerabilityHUD_VM = new CareerAbilityHUD_VM();
                    _careerAbilityLayer = new GauntletLayer(98);
                    _careerAbilityLayer.LoadMovie("CareerAbilityHUD", _careerabilityHUD_VM);
                    MissionScreen.AddLayer(_careerAbilityLayer);
                }
                else
                {
                    _abilityHUD_VM = new AbilityHUD_VM();
                    _abilityLayer = new GauntletLayer(100);
                    _abilityLayer.LoadMovie("AbilityHUD", _abilityHUD_VM);
                    MissionScreen.AddLayer(_abilityLayer);
                    
                    _careerabilityHUD_VM = new CareerAbilityHUD_VM();
                    _careerAbilityLayer = new GauntletLayer(98);
                    _careerAbilityLayer.LoadMovie("CareerAbilityHUD", _careerabilityHUD_VM);
                    MissionScreen.AddLayer(_careerAbilityLayer);

                }
                _isInitialized = true;
                return;
            }
            
            _abilityHUD_VM = new AbilityHUD_VM();
            _abilityLayer = new GauntletLayer(100);
            _abilityLayer.LoadMovie("AbilityHUD", _abilityHUD_VM);
            MissionScreen.AddLayer(_abilityLayer);
           
            _careerabilityHUD_VM = new CareerAbilityHUD_VM();
            _careerAbilityLayer = new GauntletLayer(98);
            _careerAbilityLayer.LoadMovie("CareerAbilityHUD", _careerabilityHUD_VM);
            MissionScreen.AddLayer(_careerAbilityLayer);
            
            _isInitialized = true;
        }

        private void CheckMainAgent()
        {
            if (Agent.Main != null)
            {
                var component = Agent.Main.GetComponent<AbilityComponent>();
                if (component != null)
                {
                    _hasAbility = component.CurrentAbility != null;
                    _hasCareerAbility = component.CareerAbility != null;
                }
            }
        }

        public override void OnMissionTick(float dt)
        {
            if (!_isInitialized) return;
            
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
                if (_hasCareerAbility)
                {
                    _careerabilityHUD_VM.UpdateProperties();
                }
                if (_hasAbility)
                {
                    _abilityHUD_VM.UpdateProperties();
                      
                }
                return;
            }
            
            _abilityHUD_VM.IsVisible = false; 
            _careerabilityHUD_VM.IsVisible = false;
            
            //_specialMoveHUD_VM.IsVisible = false;
        }
    }
}
