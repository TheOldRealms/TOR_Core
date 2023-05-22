using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TOR_Core.Extensions;

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
        private string _freeCareerPoints;

        public CareerObjectVM(CareerObject career)
        {
            _career = career;
            _name = career.Name.ToString();
            _spriteName = "CareerSystem\\Illustrations\\" + career.StringId;
            _abilitySpriteName = _career.GetAbilityTemplate().SpriteName;
            _abilityName = _career.GetAbilityTemplate().Name;
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
                        _choiceGroup1Condition = group.GetConditionText(Hero.MainHero);
                        break;
                    case 2:
                        _choiceGroups2.Add(new CareerChoiceGroupObjectVM(group, RefreshValues));
                        _choiceGroup2Condition = group.GetConditionText(Hero.MainHero);
                        break;
                    case 3:
                        _choiceGroups3.Add(new CareerChoiceGroupObjectVM(group, RefreshValues));
                        _choiceGroup3Condition = group.GetConditionText(Hero.MainHero);
                        break;
                    default:
                        break;
                }
            }
            _choiceGroup1Name = GameTexts.FindText("career_choicegroup1_name", _career.StringId).ToString();
            _choiceGroup2Name = GameTexts.FindText("career_choicegroup2_name", _career.StringId).ToString();
            _choiceGroup3Name = GameTexts.FindText("career_choicegroup3_name", _career.StringId).ToString();
            RefreshValues();
        }

        public override void RefreshValues()
        {
            var info = Hero.MainHero.GetExtendedInfo();
            if(info != null)
            {
                int usedPoints = info.CareerChoices.Count - 1; //Account for root choice, does not need to be taken into consideration.
                FreeCareerPoints = "Free career points: " + (Hero.MainHero.Level - usedPoints).ToString();
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
    }
}
