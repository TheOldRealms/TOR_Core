using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Engine;
using TaleWorlds.MountAndBlade;
using TOR_Core.AbilitySystem.Scripts;
using TOR_Core.CharacterDevelopment;
using TOR_Core.CharacterDevelopment.CareerSystem;
using TOR_Core.Extensions;
using TOR_Core.Utilities;

namespace TOR_Core.AbilitySystem
{
    public class CareerAbility : Ability
    {
        private Hero _ownerHero = null;
        private CareerObject _career = null;
        private int _maxCharge = 100;
        private float _currentCharge = 0;
        public ChargeType ChargeType { get; private set; } = ChargeType.CooldownOnly;
        public float ChargeLevel => _currentCharge / _maxCharge;
        public bool IsCharged => _currentCharge >= _maxCharge;

        public CareerAbility(AbilityTemplate template, Agent agent) : base(template)
        {
            _ownerHero = agent.GetHero();
            if(_ownerHero != null)
            {
                _career = _ownerHero.GetCareer();
                if(_career != null)
                {
                    ChargeType = _career.ChargeType;
                    _maxCharge = _career.MaxCharge;
                    var root = _career.RootNode;
                    
                    Template = (AbilityTemplate)template.Clone(template.StringID + "*cloned*" + _ownerHero.StringId);
                    _career.MutateAbility(Template, agent);
                }
            }
            
            if (Agent.Main.GetHero().GetAllCareerChoices().Contains("CourtleyKeystone")||true)
            {
                _currentCharge = _maxCharge;
            }
           
        }

        protected override void AddExactBehaviour<TAbilityScript>(GameEntity parentEntity, Agent casterAgent)
        {
            if(_career.AbilityScriptType != null)
            {
                parentEntity.CreateAndAddScriptComponent(_career.AbilityScriptType.Name);
                AbilityScript = parentEntity.GetFirstScriptOfType<CareerAbilityScript>();
                var prefabEntity = SpawnEntity();
                parentEntity.AddChild(prefabEntity);
                AbilityScript?.Initialize(this);
                AbilityScript?.SetAgent(casterAgent);
                parentEntity.CallScriptCallbacks();
            }
            else base.AddExactBehaviour<TAbilityScript>(parentEntity, casterAgent);
        }

        public override void ActivateAbility(Agent casterAgent)
        {
            base.ActivateAbility(casterAgent);
            if (ChargeType != ChargeType.CooldownOnly) _currentCharge = 0;
        }

        public override bool CanCast(Agent casterAgent)
        {
            if (Template.StringID.Contains("ShadowStep") && casterAgent.HasMount) return false;
            return !IsCasting &&
                   !IsOnCooldown() &&
                   IsCharged &&
                   casterAgent.IsPlayerControlled;
        }

        public void AddCharge(float amount)
        {
            if (!IsActive)
            {
                TORCommon.Say(_currentCharge+amount+ " charged of " +_maxCharge);   //TODO Remove
                var modifiedAmount = ModifyChargeAmount(amount);
                _currentCharge += modifiedAmount;
                _currentCharge = Math.Min(_maxCharge, _currentCharge);
            }
        }




        private float ModifyChargeAmount(float baseChargeAmount)
        {
            ExplainedNumber number = new ExplainedNumber(baseChargeAmount);
            if (Agent.Main.GetHero().HasAnyCareer())
            {
                if (Agent.Main.GetHero().GetAllCareerChoices().Contains("ArkayneKeystone"))
                {
                    var choice = TORCareerChoices.GetChoice("ArkayneKeystone");
                    if(choice!=null)
                    {
                        var value = choice.GetPassiveValue();
                        number.AddFactor(value);
                    }
                }
                
                if (Agent.Main.GetHero().GetAllCareerChoices().Contains("NewBloodKeystone"))
                {
                    var choice = TORCareerChoices.GetChoice("NewBloodKeystone");
                    if(choice!=null)
                    {
                        var value = choice.GetPassiveValue();
                        number.AddFactor(value);
                    }
                }
                
                if (Agent.Main.GetHero().GetAllCareerChoices().Contains("MartialleKeystone"))
                {
                    var choice = TORCareerChoices.GetChoice("MartialleKeystone");
                    if(choice!=null)
                    {
                        var value = choice.GetPassiveValue();
                        number.AddFactor(value);
                    }
                }
               
            }

            return number.ResultNumber;
        }
    }

    public enum ChargeType
    {
        CooldownOnly,
        NumberOfKills,
        DamageDone,
        DamageTaken
    }
}
