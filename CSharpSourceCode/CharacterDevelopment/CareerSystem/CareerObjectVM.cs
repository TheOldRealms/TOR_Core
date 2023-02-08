using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.Library;

namespace TOR_Core.CharacterDevelopment.CareerSystem
{
    public class CareerObjectVM : ViewModel
    {
        private string _name;
        private string _spriteName;
        private CareerObject _career;
        private Action<CareerObject> _onSelected;
        private bool _isDisabled;
        private bool _isSelected;
        private MBBindingList<CareerChoiceGroupObjectVM> _choiceGroups1;
        private MBBindingList<CareerChoiceGroupObjectVM> _choiceGroups2;
        private MBBindingList<CareerChoiceGroupObjectVM> _choiceGroups3;

        public CareerObjectVM(CareerObject career, Action<CareerObject> onSelected)
        {
            _career = career;
            _onSelected = onSelected;
            _name = career.Name.ToString();
            _spriteName = "CareerSystem\\Illustrations\\" + career.StringId;

            _choiceGroups1 = new MBBindingList<CareerChoiceGroupObjectVM>();
            _choiceGroups2 = new MBBindingList<CareerChoiceGroupObjectVM>();
            _choiceGroups3 = new MBBindingList<CareerChoiceGroupObjectVM>();

            foreach(var group in _career.ChoiceGroups)
            {
                switch (group.Tier)
                {
                    case 1:
                        _choiceGroups1.Add(new CareerChoiceGroupObjectVM(group));
                        break;
                    case 2:
                        _choiceGroups2.Add(new CareerChoiceGroupObjectVM(group));
                        break;
                    case 3:
                        _choiceGroups3.Add(new CareerChoiceGroupObjectVM(group));
                        break;
                    default:
                        break;
                }
            }
        }

        private void ExecuteSelect()
        {
            if (_onSelected != null) _onSelected(_career);
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
        public string SpriteName
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
                    OnPropertyChangedWithValue(value, "SpriteName");
                }
            }
        }

        [DataSourceProperty]
        public bool IsDisabled
        {
            get
            {
                return _isDisabled;
            }
            set
            {
                if (value != _isDisabled)
                {
                    _isDisabled = value;
                    OnPropertyChangedWithValue(value, "IsDisabled");
                }
            }
        }

        [DataSourceProperty]
        public bool IsSelected
        {
            get
            {
                return _isSelected;
            }
            set
            {
                if (value != _isSelected)
                {
                    _isSelected = value;
                    OnPropertyChangedWithValue(value, "IsSelected");
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
    }
}
