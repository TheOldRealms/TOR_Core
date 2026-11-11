using Helpers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ViewModelCollection.Party;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Engine.Screens;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TOR_Core.CampaignMechanics;
using TOR_Core.CharacterDevelopment.CareerSystem;
using TOR_Core.Utilities;
using static Helpers.PartyScreenHelper;

namespace TOR_Core.Extensions.UI
{
    [ViewModelExtension(typeof(PartyCharacterVM))]
    public class PartyCharacterVMExtension : BaseViewModelExtension
    {
        private bool _shouldButtonBeVisible;
        private bool _isButtonEnabled;
        private bool _isTroop;
        private BasicTooltipViewModel _buttonHint;
        private BasicTooltipViewModel _extendedInfoHint;
        private string _spriteTORButton;
        private TextObject disableReason;
        //Sly : not quite sure how to find the input context for the VM to compare with the hotkeys yet.
        //private HotKey _fiveStackModifier = HotKeyManager.GetCategory("GenericCampaignPanelsGameKeyCategory").GetHotKey("FiveStackModifier");
        //private HotKey _entireStackModifier = HotKeyManager.GetCategory("GenericCampaignPanelsGameKeyCategory").GetHotKey("EntireStackModifier");


        public PartyCharacterVMExtension(ViewModel vm) : base(vm)
        {
            _buttonHint = new BasicTooltipViewModel(GetButtonHintText);

            if (Hero.MainHero.HasAnyCareer())
            {

                if (PartyScreenHelper.GetActivePartyState().PartyScreenMode != PartyScreenMode.Normal)
                {
                    SpecialbuttonEventManagerHandler.Instance.Disable();
                }
                else
                {
                    var careerButton = CareerHelper.GetCareerButton();
                    if (careerButton != null)
                    {
                        careerButton.Register();
                    }
                    else
                    {
                        SpecialbuttonEventManagerHandler.Instance.Disable();
                    }
                }
            }

            RefreshValues();
        }

        public override void RefreshValues()
        {
            //Update datasource properties here. Gets called every time the base ViewModel would get refreshed.
            //Careful to always update the property, not the field behind it directly, because then the engine won't get notified and events won't be raised.

            var troopCharacter = ((PartyCharacterVM)_vm).Troop.Character;



            var isPrisoner = ((PartyCharacterVM)_vm).IsPrisonerOfPlayer;
            if (troopCharacter == null) return;

            IsTroop = !troopCharacter.IsHero;

            if (IsTroop)
            {
                var extendedInfoList = TORExtendedInfoHelper.GenererateExtendedTroopInfoToolTip(troopCharacter);
                if (!extendedInfoList.IsEmpty())
                {
                    ExtendedInfoHint = new BasicTooltipViewModel(() => extendedInfoList);
                }
            }

            ShouldButtonBeVisible = SpecialbuttonEventManagerHandler.Instance.ShouldButtonBeVisible(troopCharacter, isPrisoner);

            IsButtonEnabled = SpecialbuttonEventManagerHandler.Instance.ShouldButtonBeActive(troopCharacter, out var displaytext, isPrisoner);
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
            var troop = ((PartyCharacterVM)_vm).Troop.Character;
            var isPrisoner = ((PartyCharacterVM)_vm).IsPrisonerOfPlayer;
            var shiftPress = false;
            
            //Sly : there has to be a better way to detect this because this hotkey can be edited in the xml to something other than shift. Same deal for control. In missions and scenes the inputContext exists which can be queried for GameKeys and Hotkeys, but I don't see something in the vm.
            if (Input.IsKeyDown(InputKey.LeftShift) || Input.IsKeyDown(InputKey.RightShift))
            {
                shiftPress = true;
            }
            SpecialbuttonEventManagerHandler.Instance.OnButtonClicked(troop, isPrisoner, shiftPress);
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
                _spriteTORButton = value;
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
        public BasicTooltipViewModel ExtendedInfoHint
        {
            get => this._extendedInfoHint;
            set
            {
                if (value == this._extendedInfoHint)
                    return;
                this._extendedInfoHint = value;
                this._vm.OnPropertyChangedWithValue(nameof(ExtendedInfoHint));
            }
        }

        [DataSourceProperty]
        public bool IsTroop
        {
            get
            {
                return _isTroop;
            }
            set
            {
                if (value != _isTroop)
                {
                    _isTroop = value;
                    _vm.OnPropertyChangedWithValue(value, "IsTroop");
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