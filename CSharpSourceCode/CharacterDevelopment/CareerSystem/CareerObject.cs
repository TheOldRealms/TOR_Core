﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TOR_Core.AbilitySystem;
using TOR_Core.BattleMechanics.StatusEffect;
using TOR_Core.BattleMechanics.TriggeredEffect;
using TOR_Core.Extensions;

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

        public void Initialize(string name, Predicate<Hero> condition, string abilityID, ChargeType chargeType = ChargeType.CooldownOnly, int maxCharge = 100, Type abilityScriptType = null)
        {
            var description = GameTexts.FindText("career_description", StringId);
            base.Initialize(new TextObject(name), description);
            _condition = condition;
            ChargeType = chargeType;
            MaxCharge = maxCharge;
            AbilityTemplateID = abilityID;
            AbilityScriptType = abilityScriptType;
        }

        public bool IsConditionsMet(Hero hero)
        {
            bool result = false;
            if(_condition != null && _condition(hero))
            {
                result = true;
            }
            return result;
        }

        public void MutateAbility(AbilityTemplate ability, Agent casterAgent)
        {
            if(casterAgent != null && casterAgent.GetHero()?.GetExtendedInfo() != null)
            {
                var info = casterAgent.GetHero().GetExtendedInfo();
                var choices = AllChoices.Where(x => info.CareerChoices.Contains(x.StringId));
                foreach (var choice in choices)
                {
                    choice.MutateAbility(ability, casterAgent);
                }
            }
        }

        public void MutateTriggeredEffect(TriggeredEffectTemplate effect, Agent triggererAgent)
        {
            if (triggererAgent != null && triggererAgent.GetHero()?.GetExtendedInfo() != null)
            {
                var info = triggererAgent.GetHero().GetExtendedInfo();
                if (info.CareerID == StringId)
                {
                    var choices = AllChoices.Where(x => info.CareerChoices.Contains(x.StringId));
                    foreach (var choice in choices)
                    {
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
                if (info.CareerID == StringId)
                {
                    var choices = AllChoices.Where(x => info.CareerChoices.Contains(x.StringId));
                    foreach (var choice in choices)
                    {
                        choice.MutateStatusEffect(effect, applierAgent);
                    }
                }
            }
        }

        public AbilityTemplate GetAbilityTemplate() => AbilityFactory.GetTemplate(AbilityTemplateID);

        public List<TextObject> GetAbilityEffectLines()
        {
            var lines = new List<TextObject>();
            string[] s = RootNode.Description.ToString().Split(new string[] { "\\n" }, StringSplitOptions.RemoveEmptyEntries);
            foreach(var line in s)
            {
                lines.Add(new TextObject(line.Trim()));
            }
            return lines;
        }
    }
}