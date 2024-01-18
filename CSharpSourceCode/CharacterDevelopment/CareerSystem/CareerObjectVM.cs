using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.TwoDimension;
using TOR_Core.Extensions;
using TOR_Core.Utilities;

namespace TOR_Core.CharacterDevelopment.CareerSystem
{
    public class CareerObjectVM : ViewModel
    {
        private string _name;
        private string _spriteName;
        private string _abilitySpriteName;
        private string _abilityName;
        private MBBindingList<CareerAbilityEffectVM> _abilityDescription;
        private string _description;
        private CareerObject _career;
        private MBBindingList<CareerChoiceGroupObjectVM> _choiceGroups1;
        private MBBindingList<CareerChoiceGroupObjectVM> _choiceGroups2;
        private MBBindingList<CareerChoiceGroupObjectVM> _choiceGroups3;
        private string _choiceGroup1Name;
        private string _choiceGroup2Name;
        private string _choiceGroup3Name;
        private string _choiceGroup1Condition;
        private string _choiceGroup2Condition;
        private string _choiceGroup3Condition;
        private string _choiceGroup1Unlock;
        private string _choiceGroup2Unlock;
        private string _choiceGroup3Unlock;
        private string _freeCareerPoints;
        private bool _tier1Active;
        private bool _tier2Active;
        private bool _tier3Active;

        public CareerObjectVM(CareerObject career)
        {
            _career = career;
            _name = GameTexts.FindText("career_title", _career.StringId).ToString();
            _spriteName = "CareerSystem\\Illustrations\\" + career.StringId;
            _abilitySpriteName = _career.GetAbilityTemplate()?.SpriteName;      //in case no career ability is found deactivate this screen
            _abilityName = new TextObject(_career.GetAbilityTemplate()?.Name).ToString();
            _abilityDescription = new MBBindingList<CareerAbilityEffectVM>();
            _career.GetAbilityEffectLines().ForEach(x => _abilityDescription.Add(new CareerAbilityEffectVM(x)));
            _description = _career.Description.ToString();
            _choiceGroups1 = new MBBindingList<CareerChoiceGroupObjectVM>();
            _choiceGroups2 = new MBBindingList<CareerChoiceGroupObjectVM>();
            _choiceGroups3 = new MBBindingList<CareerChoiceGroupObjectVM>();

            foreach(var group in _career.ChoiceGroups)
            {
                switch (group.Tier)
                {
                    case 1:
                        _choiceGroups1.Add(new CareerChoiceGroupObjectVM(group, RefreshValues));
                        if(group.GetConditionText(Hero.MainHero) != _choiceGroup1Condition) _choiceGroup1Condition += group.GetConditionText(Hero.MainHero);
                        if(group.GetUnlockText(Hero.MainHero) != _choiceGroup1Unlock) _choiceGroup1Unlock+= group.GetUnlockText(Hero.MainHero);
                        break;
                    case 2:
                        _choiceGroups2.Add(new CareerChoiceGroupObjectVM(group, RefreshValues));
                        if (group.GetConditionText(Hero.MainHero) != _choiceGroup2Condition) _choiceGroup2Condition += group.GetConditionText(Hero.MainHero);
                        if(group.GetUnlockText(Hero.MainHero) != _choiceGroup2Unlock) _choiceGroup2Unlock+= group.GetUnlockText(Hero.MainHero);
                        break;
                    case 3:
                        _choiceGroups3.Add(new CareerChoiceGroupObjectVM(group, RefreshValues));
                        if (group.GetConditionText(Hero.MainHero) != _choiceGroup3Condition) _choiceGroup3Condition += group.GetConditionText(Hero.MainHero);
                        if(group.GetUnlockText(Hero.MainHero) != _choiceGroup3Unlock) _choiceGroup3Unlock+= group.GetUnlockText(Hero.MainHero);
                        break;
                    default:
                        break;
                }
            }
            _choiceGroup1Name = GameTexts.FindText("career_choicegroup1_name", _career.StringId).ToString();
            _choiceGroup2Name = GameTexts.FindText("career_choicegroup2_name", _career.StringId).ToString();
            _choiceGroup3Name = GameTexts.FindText("career_choicegroup3_name", _career.StringId).ToString();
            _tier1Active = !_career.ChoiceGroups.Where(x => x.Tier == 1).All(x => x.IsActiveForHero(Hero.MainHero));
            _tier2Active = !_career.ChoiceGroups.Where(x => x.Tier == 2).All(x => x.IsActiveForHero(Hero.MainHero));
            _tier3Active = !_career.ChoiceGroups.Where(x => x.Tier == 3).All(x => x.IsActiveForHero(Hero.MainHero));
            RefreshValues();
        }

        public override void RefreshValues()
        {
            var info = Hero.MainHero.GetExtendedInfo();
            if(info != null)
            {
                int usedPoints = info.CareerChoices.Count - 1; //Account for root choice, does not need to be taken into consideration.

                var min = Mathf.Min(TORConfig.MaximumNumberOfCareerPerkPoints, Hero.MainHero.Level);
                FreeCareerPoints = "Free career points: " + (min - usedPoints).ToString();
            }
        }

        [DataSourceProperty]
        public string Name
        {
            get
            {
                return _name;
            }
            set
            {
                if (value != _name)
                {
                    _name = value;
                    OnPropertyChangedWithValue(value, "Name");
                }
            }
        }

        [DataSourceProperty]
        public string Description
        {
            get
            {
                return _description;
            }
            set
            {
                if (value != _description)
                {
                    _description = value;
                    OnPropertyChangedWithValue(value, "Description");
                }
            }
        }

        [DataSourceProperty]
        public string SpriteName
        {
            get
            {
                return _spriteName;
            }
            set
            {
                if (value != _spriteName)
                {
                    _spriteName = value;
                    OnPropertyChangedWithValue(value, "SpriteName");
                }
            }
        }

        [DataSourceProperty]
        public string AbilitySpriteName
        {
            get
            {
                return _abilitySpriteName;
            }
            set
            {
                if (value != _abilitySpriteName)
                {
                    _abilitySpriteName = value;
                    OnPropertyChangedWithValue(value, "AbilitySpriteName");
                }
            }
        }

        [DataSourceProperty]
        public string AbilityName
        {
            get
            {
                return _abilityName;
            }
            set
            {
                if (value != _abilityName)
                {
                    _abilityName = value;
                    OnPropertyChangedWithValue(value, "AbilityName");
                }
            }
        }

        [DataSourceProperty]
        public MBBindingList<CareerAbilityEffectVM> AbilityEffects
        {
            get
            {
                return _abilityDescription;
            }
            set
            {
                if (value != _abilityDescription)
                {
                    _abilityDescription = value;
                    OnPropertyChangedWithValue(value, "AbilityEffects");
                }
            }
        }

        [DataSourceProperty]
        public MBBindingList<CareerChoiceGroupObjectVM> ChoiceGroupsTier1
        {
            get
            {
                return _choiceGroups1;
            }
            set
            {
                if (value != _choiceGroups1)
                {
                    _choiceGroups1 = value;
                    OnPropertyChangedWithValue(value, "ChoiceGroupsTier1");
                }
            }
        }

        [DataSourceProperty]
        public MBBindingList<CareerChoiceGroupObjectVM> ChoiceGroupsTier2
        {
            get
            {
                return _choiceGroups2;
            }
            set
            {
                if (value != _choiceGroups2)
                {
                    _choiceGroups2 = value;
                    OnPropertyChangedWithValue(value, "ChoiceGroupsTier2");
                }
            }
        }

        [DataSourceProperty]
        public MBBindingList<CareerChoiceGroupObjectVM> ChoiceGroupsTier3
        {
            get
            {
                return _choiceGroups3;
            }
            set
            {
                if (value != _choiceGroups3)
                {
                    _choiceGroups3 = value;
                    OnPropertyChangedWithValue(value, "ChoiceGroupsTier3");
                }
            }
        }

        [DataSourceProperty]
        public string ChoiceGroup1Name
        {
            get
            {
                return _choiceGroup1Name;
            }
            set
            {
                if (value != _choiceGroup1Name)
                {
                    _choiceGroup1Name = value;
                    OnPropertyChangedWithValue(value, "ChoiceGroup1Name");
                }
            }
        }

        [DataSourceProperty]
        public string ChoiceGroup2Name
        {
            get
            {
                return _choiceGroup2Name;
            }
            set
            {
                if (value != _choiceGroup2Name)
                {
                    _choiceGroup2Name = value;
                    OnPropertyChangedWithValue(value, "ChoiceGroup2Name");
                }
            }
        }

        [DataSourceProperty]
        public string ChoiceGroup3Name
        {
            get
            {
                return _choiceGroup3Name;
            }
            set
            {
                if (value != _choiceGroup3Name)
                {
                    _choiceGroup3Name = value;
                    OnPropertyChangedWithValue(value, "ChoiceGroup3Name");
                }
            }
        }

        [DataSourceProperty]
        public string ChoiceGroup1Condition
        {
            get
            {
                return _choiceGroup1Condition;
            }
            set
            {
                if (value != _choiceGroup1Condition)
                {
                    _choiceGroup1Condition = value;
                    OnPropertyChangedWithValue(value, "ChoiceGroup1Condition");
                }
            }
        }

        [DataSourceProperty]
        public string ChoiceGroup2Condition
        {
            get
            {
                return _choiceGroup2Condition;
            }
            set
            {
                if (value != _choiceGroup2Condition)
                {
                    _choiceGroup2Condition = value;
                    OnPropertyChangedWithValue(value, "ChoiceGroup2Condition");
                }
            }
        }
        [DataSourceProperty]
        public string ChoiceGroup3Condition
        {
            get
            {
                return _choiceGroup3Condition;
            }
            set
            {
                if (value != _choiceGroup3Condition)
                {
                    _choiceGroup3Condition = value;
                    OnPropertyChangedWithValue(value, "ChoiceGroup3Condition");
                }
            }
        }
        [DataSourceProperty]
        public string ChoiceGroup1Unlock
        {
            get
            {
                return _choiceGroup1Unlock;
            }
            set
            {
                if (value != _choiceGroup1Unlock)
                {
                    _choiceGroup1Unlock = value;
                    OnPropertyChangedWithValue(value, "ChoiceGroup1Unlock");
                }
            }
        }
        [DataSourceProperty]
        public string ChoiceGroup2Unlock
        {
            get
            {
                return _choiceGroup2Unlock;
            }
            set
            {
                if (value != _choiceGroup2Unlock)
                {
                    _choiceGroup2Unlock = value;
                    OnPropertyChangedWithValue(value, "ChoiceGroup2Unlock");
                }
            }
        }
        [DataSourceProperty]
        public string ChoiceGroup3Unlock
        {
            get
            {
                return _choiceGroup3Unlock;
            }
            set
            {
                if (value != _choiceGroup3Unlock)
                {
                    _choiceGroup3Unlock = value;
                    OnPropertyChangedWithValue(value, "ChoiceGroup3Unlock");
                }
            }
        }
        [DataSourceProperty]
        public string FreeCareerPoints
        {
            get
            {
                return _freeCareerPoints;
            }
            set
            {
                if (value != _freeCareerPoints)
                {
                    _freeCareerPoints = value;
                    OnPropertyChangedWithValue(value, "FreeCareerPoints");
                }
            }
        }

        [DataSourceProperty]
        public bool Tier1Active
        {
            get
            {
                return _tier1Active;
            }
            set
            {
                if (value != _tier1Active)
                {
                    _tier1Active = value;
                    OnPropertyChangedWithValue(value, "Tier1Active");
                }
            }
        }

        [DataSourceProperty]
        public bool Tier2Active
        {
            get
            {
                return _tier2Active;
            }
            set
            {
                if (value != _tier2Active)
                {
                    _tier2Active = value;
                    OnPropertyChangedWithValue(value, "Tier2Active");
                }
            }
        }

        [DataSourceProperty]
        public bool Tier3Active
        {
            get
            {
                return _tier3Active;
            }
            set
            {
                if (value != _tier3Active)
                {
                    _tier3Active = value;
                    OnPropertyChangedWithValue(value, "Tier3Active");
                }
            }
        }
    }
}
