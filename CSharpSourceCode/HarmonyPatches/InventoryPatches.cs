using HarmonyLib;
using SandBox.GauntletUI;
using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem.Inventory;
using TaleWorlds.CampaignSystem.ViewModelCollection.Inventory;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TOR_Core.BattleMechanics.DamageSystem;
using TOR_Core.Items;
using TOR_Core.Utilities;

namespace TOR_Core.HarmonyPatches
{
    [HarmonyPatch]
    public static class InventoryPatches
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(GauntletInventoryScreen), "OnFrameTick")]
        public static void Postfix(SPInventoryVM ____dataSource)
        {
            if (InputKey.Tilde.IsPressed())
            {
                TORCommon.CopyEquipmentToClipBoard(____dataSource);
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(SPInventoryVM), MethodType.Constructor, typeof(InventoryLogic), typeof(bool), typeof(Func<WeaponComponentData, ItemObject.ItemUsageSetFlags>), typeof(string), typeof(string))]
        public static void Postfix2(ref SPInventoryVM __instance, InventoryLogic ____inventoryLogic, Func<WeaponComponentData, ItemObject.ItemUsageSetFlags> ____getItemUsageSetFlags)
        {
            var reset = Delegate.CreateDelegate(typeof(Action<ItemVM, int>), __instance, "ResetComparedItems");
            var itemindex = Delegate.CreateDelegate(typeof(Func<EquipmentIndex, SPItemVM>), __instance, "GetItemFromIndex");
            if (reset != null && itemindex != null && ____inventoryLogic != null && ____getItemUsageSetFlags != null)
            {
                __instance.ItemMenu = new TorItemMenuVM((Action<ItemVM, int>)reset, ____inventoryLogic, ____getItemUsageSetFlags, (Func<EquipmentIndex, SPItemVM>)itemindex);
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(SPInventoryVM), "UpdateFilteredStatusOfItem")]
        public static bool SearchByStringId(SPItemVM item, SPInventoryVM __instance, Dictionary<SPInventoryVM.Filters, List<int>> ____filters, SPInventoryVM.Filters ____activeFilterIndex)
        {
            bool isFilteredByCategory = !____filters[____activeFilterIndex].Contains(item.TypeId);
            bool isFilteredBySearchString = false;
            if (__instance.IsSearchAvailable && (item.InventorySide == InventoryLogic.InventorySide.OtherInventory || item.InventorySide == InventoryLogic.InventorySide.PlayerInventory))
            {
                string text = (item.InventorySide == InventoryLogic.InventorySide.OtherInventory) ? __instance.LeftSearchText : __instance.RightSearchText;
                if (text.Length > 1)
                {
                    text = text.ToLower();
                    isFilteredBySearchString = item.StringId.ToLower().Contains(text) || item.ItemDescription.ToLower().Contains(text);
                }
            }
            item.IsFiltered = (isFilteredByCategory || !isFilteredBySearchString);
            return false;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(ItemMenuVM), "SetItem")]
        public static void PostFix3(ref ItemMenuVM __instance, SPItemVM item, ItemVM comparedItem, BasicCharacterObject character, int alternativeUsageIndex)
        {
            if (__instance is TorItemMenuVM)
            {
                var torvm = __instance as TorItemMenuVM;
                torvm.SetItemExtra(item, comparedItem, character, alternativeUsageIndex);
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
    }
}
