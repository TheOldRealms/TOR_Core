using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Library.NewsManager;
using TaleWorlds.MountAndBlade;
using TOR_Core.BattleMechanics.DamageSystem;
using TOR_Core.Items;
using TOR_Core.Utilities;

namespace TOR_Core.Extensions
{
    public static class ItemObjectExtensions
    {
        public static bool IsMeleeWeapon(this ItemObject item)
        {
            return item.ItemType == ItemObject.ItemTypeEnum.OneHandedWeapon ||
                item.ItemType == ItemObject.ItemTypeEnum.TwoHandedWeapon ||
                item.ItemType == ItemObject.ItemTypeEnum.Polearm;
        }

        public static bool IsWeapon(this ItemObject item)
        {
            var itemType = item.ItemType;
            var result = itemType == ItemObject.ItemTypeEnum.OneHandedWeapon ||
                     itemType == ItemObject.ItemTypeEnum.TwoHandedWeapon ||
                     itemType == ItemObject.ItemTypeEnum.Thrown ||
                     itemType == ItemObject.ItemTypeEnum.Bow ||
                     itemType == ItemObject.ItemTypeEnum.Crossbow ||
                     itemType == ItemObject.ItemTypeEnum.Polearm ||
                     itemType == ItemObject.ItemTypeEnum.Musket ||
                     itemType == ItemObject.ItemTypeEnum.Pistol;
            return result;
        }

        public static bool IsArmor(this ItemObject item)
        {
            var itemType = item.ItemType;
            var result = itemType == ItemObject.ItemTypeEnum.BodyArmor ||
                     itemType == ItemObject.ItemTypeEnum.Cape ||
                     itemType == ItemObject.ItemTypeEnum.ChestArmor ||
                     itemType == ItemObject.ItemTypeEnum.HandArmor ||
                     itemType == ItemObject.ItemTypeEnum.HeadArmor ||
                     itemType == ItemObject.ItemTypeEnum.LegArmor;
            return result;
        }

        public static bool IsShield(this ItemObject item)
        {
            var itemType = item.ItemType;
            var result = itemType == ItemObject.ItemTypeEnum.Shield;
            return result;
        }

        public static bool IsEnchantable(this ItemObject item)
        {
            return (item.HasArmorComponent || item.HasWeaponComponent) && !item.IsMagicalItem();
        }

        public static bool IsInventoryUsable(this ItemObject item)
        {
            if (item.HasAnyTrait())
            {
                return item.GetTraits().Any(trait => trait.OnInventoryUseScript != null && !string.IsNullOrWhiteSpace(trait.OnInventoryUseScript.InventoryScriptName));
            }

            return false;

        }

        public static List<ItemTrait> GetTraits(this ItemObject item)
        {
            if (item == null) return new List<ItemTrait>();
            List<ItemTrait> result = [];
            var props = ExtendedItemObjectManager.GetAdditionalProperties(item.StringId);
            if (props == null) props = ExtendedItemObjectProperties.CreateDefault(item.StringId);
            if (props.ItemTraits != null && props.ItemTraits.Count > 0)
            {
                foreach (var trait in props.ItemTraits)
                {
                    var itemTrait = ItemTrait.All.FirstOrDefault(x => x.ItemTraitStringId == trait);
                    if (itemTrait != null)
                    {
                        result.Add(itemTrait);
                    }
                }
            }
            return result;
        }

        public static List<ItemTrait> GetTraits(this ItemObject item, Agent agent)
        {
            var result = item.GetTraits();
            var comp = agent.GetComponent<ItemTraitAgentComponent>();
            if (comp != null)
            {
                if (result == null) result = new List<ItemTrait>();
                result.AddRange(comp.GetDynamicTraits(item));
            }

            return result;
        }

        public static ExtendedItemObjectProperties GetTorSpecificData(this ItemObject item)
        {
            var result = ExtendedItemObjectManager.GetAdditionalProperties(item.StringId);
            if (result == null) result = ExtendedItemObjectProperties.CreateDefault(item.StringId);
            return result;
        }

        public static ExtendedItemObjectProperties GetTorSpecificData(this ItemObject item, Agent agent)
        {
            var result = item.GetTorSpecificData();
            if (result == null) result = ExtendedItemObjectProperties.CreateDefault(item.StringId);
            var comp = agent.GetComponent<ItemTraitAgentComponent>();
            if (comp != null)
            {
                result.ItemTraits.AddRange(comp.GetDynamicTraitIds(item));
            }

            return result;
        }

        public static bool HasAnyTrait(this ItemObject item)
        {
            if (item.GetTraits() != null)
            {
                return item.GetTraits().Count > 0;
            }
            else return false;
        }

        public static bool HasAnyLootTraits(this ItemObject item)
        {
            var traits = item.GetTraits();
            if (!traits.Any()) return false;
            foreach (var trait in traits)
            {
                return trait.ItemTraitStringId.Contains("lesser_loot");
            }

            return false;
        }

        public static bool HasAnyTrait(this ItemObject item, Agent agent)
        {
            if (item.GetTraits(agent) != null)
            {
                return item.GetTraits(agent).Count > 0;
            }
            else return false;
        }

        public static bool IsTorItem(this ItemObject item)
        {
            return item.StringId.StartsWith("tor_");
        }

        public static bool IsMagicalItem(this ItemObject item)
        {
            var info = item.GetTorSpecificData();
            if (info != null)
            {
                return info.DamageProportions.Any(x => x.DamageType != DamageType.Physical) || info.ItemTraits.Count > 0;
            }
            return false;
        }

        /// <summary>
        /// True if the item is, or can have, ammo and its id contains "grenade" or is WeaponClass.Boulder
        /// </summary>
        /// <remarks>Has a usage in the dismemberment system to determine if the body parts need to be flung away from the impact point.</remarks>
        public static bool IsExplosiveAmmunition(this ItemObject itemObject)
        {
            return IsAmmunitionItem(itemObject) && itemObject.StringId.Contains("grenade") ||
                itemObject.WeaponComponent?.PrimaryWeapon.WeaponClass == WeaponClass.Boulder;
        }

        /// <summary>
        /// True if the item has a weapon component of IsRangedWeapon or IsAmmo and has one of the AffectsArea flags.
        /// </summary>
        /// <remarks>Introduced for the Bullet Proof gunpowder perk to exclude both grenades and flamethrowers as IsExplosiveAmmunition couldn't be adapted without affecting its usage in the dismemberment system and drakegun canisters are cartridge-typed ammos like musket balls.</remarks>
        public static bool IsAreaAffectingAmmunition(this ItemObject itemObject)
        {
            //IsAmmunitionItem has built-in null checks for itemObject, WeaponComponent, and PrimaryWeapon
            return IsAmmunitionItem(itemObject) && itemObject.WeaponComponent.PrimaryWeapon.WeaponFlags.HasAnyFlag(WeaponFlags.AffectsArea | WeaponFlags.AffectsAreaBig);
        }

        /// <summary>
        /// True if the the item is an arrow, bolt, or cartridge, and has no AffectArea flag.
        /// </summary>
        /// <remarks>Current usage is by TORAgentApplyDamageModel where !weapon.IsEmpty; may need to add IsAmmunitionItem in the future if its usage is expanded to include contexts that can have null items, components, etc...</remarks>
        public static bool IsSmallArmsAmmunition(this ItemObject itemObject)
        {
            return itemObject.WeaponComponent.PrimaryWeapon.IsSmallArmsAmmunition() && !itemObject.IsAreaAffectingAmmunition();
        }

        private static bool IsSmallArmsAmmunition(this WeaponComponentData weapon)
        {
            bool result = false;
            switch (weapon.WeaponClass)
            {
                case WeaponClass.Arrow:
                case WeaponClass.Bolt:
                case WeaponClass.Cartridge:
                    result = true;
                    break;
                default:
                    break;
            }
            return result;
        }

        public static bool IsMagicalStaff(this ItemObject itemObject)
        {
            if (itemObject == null) return false;
            return itemObject.StringId.Contains("staff") && itemObject.WeaponComponent.PrimaryWeapon.IsMeleeWeapon;
        }

        public static bool IsGunPowderWeapon(this ItemObject itemObject)
        {
            if (itemObject == null) return false;
            if (itemObject.WeaponComponent?.PrimaryWeapon != null)
            {
                return itemObject.WeaponComponent.PrimaryWeapon.IsGunPowderWeapon();
            }

            return false;
        }

        public static bool IsGunPowderWeapon(this WeaponComponentData weapon)
        {
            if (weapon == null || !weapon.IsRangedWeapon) return false;
            return weapon.WeaponClass == WeaponClass.Cartridge || weapon.AmmoClass == WeaponClass.Cartridge;
        }

        /// <summary>
        /// Checks if the current weapon is a blunderbuss, or is scatter/grenade ammunition
        /// </summary>
        public static bool IsSpecialAmmunitionItem(this ItemObject itemObject)
        {
            if (!IsAmmunitionItem(itemObject))
                return false;

            if (itemObject.ToString().Contains("blunderbuss"))
            {
                return true;
            }

            return itemObject.StringId.Contains("grenade") || itemObject.StringId.Contains("scatter");
        }

        public static bool IsFlameThrowerItem(this ItemObject itemObject)
        {
            if (!IsAmmunitionItem(itemObject)) return false;

            return itemObject.StringId.Contains("canister") || itemObject.StringId.Contains("weapon_gun_drakegun");
        }

        public static bool IsAmmunitionItem(this ItemObject itemObject)
        {
            if (itemObject?.WeaponComponent?.PrimaryWeapon == null) return false;

            return itemObject.WeaponComponent.PrimaryWeapon.IsRangedWeapon ||
                   itemObject.WeaponComponent.PrimaryWeapon.IsAmmo;
        }

        public static void CopyPropertiesFrom(this ItemObject item, ItemObject other)
        {
            AccessTools.Property(typeof(ItemObject), "Culture").SetValue(item, other.Culture);
            AccessTools.Property(typeof(ItemObject), "ItemComponent").SetValue(item, other.ItemComponent);
            AccessTools.Property(typeof(ItemObject), "MultiMeshName").SetValue(item, other.MultiMeshName);
            AccessTools.Property(typeof(ItemObject), "HolsterMeshName").SetValue(item, other.HolsterMeshName);
            AccessTools.Property(typeof(ItemObject), "HolsterWithWeaponMeshName").SetValue(item, other.HolsterWithWeaponMeshName);
            AccessTools.Property(typeof(ItemObject), "ItemHolsters").SetValue(item, other.ItemHolsters);
            AccessTools.Property(typeof(ItemObject), "HolsterPositionShift").SetValue(item, other.HolsterPositionShift);
            AccessTools.Property(typeof(ItemObject), "FlyingMeshName").SetValue(item, other.FlyingMeshName);
            AccessTools.Property(typeof(ItemObject), "BodyName").SetValue(item, other.BodyName);
            AccessTools.Property(typeof(ItemObject), "SkeletonName").SetValue(item, other.SkeletonName);
            AccessTools.Property(typeof(ItemObject), "StaticAnimationName").SetValue(item, other.StaticAnimationName);
            AccessTools.Property(typeof(ItemObject), "HolsterBodyName").SetValue(item, other.HolsterBodyName);
            AccessTools.Property(typeof(ItemObject), "CollisionBodyName").SetValue(item, other.CollisionBodyName);
            AccessTools.Property(typeof(ItemObject), "RecalculateBody").SetValue(item, other.RecalculateBody);
            AccessTools.Property(typeof(ItemObject), "PrefabName").SetValue(item, other.PrefabName);
            AccessTools.Property(typeof(ItemObject), "Name").SetValue(item, other.Name);
            AccessTools.Property(typeof(ItemObject), "ItemFlags").SetValue(item, other.ItemFlags);
            AccessTools.Property(typeof(ItemObject), "Value").SetValue(item, other.Value);
            AccessTools.Property(typeof(ItemObject), "Weight").SetValue(item, other.Weight);
            AccessTools.Property(typeof(ItemObject), "Difficulty").SetValue(item, other.Difficulty);
            AccessTools.Property(typeof(ItemObject), "ArmBandMeshName").SetValue(item, other.ArmBandMeshName);
            AccessTools.Property(typeof(ItemObject), "IsFood").SetValue(item, other.IsFood);
            AccessTools.Property(typeof(ItemObject), "ScaleFactor").SetValue(item, other.ScaleFactor);
            AccessTools.Property(typeof(ItemObject), "WeaponDesign").SetValue(item, other.WeaponDesign);
            item.Type = other.Type;
        }
    }
}