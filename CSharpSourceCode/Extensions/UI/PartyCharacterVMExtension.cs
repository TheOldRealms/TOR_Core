using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.ViewModelCollection.Encyclopedia.Pages;
using TaleWorlds.CampaignSystem.ViewModelCollection.Party;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;
using TaleWorlds.ScreenSystem;
using TOR_Core.CampaignMechanics;
using TOR_Core.CharacterDevelopment.CareerSystem;
using TOR_Core.Utilities;

namespace TOR_Core.Extensions.UI
{
    [ViewModelExtension(typeof(PartyCharacterVM))]
    public class PartyCharacterVMExtension : BaseViewModelExtension
    {
        private bool _shouldButtonBeVisible;
        private bool _isButtonEnabled;
        private BasicTooltipViewModel _buttonHint;
        private bool _shouldButtonBeVisible2;
        private string _spriteTORButton;

        public PartyCharacterVMExtension(ViewModel vm) : base(vm)
        {
            _buttonHint = new BasicTooltipViewModel(GetButtonHintText);
            RefreshValues();
            
        }

        public override void RefreshValues()
        {
            //Update datasource properties here. Gets called every time the base ViewModel would get refreshed.
            //Careful to always update the property, not the field behind it directly, because then the engine won't get notified and events won't be raised.
            var troop = ( (PartyCharacterVM)_vm ).Troop.Character;
            if(troop==null) return;
            var showButton= CareerHelper.ConditionsMetToShowSuperButton(troop);

            ShouldButtonBeVisible = showButton;
            IsButtonEnabled = showButton && CareerHelper.ConditionsMetToEnableSuperButton(troop);
            
            

            ShouldButtonBeVisible2 = false;
            //General\Mission\PersonalKillfeed\kill_feed_skull
            SpriteTORButton = "winds_icon_45";



           
        }

       



        private string GetButtonHintText()
        {
            //This method will get called every time the hint shows up, so you can dynamically construct the hint text based on context, not just static text
            return "This is Z3rca's button. This text will show up as hint.";
        }

        public void ExecuteButtonClick()
        {
            var troop = ( (PartyCharacterVM)_vm ).Troop.Character;
            SpecialbuttonEventManagerHandler.Instance.OnButtonClicked(troop.StringId);
            
           
            TORCommon.Say("Button clicked.");
        }
        
        [DataSourceProperty]
        public string SpriteTORButton
        {
            get
            {
                return _spriteTORButton;
            }
            set
            { 
                _spriteTORButton=value;
                _vm.OnPropertyChangedWithValue(value, "SpriteTORButton");
            }
        }
        
        [DataSourceProperty]
        public bool ShouldButtonBeVisible2
        {
            get
            {
                return _shouldButtonBeVisible2;
            }
            set
            {
                if (value != _shouldButtonBeVisible)
                {
                    _shouldButtonBeVisible2 = value;
                    _vm.OnPropertyChangedWithValue(value, "ShouldButtonBeVisible2");
                }
            }
        }
        
        [DataSourceProperty]
        public bool ShouldButtonBeVisible
        {
            get
            {
                return _shouldButtonBeVisible;
            }
            set
            {
                if (value != _shouldButtonBeVisible)
                {
                    _shouldButtonBeVisible = value;
                    _vm.OnPropertyChangedWithValue(value, "ShouldButtonBeVisible");
                }
            }
        }

        [DataSourceProperty]
        public bool IsButtonEnabled
        {
            get
            {
                return _isButtonEnabled;
            }
            set
            {
                if (value != _isButtonEnabled)
                {
                    _isButtonEnabled = value;
                    _vm.OnPropertyChangedWithValue(value, "IsButtonEnabled");
                }
            }
        }

        [DataSourceProperty]
        public BasicTooltipViewModel ButtonHint
        {
            get
            {
                return _buttonHint;
            }
            set
            {
                if (value != _buttonHint)
                {
                    _buttonHint = value;
                    _vm.OnPropertyChangedWithValue(value, "ButtonHint");
                }
            }
        }
    }

    
}
