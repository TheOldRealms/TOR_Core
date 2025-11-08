using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.ViewModelCollection.CharacterDeveloper;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TOR_Core.AbilitySystem.SpellBook;
using TOR_Core.Extensions.UI;
using TOR_Core.Extensions;

namespace TOR_Core.CharacterDevelopment.CareerSystem
{
    [ViewModelExtension(typeof(CharacterDeveloperVM))]
    public class CharacterDeveloperVMExtension : BaseViewModelExtension
    {
        private bool _hasCareer = false;
        private bool _isSpellCaster = false;

        public CharacterDeveloperVMExtension(ViewModel vm) : base(vm)
        {
            
            HasCareer = Hero.MainHero.HasAnyCareer();
            IsSpellCaster = Hero.MainHero.IsSpellCaster()||Hero.MainHero.PartyBelongedTo!=null&&Hero.MainHero.PartyBelongedTo.GetMemberHeroes().Any(x=> x.IsSpellCaster());
        }

        private void ExecuteNavigateToCareers()
        {
            var characterDeveloperVm = (CharacterDeveloperVM) this._vm;
            if (characterDeveloperVm != null)
            {
                characterDeveloperVm.ExecuteDone();     // saves the changes on the Character. A lot player were confused that their changes were not saved.
            }
            
            var state = Game.Current.GameStateManager.CreateState<CareerScreenGameState>();
            Game.Current.GameStateManager.PushState(state);
        }
        
        private void ExecuteOpenSpells()
        {
            var state = Game.Current.GameStateManager.CreateState<SpellBookState>();
            Game.Current.GameStateManager.PushState(state);
        }

        
        [DataSourceProperty]
        public bool IsSpellCaster
        {
            get
            {
                return _isSpellCaster;
            }
            set
            {
                if (value != _isSpellCaster)
                {
                    _isSpellCaster = value;
                    _vm.OnPropertyChangedWithValue(value, "IsSpellcaster");
                }
            }
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
