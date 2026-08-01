using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Core.ImageIdentifiers;
using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.GauntletUI.Widgets;
using TaleWorlds.MountAndBlade.View.Tableaus;
using TOR_Core.CampaignMechanics.Crafting;
using TOR_Core.CharacterDevelopment;
using TOR_Core.Extensions;
using TOR_Core.Utilities;

namespace TOR_Core.HarmonyPatches
{
    [HarmonyPatch]
    [HarmonyPatchCategory("LatePatches")]
    public static class ItemPatches
    {
        private const float MERCHANDISE_STAFF_PRODUCTION_WEIGHT = 0.15f; // times less staff production for towns
        private static float GetWorkshopProductionWeight(ItemObject item)
        {
            if (item == null)
            {
                return 1f;
            }

            if (!item.NotMerchandise
                && item.StringId.Contains("staff")
                && item.IsMeleeWeapon()
                && item.IsMagicalItem())
            {
                return MERCHANDISE_STAFF_PRODUCTION_WEIGHT;
            }

            return 1f;
        }

        private static bool IsStrictTorWorkshopTown(Town town)
        {
            string cultureStringId = town?.Culture?.StringId;
            return cultureStringId == TORConstants.Cultures.SYLVANIA
                || cultureStringId == TORConstants.Cultures.GREENSKIN
                || cultureStringId == TORConstants.Cultures.DAWI;
        }
        private static bool IsRestrictedMerchandiseCategory(ItemCategory itemCategory)
        {
            return itemCategory != null && !itemCategory.IsTradeGood;
        }

        private static bool IsRestrictedMerchandiseItem(ItemObject item)
        {
            return item != null
                && !item.NotMerchandise
                && IsRestrictedMerchandiseCategory(item.ItemCategory);
        }

        private static float GetNativeWorkshopSelectionWeight(ItemObject item)
        {
            int clampedValue = item.Value > 100 ? item.Value : 100;
            return (1f / (clampedValue + 100f)) * GetWorkshopProductionWeight(item);
        }
        private static bool IsVampireCountTown(Town town)
        {
            return town?.Culture?.StringId == TORConstants.Cultures.SYLVANIA;
        }

        private static bool IsVanillaHorseAllowedInStrictTown(Town town, ItemObject item)
        {
            return IsVampireCountTown(town) && item?.HasHorseComponent == true;
        }
        private static bool IsAllowedItemInStrictTown(Town town, ItemObject item)
        {
            return item != null && (item.IsTorItem() || IsVanillaHorseAllowedInStrictTown(town, item));
        }

        private static Town GetTradeTownFromSaleContext(PartyBase buyerParty, Settlement currentSettlement)
        {
            Settlement settlement = buyerParty?.Settlement ?? currentSettlement;
            if (settlement == null)
            {
                return null;
            }

            if (settlement.Town != null)
            {
                return settlement.Town;
            }

            if (settlement.IsVillage)
            {
                return settlement.Village.TradeBound?.Town ?? settlement.Village.Bound?.Town;
            }

            return null;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(WeaponComponentData), "GetRelevantSkillFromWeaponClass")]
        public static bool AddGunpowderRelevantSkill(ref SkillObject __result, WeaponClass weaponClass)
        {
            if (weaponClass == WeaponClass.Cartridge || weaponClass == WeaponClass.Musket || weaponClass == WeaponClass.Pistol)
            {
                __result = TORSkills.GunPowder;
                return false;
            }
            else return true;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(WorkshopsCampaignBehavior), "IsItemPreferredForTown")]
        public static void OnlyPreferTorItemsInStrictTownWorkshops(ref bool __result, ItemObject item, Town townComponent)
        {
            if (!__result || !IsStrictTorWorkshopTown(townComponent) || !IsRestrictedMerchandiseItem(item))
            {
                return;
            }

            if (item.Culture == townComponent.Culture)
            {
                __result = IsAllowedItemInStrictTown(townComponent, item);
            }
        }

        // aux only will leak vanilla 
        [HarmonyPrefix]
        [HarmonyPatch(typeof(WorkshopsCampaignBehavior), "GetRandomItem")]
        public static bool OnlyProduceTorItemsInStrictTownWorkshops(
            ref EquipmentElement __result,
            ItemCategory itemGroupBase,
            Town townComponent,
            Dictionary<ItemCategory, List<ItemObject>> ____itemsInCategory)
        {
            if (!IsStrictTorWorkshopTown(townComponent) || !IsRestrictedMerchandiseCategory(itemGroupBase))
            {
                return true;
            }

            if (!____itemsInCategory.TryGetValue(itemGroupBase, out List<ItemObject> allItemsInGroup))
            {
                return true;
            }

            List<(ItemObject Item, float Weight)> exactCultureAllowedItems = new();
            List<(ItemObject Item, float Weight)> fallbackAllowedItems = new();

            foreach (ItemObject item in allItemsInGroup)
            {
                if (!IsRestrictedMerchandiseItem(item) || item.ItemCategory != itemGroupBase || !IsAllowedItemInStrictTown(townComponent, item))
                {
                    continue;
                }

                float weight = GetNativeWorkshopSelectionWeight(item);

                if (item.Culture == townComponent.Culture)
                {
                    exactCultureAllowedItems.Add((item, weight));
                }
                else
                {
                    fallbackAllowedItems.Add((item, weight));
                }
            }

            List<(ItemObject Item, float Weight)> itemsToChooseFrom = exactCultureAllowedItems.Count > 0
                ? exactCultureAllowedItems
                : fallbackAllowedItems;

            if (itemsToChooseFrom.Count < 1)
            {
                return true;
            }

            ItemObject itemObject = MBRandom.ChooseWeighted(itemsToChooseFrom);

            ItemModifierGroup itemModifierGroup = itemObject?.ItemComponent?.ItemModifierGroup;
            ItemModifier itemModifier = itemModifierGroup?.GetRandomItemModifierProductionScoreBased();

            __result = new EquipmentElement(itemObject, itemModifier, null, false);
            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(SellItemsAction), "Apply")]
        public static bool PreventVanillaMerchandiseFromEnteringStrictTownMarkets(
            PartyBase receiverParty,
            PartyBase payerParty,
            ItemRosterElement subject,
            int number,
            Settlement currentSettlement)
        {
            if (number < 1 || payerParty?.Settlement == null)
            {
                return true;
            }

            Town destinationTown = GetTradeTownFromSaleContext(payerParty, currentSettlement);
            if (!IsStrictTorWorkshopTown(destinationTown))
            {
                return true;
            }

            ItemObject item = subject.EquipmentElement.Item;
            if (!IsRestrictedMerchandiseItem(item))
            {
                return true;
            }

            return IsAllowedItemInStrictTown(destinationTown, item);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(ItemTableau), "RefreshItemTableau")]
        public static void AddParticlesToItemIfNeeded(ItemTableau __instance, ItemRosterElement ____itemRosterElement, GameEntity ____itemTableauEntity)
        {
            if (TORArtisanDistrictCampaignBehavior.Instance?.ItemBeingCrafted?.EquipmentElement.Item == ____itemRosterElement.EquipmentElement.Item)
            {
                if (____itemRosterElement.EquipmentElement.Item != null && ____itemTableauEntity != null && ____itemRosterElement.EquipmentElement.Item.IsMeleeWeapon())
                {
                    ____itemTableauEntity.RemoveAllParticleSystems();
                    if (TORArtisanDistrictCampaignBehavior.Instance?.ItemBeingCrafted?.ItemTraits?.Where(x => x.WeaponParticlePreset != null).Count() > 0)
                    {
                        var item = ____itemRosterElement.EquipmentElement.Item;
                        if (item.PrimaryWeapon != null)
                        {
                            var length = item.PrimaryWeapon.GetRealWeaponLength();
                            float startOffsetPrc = 0;
                            switch (item.PrimaryWeapon.WeaponClass)
                            {
                                case WeaponClass.OneHandedSword:
                                case WeaponClass.TwoHandedSword:
                                    startOffsetPrc = 0.3f;
                                    break;
                                case WeaponClass.LowGripPolearm:
                                case WeaponClass.OneHandedPolearm:
                                case WeaponClass.TwoHandedPolearm:
                                    startOffsetPrc = 0.7f;
                                    break;
                                default:
                                    startOffsetPrc = 0.85f;
                                    break;
                            }
                            float startOffset = length * startOffsetPrc;
                            float effectlength = length - startOffset;
                            int num = (int)(effectlength / 0.1f);
                            if (num <= 0) num = 1;

                            foreach (var itemTrait in TORArtisanDistrictCampaignBehavior.Instance.ItemBeingCrafted.ItemTraits)
                            {
                                if (itemTrait != null && itemTrait.WeaponParticlePreset != null)
                                {
                                    if (!itemTrait.WeaponParticlePreset.IsUniqueSingleCopy)
                                    {
                                        for (int j = 0; j < num; j++)
                                        {
                                            MatrixFrame localFrame = new MatrixFrame(Mat3.Identity, new Vec3(0, 0, 0));
                                            localFrame.Elevate(startOffset + j * 0.1f);
                                            var psys = ParticleSystem.CreateParticleSystemAttachedToEntity(itemTrait.WeaponParticlePreset.ParticlePrefab, ____itemTableauEntity, ref localFrame);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(ItemTableauWidget), "StringId", MethodType.Setter)]
        public static void ForceUpdateRender(ItemTableauWidget __instance, string ____stringId)
        {
            __instance.TextureProvider?.SetProperty("StringId", ____stringId);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(ItemImageIdentifierVM), MethodType.Constructor, [typeof(ItemObject), typeof(string)])]
        public static void ForceUpdateRender2(ItemImageIdentifierVM __instance)
        {
            ImageIdentifier imageIdentifier = (ImageIdentifier)AccessTools.Property(typeof(ItemImageIdentifierVM), "ImageIdentifier").GetValue(__instance);
            AccessTools.Property(typeof(ItemImageIdentifierVM), "Id").SetValue(__instance, imageIdentifier.Id, null);
            __instance.OnPropertyChangedWithValue(imageIdentifier.Id, "Id");
        }
    }
}