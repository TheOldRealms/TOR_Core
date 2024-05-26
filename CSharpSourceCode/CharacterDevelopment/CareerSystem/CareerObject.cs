using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TOR_Core.AbilitySystem;
using TOR_Core.BattleMechanics.StatusEffect;
using TOR_Core.BattleMechanics.TriggeredEffect;
using TOR_Core.CharacterDevelopment.CareerSystem.CareerButton;
using TOR_Core.Extensions;
using TOR_Core.Extensions.ExtendedInfoSystem;

namespace TOR_Core.CharacterDevelopment.CareerSystem
{
    public class CareerObject : PropertyObject
    {
        private Predicate<Hero> _condition;
        public ChargeType ChargeType { get; private set; }
        public int MaxCharge { get; private set; }
        public string AbilityTemplateID { get; private set; }
        public Type AbilityScriptType { get; private set; }
        public CareerChoiceObject RootNode { get; set; }
        public List<CareerChoiceGroupObject> ChoiceGroups { get; private set; } = new List<CareerChoiceGroupObject>();

        public delegate float ChargeFunction(Agent affectorAgent, Agent affectedAgent, ChargeType chargeType, int chargeValue, AttackTypeMask mask, CareerHelper.ChargeCollisionFlag collisionFlag);

        private ChargeFunction _chargeFunction;

        public float GetCalculatedCareerAbilityCharge(Agent affector, Agent affected, ChargeType chargeType, int chargeValue, AttackTypeMask mask, CareerHelper.ChargeCollisionFlag collisionFlag)
        {
            float result = 0f;
            if (_chargeFunction != null)
            {
                return _chargeFunction.Invoke(affector, affected, chargeType, chargeValue, mask, collisionFlag);
            }

            return result;
        }

        public List<CareerChoiceObject> AllChoices
        {
            get
            {
                List<CareerChoiceObject> result = new List<CareerChoiceObject>();
                ChoiceGroups.ForEach(x => x.Choices.ForEach(y => result.Add(y)));
                return result;
            }
        }

        public CareerObject(string stringId) : base(stringId) { }

        public override string ToString() => Name.ToString();

        public void Initialize(string name, Predicate<Hero> condition, string abilityID, ChargeFunction function = null, int maxCharge = 100, Type abilityScriptType = null)
        {
            var description = GameTexts.FindText("career_description", StringId);
            base.Initialize(new TextObject(name), description);
            _condition = condition;
            MaxCharge = maxCharge;
            AbilityTemplateID = abilityID;
            AbilityScriptType = abilityScriptType;
            
            _chargeFunction = function;
            if (_chargeFunction == null)
                ChargeType = ChargeType.CooldownOnly;
            else
            {
                ChargeType = ChargeType.Custom;     //you can either have cooldown or custom, but in game the Charge types are still applicable. 
            }
            
            AfterInitialized();
        }

        public bool IsConditionsMet(Hero hero)
        {
            return _condition != null && _condition(hero);
        }

        public void MutateAbility(AbilityTemplate ability, Agent casterAgent)
        {
            if (casterAgent != null && casterAgent.GetHero()?.GetExtendedInfo() != null)
            {
                var info = casterAgent.GetHero().GetExtendedInfo();
                var root = casterAgent.GetHero().GetCareer().RootNode;
                var choices = AllChoices.Where(x => info.CareerChoices.Contains(x.StringId));
                List<CareerChoiceObject> modifications = new List<CareerChoiceObject> { root };
                modifications.AddRange(choices);
                foreach (var choice in modifications.Where(choice => choice.HasMutations()))
                {
                    choice.MutateAbility(ability, casterAgent);
                }
            }
        }

        public void MutateTriggeredEffect(TriggeredEffectTemplate effect, Agent triggererAgent)
        {
            if (triggererAgent != null && triggererAgent.GetHero()?.GetExtendedInfo() != null)
            {
         
                var root = triggererAgent.GetHero().GetCareer().RootNode;
                var info = triggererAgent.GetHero().GetExtendedInfo();
                if (info.CareerID == StringId)
                {
                    List<CareerChoiceObject> modifications = new List<CareerChoiceObject> { root };
                    modifications.AddRange(AllChoices.Where(x => info.CareerChoices.Contains(x.StringId)));
                    foreach (var choice in modifications)
                    {
                        if (choice.HasMutations())
                            choice.MutateTriggeredEffect(effect, triggererAgent);
                    }
                }
            }
        }

        public void MutateStatusEffect(StatusEffectTemplate effect, Agent applierAgent)
        {
            if (applierAgent != null && applierAgent.GetHero()?.GetExtendedInfo() != null)
            {
                var info = applierAgent.GetHero().GetExtendedInfo();
                if (info.CareerID != StringId) return;
                var choices = new List<CareerChoiceObject>();
                choices.Add(RootNode);
                choices.AddRange(AllChoices.Where(x => info.CareerChoices.Contains(x.StringId)));

                foreach (var choice in choices.Where(choice => choice.HasMutations()))
                {
                    choice.MutateStatusEffect(effect, applierAgent);
                }
            }
        }

        public AbilityTemplate GetAbilityTemplate() => AbilityFactory.GetTemplate(AbilityTemplateID);

        public List<TextObject> GetAbilityEffectLines()
        {
            var lines = new List<TextObject>();
            string[] s = RootNode.Description.ToString().Split(new string[] { "\\n" }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in s)
            {
                lines.Add(new TextObject(line.Trim()));
            }

            return lines;
        }
    }
}