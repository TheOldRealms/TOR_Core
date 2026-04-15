using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;
using TaleWorlds.Library;

namespace TOR_Core.CampaignMechanics.Crafting
{
    public class EnchantableItemVM : ViewModel
    {
        private EquipmentElement _item;
        private string _itemName;
        private ImageIdentifierVM _imageIdentifier;
        private bool _isSelected;
        private Action<EnchantableItemVM> _onItemSelected;
        public EquipmentElement Item => _item;

        public EnchantableItemVM(EquipmentElement item, Action<EnchantableItemVM> onItemSelected)
        {
            _item = item;
            _onItemSelected = onItemSelected;
            ItemName = item.GetModifiedItemName().ToString();
            ImageIdentifier = new ItemImageIdentifierVM(item.Item, Clan.PlayerClan?.Banner.Serialize());
            ImageIdentifier.RefreshValues();
        }

        private void ExecuteSelectItem()
        {
            if (!IsSelected)
            {
                IsSelected = true;
                _onItemSelected?.Invoke(this);
            }
        }

        public void DeselectItem()
        {
            IsSelected = false;
        }

        public override void OnFinalize()
        {
            base.OnFinalize();
            _item = default;
            _imageIdentifier = null;
            _onItemSelected = null;
        }

        [DataSourceProperty]
        public string ItemName
        {
            get => _itemName;
            set
            {
                if (_itemName != value)
                {
                    _itemName = value;
                    OnPropertyChangedWithValue(value, "ItemName");
                }
            }
        }

        [DataSourceProperty]
        public ImageIdentifierVM ImageIdentifier
        {
            get => _imageIdentifier;
            set
            {
                if (_imageIdentifier != value)
                {
                    _imageIdentifier = value;
                    OnPropertyChangedWithValue(value, "ImageIdentifier");
                }
            }
        }

        [DataSourceProperty]
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    OnPropertyChangedWithValue(value, "IsSelected");
                }
            }
        }
    }
}