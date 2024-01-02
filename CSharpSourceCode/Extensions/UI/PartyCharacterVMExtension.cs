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
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;
using TaleWorlds.ScreenSystem;
using TOR_Core.CampaignMechanics;
using TOR_Core.CharacterDevelopment.CareerSystem;
using TOR_Core.CharacterDevelopment.CareerSystem.CareerButton;
using TOR_Core.Utilities;

namespace TOR_Core.Extensions.UI
{
    [ViewModelExtension(typeof(PartyCharacterVM))]
    public class PartyCharacterVMExtension : BaseViewModelExtension
    {
        private bool _shouldButtonBeVisible;
        private bool _isButtonEnabled;
        private BasicTooltipViewModel _buttonHint;
        private string _spriteTORButton;
        private TextObject disableReason;
        

        public PartyCharacterVMExtension(ViewModel vm) : base(vm)
        {
            _buttonHint = new BasicTooltipViewModel(GetButtonHintText);

            var career = Hero.MainHero.GetCareer().CareerButtonBehavior;
            if (career != null)
            {
                career.Register();
            }
            else
            {
                SpecialbuttonEventManagerHandler.Instance.Disable();
            }
            
            RefreshValues();
            
        }

        public override void RefreshValues()
        {
            //Update datasource properties here. Gets called every time the base ViewModel would get refreshed.
            //Careful to always update the property, not the field behind it directly, because then the engine won't get notified and events won't be raised.
            var troop = ( (PartyCharacterVM)_vm ).Troop.Character;
            if(troop==null) return;

            ShouldButtonBeVisible = SpecialbuttonEventManagerHandler.Instance.ShouldButtonBeVisible(troop);
            
            var textObect = new TextObject();
            
            
            IsButtonEnabled =  SpecialbuttonEventManagerHandler.Instance.ShouldButtonBeActive(troop, out var displaytext);
            disableReason = displaytext;
            
            SpriteTORButton = CareerHelper.GetButtonSprite();
        }

        private string GetButtonHintText()
        {
            //This method will get called every time the hint shows up, so you can dynamically construct the hint text based on context, not just static text
            return disableReason.ToString();
        }

        public void ExecuteButtonClick()
        {
            var troop = ( (PartyCharacterVM)_vm ).Troop.Character;
            SpecialbuttonEventManagerHandler.Instance.OnButtonClicked(troop);
            
            ((PartyCharacterVM)_vm).RefreshValues();

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
