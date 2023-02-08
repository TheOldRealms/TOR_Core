using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.Library;

namespace TOR_Core.CharacterDevelopment.CareerSystem
{
    public class CareerChoiceGroupObjectVM : ViewModel
    {
        private CareerChoiceGroupObject _choiceGroup;
        private MBBindingList<CareerChoiceObjectVM> _choices;
        private string _groupName;

        public CareerChoiceGroupObjectVM(CareerChoiceGroupObject choiceGroup)
        {
            _choiceGroup = choiceGroup;
            _groupName = _choiceGroup.Name.ToString();
            _choices = new MBBindingList<CareerChoiceObjectVM>();
            choiceGroup.Choices.ForEach(x => _choices.Add(new CareerChoiceObjectVM(x)));
        }

        [DataSourceProperty]
        public string GroupName
        {
            get
            {
                return _groupName;
            }
            set
            {
                if (value != _groupName)
                {
                    _groupName = value;
                    OnPropertyChangedWithValue(value, "GroupName");
                }
            }
        }

        [DataSourceProperty]
        public MBBindingList<CareerChoiceObjectVM> Choices
        {
            get
            {
                return _choices;
            }
            set
            {
                if (value != _choices)
                {
                    _choices = value;
                    OnPropertyChangedWithValue(value, "Choices");
                }
            }
        }
    }
}
