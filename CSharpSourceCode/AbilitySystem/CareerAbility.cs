using System;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
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
                    || Hero.MainHero.HasCareer(TORCareers.Runelord)
                    || Hero.MainHero.HasCareerChoice("LegendsOfMalokKeystone")
                    || Hero.MainHero.HasCareerChoice("CourtleyKeystone")
                    || Hero.MainHero.HasCareerChoice("EnhancedHorseCombatKeystone")
                    || Hero.MainHero.HasCareerChoice("SwampRiderKeystone")
                    || Hero.MainHero.HasCareerChoice("LiberMortisKeystone")
                    || Hero.MainHero.HasCareerChoice("WellspringOfDharKeystone")
                    || Hero.MainHero.HasCareerChoice("ProtectorOfTheWoodsKeystone")
                    || Hero.MainHero.HasCareerChoice("ArielsBlessingKeystone")
                    || Hero.MainHero.HasCareerChoice("SecularOrdersKeystone")
                    || Hero.MainHero.HasCareerChoice("TunnelWatchKeystone")
                    || Hero.MainHero.HasCareerChoice("GiantSlayerKeystone")
                    || Hero.MainHero.HasCareerChoice("WardenOfCythralKeystone")
                    || Hero.MainHero.HasCareerChoice("BonesAnFirepitzKeystone"))
                {
                    _currentCharge = _maxCharge;
                    SetCoolDown(0);
                }
                else
                    SetCoolDown(Template.CoolDown);

            }
        }

        protected override void AddExactBehaviour<TAbilityScript>(ref GameEntity parentEntity, Agent casterAgent)
        {
            if (_career.AbilityScriptType != null)
            {
                parentEntity.CreateAndAddScriptComponent(_career.AbilityScriptType.Name, false);
                AbilityScript = parentEntity.GetFirstScriptOfType<CareerAbilityScript>();
                var prefabEntity = SpawnEntity();
                parentEntity.AddChild(prefabEntity);
                AbilityScript?.Initialize(this, ref parentEntity);
                AbilityScript?.SetCasterAgent(casterAgent);

                string spellName = Template?.Name?.ToString();
                if (string.IsNullOrWhiteSpace(spellName))
                {
                    spellName = Template?.StringID ?? "unknown_spell";
                }

                string casterName = casterAgent?.Name?.ToString();
                if (string.IsNullOrWhiteSpace(casterName))
                {
                    casterName = "unknown_caster";
                }

                TORCommon.Log(
                    $"spell cast start | spell={spellName} | caster={casterName} | side={casterAgent?.Team?.Side} | ai={casterAgent?.IsAIControlled}",
                    NLog.LogLevel.Info);
            }
            else
            {
                base.AddExactBehaviour<TAbilityScript>(ref parentEntity, casterAgent);
            }
        }

        public override void ActivateAbility(Agent casterAgent)
        {
            base.ActivateAbility(casterAgent);
            if (ChargeType != ChargeType.CooldownOnly) _currentCharge = 0;

            var choices = Hero.MainHero.GetAllCareerChoices();

            if ((choices.Contains("SecretsOfTheGrailKeystone") ||
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
                disabledReason = TORTextHelper.GetTextObject("tor_career_ability_not_charged", "Ability not charged");
                return true;
            }
            if (casterAgent.HasMount && IsNotUsableMounted(Template.StringID))
            {
                disabledReason = TORTextHelper.GetTextObject("tor_career_ability_not_usable_mounted", "Not usable mounted");
                return true;
            }

            if (casterAgent.WieldedWeapon.IsEmpty && IsNotUsableWithoutWeapon(Template.StringID))
            {
                disabledReason = TORTextHelper.GetTextObject("tor_career_ability_not_usable_without_weapon", "Not usable without weapon");
                return true;
            }

            // Check if summoning career abilities can be used (mission agent limit)
            if (IsSummoningCareerAbility() && !TORSummonHelper.CanSummon())
            {
                disabledReason = TORTextHelper.GetTextObject("tor_ability_mission_agent_limit", "Too many agents on battlefield");
                return true;
            }

            return base.IsDisabled(casterAgent, out disabledReason);
        }

        private bool IsSummoningCareerAbility()
        {
            // Necromancer's Greater Harbinger and Spellsinger's Wrath of the Wood are primarily summoning abilities
            return _career == TORCareers.Necromancer || _career == TORCareers.Spellsinger;
        }

        private static bool IsNotUsableMounted(string templateID)
        {
            return templateID.Contains("ShadowStep") ||
                   templateID.Contains("AxeOfUlric") ||
                   templateID.Contains("ArmedToDaTeef");
        }

        private static bool IsNotUsableWithoutWeapon(string templateID)
        {
            return templateID.Contains("AxeOfUlric") ||
                   templateID.Contains("ArmedToDaTeef") ||
                   templateID.Contains("Accusation") ||
                   templateID.Contains("KnightlyStrike") ||
                   templateID.Contains("KnightlyCharge") ||
                   templateID.Contains("ArrowOfKurnous");
        }

        public override bool CanCast(Agent casterAgent, out TextObject failureReason)
        {
            if (Hero.MainHero.HasCareer(TORCareers.ImperialMagister))
            {
                if (usageCounter >= Template.ScaleVariable1)
                {
                    failureReason = TORTextHelper.GetTextObject("tor_ability_not_enough_usages_text", "Not enough Usages (maximum {MAXIMUM})");
                    failureReason.SetTextVariable("MAXIMUM", (int)Template.ScaleVariable1);
                    return false;
                }
            }

            // Waywatcher requires a bow to be wielded
            if (Hero.MainHero.HasCareer(TORCareers.Waywatcher))
            {
                var weapon = casterAgent.WieldedWeapon;
                if (weapon.IsEmpty || weapon.CurrentUsageItem == null || !weapon.CurrentUsageItem.IsRangedWeapon || weapon.CurrentUsageItem.WeaponClass == WeaponClass.Crossbow)
                {
                    failureReason = new TextObject("{=tor_ability_requires_bow}You must wield a bow to use this ability");
                    return false;
                }
            }

            if (IsSingleTarget && !((SingleTargetCrosshair)Crosshair).IsTargetLocked)
            {
                failureReason = TORTextHelper.GetTextObject("tor_ability_no_target_locked_text", "No target locked");
                return false;
            }
            if (!casterAgent.IsPlayerControlled)
            {
                failureReason = TORTextHelper.GetTextObject("tor_ability_not_player_controlled_text", "Caster is not player controlled");
                return false;
            }

            return base.CanCast(casterAgent, out failureReason);
        }

        public void AddCharge(float amount)
        {
            if (amount >= 0 && _currentCharge >= _maxCharge)
                return;

            if (!IsActive)
            {
                _currentCharge += amount;
                _currentCharge = Mathf.Clamp(_currentCharge, 0, _maxCharge);
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