using System;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Engine;
using TaleWorlds.MountAndBlade;
using TOR_Core.AbilitySystem.Crosshairs;
using TOR_Core.AbilitySystem.Scripts;
using TOR_Core.Battle.CrosshairMissionBehavior;
using TOR_Core.CharacterDevelopment;
using TOR_Core.CharacterDevelopment.CareerSystem;
using TOR_Core.Extensions;

namespace TOR_Core.AbilitySystem
{
    public class CareerAbility : Ability
    {
        private readonly CareerObject _career;
        private float _currentCharge;
        private readonly int _maxCharge = 1000;
        private readonly Hero _ownerHero;
        private bool _doubleUse;
        private bool _requiresDisabledCrosshair = false;
        public bool RequiresDisabledCrosshairDuringAbility => _requiresDisabledCrosshair;
        public ChargeType ChargeType { get; } = ChargeType.CooldownOnly;
        public float ChargeLevel => _currentCharge / _maxCharge;
        public bool IsCharged => ChargeType == ChargeType.CooldownOnly || _currentCharge >= _maxCharge;

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

                    if (Template.StringID.Contains("ShadowStep"))
                    {
                        _requiresDisabledCrosshair = true;
                    }
                }

                if (Hero.MainHero.HasCareer(TORCareers.WitchHunter)
                    || Hero.MainHero.HasCareerChoice("CourtleyKeystone")
                    || Hero.MainHero.HasCareerChoice("EnhancedHorseCombatKeystone")
                    || Hero.MainHero.HasCareerChoice("NoRestAgainstEvilKeystone")
                    || Hero.MainHero.HasCareerChoice("LiberMortisKeystone"))
                    _currentCharge = _maxCharge;
                else
                    SetCoolDown(Template.CoolDown);
            }
        }

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

            if (_career.AllChoices.Any(x => x.StringId == "SecretsOfTheGrailKeystone") && _doubleUse == false)
            {
                _currentCharge = _maxCharge;
                _doubleUse = true;
            }
        }

        public override bool CanCast(Agent casterAgent)
        {

            if (Template.StringID.Contains("ShadowStep") && casterAgent.HasMount) return false;

            if (IsSingleTarget && !((SingleTargetCrosshair)Crosshair).IsTargetLocked) return false;

            return !_isLocked && !IsCasting &&
                   !IsOnCooldown() &&
                   IsCharged &&
                   casterAgent.IsPlayerControlled;
        }

        public void AddCharge(float amount)
        {
            if (!IsActive)
            {
                _currentCharge += amount;
                _currentCharge = Math.Min(_maxCharge, _currentCharge);
            }

            if (_currentCharge == _maxCharge)
            {
                _doubleUse = false;
            }
        }
    }

    public enum ChargeType
    {
        CooldownOnly,
        NumberOfKills,
        DamageDone,
        Healed,
        DamageTaken,
        Custom,
    }
}