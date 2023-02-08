using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ViewModelCollection;
using TaleWorlds.Library;

namespace TOR_Core.CharacterDevelopment.CareerSystem
{
    public class CareerScreenVM : ViewModel
    {
        private Action _closeAction;
        private MBBindingList<CareerObjectVM> _careers;
        private HeroViewModel _heroVM;
        private CareerObjectVM _currentCareerVM;

        public CareerScreenVM(Action closeAction)
        {
            _closeAction = closeAction;
            _careers = new MBBindingList<CareerObjectVM>();
            TORCareers.All.ForEach(x => _careers.Add(new CareerObjectVM(x, OnCareerSelected)));
            _heroVM = new HeroViewModel();
            _heroVM.FillFrom(Hero.MainHero);
            _currentCareerVM = _careers[0];
        }

        private void OnCareerSelected(CareerObject career)
        {
            throw new NotImplementedException();
        }

        private void ExecuteClose()
        {
            _closeAction();
        }

        [DataSourceProperty]
        public MBBindingList<CareerObjectVM> Careers
        {
            get
            {
                return _careers;
            }
            set
            {
                if (value != _careers)
                {
                    _careers = value;
                    OnPropertyChangedWithValue(value, "Careers");
                }
            }
        }

        [DataSourceProperty]
        public HeroViewModel HeroVM
        {
            get
            {
                return _heroVM;
            }
            set
            {
                if (value != _heroVM)
                {
                    _heroVM = value;
                    OnPropertyChangedWithValue(value, "Hero");
                }
            }
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
