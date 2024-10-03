using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ViewModelCollection;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TOR_Core.AbilitySystem.SpellBook;
using TOR_Core.AbilitySystem.Spells.Prayers;
using TOR_Core.Extensions;

namespace TOR_Core.CharacterDevelopment.CareerSystem
{
    public class CareerScreenVM : ViewModel
    {
        private Action _closeAction;
        private CareerObjectVM _currentCareerVM;
        private bool _hasBattlePrayers;

        public CareerScreenVM(Action closeAction)
        {
            _closeAction = closeAction;
            _currentCareerVM = new CareerObjectVM(Hero.MainHero.GetCareer()); 
            HasBattlePrayers = CareerHelper.IsPriestCareer(Hero.MainHero.GetCareer());
        }

        private void ExecuteClose()
        {
            _closeAction();
        }

        private void OpenBattlePrayers()
        {
            var state = Game.Current.GameStateManager.CreateState<BattlePrayerBookState>();
            Game.Current.GameStateManager.PushState(state);
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
        
        [DataSourceProperty]
        public bool HasBattlePrayers
        {
            get
            {
                return this._hasBattlePrayers;
            }
            set
            {
                if (value != this._hasBattlePrayers)
                {
                    this._hasBattlePrayers = value;
                    base.OnPropertyChangedWithValue(value, "HasBattlePrayers");
                }
            }
        }
    }
}
