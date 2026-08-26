using System;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TOR_Core.Extensions;
using TOR_Core.Items;

namespace TOR_Core.CampaignMechanics.Crafting
{
    public class EnchantableTraitVM : ViewModel
    {
        private bool _isSelected;
        private ItemTrait _trait;
        private string _traitName;
        private BasicTooltipViewModel _itemTraitDescriptionHint;
        private string _itemTraitDescription;
        private Action<EnchantableTraitVM, bool> _onSelected;
        private string _iconName;
        public ItemTrait ItemTrait => _trait;

        public EnchantableTraitVM(ItemTrait trait, Action<EnchantableTraitVM, bool> onSelected)
        {
            _trait = trait;
            _onSelected = onSelected;
            IsSelected = false;
            TraitName = new TextObject(trait.ItemTraitName).ToString();
            IconName = trait.IconName;
            //Text on the right side of the screen that appears under the weapon preview and above the enchantment ingredients.
            ItemTraitDescription = new TextObject(trait.ItemTraitDescription).ToString();
            //Tooltip shown on hover of an enchantment in the vertical list in the middle.
            ItemTraitDescriptionHint = new BasicTooltipViewModel(GetHintText);
        }

        private string GetHintText()
        {
            var text = string.IsNullOrEmpty(ItemTraitDescription) ? TORTextHelper.GetText("tor_enchant_no_description", "No description available.") : ItemTraitDescription;
            return text;
        }

        private void ExecuteSelectTrait()
        {
            IsSelected = !IsSelected;
            _onSelected?.Invoke(this, IsSelected);
        }

        public void DeselectTrait()
        {
            IsSelected = false;
        }

        public override void OnFinalize()
        {
            base.OnFinalize();
            _trait = null;
            _onSelected = null;
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

        [DataSourceProperty]
        public string TraitName
        {
            get => _traitName;
            set
            {
                if (_traitName != value)
                {
                    _traitName = value;
                    OnPropertyChangedWithValue(value, "TraitName");
                }
            }
        }

        [DataSourceProperty]
        public string IconName
        {
            get => _iconName;
            set
            {
                if (_iconName != value)
                {
                    _iconName = value;
                    OnPropertyChangedWithValue(value, "IconName");
                }
            }
        }

        [DataSourceProperty]
        public BasicTooltipViewModel ItemTraitDescriptionHint
        {
            get => _itemTraitDescriptionHint;
            set
            {
                if (_itemTraitDescriptionHint != value)
                {
                    _itemTraitDescriptionHint = value;
                    OnPropertyChangedWithValue(value, "ItemTraitDescriptionHint");
                }
            }
        }

        [DataSourceProperty]
        public string ItemTraitDescription
        {
            get => _itemTraitDescription;
            set
            {
                if (_itemTraitDescription != value)
                {
                    _itemTraitDescription = value;
                    OnPropertyChangedWithValue(value, "ItemTraitDescription");
                }
            }
        }
    }
}