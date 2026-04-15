using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;
using TOR_Core.Items;

namespace TOR_Core.CampaignMechanics.Crafting
{
    public class EnchantingIngredientVM : ViewModel
    {
        private readonly ItemObject _ingredient;
        private string _ingredientName;
        private string _ingredientId;
        private int _currentAmount;
        private int _pendingAmount;
        private string _currentAmountString;
        private string _pendingAmountString;
        private BasicTooltipViewModel _ingredientTooltip;
        private readonly TorTradeGoodType _torTradeGoodType = TorTradeGoodType.Invalid;
        private string _itemImageTextureProviderName = "ItemImageTextureProvider";

        public ItemObject Item => _ingredient;
        public TorTradeGoodType IngredientType => _torTradeGoodType;
        public int CurrentAmount => _currentAmount;
        public int PendingAmount => _pendingAmount;

        public EnchantingIngredientVM(TorTradeGoodType ingredientType, ItemObject itemObject, int amount)
        {
            _ingredient = itemObject;
            _torTradeGoodType = ingredientType;
            IngredientName = Item.Name.ToString();
            IngredientId = Item.StringId;
            _currentAmount = amount;
            CurrentAmountText = _currentAmount.ToString();
            _pendingAmount = 0;
            PendingAmountText = _pendingAmount.ToString();
            IngredientTooltip = new BasicTooltipViewModel(GetHintText);
            RefreshValues();
        }

        public override void RefreshValues()
        {
            if (_currentAmount <= 0) CurrentAmountText = " ";
            if (_pendingAmount >= 0) PendingAmountText = " ";
        }

        public void AddCurrentAmount(int amount)
        {
            _currentAmount += amount;
            CurrentAmountText = _currentAmount.ToString();
            RefreshValues();
        }

        public void AddPendingAmount(int amount)
        {
            _pendingAmount += amount;
            PendingAmountText = _pendingAmount.ToString();
            RefreshValues();
        }

        private string GetHintText() => Item.Name.ToString();

        internal void ResetPendingAmount()
        {
            _pendingAmount = 0;
            PendingAmountText = _pendingAmount.ToString();
            RefreshValues();
        }

        [DataSourceProperty]
        public string TextureProviderName
        {
            get => _itemImageTextureProviderName;
            set
            {
                if (_itemImageTextureProviderName != value)
                {
                    _itemImageTextureProviderName = value;
                    OnPropertyChangedWithValue(value, "TextureProviderName");
                }
            }
        }

        [DataSourceProperty]
        public string IngredientName
        {
            get => _ingredientName;
            set
            {
                if (_ingredientName != value)
                {
                    _ingredientName = value;
                    OnPropertyChangedWithValue(value, "IngredientName");
                }
            }
        }

        [DataSourceProperty]
        public string IngredientId
        {
            get => _ingredientId;
            set
            {
                if (_ingredientId != value)
                {
                    _ingredientId = value;
                    OnPropertyChangedWithValue(value, "IngredientId");
                }
            }
        }

        [DataSourceProperty]
        public string CurrentAmountText
        {
            get => _currentAmountString;
            set
            {
                if (_currentAmountString != value)
                {
                    _currentAmountString = value;
                    OnPropertyChangedWithValue(value, "CurrentAmountText");
                }
            }
        }

        [DataSourceProperty]
        public string PendingAmountText
        {
            get => _pendingAmountString;
            set
            {
                if (_pendingAmountString != value)
                {
                    _pendingAmountString = value;
                    OnPropertyChangedWithValue(value, "PendingAmountText");
                }
            }
        }

        [DataSourceProperty]
        public BasicTooltipViewModel IngredientTooltip
        {
            get => _ingredientTooltip;
            set
            {
                if (_ingredientTooltip != value)
                {
                    _ingredientTooltip = value;
                    OnPropertyChangedWithValue(value, "IngredientTooltip");
                }
            }
        }
    }
}