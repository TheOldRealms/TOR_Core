using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ViewModelCollection;
using TaleWorlds.Library;
using TOR_Core.Extensions;

namespace TOR_Core.CharacterDevelopment.CareerSystem
{
    public class CareerScreenVM : ViewModel
    {
        private Action _closeAction;
        private CareerObjectVM _currentCareerVM;

        public CareerScreenVM(Action closeAction)
        {
            _closeAction = closeAction;
            _currentCareerVM = new CareerObjectVM(Hero.MainHero.GetCareer());
        }

        private void ExecuteClose()
        {
            _closeAction();
        }

        [DataSourceProperty]
        public CareerObjectVM CurrentCareer
        {
            get
            {
                return _currentCareerVM;
            }
            set
            {
                if (value != _currentCareerVM)
                {
                    _currentCareerVM = value;
                    OnPropertyChangedWithValue(value, "CurrentCareer");
                }
            }
        }
    }
}
