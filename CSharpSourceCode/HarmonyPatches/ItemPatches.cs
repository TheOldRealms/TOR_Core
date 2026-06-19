using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
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
        public static void OnlyProduceTorItems(ref bool __result, ItemObject item, Town townComponent)
        {
            if (__result && item.Culture == townComponent.Culture) __result = item.IsTorItem();
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(WorkshopsCampaignBehavior), "GetRandomItemAux")]
        public static bool OnlyProduceCultureMatchingItems(ref EquipmentElement __result, ItemCategory itemGroupBase, Town townComponent, Dictionary<ItemCategory, List<ItemObject>> ____itemsInCategory)
        {
            if (itemGroupBase.IsTradeGood) return true;

            if (townComponent == null) return true;

            if (____itemsInCategory.TryGetValue(itemGroupBase, out var allItemsInGroup))
            {
                var weightedItems = new List<(ItemObject Item, float Weight)>();

                foreach (ItemObject item in allItemsInGroup)
                {
                    if (item.ItemCategory == itemGroupBase && (item.Culture == null || item.Culture == townComponent.Culture))
                    {
                        weightedItems.Add((item, GetWorkshopProductionWeight(item)));
                    }
                }

                if (weightedItems.Count < 1)
                {
                    return true;
                }

                ItemObject itemObject = MBRandom.ChooseWeighted(weightedItems);

                ItemModifierGroup itemModifierGroup = null;
                if (itemObject != null)
                {
                    ItemComponent itemComponent = itemObject.ItemComponent;
                    itemModifierGroup = itemComponent?.ItemModifierGroup;
                }

                ItemModifier itemModifier = null;
                if (itemModifierGroup != null)
                {
                    itemModifier = itemModifierGroup.GetRandomItemModifierProductionScoreBased();
                }

                __result = new EquipmentElement(itemObject, itemModifier, null, false);
                return false;
            }
            return true;
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