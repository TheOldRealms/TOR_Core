using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ViewModelCollection.Party;
using TaleWorlds.Core.ViewModelCollection.Selector;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.TwoDimension;
using TOR_Core.CampaignMechanics.CustomResources;
using TOR_Core.CampaignMechanics.Formations;
using TOR_Core.Utilities;

namespace TOR_Core.Extensions.UI
{
    [ViewModelExtension(typeof(PartyVM), "RefreshCurrentCharacterInformation")]
    public class PartyVMExtension : BaseViewModelExtension
    {
        public static PartyVM ViewModelInstance { get; private set; }

        private bool _customResourceUpkeepVisible;
        private string _customResourceUpkeepText = string.Empty;
        private HintViewModel _hintViewModel;
        private Sprite _customResourceUpkeepSprite;
        private MBBindingList<PendingResourceCostVM> _pendingResourceCosts;
        private SelectorVM<SelectorItemVM> _characterFormationSelector;
        private bool _torFormationSelectionVisible;

        public PartyVMExtension(ViewModel vm) : base(vm)
        {
            var partyVm = vm as PartyVM;
            ViewModelInstance = partyVm;

            _hintViewModel = new HintViewModel(TORTextHelper.GetTextObject("tor_generic_upkeep", "Upkeep"));
            _pendingResourceCosts = new MBBindingList<PendingResourceCostVM>();
        }

        public override void RefreshValues()
        {
            base.RefreshValues();
            PendingResourceCosts.Clear();

            var pendingResourceCost = CustomResourceManager.GetPendingResources();
            ((PartyVM)(_vm)).CurrentCharacter.InitializeUpgrades();
            foreach (var item in pendingResourceCost)
            {
                MBTextManager.SetTextVariable("PAY_OR_GET", (item.Value > 0) ? 0 : 1);
                MBTextManager.SetTextVariable("TRADE_AMOUNT", MathF.Abs(item.Value));
                string text = item.Value == 0 ? "" : GameTexts.FindText("str_customresource_trade_label", null).ToString() + item.Key.GetCustomResourceIconAsText();
                PendingResourceCosts.Add(new PendingResourceCostVM(text));
            }

            var partyVm = (PartyVM)_vm;
            var troop = partyVm.CurrentCharacter.Character;
            TORFormationSelectionVisible = TORFormationAssignments.HasPlayerCultureFormations && partyVm.IsCurrentCharacterFormationEnabled;
            if (TORFormationSelectionVisible)
            {
                if (_characterFormationSelector == null)
                {
                    CharacterFormationSelector = new SelectorVM<SelectorItemVM>(0, OnFormationSelected);
                    for (var i = 0; i < 8; i++)
                    {
                        var icon = $"<img src=\"{TORFormationAssignments.GetFormationIcon(i)}\"/>";
                        var name = GameTexts.FindText("tor_formation", $"{TORFormationAssignments.FormationCultureKey}_{i + 1}");
                        _characterFormationSelector.AddItem(new SelectorItemVM(icon, name));
                    }
                }

                _characterFormationSelector.SetOnChangeAction(null);
                _characterFormationSelector.SelectedIndex = (int)TORFormationAssignments.GetFormation(troop);
                _characterFormationSelector.SetOnChangeAction(OnFormationSelected);
            }
            if (troop.HasCustomResourceUpkeepRequirement())
            {
                CustomResourceUpkeepVisible = true;
                var resource = troop.GetCustomResourceRequiredForUpkeep();
                CustomResourceUpkeepText = resource.Item2.ToString();
                CustomResourceUpkeepSprite = UIResourceManager.SpriteData.GetSprite(resource.Item1.LargeIconName);
            }
            else
            {
                CustomResourceUpkeepVisible = false;
            }
        }

        private void OnFormationSelected(SelectorVM<SelectorItemVM> selector)
        {
            Campaign.Current.SetPlayerFormationPreference(((PartyVM)_vm).CurrentCharacter.Character, (FormationClass)selector.SelectedIndex);
        }

        [DataSourceProperty]
        public SelectorVM<SelectorItemVM> CharacterFormationSelector
        {
            get => _characterFormationSelector;
            private set
            {
                _characterFormationSelector = value;
                _vm.OnPropertyChangedWithValue(value, "CharacterFormationSelector");
            }
        }

        [DataSourceProperty]
        public bool TORFormationSelectionVisible
        {
            get => _torFormationSelectionVisible;
            private set
            {
                if (_torFormationSelectionVisible != value)
                {
                    _torFormationSelectionVisible = value;
                    _vm.OnPropertyChangedWithValue(value, "TORFormationSelectionVisible");
                }
            }
        }

        public override void OnFinalize()
        {
            base.OnFinalize();
            ViewModelInstance = null;
        }

        [DataSourceProperty]
        public bool CustomResourceUpkeepVisible
        {
            get
            {
                return this._customResourceUpkeepVisible;
            }
            set
            {
                if (value != this._customResourceUpkeepVisible)
                {
                    this._customResourceUpkeepVisible = value;
                    _vm.OnPropertyChangedWithValue(value, "CustomResourceUpkeepVisible");
                }
            }
        }

        [DataSourceProperty]
        public string CustomResourceUpkeepText
        {
            get
            {
                return this._customResourceUpkeepText;
            }
            set
            {
                if (value != this._customResourceUpkeepText)
                {
                    this._customResourceUpkeepText = value;
                    _vm.OnPropertyChangedWithValue(value, "CustomResourceUpkeepText");
                }
            }
        }

        [DataSourceProperty]
        public HintViewModel CustomResourceUpkeepHint
        {
            get
            {
                return this._hintViewModel;
            }
            set
            {
                if (value != this._hintViewModel)
                {
                    this._hintViewModel = value;
                    _vm.OnPropertyChangedWithValue(value, "CustomResourceUpkeepHint");
                }
            }
        }

        [DataSourceProperty]
        public Sprite CustomResourceUpkeepSprite
        {
            get
            {
                return this._customResourceUpkeepSprite;
            }
            set
            {
                if (value != this._customResourceUpkeepSprite)
                {
                    this._customResourceUpkeepSprite = value;
                    _vm.OnPropertyChangedWithValue(value, "CustomResourceUpkeepSprite");
                }
            }
        }

        [DataSourceProperty]
        public MBBindingList<PendingResourceCostVM> PendingResourceCosts
        {
            get
            {
                return this._pendingResourceCosts;
            }
            set
            {
                if (value != this._pendingResourceCosts)
                {
                    this._pendingResourceCosts = value;
                    _vm.OnPropertyChangedWithValue(value, "PendingResourceCosts");
                }
            }
        }
    }

    public class PendingResourceCostVM : ViewModel
    {
        private string _text;

        public PendingResourceCostVM(string text)
        {
            _text = text;
        }

        [DataSourceProperty]
        public string PendingResourceText
        {
            get
            {
                return this._text;
            }
            set
            {
                if (value != this._text)
                {
                    this._text = value;
                    OnPropertyChangedWithValue(value, "PendingResourceText");
                }
            }
        }
    }
}