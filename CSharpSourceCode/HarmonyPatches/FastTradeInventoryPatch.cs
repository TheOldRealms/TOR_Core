using HarmonyLib;
using Helpers;
using System;
using System.Collections.Generic;
using System.Reflection;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Inventory;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.ViewModelCollection.Inventory;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TOR_Core.HarmonyPatches
{
    // Trading and Donating all stops when gold/xp thresold is reached
    internal static class FastTradeInventoryPatch
    {
        private static readonly MethodInfo RefreshInformationValuesMethod =
            AccessTools.Method(typeof(SPInventoryVM), "RefreshInformationValues");

        private static readonly MethodInfo LeftInventoryOwnerGoldGetter =
            AccessTools.PropertyGetter(typeof(SPInventoryVM), "LeftInventoryOwnerGold");

        [HarmonyPrefix]
        [HarmonyPatch("ExecuteSellAllItems")]
        private static bool ExecuteSellAllItemsPrefix(
            SPInventoryVM __instance,
            InventoryLogic ____inventoryLogic,
            CharacterObject ____currentCharacter)
        {
            // ctrl
            if (__instance.IsEntireStackModifierActive)
            {
                return true;
            }

            if (__instance.IsTrading)
            {
                bool handled = TryQueueTradeSellAllUntilStuck(__instance, ____inventoryLogic, ____currentCharacter);
                return !handled;
            }

            // only when donating and player party has relevant perks
            if (!____inventoryLogic.IsDiscardDonating || !PerkHelper.PlayerHasAnyItemDonationPerk())
            {
                return true;
            }

            bool donationHandled = TryQueueDonationDiscardAllStopAtXpCap(__instance, ____inventoryLogic, ____currentCharacter);
            return !donationHandled;
        }

        [HarmonyPrefix]
        [HarmonyPatch("ExecuteBuyAllItems")]
        private static bool ExecuteBuyAllItemsPrefix(
            SPInventoryVM __instance,
            InventoryLogic ____inventoryLogic,
            CharacterObject ____currentCharacter)
        {
            if (__instance.IsEntireStackModifierActive)
            {
                return true;
            }

            if (!__instance.IsTrading)
            {
                return true;
            }

            bool handled = TryQueueTradeBuyAllUntilStuck(__instance, ____inventorylogic: ____inventoryLogic, ____currentCharacter);
            return !handled;
        }

        private static bool TryQueueTradeSellAllUntilStuck(
            SPInventoryVM inventoryVm,
            InventoryLogic inventoryLogic,
            CharacterObject currentCharacter)
        {
            inventoryVm.IsRefreshed = false;

            bool queuedAny = false;

            while (true)
            {
                int remainingBuyerGold = GetLeftInventoryOwnerGold(inventoryVm);
                if (remainingBuyerGold <= 0)
                {
                    break;
                }

                int totalAmountBefore = inventoryLogic.TotalAmount;

                List<TransferCommand> transferCommands = BuildTradeSellCommandsInUiOrder(
                    inventoryVm,
                    currentCharacter,
                    remainingBuyerGold);

                if (transferCommands.Count == 0)
                {
                    break;
                }

                inventoryLogic.AddTransferCommands(transferCommands);
                queuedAny = true;

                if (inventoryLogic.TotalAmount == totalAmountBefore)
                {
                    break;
                }
            }

            if (!queuedAny)
            {
                inventoryVm.IsRefreshed = true;
                return false;
            }

            RefreshInformationValuesMethod.Invoke(inventoryVm, null);
            inventoryVm.ExecuteRemoveZeroCounts();
            inventoryVm.IsRefreshed = true;

            return true;
        }

        private static bool TryQueueTradeBuyAllUntilStuck(
            SPInventoryVM inventoryVm,
            InventoryLogic ____inventorylogic,
            CharacterObject currentCharacter)
        {
            inventoryVm.IsRefreshed = false;

            bool queuedAny = false;

            while (true)
            {
                long remainingPlayerGold = (long)Hero.MainHero.Gold - ____inventorylogic.TotalAmount;
                if (remainingPlayerGold <= 0)
                {
                    break;
                }

                int totalAmountBefore = ____inventorylogic.TotalAmount;

                List<TransferCommand> transferCommands = BuildTradeBuyCommandsInUiOrder(
                    inventoryVm,
                    currentCharacter,
                    remainingPlayerGold);

                if (transferCommands.Count == 0)
                {
                    break;
                }

                ____inventorylogic.AddTransferCommands(transferCommands);
                queuedAny = true;

                if (____inventorylogic.TotalAmount == totalAmountBefore)
                {
                    break;
                }
            }

            if (!queuedAny)
            {
                inventoryVm.IsRefreshed = true;
                return false;
            }

            RefreshInformationValuesMethod.Invoke(inventoryVm, null);
            inventoryVm.ExecuteRemoveZeroCounts();
            inventoryVm.IsRefreshed = true;

            return true;
        }

        private static int GetLeftInventoryOwnerGold(SPInventoryVM inventoryVm)
        {
            return (int)LeftInventoryOwnerGoldGetter.Invoke(inventoryVm, null);
        }

        private static List<TransferCommand> BuildTradeSellCommandsInUiOrder(
            SPInventoryVM inventoryVm,
            CharacterObject currentCharacter,
            int remainingBuyerGold)
        {
            List<TransferCommand> transferCommands = new List<TransferCommand>();

            InventoryLogic.InventorySide fromSide = InventoryLogic.InventorySide.PlayerInventory;
            InventoryLogic.InventorySide toSide = InventoryLogic.InventorySide.OtherInventory;

            long remainingGoldBudget = remainingBuyerGold;

            MBBindingList<SPItemVM> playerItems = inventoryVm.RightItemListVM;
            for (int i = 0; i < playerItems.Count; i++)
            {
                SPItemVM itemVm = playerItems[i];
                if (itemVm == null || itemVm.IsFiltered || itemVm.IsLocked || !itemVm.IsTransferable)
                {
                    continue;
                }

                ItemRosterElement itemElement = itemVm.ItemRosterElement;
                int availableCount = itemElement.Amount;
                if (availableCount <= 0)
                {
                    continue;
                }

                int unitPrice = itemVm.ItemCost;

                // though that shouldn't happen in practice
                if (unitPrice <= 0)
                {
                    transferCommands.Add(TransferCommand.Transfer(
                        availableCount,
                        fromSide,
                        toSide,
                        itemElement,
                        EquipmentIndex.None,
                        EquipmentIndex.None,
                        currentCharacter));

                    continue;
                }

                if (remainingGoldBudget <= 0)
                {
                    continue;
                }

                int maxAffordableCount = (int)Math.Min(availableCount, remainingGoldBudget / unitPrice);
                if (maxAffordableCount <= 0)
                {
                    continue;
                }

                transferCommands.Add(TransferCommand.Transfer(
                    maxAffordableCount,
                    fromSide,
                    toSide,
                    itemElement,
                    EquipmentIndex.None,
                    EquipmentIndex.None,
                    currentCharacter));

                remainingGoldBudget -= (long)unitPrice * maxAffordableCount;
            }

            return transferCommands;
        }

        private static List<TransferCommand> BuildTradeBuyCommandsInUiOrder(
            SPInventoryVM inventoryVm,
            CharacterObject currentCharacter,
            long remainingPlayerGold)
        {
            List<TransferCommand> transferCommands = new List<TransferCommand>();

            InventoryLogic.InventorySide fromSide = InventoryLogic.InventorySide.OtherInventory;
            InventoryLogic.InventorySide toSide = InventoryLogic.InventorySide.PlayerInventory;

            long remainingGoldBudget = remainingPlayerGold;

            MBBindingList<SPItemVM> merchantItems = inventoryVm.LeftItemListVM;
            for (int i = 0; i < merchantItems.Count; i++)
            {
                SPItemVM itemVm = merchantItems[i];
                if (itemVm == null || itemVm.IsFiltered || itemVm.IsLocked || !itemVm.IsTransferable)
                {
                    continue;
                }

                ItemRosterElement itemElement = itemVm.ItemRosterElement;
                int availableCount = itemElement.Amount;
                if (availableCount <= 0)
                {
                    continue;
                }

                int unitPrice = itemVm.ItemCost;

                if (unitPrice <= 0)
                {
                    transferCommands.Add(TransferCommand.Transfer(
                        availableCount,
                        fromSide,
                        toSide,
                        itemElement,
                        EquipmentIndex.None,
                        EquipmentIndex.None,
                        currentCharacter));

                    continue;
                }

                if (remainingGoldBudget <= 0)
                {
                    continue;
                }

                int maxAffordableCount = (int)Math.Min(availableCount, remainingGoldBudget / unitPrice);
                if (maxAffordableCount <= 0)
                {
                    continue;
                }

                transferCommands.Add(TransferCommand.Transfer(
                    maxAffordableCount,
                    fromSide,
                    toSide,
                    itemElement,
                    EquipmentIndex.None,
                    EquipmentIndex.None,
                    currentCharacter));

                remainingGoldBudget -= (long)unitPrice * maxAffordableCount;
            }

            return transferCommands;
        }

        private static bool TryQueueDonationDiscardAllStopAtXpCap(
            SPInventoryVM inventoryVm,
            InventoryLogic inventoryLogic,
            CharacterObject currentCharacter)
        {
            int maximumXpAmountPartyCanGet = MobilePartyHelper.GetMaximumXpAmountPartyCanGet(MobileParty.MainParty);
            float remainingXpCapacity = maximumXpAmountPartyCanGet - inventoryLogic.XpGainFromDonations;

            if (remainingXpCapacity <= 0f)
            {
                return true;
            }

            ItemDiscardModel itemDiscardModel = Campaign.Current.Models.ItemDiscardModel;

            List<TransferCommand> transferCommands = BuildDonationCommandsInUiOrderNoWaste(
                inventoryVm,
                currentCharacter,
                itemDiscardModel,
                ref remainingXpCapacity);

            if (transferCommands.Count == 0)
            {
                return true;
            }

            inventoryVm.IsRefreshed = false;
            inventoryLogic.AddTransferCommands(transferCommands);
            RefreshInformationValuesMethod.Invoke(inventoryVm, null);
            inventoryVm.ExecuteRemoveZeroCounts();
            inventoryVm.IsRefreshed = true;

            return true;
        }

        private static List<TransferCommand> BuildDonationCommandsInUiOrderNoWaste(
            SPInventoryVM inventoryVm,
            CharacterObject currentCharacter,
            ItemDiscardModel itemDiscardModel,
            ref float remainingXpCapacity)
        {
            List<TransferCommand> transferCommands = new List<TransferCommand>();

            InventoryLogic.InventorySide fromSide = InventoryLogic.InventorySide.PlayerInventory;
            InventoryLogic.InventorySide toSide = InventoryLogic.InventorySide.OtherInventory;

            MBBindingList<SPItemVM> playerInventoryItems = inventoryVm.RightItemListVM;
            for (int i = 0; i < playerInventoryItems.Count && remainingXpCapacity > 0f; i++)
            {
                SPItemVM itemVm = playerInventoryItems[i];
                if (itemVm == null || itemVm.IsFiltered || itemVm.IsLocked || !itemVm.IsTransferable)
                {
                    continue;
                }

                ItemRosterElement itemElement = itemVm.ItemRosterElement;
                int availableCount = itemElement.Amount;
                if (availableCount <= 0)
                {
                    continue;
                }

                ItemObject item = itemElement.EquipmentElement.Item;
                if (!itemDiscardModel.PlayerCanDonateItem(item))
                {
                    continue;
                }

                int xpBonusPerItem = itemDiscardModel.GetXpBonusForDiscardingItem(item);
                if (xpBonusPerItem <= 0)
                {
                    continue;
                }

                int maxCountByXp = (int)Math.Floor(remainingXpCapacity / xpBonusPerItem);
                int transferCount = Math.Min(availableCount, maxCountByXp);
                if (transferCount <= 0)
                {
                    continue;
                }

                transferCommands.Add(TransferCommand.Transfer(
                    transferCount,
                    fromSide,
                    toSide,
                    itemElement,
                    EquipmentIndex.None,
                    EquipmentIndex.None,
                    currentCharacter));

                remainingXpCapacity -= xpBonusPerItem * transferCount;
            }

            return transferCommands;
        }
    }
}
