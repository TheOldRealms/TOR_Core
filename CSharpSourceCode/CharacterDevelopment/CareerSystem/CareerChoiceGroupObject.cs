using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NLog;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TOR_Core.Utilities;

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

        public void Initialize(string name, CareerObject ownerCareer, int tier, ConditionDelegate conditionDelegate, UnlockDelegate unlockDelegate = null)
        {
            TextObject nameText = new TextObject(name);

            // Look up externalized name by StringId (mirrors CareerChoiceObject pattern)
            if (GameTexts.TryGetText("tor_career_choicegroup_name", out var nameOverride, StringId))
            {
                if (nameOverride != null)
                {
                    if (!nameOverride.Contains(nameText))
                    {
                        TORCommon.Log(String.Format("Career choice group name has been modified in externalized strings. this should be adapted in code: \n CODE {0} \n XML {1}", nameText, nameOverride), LogLevel.Warn);
                    }
                    nameText = new TextObject(nameOverride.Value.ToString());
                }

                if (nameOverride == null)
                {
                    TORCommon.Log(String.Format("tor_career_choicegroup_name {0} was not found in externalized strings (tor_strings)", StringId), LogLevel.Warn);
                }
            }

            base.Initialize(nameText, new TextObject("Choice group for " + name));
            OwnerCareer = ownerCareer;
            Tier = tier;
            _conditionDelegate = conditionDelegate;
            _unlockDelegate = unlockDelegate;
            OwnerCareer.ChoiceGroups.Add(this);
            AfterInitialized();
        }

        public bool IsActiveForHero(Hero hero)
        {
            var value = false;
            if (_conditionDelegate != null)
            {
                value = _conditionDelegate(hero, out _);
            }

            if (_unlockDelegate != null)
            {
                value = _unlockDelegate(hero, out _);
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