using System;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.ViewModelCollection.Inventory;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;
using TOR_Core.Items.InventoryUseScripts;
using static Helpers.InventoryScreenHelper;

namespace TOR_Core.Extensions.UI
{
    [ViewModelExtension(typeof(SPItemVM), "UpdateCanBeSlaughtered")]
    public class SPItemVMExtension : BaseViewModelExtension
    {
        private static HintViewModel _sharedUseHint;
        private HintViewModel _useHint;
        private bool _isUsableItem;

        public SPItemVMExtension(ViewModel vm) : base(vm)
        {
            _sharedUseHint ??= new HintViewModel(TORTextHelper.GetTextObject("tor_item_hint_use", "Use Item"));
            _useHint = _sharedUseHint;
        }

        public override void RefreshValues()
        {
            var selectedItem = ((SPItemVM)_vm).ItemRosterElement.EquipmentElement.Item;
            IsUsableItem = selectedItem.IsInventoryUsable() && GetActiveInventoryState().InventoryMode == InventoryMode.Default;
        }

        private void ExecuteUseItem()
        {
            var selectedItem = ((SPItemVM)_vm).ItemRosterElement.EquipmentElement.Item;
            foreach (var trait in selectedItem.GetTraits())
            {
                if (trait.OnInventoryUseScript != null && !string.IsNullOrWhiteSpace(trait.OnInventoryUseScript.InventoryScriptName) && trait.OnInventoryUseScript.InventoryScriptName != "invalid")
                {
                    object script;
                    if (trait.OnInventoryUseScript.InventoryScriptArguments != null && trait.OnInventoryUseScript.InventoryScriptArguments.Count > 0)
                    {
                        script = Activator.CreateInstance(Type.GetType(trait.OnInventoryUseScript.InventoryScriptName), [trait.OnInventoryUseScript.InventoryScriptArguments.ToArray()]);
                    }
                    else
                    {
                        script = Activator.CreateInstance(Type.GetType(trait.OnInventoryUseScript.InventoryScriptName));
                    }
                    if (script is BaseInventoryUseScript inventoryUseScript)
                    {
                        inventoryUseScript.UseScript(MobileParty.MainParty, selectedItem);
                    }
                }
            }
        }

        [DataSourceProperty]
        public HintViewModel UseHint
        {
            get
            {
                return this._useHint;
            }
            set
            {
                if (value != this._useHint)
                {
                    this._useHint = value;
                    _vm.OnPropertyChangedWithValue(value, "UseHint");
                }
            }
        }

        [DataSourceProperty]
        public bool IsUsableItem
        {
            get
            {
                return this._isUsableItem;
            }
            set
            {
                if (value != this._isUsableItem)
                {
                    this._isUsableItem = value;
                    _vm.OnPropertyChangedWithValue(value, "IsUsableItem");
                }
            }
        }
    }
}
