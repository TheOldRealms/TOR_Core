﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ViewModelCollection.CharacterDeveloper;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TOR_Core.Extensions.UI;
using TOR_Core.Extensions;

namespace TOR_Core.CharacterDevelopment.CareerSystem
{
    [ViewModelExtension(typeof(CharacterDeveloperVM))]
    public class CharacterDeveloperVMExtension : BaseViewModelExtension
    {
        private bool _hasCareer = false;

        public CharacterDeveloperVMExtension(ViewModel vm) : base(vm)
        {
            HasCareer = Hero.MainHero.HasAnyCareer();
        }

        private void ExecuteNavigateToCareers()
        {
            var state = Game.Current.GameStateManager.CreateState<CareerScreenGameState>();
            Game.Current.GameStateManager.PushState(state);
        }

        [DataSourceProperty]
        public bool HasCareer
        {
            get
            {
                return _hasCareer;
            }
            set
            {
                if (value != _hasCareer)
                {
                    _hasCareer = value;
                    _vm.OnPropertyChangedWithValue(value, "HasCareer");
                }
            }
        }
    }
}
