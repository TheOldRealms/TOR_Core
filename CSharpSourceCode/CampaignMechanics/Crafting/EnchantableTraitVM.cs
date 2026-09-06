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
        private bool _isEnabled;
        private string _unmetRequirement;
        public ItemTrait ItemTrait => _trait;

        /// <summary>
        /// Why this enchantment cannot be applied right now, or null if it can. A known
        /// blueprint with an unmet requirement is shown greyed with the reason rather than
        /// hidden, so the player can see what to work toward.
        /// </summary>
        public string UnmetRequirement => _unmetRequirement;

        public EnchantableTraitVM(ItemTrait trait, Action<EnchantableTraitVM, bool> onSelected, string unmetRequirement = null)
        {
            _trait = trait;
            _onSelected = onSelected;
            _unmetRequirement = unmetRequirement;
            IsSelected = false;
            IsEnabled = unmetRequirement == null;
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

            if (!string.IsNullOrEmpty(_unmetRequirement))
            {
                text += "\n\n" + _unmetRequirement;
            }

            return text;
        }

        private void ExecuteSelectTrait()
        {
            // The prefab greys the row out, but a disabled ButtonWidget can still route a
            // click in some navigation paths, so refuse it here too rather than trusting the
            // view to be the only gate.
            if (!IsEnabled) return;

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
        public bool IsEnabled
        {
            get => _isEnabled;
            set
            {
                if (_isEnabled != value)
                {
                    _isEnabled = value;
                    OnPropertyChangedWithValue(value, "IsEnabled");
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