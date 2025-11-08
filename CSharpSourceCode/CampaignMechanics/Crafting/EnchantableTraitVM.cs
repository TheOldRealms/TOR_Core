using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;
using TaleWorlds.Localization;
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
            TraitName = trait.ItemTraitName;
            IconName = trait.IconName;
            ItemTraitDescriptionHint = new BasicTooltipViewModel(GetHintText);
            ItemTraitDescription = new TextObject(trait.ItemTraitDescription).ToString();
        }

        private string GetHintText() => string.IsNullOrEmpty(_trait.ItemTraitDescription) ? "No description available." : _trait.ItemTraitDescription;

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
