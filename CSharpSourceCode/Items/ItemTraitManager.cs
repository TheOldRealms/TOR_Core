using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using TaleWorlds.LinQuick;
using TaleWorlds.MountAndBlade.GauntletUI.Widgets;
using TOR_Core.Utilities;
using LogLevel = NLog.LogLevel;

namespace TOR_Core.Items
{
    public class ItemTraitManager
    {
        private static readonly Lazy<ItemTraitManager> _instance = new(() => new ItemTraitManager());
        private static readonly string _fileName = "tor_itemtraits.xml";
        private List<ItemTrait> _itemTraits;
        private Dictionary<string, ItemTrait> _itemTraitByStringId;
        private List<ItemTrait> _itemTraitCacheSource;
        private int _itemTraitCacheSourceCount;

        public static ItemTraitManager Instance => _instance.Value;

        private ItemTraitManager()
        {
            _itemTraits = [];
        }

        public static void LoadItemTraits()
        {
            var path = TORPaths.TORCoreModuleExtendedDataPath + _fileName;
            if (File.Exists(path))
            {
                XmlSerializer serializer = new(typeof(List<ItemTrait>), new XmlRootAttribute("ItemTraits"));
                using FileStream fileStream = new(path, FileMode.Open);
                Instance._itemTraits = (List<ItemTrait>)serializer.Deserialize(fileStream);
            }
            else
            {
                throw new FileNotFoundException($"The file at path {path} does not exist.");
            }

            Instance._itemTraits = Instance._itemTraits.Where(ValidateItemTrait).ToListQ();
            Instance.RebuildItemTraitCache();
        }

        public List<ItemTrait> GetItemTraits()
        {
            return _itemTraits;
        }

        public ItemTrait GetItemTraitByStringId(string itemTraitStringId)
        {
            if (string.IsNullOrWhiteSpace(itemTraitStringId))
            {
                return null;
            }

            if (_itemTraitByStringId == null ||
                !ReferenceEquals(_itemTraitCacheSource, _itemTraits) ||
                _itemTraitCacheSourceCount != _itemTraits.Count)
            {
                RebuildItemTraitCache();
            }

            _itemTraitByStringId.TryGetValue(itemTraitStringId, out var itemTrait);
            return itemTrait;
        }

        private void RebuildItemTraitCache()
        {
            var itemTraitCacheSource = _itemTraits;
            var itemTraitByStringId = new Dictionary<string, ItemTrait>(StringComparer.Ordinal);

            foreach (var itemTrait in itemTraitCacheSource)
            {
                if (string.IsNullOrWhiteSpace(itemTrait?.ItemTraitStringId) ||
                    itemTraitByStringId.ContainsKey(itemTrait.ItemTraitStringId))
                {
                    continue;
                }

                itemTraitByStringId.Add(itemTrait.ItemTraitStringId, itemTrait);
            }
            _itemTraitByStringId = itemTraitByStringId;
            _itemTraitCacheSource = itemTraitCacheSource;
            _itemTraitCacheSourceCount = itemTraitCacheSource.Count;
        }


        private static bool ValidateItemTrait(ItemTrait itemTrait)
        {
            switch (itemTrait.ValidItemType)
            {
                case ItemTraitItemType.Invalid:
                    break;
                case ItemTraitItemType.Melee:
                case ItemTraitItemType.Weapon:
                case ItemTraitItemType.Ranged:
                case ItemTraitItemType.Thrown:
                case ItemTraitItemType.Shield:
                case ItemTraitItemType.Ammo:
                    if (itemTrait.StatsTuple != null && !IsValidForExchangableEquipmentItems(itemTrait, out var equipmentReason))
                    {
                        TORCommon.Log(equipmentReason, LogLevel.Error);
                        return false;
                    }
                    break;
                case ItemTraitItemType.Armor:
                    if (itemTrait.StatsTuple != null && !IsValidForArmor(itemTrait, out var armorReason))
                    {
                        TORCommon.Log(armorReason, LogLevel.Error);
                        return false;
                    }
                    break;
            }


            return true;
        }

        private static bool IsValidForExchangableEquipmentItems(ItemTrait itemTrait, out string reason)
        {
            reason = "";
            var isValid = true;
            switch (itemTrait.StatsTuple.StatType)
            {
                case ItemTraitStatType.HealthMax:
                case ItemTraitStatType.HealthRegen:
                case ItemTraitStatType.WindsOfMagicMax:
                case ItemTraitStatType.WindsOfMagicRegen:
                case ItemTraitStatType.Skill:
                case ItemTraitStatType.SpellRadius:
                case ItemTraitStatType.MovementSpeed:
                    isValid = false;
                    break;
                case ItemTraitStatType.ShieldHealth:
                    if (itemTrait.ValidItemType != ItemTraitItemType.Shield)
                    {
                        reason = itemTrait.ItemTraitStringId + " failed creation. Stat type " + itemTrait.StatsTuple.StatType + " does not match" + itemTrait.ValidItemType;
                        isValid = false;
                    }

                    break;
                case ItemTraitStatType.ShieldDamage:
                    if (itemTrait.ValidItemType == ItemTraitItemType.Shield)
                        isValid = false;
                    break;
                case ItemTraitStatType.SwingSpeed:
                case ItemTraitStatType.Cleave:
                    if (itemTrait.ValidItemType != ItemTraitItemType.Melee)
                    {
                        isValid = false;
                    }

                    break;
                case ItemTraitStatType.MissileSpeed:
                case ItemTraitStatType.MultiPenetration:
                case ItemTraitStatType.ShieldPenetration:
                    if (itemTrait.ValidItemType != ItemTraitItemType.Ranged && itemTrait.ValidItemType != ItemTraitItemType.Thrown && itemTrait.ValidItemType != ItemTraitItemType.Ammo)
                    {
                        isValid = false;
                    }
                    break;
                case ItemTraitStatType.ReloadSpeed:
                    if (itemTrait.ValidItemType != ItemTraitItemType.Ranged)
                        isValid = false;
                    break;

            }

            if (!isValid)
            {
                reason = itemTrait.ItemTraitStringId + " failed creation. Stat type " + itemTrait.StatsTuple.StatType + " does not match " + itemTrait.ValidItemType;
                return false;
            }

            return true;
        }


        private static bool IsValidForArmor(ItemTrait itemTrait, out string reason)
        {
            reason = "";
            var isValid = true;

            switch (itemTrait.StatsTuple.StatType)
            {
                case ItemTraitStatType.MissileSpeed:
                case ItemTraitStatType.SwingSpeed:
                case ItemTraitStatType.ReloadSpeed:
                case ItemTraitStatType.ShieldDamage:
                case ItemTraitStatType.Cleave:
                case ItemTraitStatType.MultiPenetration:
                case ItemTraitStatType.ArmorPenetration:
                case ItemTraitStatType.ShieldHealth:
                    isValid = false;
                    break;
            }

            if (!isValid)
            {
                reason = itemTrait.ItemTraitStringId + " failed creation. Stat type " + itemTrait.StatsTuple.StatType + " does not match" + itemTrait.ValidItemType;
                return false;
            }

            return true;
        }


    }


    public class TORInvalidItemTraitException(string reason) : Exception(reason)
    {

    }
}