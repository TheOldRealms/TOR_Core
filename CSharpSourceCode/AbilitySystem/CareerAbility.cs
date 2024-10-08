using System;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Engine;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.TwoDimension;
using TOR_Core.AbilitySystem.Crosshairs;
using TOR_Core.AbilitySystem.Scripts;
using TOR_Core.Battle.CrosshairMissionBehavior;
using TOR_Core.CharacterDevelopment;
using TOR_Core.CharacterDevelopment.CareerSystem;
using TOR_Core.Extensions;
using TOR_Core.HarmonyPatches;
using TOR_Core.Utilities;

namespace TOR_Core.AbilitySystem
{
    public class CareerAbility : Ability
    {
        private int usageCounter;
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
            usageCounter = 0;
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
                    ||Hero.MainHero.HasCareerChoice("LegendsOfMalokKeystone")
                    || Hero.MainHero.HasCareerChoice("CourtleyKeystone")
                    || Hero.MainHero.HasCareerChoice("EnhancedHorseCombatKeystone")
                    || Hero.MainHero.HasCareerChoice("SwampRiderKeystone")
                    || Hero.MainHero.HasCareerChoice("LiberMortisKeystone")
                    || Hero.MainHero.HasCareerChoice("WellspringOfDharKeystone")
                    || Hero.MainHero.HasCareerChoice("ProtectorOfTheWoodsKeystone")
                    || Hero.MainHero.HasCareerChoice("ArielsBlessingKeystone"))
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
                AbilityScript?.SetCasterAgent(casterAgent);
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

            var choices = Hero.MainHero.GetAllCareerChoices();

            if ((choices.Contains("SecretsOfTheGrailKeystone")||
                 choices.Contains("EverlingsSecretKeystone")
                ) && _doubleUse == false)
            {
                _currentCharge = _maxCharge;
                _doubleUse = true;
            }

            usageCounter++;
        }

        public override bool IsDisabled(Agent casterAgent, out TextObject disabledReason)
        {
            if (!IsCharged)
            {
                disabledReason = new TextObject("{=!}Ability not charged");
                return true;
            }
            if (IsNotUsableMounted(Template.StringID) && casterAgent.HasMount)
            {
                disabledReason = new TextObject("{=!}Not usable mounted");
                return true;
            }

            return base.IsDisabled(casterAgent, out disabledReason);
        }

        private static bool IsNotUsableMounted(string templateID)
        {
            return templateID.Contains("ShadowStep" ) || 
                   templateID.Contains("AxeOfUlric");
        }

        public override bool CanCast(Agent casterAgent, out TextObject failureReason)
        {
            if (Hero.MainHero.HasCareer(TORCareers.ImperialMagister))
            {
                if (usageCounter >= Template.ScaleVariable1)
                {
                    failureReason = new TextObject($"Not enough Usages (maximum {Template.ScaleVariable1})");
                    return false;
                }
            }
            if (IsSingleTarget && !((SingleTargetCrosshair)Crosshair).IsTargetLocked)
            {
                failureReason = new TextObject("No target locked");
                return false;
            }
            if (!casterAgent.IsPlayerControlled)
            {
                failureReason = new TextObject("Caster is not player controlled");
                return false;
            }

            return base.CanCast(casterAgent, out failureReason);
        }

        public void AddCharge(float amount)
        {
            if (amount>=0 && _currentCharge >= _maxCharge)
                return;
            
            if (!IsActive)
            {
                _currentCharge += amount;
                _currentCharge = Mathf.Clamp(_currentCharge,0, _maxCharge);
            }

            if (_doubleUse) //remove doubleUse in case of special perks that allow for a "second" usage.
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