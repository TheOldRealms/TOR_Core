using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem.ViewModelCollection.CharacterDeveloper;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TOR_Core.Extensions.UI;

namespace TOR_Core.CharacterDevelopment.CareerSystem
{
    [ViewModelExtension(typeof(CharacterDeveloperVM))]
    public class CharacterDeveloperVMExtension : BaseViewModelExtension
    {
        public CharacterDeveloperVMExtension(ViewModel vm) : base(vm)
        {

        }

        private void ExecuteNavigateToCareers()
        {
            var state = Game.Current.GameStateManager.CreateState<CareerScreenGameState>();
            Game.Current.GameStateManager.PushState(state);
        }
    }
}
