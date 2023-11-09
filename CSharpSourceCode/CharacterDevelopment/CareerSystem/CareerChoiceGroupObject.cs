using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace TOR_Core.CharacterDevelopment.CareerSystem
{
    public class CareerChoiceGroupObject : PropertyObject
    {
        public delegate bool ConditionDelegate(Hero hero, out string text);
        private ConditionDelegate _conditionDelegate;
        
        public delegate bool UnlockDelegate(Hero hero, out string text);
        private UnlockDelegate _unlockDelegate;
        public CareerObject OwnerCareer { get; private set; }
        public int Tier { get; private set; }
        public List<CareerChoiceObject> Choices { get; private set; } = new List<CareerChoiceObject>();
        public override string ToString() => Name.ToString();

        public CareerChoiceGroupObject(string stringId) : base(stringId) { }
        
        
        public void Initialize(string name,CareerObject ownerCareer, int tier, ConditionDelegate conditionDelegate, UnlockDelegate unlockDelegate=null)
        {
            base.Initialize(new TextObject(name), new TextObject("Choice group for " + name));
            OwnerCareer = ownerCareer;
            Tier = tier;
            _conditionDelegate = conditionDelegate;
            _unlockDelegate = unlockDelegate;
            OwnerCareer.ChoiceGroups.Add(this);
            AfterInitialized();
        }

        public void Initialize(string name, CareerObject[] ownerCareers, int tier, ConditionDelegate conditionDelegate, UnlockDelegate unlockDelegate=null)
        {
            if (ownerCareers.Length < 2)
            {
                Initialize (name, ownerCareers[0], tier, conditionDelegate, unlockDelegate);
            }
            else
            {
                foreach (var career in ownerCareers)
                {
                    Initialize(name,career,tier,conditionDelegate,unlockDelegate);
                }
            }
            
        }

        public bool IsActiveForHero(Hero hero)
        {
            var value = false;
            if (_conditionDelegate != null)
            {
                value=  _conditionDelegate(hero, out _);
            }
            
            if (_unlockDelegate != null)
            {
                value=  _unlockDelegate(hero, out _);
            }

            if (value)
            {
                TORCareerChoices.Instance.GetCareerChoices(OwnerCareer).UnlockCareerBenefits(Tier);
            }
            
            return value;
        }

        public string GetConditionText(Hero hero)
        {
            string reason = "Condition not found.";
            if (_conditionDelegate != null) _ = _conditionDelegate(hero, out reason);
            return reason;
        }
        
        public string GetUnlockText(Hero hero)
        {
            string unlockText = "";
            if (_unlockDelegate != null) _ = _unlockDelegate(hero, out unlockText);
            return unlockText;
        }
    }
}
