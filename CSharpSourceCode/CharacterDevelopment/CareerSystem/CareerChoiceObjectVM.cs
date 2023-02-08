using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.Library;

namespace TOR_Core.CharacterDevelopment.CareerSystem
{
    public class CareerChoiceObjectVM : ViewModel
    {
        private CareerChoiceObject _choice;
        private string _name;

        public CareerChoiceObjectVM(CareerChoiceObject choice)
        {
            _choice = choice;
            _name = _choice.Name.ToString();
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
    }
}
