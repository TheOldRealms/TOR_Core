using System.Collections.Generic;
using SandBox.View.Map;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.DotNet;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.GauntletUI.Widgets.Scoreboard;
using TaleWorlds.MountAndBlade.View.MissionViews;
using TaleWorlds.ObjectSystem;
using TOR_Core.AbilitySystem.Crosshairs;
using TOR_Core.BattleMechanics.TriggeredEffect.Scripts;
using TOR_Core.Extensions;
using TOR_Core.GameManagers;
using TOR_Core.Utilities;

namespace TOR_Core.AbilitySystem.Scripts
{
    public class SummonChampionScript : CareerAbilityScript
    {
        private string _summonedChampionId;

        private bool _summoned;
        
        private Vec3 _targetPosition;

        private Agent _champion;

        private bool _championIsActive;

        private bool _isDisabled;
        
        private GameKey _specialMoveKey;


        protected override void OnInit()
        {
            var effects = this.GetEffectsToTrigger();
            
            foreach (var effect in effects)
            {
                if (effect.SummonedTroopId != "none")
                {
                    _summonedChampionId = effect.SummonedTroopId;
                    if (_summonedChampionId.Contains("_plate") && _summonedChampionId.Contains("two_handed"))
                    {
                        _summonedChampionId = "tor_vc_grave_guard_champion_plate_two_handed";
                    }
                    break;
                }
               
            }
            
            _specialMoveKey = HotKeyManager.GetCategory(nameof(TORGameKeyContext)).GetGameKey("SpecialMove");

            _targetPosition = GameEntity.GlobalPosition;
        }
        
        protected override void OnTick(float dt)
        {
            if (!_summoned)
            {
                var data = TORSummonHelper.GetAgentBuildData(_casterAgent,_summonedChampionId);
                bool leftSide = false;
            
                _champion = TORSummonHelper.SpawnAgent(data, _targetPosition);
                
                _casterAgent.UnsetSpellCasterMode();
                _casterAgent.Controller = Agent.ControllerType.None;

                if (Hero.MainHero.HasCareerChoice("CodexMortificaKeystone"))
                {
                    _casterAgent.ApplyStatusEffect("greater_harbinger_ward_protection",null,9999f);
                }
                
                _champion.ApplyStatusEffect("greater_harbinger_debuff",null,9999f);
                _champion.Controller = Agent.ControllerType.Player;
                _summoned = true;
                _championIsActive = true;
            }

            

            if (!_casterAgent.IsActive() && !_isDisabled)
            {
                Blow blow = new Blow();
                blow.OwnerId = _casterAgent.Index;
                _champion.Die(blow);
                _isDisabled = true;
                Stop();
            }

            if (!_champion.IsActive()&& !_isDisabled)
            {
                _isDisabled = true;
                Stop();
            }
            
            if ((Input.IsKeyPressed(_specialMoveKey.KeyboardKey.InputKey) ||
                 Input.IsKeyPressed(_specialMoveKey.ControllerKey.InputKey)) 
                && Hero.MainHero.HasCareerChoice("DeArcanisKadonKeystone"))
            {
                switchBetweenAgents();
            }

        }

        public override void Stop()
        {
            base.Stop();
            _casterAgent.RemoveStatusEffect("greater_harbinger_ward_protection");
            TORCommon.Say("stop");
        }


        private void switchBetweenAgents()
        {
            if (!_championIsActive && _summoned)
            {
                if (_champion.Health>0)
                {
                    _casterAgent.Controller = Agent.ControllerType.None;
                    _champion.Controller = Agent.ControllerType.Player;
                    
                    if (Hero.MainHero.HasCareerChoice("CodexMortificaKeystone"))
                    {
                        _casterAgent.ApplyStatusEffect("greater_harbinger_ward_protection",null,9999f);
                    }
                }

                _championIsActive = true;

            }
            else
            {
                if (_casterAgent.Health>0)
                {
                    _casterAgent.Controller = Agent.ControllerType.Player;
                    _champion.Controller = Agent.ControllerType.AI;
                    _casterAgent.RemoveStatusEffect("greater_harbinger_ward_protection");
                }  
                
                _championIsActive = false;
            }
            
        }
    }
}