using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TOR_Core.AbilitySystem;
using TOR_Core.Extensions;

namespace TOR_Core.CharacterDevelopment.CareerSystem
{
    public class CareerObject : PropertyObject
    {
        private Predicate<Hero> _condition;
        private AbilityTemplate _careerAbility;
        private CareerNode _rootNode;
        public CareerObject(string stringId) : base(stringId) { }

        public override string ToString() => Name.ToString();

        public void Initialize(string name, string description, Predicate<Hero> condition, string careerAbilityID)
        {
            base.Initialize(new TextObject(name), new TextObject(description));
            _condition = condition;
            _careerAbility = AbilityFactory.GetTemplate(careerAbilityID);
            AddNode(new CareerNode(name + "Root", (hero) => hero.AddAbility(_careerAbility.StringID)));
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

        public void AddNode(CareerNode node, string nextNodeId = null)
        {
            if (nextNodeId == null && _rootNode == null && node != null) _rootNode = node;
            else
            {

            }
        }
    }
}
