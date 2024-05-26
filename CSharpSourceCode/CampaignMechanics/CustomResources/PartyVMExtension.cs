using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem.ViewModelCollection.Party;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.GauntletUI;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.TwoDimension;
using TOR_Core.Extensions;
using TOR_Core.Extensions.UI;

namespace TOR_Core.CampaignMechanics.CustomResources
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

        public PartyVMExtension(ViewModel vm) : base(vm) 
        {
            var partyVm  = vm as PartyVM;
            ViewModelInstance = partyVm;
            
            _hintViewModel = new HintViewModel(new TextObject("{=upkeep}Upkeep"));
            _pendingResourceCosts = new MBBindingList<PendingResourceCostVM>();
        }

        public override void RefreshValues()
        {
            base.RefreshValues();
            PendingResourceCosts.Clear();

            var pendingResourceCost = CustomResourceManager.GetPendingResources();
            ((PartyVM)(_vm)).CurrentCharacter.InitializeUpgrades();
            foreach(var item in pendingResourceCost)
            {
                MBTextManager.SetTextVariable("PAY_OR_GET", (item.Value > 0) ? 0 : 1);
                MBTextManager.SetTextVariable("TRADE_AMOUNT", MathF.Abs(item.Value));
                string text = item.Value == 0 ? "" : GameTexts.FindText("str_customresource_trade_label", null).ToString() + item.Key.GetCustomResourceIconAsText();
                PendingResourceCosts.Add(new PendingResourceCostVM(text));
            }

            var troop = ((PartyVM)_vm).CurrentCharacter.Character;
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
