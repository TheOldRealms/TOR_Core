using System;
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
        private readonly CareerObject _career;
        private float _currentCharge;
        private readonly int _maxCharge = 100;
        private readonly Hero _ownerHero;

        public CareerAbility(AbilityTemplate template, Agent agent) : base(template)
        {
            _ownerHero = agent.GetHero();
            if (_ownerHero != null)
            {
                _career = _ownerHero.GetCareer();
                if (_career != null)
                {
                    ChargeType = _career.ChargeType;
                    _maxCharge = _career.MaxCharge;
                    var root = _career.RootNode;

                    Template = (AbilityTemplate)template.Clone(template.StringID + "*cloned*" + _ownerHero.StringId);
                    _career.MutateAbility(Template, agent);
                }

                if (Hero.MainHero.GetAllCareerChoices().Contains("CourtleyKeystone") || Hero.MainHero.GetAllCareerChoices().Contains("EnhancedHorseCombatKeystone"))
                    _currentCharge = _maxCharge;
                else
                    SetCoolDown(Template.CoolDown);
            }
        }

        public ChargeType ChargeType { get; } = ChargeType.CooldownOnly;
        public float ChargeLevel => _currentCharge / _maxCharge;
        public bool IsCharged => ChargeType == ChargeType.CooldownOnly || _currentCharge >= _maxCharge;

        protected override void AddExactBehaviour<TAbilityScript>(GameEntity parentEntity, Agent casterAgent)
        {
            if (_career.AbilityScriptType != null)
            {
                parentEntity.CreateAndAddScriptComponent(_career.AbilityScriptType.Name);
                AbilityScript = parentEntity.GetFirstScriptOfType<CareerAbilityScript>();
                var prefabEntity = SpawnEntity();
                parentEntity.AddChild(prefabEntity);
                AbilityScript?.Initialize(this);
                AbilityScript?.SetAgent(casterAgent);
                parentEntity.CallScriptCallbacks();
            }
            else
            {
                base.AddExactBehaviour<TAbilityScript>(parentEntity, casterAgent);
            }
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
                TORCommon.Say(_currentCharge + amount + " charged of " + _maxCharge); //TODO Remove
                var modifiedAmount = ModifyChargeAmount(amount);
                _currentCharge += modifiedAmount;
                _currentCharge = Math.Min(_maxCharge, _currentCharge);
            }
        }
        
        private float ModifyChargeAmount(float baseChargeAmount)
        {
            var number = new ExplainedNumber(baseChargeAmount);
            if (Agent.Main.GetHero().HasAnyCareer())
            {
                if (Agent.Main.GetHero().GetAllCareerChoices().Contains("ArkayneKeystone"))
                {
                    var choice = TORCareerChoices.GetChoice("ArkayneKeystone");
                    if (choice != null)
                    {
                        var value = choice.GetPassiveValue();
                        number.AddFactor(value);
                    }
                }

                if (Agent.Main.GetHero().GetAllCareerChoices().Contains("DreadKnightKeystone"))
                {
                    var choice = TORCareerChoices.GetChoice("DreadKnightKeystone");
                    if (choice != null)
                    {
                        var value = choice.GetPassiveValue();
                        number.AddFactor(value);
                    }
                }

                if (Agent.Main.GetHero().GetAllCareerChoices().Contains("NewBloodKeystone"))
                {
                    var choice = TORCareerChoices.GetChoice("NewBloodKeystone");
                    if (choice != null)
                    {
                        var value = choice.GetPassiveValue();
                        number.AddFactor(value);
                    }
                }

                if (Agent.Main.GetHero().GetAllCareerChoices().Contains("MartialleKeystone"))
                {
                    var choice = TORCareerChoices.GetChoice("MartialleKeystone");
                    if (choice != null)
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