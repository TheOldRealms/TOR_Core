using System;
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
        public CareerObject(string stringId) : base(stringId) { }

        public override string ToString() => Name.ToString();

        public void Initialize(string name, string description, Predicate<Hero> condition, string abilityID, ChargeType chargeType = ChargeType.CooldownOnly, int maxCharge = 100, Type abilityScriptType = null)
        {
            base.Initialize(new TextObject(name), new TextObject(description));
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
                if(info.CareerID == StringId)
                {
                    var choices = TORCareerChoices.All.Where(x => info.CareerChoices.Contains(x.StringId));
                    foreach(var choice in choices)
                    {
                        choice.MutateAbility(ability, casterAgent);
                    }
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
                    var choices = TORCareerChoices.All.Where(x => info.CareerChoices.Contains(x.StringId));
                    foreach (var choice in choices)
                    {
                        choice.MutateTriggeredEffect(effect, triggererAgent);
                    }
                }
            }
        }

        internal void MutateStatusEffect(StatusEffectTemplate effect, Agent applierAgent)
        {
            if (applierAgent != null && applierAgent.GetHero()?.GetExtendedInfo() != null)
            {
                var info = applierAgent.GetHero().GetExtendedInfo();
                if (info.CareerID == StringId)
                {
                    var choices = TORCareerChoices.All.Where(x => info.CareerChoices.Contains(x.StringId));
                    foreach (var choice in choices)
                    {
                        choice.MutateStatusEffect(effect, applierAgent);
                    }
                }
            }
        }
    }
}
