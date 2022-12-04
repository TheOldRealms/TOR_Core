using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TOR_Core.AbilitySystem;
using TOR_Core.BattleMechanics.TriggeredEffect;
using TOR_Core.Extensions;

namespace TOR_Core.CharacterDevelopment.CareerSystem
{
    public class CareerObject : PropertyObject
    {
        private Predicate<Hero> _condition;
        private ChargeType _chargeType;
        public string AbilityTemplateID { get; private set; }
        public Type AbilityScriptType { get; private set; }
        public CareerObject(string stringId) : base(stringId) { }

        public override string ToString() => Name.ToString();

        public void Initialize(string name, string description, Predicate<Hero> condition, string abilityID, ChargeType chargeType, Type abilityScriptType = null)
        {
            base.Initialize(new TextObject(name), new TextObject(description));
            _condition = condition;
            _chargeType = chargeType;
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

        public void MutateAbility(CareerAbility ability, Hero hero)
        {
            if(hero != null && hero.GetExtendedInfo() != null)
            {
                var info = hero.GetExtendedInfo();
                if(info.CareerID == StringId)
                {
                    ability.ChargeType = _chargeType;
                    var choices = TORCareerChoices.All.Where(x => hero.GetExtendedInfo().CareerChoices.Contains(x.StringId));
                    foreach(var choice in choices)
                    {
                        choice.MutateAbility(ability, hero);
                    }
                }
            }
        }

        public void MutateTriggeredEffect(TriggeredEffectTemplate effect, Hero hero)
        {
            if (hero != null && hero.GetExtendedInfo() != null)
            {
                var info = hero.GetExtendedInfo();
                if (info.CareerID == StringId)
                {
                    var choices = TORCareerChoices.All.Where(x => hero.GetExtendedInfo().CareerChoices.Contains(x.StringId));
                    foreach (var choice in choices)
                    {
                        choice.MutateEffect(effect, hero);
                    }
                }
            }
        }
    }
}
