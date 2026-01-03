using HarmonyLib;
using SandBox.GauntletUI;
using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Inventory;
using TaleWorlds.CampaignSystem.ViewModelCollection.Inventory;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.ViewModelCollection.Party;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;
using TOR_Core.BattleMechanics.DamageSystem;
using TOR_Core.Extensions;
using TOR_Core.Items;
using TOR_Core.Utilities;

namespace TOR_Core.HarmonyPatches
{
    [HarmonyPatch]
    public static class InventoryPatches
    {
        private static readonly FieldInfo CurrentCharacterField =
    AccessTools.Field(typeof(SPInventoryVM), "_currentCharacter");

        private static readonly FieldInfo InventoryLogicField =
            AccessTools.Field(typeof(SPInventoryVM), "_inventoryLogic");
        [HarmonyPostfix]
        [HarmonyPatch(typeof(GauntletInventoryScreen), "OnFrameTick")]
        public static void Postfix(SPInventoryVM ____dataSource)
        {
            if (InputKey.Tilde.IsPressed())
            {
                TORCommon.CopyEquipmentToClipBoard(____dataSource);
            }
            else if (InputKey.Comma.IsPressed() && Clipboard.ContainsText() && Game.Current.CheatMode)
            {
                string text = string.Empty;
                Thread thread = new Thread(() => text = Clipboard.GetText());
                thread.SetApartmentState(ApartmentState.STA);
                thread.Start();
                thread.Join(); //Wait for the thread to end
                var list = text.Split(',');
                var logic = InventoryLogicField.GetValue(____dataSource) as InventoryLogic;
                List<Tuple<string, int>> itemList = new List<Tuple<string, int>>();
                for (int i = 0; i < list.Length; i++)
                {
                    itemList.Add(new Tuple<string, int>(list[i], i));
                }
                EquipItemsFromClipboard(itemList, logic, ____dataSource);
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(SPInventoryVM), MethodType.Constructor, typeof(InventoryLogic), typeof(bool), typeof(Func<WeaponComponentData, ItemObject.ItemUsageSetFlags>))]
        public static void Postfix2(ref SPInventoryVM __instance, InventoryLogic ____inventoryLogic, Func<WeaponComponentData, ItemObject.ItemUsageSetFlags> ____getItemUsageSetFlags)
        {
            var reset = Delegate.CreateDelegate(typeof(Action<ItemVM, int>), __instance, "ResetComparedItems");
            var itemindex = Delegate.CreateDelegate(typeof(Func<EquipmentIndex, SPItemVM>), __instance, "GetItemFromIndex");
            if (reset != null && itemindex != null && ____inventoryLogic != null && ____getItemUsageSetFlags != null)
            {
                if (__instance.ItemMenu != null)
                {
                    __instance.ItemMenu.OnFinalize();
                }
                __instance.ItemMenu = new TorItemMenuVM((Action<ItemVM, int>)reset, ____inventoryLogic, ____getItemUsageSetFlags, (Func<EquipmentIndex, SPItemVM>)itemindex);
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(SPInventoryVM), "UpdateFilteredStatusOfItem")]
        public static bool SearchByStringId(
         SPItemVM item,
         SPInventoryVM __instance,
         Dictionary<SPInventoryVM.Filters, List<int>> ____filters,
         SPInventoryVM.Filters ____activeFilterIndex)
        {
            bool isFilteredByCategory = !____filters[____activeFilterIndex].Contains(item.TypeId);
            bool isFilteredBySearchString = false;

            if (__instance.IsSearchAvailable &&
                (item.InventorySide == InventoryLogic.InventorySide.OtherInventory ||
                 item.InventorySide == InventoryLogic.InventorySide.PlayerInventory))
            {
                string text = (item.InventorySide == InventoryLogic.InventorySide.OtherInventory)
                    ? __instance.LeftSearchText
                    : __instance.RightSearchText;

                if (text.Length > 1)
                {
                    bool matchFound =
                        (!string.IsNullOrEmpty(item.StringId) &&
                         item.StringId.IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0) ||
                        (!string.IsNullOrEmpty(item.ItemDescription) &&
                         item.ItemDescription.IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0);

                    isFilteredBySearchString = !matchFound;
                }
            }
            item.IsFiltered = (isFilteredByCategory || isFilteredBySearchString);
            return false;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(ItemMenuVM), "SetItem")]
        public static void PostFix3(ref ItemMenuVM __instance, SPItemVM item, ItemVM comparedItem, BasicCharacterObject character, int alternativeUsageIndex)
        {
            if (__instance is TorItemMenuVM torvm)
            {
                torvm.SetItemExtra(item, comparedItem, character, alternativeUsageIndex);
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(SPInventoryVM), "RefreshCharacterCanUseItem")]
        public static void PostFix4(ref SPInventoryVM __instance)
        {
            var currentCharacter = CurrentCharacterField.GetValue(__instance) as CharacterObject;

            for (int i = 0; i < __instance.RightItemListVM.Count; i++)
            {
                __instance.RightItemListVM[i].CanCharacterUseItem = ExtendedItemObjectManager.CanCharacterUseItem(__instance.RightItemListVM[i].ItemRosterElement.EquipmentElement.Item, currentCharacter);
            }
            for (int j = 0; j < __instance.LeftItemListVM.Count; j++)
            {
                __instance.LeftItemListVM[j].CanCharacterUseItem = ExtendedItemObjectManager.CanCharacterUseItem(__instance.LeftItemListVM[j].ItemRosterElement.EquipmentElement.Item, currentCharacter);
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(SPInventoryVM), "IsItemEquipmentPossible")]
        public static void PostFix5(ref SPInventoryVM __instance, ref bool __result, SPItemVM itemVM)
        {
            var currentCharacter = CurrentCharacterField.GetValue(__instance) as CharacterObject;
            if (__result)
            {
                __result = ExtendedItemObjectManager.CanCharacterUseItem(itemVM.ItemRosterElement.EquipmentElement.Item, currentCharacter);
                if (!__result)
                {
                    TextObject textObject = TORTextHelper.GetTextObject("tor_inventory_race_restriction", "You are not the correct race to equip this item.");
                    MBInformationManager.AddQuickInformation(textObject);
                }
            }
        }

        private static Color GetColorForDamageType(DamageType type)
        {
            switch (type)
            {
                case DamageType.Physical:
                    return new Color { Red = 63, Green = 63, Blue = 63, Alpha = 1 };
                case DamageType.Magical:
                    return new Color { Red = 255, Green = 165, Blue = 90, Alpha = 1 };
                case DamageType.Fire:
                    return new Color { Red = 1, Green = 165, Blue = 255, Alpha = 1 };
                default:
                    return Color.White;
            }
        }

        private static void EquipItemsFromClipboard(List<Tuple<string, int>> itemList, InventoryLogic logic, SPInventoryVM inventoryVM)
        {
            List<TransferCommand> commands = new List<TransferCommand>();
            var playerItems = logic.GetElementsInRoster(InventoryLogic.InventorySide.PlayerInventory);
            var otherItems = logic.GetElementsInRoster(InventoryLogic.InventorySide.OtherInventory);
            foreach (var tuple in itemList)
            {
                if (tuple.Item1.StartsWith("Item."))
                {
                    var itemId = tuple.Item1.Split('.').Last();
                    var itemObject = MBObjectManager.Instance.GetObject<ItemObject>(itemId);
                    if (itemObject != null)
                    {

                        var element = playerItems.FirstOrDefault(x => x.EquipmentElement.Item.StringId == itemObject.StringId);
                        var side = InventoryLogic.InventorySide.PlayerInventory;
                        if (element.IsEmpty)
                        {
                            element = otherItems.FirstOrDefault(x => x.EquipmentElement.Item.StringId == itemObject.StringId);
                            side = InventoryLogic.InventorySide.OtherInventory;
                        }
                        if (!element.IsEmpty)
                        {
                            var command = TransferCommand.Transfer(1, side, InventoryLogic.InventorySide.BattleEquipment, element, EquipmentIndex.None, GetItemTypeWithIndex(tuple.Item2), inventoryVM.CharacterList.SelectedItem.Hero.CharacterObject);
                            commands.Add(command);
                        }
                    }
                }
                else
                {
                    var itemFromslot = GetItemFromIndex(GetItemTypeWithIndex(tuple.Item2), inventoryVM);
                    if (itemFromslot != null && !itemFromslot.ItemRosterElement.IsEmpty)
                    {
                        var command = TransferCommand.Transfer(1, InventoryLogic.InventorySide.BattleEquipment, InventoryLogic.InventorySide.PlayerInventory, itemFromslot.ItemRosterElement, GetItemTypeWithIndex(tuple.Item2), EquipmentIndex.None, inventoryVM.CharacterList.SelectedItem.Hero.CharacterObject);
                        commands.Add(command);
                    }
                }
            }
            if (commands.Count > 0) logic.AddTransferCommands(commands);
        }

        public static EquipmentIndex GetItemTypeWithIndex(int index)
        {
            switch (index)
            {
                case 0:
                    return EquipmentIndex.Weapon0;
                case 1:
                    return EquipmentIndex.Weapon1;
                case 2:
                    return EquipmentIndex.Weapon2;
                case 3:
                    return EquipmentIndex.Weapon3;
                case 4:
                    return EquipmentIndex.Head;
                case 5:
                    return EquipmentIndex.Body;
                case 6:
                    return EquipmentIndex.Cape;
                case 7:
                    return EquipmentIndex.Gloves;
                case 8:
                    return EquipmentIndex.Leg;
                case 9:
                    return EquipmentIndex.Horse;
                case 10:
                    return EquipmentIndex.HorseHarness;
                default:
                    return EquipmentIndex.None;
            }
        }

        private static SPItemVM GetItemFromIndex(EquipmentIndex itemType, SPInventoryVM inventoryVM)
        {
            switch (itemType)
            {
                case EquipmentIndex.Weapon0:
                    return inventoryVM.CharacterWeapon1Slot;
                case EquipmentIndex.Weapon1:
                    return inventoryVM.CharacterWeapon2Slot;
                case EquipmentIndex.Weapon2:
                    return inventoryVM.CharacterWeapon3Slot;
                case EquipmentIndex.Weapon3:
                    return inventoryVM.CharacterWeapon4Slot;
                case EquipmentIndex.ExtraWeaponSlot:
                    return inventoryVM.CharacterBannerSlot;
                case EquipmentIndex.Head:
                    return inventoryVM.CharacterHelmSlot;
                case EquipmentIndex.Body:
                    return inventoryVM.CharacterTorsoSlot;
                case EquipmentIndex.Leg:
                    return inventoryVM.CharacterBootSlot;
                case EquipmentIndex.Gloves:
                    return inventoryVM.CharacterGloveSlot;
                case EquipmentIndex.Cape:
                    return inventoryVM.CharacterCloakSlot;
                case EquipmentIndex.Horse:
                    return inventoryVM.CharacterMountSlot;
                case EquipmentIndex.HorseHarness:
                    return inventoryVM.CharacterMountArmorSlot;
                default:
                    return null;
            }
        }
    }
}