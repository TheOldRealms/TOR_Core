using HarmonyLib;
using Helpers;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;
using TaleWorlds.SaveSystem;
using TOR_Core.CampaignMechanics.TORCustomSettlement;
using TOR_Core.Extensions;
using TOR_Core.Items;
using TOR_Core.Utilities;
using static TaleWorlds.CampaignSystem.CampaignBehaviors.CraftingCampaignBehavior;

namespace TOR_Core.CampaignMechanics.Crafting
{
    public class TORArtisanDistrictCampaignBehavior : CampaignBehaviorBase
    {
        private bool _hasSmithyBeenRemoved;
        private Dictionary<ItemObject, TorItemDuplicationData> _customCraftedItems = [];
        public static TORArtisanDistrictCampaignBehavior Instance => Campaign.Current.GetCampaignBehavior<TORArtisanDistrictCampaignBehavior>();
        public TorItemBeingCraftedData ItemBeingCrafted { get; set; } = null;

        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionStart);
            TORCampaignEvents.Instance.ItemDuplicated += OnItemDuplicated;
            CampaignEvents.OnAfterSessionLaunchedEvent.AddNonSerializedListener(this, AfterSessionLaunched);
        }

        private void OnItemDuplicated(object sender, ItemDuplicatedEventArgs e)
        {
            if (!_customCraftedItems.ContainsKey(e.NewItem))
            {
                var copyfromStringId = e.OldItem.StringId;
                _customCraftedItems.Add(e.NewItem, new TorItemDuplicationData { OriginalItemStringId = e.OldItem.StringId, NewItemName = e.NewItem.Name.ToString(), ItemTraits = e.Traits, IsPlayerCrafted = e.NewItem.IsCraftedByPlayer });
                ExtendedItemObjectManager.AddCraftedItem(copyfromStringId, e.NewItem.StringId, e.Traits);
            }
        }

        private void AfterSessionLaunched(CampaignGameStarter obj)
        {
            var menu = Campaign.Current.GameMenuManager.GetGameMenu("town");
            menu?.RemoveGameMenuOption("town_smithy");
            TORSettlementMenuHelpers.RearrangeTownMenus(menu, "town_artisan", "town_backstreet");
        }


        private void OnSessionStart(CampaignGameStarter starter)
        {
            AccessTools.Property(typeof(ItemObject), "Name").SetValue(DefaultItems.IronIngot6, TORTextHelper.GetTextObject("ironingot6_name", "Gromril{@Plural}loads of gromril{\\@}"));
            TorEnchantingIngredients.LoadIngredients();
            AddTownMenu(starter);

            //this is basically redundant, but left it for the moment. The game triggers calls to validation via CraftingPatches. This needs to be earlier in NewGamePartialFollowUp to predate the crafting order generation that the game performs in the crafting behavior.
            var smithingModel = Campaign.Current.Models.GetSmithingModel();
            if (smithingModel != null)
            {
                smithingModel.ValidateHiddenCraftingTemplates();
            }
        }

        private void AddTownMenu(CampaignGameStarter starter)
        {
            starter.AddGameMenuOption("town", "town_artisan", TORTextHelper.GetText("tor_artisan_district_menu_option_text", "Go to the artisan district"),
                args =>
                {
                    args.optionLeaveType = GameMenuOption.LeaveType.Submenu;
                    return MenuHelper.SetOptionProperties(args, true, false, TextObject.GetEmpty());
                },
                args =>
                {
                    GameMenu.SwitchToMenu("town_artisan");
                },
                false, 4, false, null);

            starter.AddGameMenu("town_artisan", "{ARTISAN_INTRODUCTION}",
                delegate (MenuCallbackArgs args)
                {
                    args.MenuTitle = TORTextHelper.GetTextObject("tor_artisan_district_title_text", "Artisan District");
                    var intro = TORTextHelper.GetTextObject("tor_settlement_artisan_introduction", "You have arrived at {SETTLEMENT_NAME}'s Artisan District. It is a lively place where artisans and craftsmen of varying professions are busy in their workshops.");
                    MBTextManager.SetTextVariable("SETTLEMENT_NAME", Settlement.CurrentSettlement.Name);
                    MBTextManager.SetTextVariable("ARTISAN_INTRODUCTION", intro);
                },
                GameMenu.MenuOverlayType.SettlementWithBoth, GameMenu.MenuFlags.None, null);

            starter.AddGameMenuOption("town_artisan", "town_artisan_smithy", TORTextHelper.GetText("tor_artisan_weaponsmith_option_text", "Visit the weaponsmith"),
                game_menu_craft_weapon_on_condition,
                args =>
                {
                    CraftingHelper.OpenCrafting(CraftingTemplate.All[0], null);
                },
                false, -1, false, null);

            starter.AddGameMenuOption("town_artisan", "town_artisan_enchanting", TORTextHelper.GetText("tor_artisan_enchanter_option_text", "Visit the enchanter"),
                game_menu_enchant_weapon_on_condition,
                args =>
                {
                    EnchantingScreen.Open();
                },
                false, -1, false, null);

            starter.AddGameMenuOption("town_artisan", "town_artisan_leave", TORTextHelper.GetText("tor_artisan_leave_option_text", "Leave"),
                args =>
                {
                    args.optionLeaveType = GameMenuOption.LeaveType.Leave;
                    return true;
                },
                args =>
                {
                    GameMenu.SwitchToMenu("town");
                },
                true, -1, false, null);
        }

        private static bool game_menu_craft_weapon_on_condition(MenuCallbackArgs args)
        {
            bool canPlayerDo = Campaign.Current.Models.SettlementAccessModel.CanMainHeroDoSettlementAction(Settlement.CurrentSettlement, SettlementAccessModel.SettlementAction.Craft, out bool shouldBeDisabled, out TextObject disabledText);
            args.optionLeaveType = GameMenuOption.LeaveType.Craft;
            ICraftingCampaignBehavior campaignBehavior = Campaign.Current.GetCampaignBehavior<ICraftingCampaignBehavior>();
            if (Settlement.CurrentSettlement.IsTown &&
                campaignBehavior != null &&
                campaignBehavior.CraftingOrders != null &&
                campaignBehavior.CraftingOrders.TryGetValue(Settlement.CurrentSettlement.Town, out CraftingOrderSlots craftingOrderSlots) &&
                craftingOrderSlots.CustomOrders.Count > 0)
            {
                args.OptionQuestData |= GameMenuOption.IssueQuestFlags.ActiveIssue;
            }
            return MenuHelper.SetOptionProperties(args, canPlayerDo, shouldBeDisabled, disabledText);
        }

        private static bool game_menu_enchant_weapon_on_condition(MenuCallbackArgs args)
        {
            bool canPlayerDo = Campaign.Current.Models.SettlementAccessModel.CanMainHeroDoSettlementAction(Settlement.CurrentSettlement, SettlementAccessModel.SettlementAction.Craft, out bool shouldBeDisabled, out TextObject disabledText);
            args.optionLeaveType = GameMenuOption.LeaveType.Craft;
            return MenuHelper.SetOptionProperties(args, canPlayerDo, shouldBeDisabled, disabledText);
        }

        public void InitializeSavedCraftedItems()
        {
            var missingSourceItems = new List<ItemObject>();

            foreach (var element in _customCraftedItems)
            {
                var duplicateItem = element.Key;
                var duplicatedItemData = element.Value;
                var copyFrom = MBObjectManager.Instance.GetObject<ItemObject>(duplicatedItemData.OriginalItemStringId);

                if (copyFrom == null)
                {
                    missingSourceItems.Add(duplicateItem);
                    continue;
                }

                duplicateItem.CopyPropertiesFrom(copyFrom);
                AccessTools.Property(typeof(ItemObject), "Name").SetValue(duplicateItem, new TextObject(duplicatedItemData.NewItemName));
                duplicateItem.Initialize();

                if (duplicatedItemData.IsPlayerCrafted)
                {
                    ItemObject.InitAsPlayerCraftedItem(ref duplicateItem);
                }

                duplicateItem.DetermineItemCategoryForItem();
                MBObjectManager.Instance.RegisterObject(duplicateItem);
                duplicateItem.AfterInitialized();

                ExtendedItemObjectManager.AddCraftedItem(duplicatedItemData.OriginalItemStringId, duplicateItem.StringId, duplicatedItemData.ItemTraits);
            }

            foreach (var missingSourceItem in missingSourceItems)
            {
                ForgetDuplicatedItem(missingSourceItem);
            }
        }

        public void ForgetDuplicatedItem(ItemObject item)
        {
            if (item == null)
            {
                return;
            }

            _customCraftedItems.Remove(item);
            ExtendedItemObjectManager.RemoveRuntimeDuplicatedItem(item);
        }

        public override void SyncData(IDataStore dataStore)
        {
            dataStore.SyncData("_customCraftedItems", ref _customCraftedItems);
        }

        ~TORArtisanDistrictCampaignBehavior()
        {
            TORCampaignEvents.Instance.ItemDuplicated -= OnItemDuplicated;
        }
    }

    public class TorItemDuplicationData
    {
        [SaveableProperty(1)]
        public string OriginalItemStringId { get; set; }
        [SaveableProperty(2)]
        public string NewItemName { get; set; }
        [SaveableProperty(3)]
        public List<string> ItemTraits { get; set; }
        [SaveableProperty(4)]
        public bool IsPlayerCrafted { get; set; }
    }

    public class TorItemBeingCraftedData
    {
        public EquipmentElement EquipmentElement { get; set; }
        public List<ItemTrait> ItemTraits { get; set; }
    }
}