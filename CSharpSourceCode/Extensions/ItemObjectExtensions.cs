using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TOR_Core.Items;

namespace TOR_Core.Extensions
{
    public static class ItemObjectExtensions
    {
        public static List<ItemTrait> GetTraits(this ItemObject item)
        {
            var result = ExtendedItemObjectManager.GetAdditionalProperties(item.StringId);
            if (result == null) result = ExtendedItemObjectProperties.CreateDefault(item.StringId);
            return result.ItemTraits.ToList();
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
                result.ItemTraits.AddRange(comp.GetDynamicTraits(item));
            }

            return result;
        }

        public static bool HasTrait(this ItemObject item)
        {
            if (item.GetTraits() != null)
            {
                return item.GetTraits().Count > 0;
            }
            else return false;
        }

        public static bool HasTrait(this ItemObject item, Agent agent)
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


        public static bool IsExplosiveAmmunition(this ItemObject itemObject)
        {
            return IsAmmunitionItem(itemObject) && itemObject.ToString().Contains("grenade") ||
                itemObject.ToString().Contains("cannon") ;
        }

        /// <summary>
        /// Checks if the current weapon is shooting scatter shots or grenades, or is scatter/grenade ammunition
        /// </summary>
        /// <param name="itemObject"></param>
        /// <returns></returns>
        public static bool IsSpecialAmmunitionItem(this ItemObject itemObject)
        {
            if (!IsAmmunitionItem(itemObject))
                return false;

            if (itemObject.ToString().Contains("blunderbuss"))
            {
                return true;
            }

            return itemObject.ToString().Contains("grenade") || itemObject.ToString().Contains("scatter");
        }

        public static bool IsAmmunitionItem(this ItemObject itemObject)
        {
            if (itemObject?.WeaponComponent?.PrimaryWeapon == null) return false;

            return itemObject.WeaponComponent.PrimaryWeapon.IsRangedWeapon ||
                   itemObject.WeaponComponent.PrimaryWeapon.IsAmmo;
        }
    }
}