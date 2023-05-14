using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;
using TOR_Core.Extensions;

namespace TOR_Core.CharacterDevelopment.CareerSystem
{
    public class CareerChoiceObjectVM : ViewModel
    {
        private CareerChoiceObject _choice;
        private string _description;
        private string _name;
        private bool _isTaken;
        private bool _isFreeToTake;

        public CareerChoiceObjectVM(CareerChoiceObject choice)
        {
            _choice = choice;
            _name = _choice.Name.ToString();
            _description = _choice.Description.ToString();
            RefreshValues();
        }

        public override void RefreshValues()
        {
            IsTaken = Hero.MainHero.HasCareerChoice(_choice);
            IsFreeToTake = !_isTaken;
        }

        public void SelectChoice()
        {
            if (Hero.MainHero.TryAddCareerChoice(_choice)) RefreshValues();
        }

        public void DeSelectChoice()
        {
            if(Hero.MainHero.TryRemoveCareerChoice(_choice)) RefreshValues();
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
        public bool IsTaken
        {
            get
            {
                return _isTaken;
            }
            set
            {
                if (value != _isTaken)
                {
                    _isTaken = value;
                    OnPropertyChangedWithValue(value, "IsTaken");
                }
            }
        }

        [DataSourceProperty]
        public bool IsFreeToTake
        {
            get
            {
                return _isFreeToTake;
            }
            set
            {
                if (value != _isFreeToTake)
                {
                    _isFreeToTake = value;
                    OnPropertyChangedWithValue(value, "IsFreeToTake");
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
    }
}
