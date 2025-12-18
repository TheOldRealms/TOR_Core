using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TOR_Core.Extensions;
using TOR_Core.Items;
using TOR_Core.Models;
using TOR_Core.Utilities;

namespace TOR_Core.CampaignMechanics.Crafting
{
    public class EnchantingVM : ViewModel
    {
        private readonly Action _closeAction;
        private MBBindingList<EnchantableItemVM> _items = [];
        private EnchantableItemVM _selectedItem;
        private EnchantingItemTableauVM _itemTableau;
        private bool _isPreviewEnabled;
        private string _screenTitle;
        private MBBindingList<EnchantableTraitVM> _traits = [];
        private MBBindingList<EnchantableTraitVM> _selectedTraits = [];
        private MBBindingList<EnchantingIngredientVM> _ingredients = [];

        public EnchantingVM(Action closeScreen)
        {
            _closeAction = closeScreen;
            RefreshValues();
        }

        public override void RefreshValues()
        {
            ScreenTitle = Hero.MainHero?.Culture?.StringId == TORConstants.Cultures.DAWI ? TORTextHelper.GetText("tor_runesmithing_title", "Runesmithing") : TORTextHelper.GetText("tor_enchanting_title", "Enchanting");
            Items.Clear();
            Traits.Clear();
            SelectedTraits.Clear();
            Ingredients.Clear();
            ItemTableau = new EnchantingItemTableauVM();
            IsPreviewEnabled = false;
            foreach (var item in MobileParty.MainParty.ItemRoster)
            {
                if (item.EquipmentElement.Item.IsEnchantable())
                {
                    EnchantableItemVM enchantableItem = new(item.EquipmentElement, OnItemSelected);
                    Items.Add(enchantableItem);
                }
            }
            foreach (var ingredient in TorEnchantingIngredients.All)
            {
                Ingredients.Add(new EnchantingIngredientVM(ingredient.Key, ingredient.Value, MobileParty.MainParty.ItemRoster.GetItemNumber(ingredient.Value)));
            }
        }

        private void OnTraitSelected(EnchantableTraitVM itemTrait, bool isSelected)
        {
            if (isSelected)
            {
                var maxTraits = CalculateMaxTraits();
                if (SelectedTraits.Count >= maxTraits)
                {
                    var selectHint = GameTexts.FindText("tor_enchant_hint_max_traits_selectable");
                    selectHint.SetTextVariable("MAX_TRAITS", maxTraits);
                    InformationManager.ShowInquiry(new InquiryData(TORTextHelper.GetText("tor_enchanting_title_text", "Enchanting"), selectHint.ToString(), true, false, TORTextHelper.GetText("tor_inquiry_ok_text", "OK"), null, null, null), true);
                    itemTrait.DeselectTrait();
                    return;
                }
                if (!SelectedTraits.Contains(itemTrait))
                {
                    SelectedTraits.Add(itemTrait);
                    var ingredient = Ingredients.FirstOrDefault(x => x.IngredientType == itemTrait.ItemTrait.IngredientItem);
                    ingredient?.AddPendingAmount(-CalculateIngredientAmount(itemTrait.ItemTrait));
                }
            }
            else
            {
                if (SelectedTraits.Contains(itemTrait))
                {
                    SelectedTraits.Remove(itemTrait);
                    var ingredient = Ingredients.FirstOrDefault(x => x.IngredientType == itemTrait.ItemTrait.IngredientItem);
                    ingredient?.AddPendingAmount(CalculateIngredientAmount(itemTrait.ItemTrait));
                }
            }
            TORArtisanDistrictCampaignBehavior.Instance.ItemBeingCrafted = new TorItemBeingCraftedData { EquipmentElement = SelectedItem.Item, ItemTraits = SelectedTraits.Select(x => x.ItemTrait).ToList() };
            ItemTableau.FillFrom(SelectedItem.Item);
        }

        private int CalculateMaxTraits()
        {
            var traitAmount = 1;
            var model = (TOREnchantmentCraftingModel)Campaign.Current.Models.GetGameModels().FirstOrDefault(x => x.GetType() == typeof(TOREnchantmentCraftingModel));
            if (model != null)
            {
                traitAmount = model.MaximumAmountOfEnchantments(PartyBase.MainParty.MobileParty.GetMemberHeroes());
            }

            return traitAmount;
        }

        private int CalculateIngredientAmount(ItemTrait itemTrait)
        {
            var cost = 0;
            cost = itemTrait.IngredientAmount;
            var model = (TOREnchantmentCraftingModel)Campaign.Current.Models.GetGameModels().FirstOrDefault(x => x.GetType() == typeof(TOREnchantmentCraftingModel));
            if (model != null)
            {
                cost = model.GetEffectiveIngredientAmount(PartyBase.MainParty.MobileParty.GetMemberHeroes(), itemTrait,
                    itemTrait.IngredientItem);
            }

            return cost;
        }

        private void OnItemSelected(EnchantableItemVM item)
        {
            Traits.Clear();
            SelectedTraits.Clear();
            SelectedItem?.DeselectItem();
            SelectedItem = item;
            foreach (var hero in MobileParty.MainParty.GetMemberHeroes())
            {
                var traits = hero.GetExtendedInfo().KnownEnchantmentBlueprints;

                foreach (var trait in traits)
                {
                    foreach (var x in ItemTrait.All)
                    {
                        if (x.ItemTraitStringId == trait)
                        {
                            var he = hero.HasKnownEnchantmentBlueprint(x.ItemTraitStringId);
                            var ve = ItemTrait.IsValidFor(x, item.Item.Item.ItemType);
                        }

                    }

                }

                foreach (var trait in ItemTrait.All.Where(x => x.IsCraftable &&
                                                               hero.HasKnownEnchantmentBlueprint(x.ItemTraitStringId) &&
                                                               ItemTrait.IsValidFor(x, item.Item.Item.ItemType))
                             .OrderBy(y => y.ItemTraitName))
                {
                    Traits.Add(new EnchantableTraitVM(trait, OnTraitSelected));
                }
            }

            foreach (var ingredient in Ingredients)
            {
                ingredient.ResetPendingAmount();
            }
            TORArtisanDistrictCampaignBehavior.Instance.ItemBeingCrafted = new TorItemBeingCraftedData { EquipmentElement = item.Item, ItemTraits = SelectedTraits.Select(x => x.ItemTrait).ToList() };
            ItemTableau.FillFrom(item.Item);
            IsPreviewEnabled = true;
        }

        private void ExecuteEnchant()
        {
            if (Traits.Count <= 0)
            {
                InformationManager.ShowInquiry(new InquiryData(TORTextHelper.GetText("tor_enchanting_title_text", "Enchanting"), TORTextHelper.GetText("tor_enchanting_no_blueprints_text", "You don't know any enchantment blueprints."), true, false, TORTextHelper.GetText("tor_inquiry_ok_text", "OK"), null, null, null), true);
                return;
            }
            else if (SelectedTraits.Count == 0)
            {
                InformationManager.ShowInquiry(new InquiryData(TORTextHelper.GetText("tor_enchanting_title_text", "Enchanting"), TORTextHelper.GetText("tor_enchanting_no_traits_selected_text", "No traits are selected."), true, false, TORTextHelper.GetText("tor_inquiry_ok_text", "OK"), null, null, null), true);
                return;
            }
            else if (Ingredients.Any(x => Math.Abs(x.PendingAmount) > x.CurrentAmount))
            {
                InformationManager.ShowInquiry(new InquiryData(TORTextHelper.GetText("tor_enchanting_title_text", "Enchanting"), TORTextHelper.GetText("tor_enchanting_missing_ingredients_text", "Missing ingredients."), true, false, TORTextHelper.GetText("tor_inquiry_ok_text", "OK"), null, null, null), true);
                return;
            }
            else
            {
                var item = _selectedItem.Item.Item;
                var modifier = _selectedItem.Item.ItemModifier;
                InformationManager.ShowTextInquiry(new TextInquiryData(TORTextHelper.GetText("tor_enchanting_title_text", "Enchanting"), TORTextHelper.GetText("tor_enchanting_enter_name_text", "Enter name for your enchanted item"), true, true, TORTextHelper.GetText("tor_inquiry_ok_text", "OK"), TORTextHelper.GetText("tor_inquiry_cancel_text", "Cancel"), (name) => CreateNewItem(name, item, modifier), () => InformationManager.HideInquiry()));
            }
        }

        private void CreateNewItem(string newItemName, ItemObject item, ItemModifier modifier)
        {
            List<string> traits = _selectedTraits.Select(x => x.ItemTrait.ItemTraitStringId).ToList();
            var newItem = EnchantmentHelper.CreateEnchantedItem(item, traits, newItemName, true, modifier);

            if (newItem == null)
            {
                InformationManager.ShowInquiry(new InquiryData(TORTextHelper.GetText("tor_enchanting_title_text", "Enchanting"), TORTextHelper.GetText("tor_enchanting_failed_text", "Enchanting failed."), true, false, TORTextHelper.GetText("tor_inquiry_ok_text", "OK"), null, null, null), true);
                return;
            }
            var oldItem = new EquipmentElement(item, modifier);
            var equipmentItem = new EquipmentElement(newItem, modifier);
            MobileParty.MainParty.ItemRoster.AddToCounts(equipmentItem, 1);

            InformationManager.ShowInquiry(new InquiryData(TORTextHelper.GetText("tor_enchanting_title_text", "Enchanting"), TORTextHelper.GetText("tor_enchanting_success_text", "Enchanting successful. Item was added to inventory."), true, false, TORTextHelper.GetText("tor_inquiry_ok_text", "OK"), null, null, null), true);
            MobileParty.MainParty.ItemRoster.AddToCounts(oldItem, -1);

            foreach (var ingredient in Ingredients.Where(x => x.PendingAmount != 0))
            {
                MobileParty.MainParty.ItemRoster.AddToCounts(ingredient.Item, ingredient.PendingAmount);
            }

            RefreshValues();
        }

        private void ExecuteClose()
        {
            TORArtisanDistrictCampaignBehavior.Instance.ItemBeingCrafted = null;
            _closeAction();
        }

        [DataSourceProperty]
        public MBBindingList<EnchantableItemVM> Items
        {
            get => _items;
            set
            {
                if (_items != value)
                {
                    _items = value;
                    OnPropertyChangedWithValue(value, "Items");
                }
            }
        }

        [DataSourceProperty]
        public MBBindingList<EnchantingIngredientVM> Ingredients
        {
            get => _ingredients;
            set
            {
                if (_ingredients != value)
                {
                    _ingredients = value;
                    OnPropertyChangedWithValue(value, "Ingredients");
                }
            }
        }

        [DataSourceProperty]
        public EnchantableItemVM SelectedItem
        {
            get => _selectedItem;
            set
            {
                if (_selectedItem != value)
                {
                    _selectedItem = value;
                    OnPropertyChangedWithValue(value, "SelectedItem");
                }
            }
        }

        [DataSourceProperty]
        public EnchantingItemTableauVM ItemTableau
        {
            get => _itemTableau;
            set
            {
                if (_itemTableau != value)
                {
                    _itemTableau = value;
                    OnPropertyChangedWithValue(value, "ItemTableau");
                }
            }
        }

        [DataSourceProperty]
        public bool IsPreviewEnabled
        {
            get => _isPreviewEnabled;
            set
            {
                if (_isPreviewEnabled != value)
                {
                    _isPreviewEnabled = value;
                    OnPropertyChangedWithValue(value, "IsPreviewEnabled");
                }
            }
        }

        [DataSourceProperty]
        public MBBindingList<EnchantableTraitVM> Traits
        {
            get => _traits;
            set
            {
                if (_traits != value)
                {
                    _traits = value;
                    OnPropertyChangedWithValue(value, "Traits");
                }
            }
        }

        [DataSourceProperty]
        public MBBindingList<EnchantableTraitVM> SelectedTraits
        {
            get => _selectedTraits;
            set
            {
                if (_selectedTraits != value)
                {
                    _selectedTraits = value;
                    OnPropertyChangedWithValue(value, "SelectedTraits");
                }
            }
        }

        [DataSourceProperty]
        public string ScreenTitle
        {
            get => _screenTitle;
            set
            {
                if (_screenTitle != value)
                {
                    _screenTitle = value;
                    OnPropertyChangedWithValue(value, "ScreenTitle");
                }
            }
        }
    }
}