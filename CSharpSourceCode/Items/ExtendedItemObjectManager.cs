using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.LinQuick;
using TaleWorlds.ObjectSystem;
using TOR_Core.Extensions;
using TOR_Core.Utilities;

namespace TOR_Core.Items
{
    public static class ExtendedItemObjectManager
    {
        private static Dictionary<string, ExtendedItemObjectProperties> _itemToInfoMap = [];
        private static string XMLPath = TORPaths.TORCoreModuleExtendedDataPath + "tor_extendeditemproperties.xml";
        private static HashSet<string> _runtimeDuplicatedItemIds = [];
        private static List<string> _humanCompatibleRaces = new()
        {
            "human",
            "vampire",
            "chaos_ud_cultist",
            "elf",
            "necrarch",
            "bretonnian"
        };

        internal static ExtendedItemObjectProperties GetAdditionalProperties(string itemId)
        {
            ExtendedItemObjectProperties info = null;
            _itemToInfoMap.TryGetValue(itemId, out info);
            if (info != null) info = info.Clone();
            return info;
        }

        internal static ExtendedItemObjectProperties GetAdditionalPropertiesReadOnly(string itemId)
        {
            _itemToInfoMap.TryGetValue(itemId, out var info);
            return info;
        }

        public static bool HasMagicItemId(string uiStringID)
        {
            var modifier = MBObjectManager.Instance.GetObjectTypeList<ItemModifier>().FirstOrDefaultQ(x => uiStringID.EndsWith(x.StringId));

            if (modifier == null)
            {
                var entry = _itemToInfoMap.FirstOrDefaultQ(x => uiStringID == x.Key);

                if (entry.Key != null && entry.Value.ItemTraits.AnyQ()) return true;
            }
            else
            {
                var entrys = _itemToInfoMap.Where(x => uiStringID.Contains(x.Key));

                return entrys.AnyQ(entry => entry.Key.EndsWith(modifier.StringId));
            }

            return false;
        }
        public static bool IsRuntimeDuplicatedItem(ItemObject item)
        {
            return item != null && _runtimeDuplicatedItemIds.Contains(item.StringId);
        }

        public static void AddCraftedItem(string oldId, string newId, List<string> traits)
        {
            _runtimeDuplicatedItemIds.Add(newId);

            if (_itemToInfoMap.ContainsKey(newId))
            {
                _itemToInfoMap[newId].ItemTraits = traits ?? new List<string>();
                return;
            }
            if (_itemToInfoMap.TryGetValue(oldId, out ExtendedItemObjectProperties info))
            {
                ExtendedItemObjectProperties newInfo = info.Clone();
                newInfo.ItemStringId = newId;
                newInfo.Description = "Crafted " + info.Description;
                newInfo.ItemTraits = traits ?? new List<string>();
                _itemToInfoMap.Add(newId, newInfo);
            }
            else
            {
                var newInfo = ExtendedItemObjectProperties.CreateDefault(newId);
                newInfo.ItemStringId = newId;
                newInfo.Description = "Crafted " + newInfo.Description;
                newInfo.ItemTraits = traits ?? new List<string>();
                _itemToInfoMap.Add(newId, newInfo);
            }
        }

        public static bool CanCharacterUseItem(ItemObject item, CharacterObject character)
        {
            return CanCharacterUseItemBasedOnRace(item, character);
        }

        public static bool CanCharacterUseItemBasedOnRace(ItemObject item, BasicCharacterObject character)
        {
            if (!item.HasArmorComponent || item.ItemType == ItemObject.ItemTypeEnum.HorseHarness) return true; //non-armor items can be used by anyone
            var info = item.GetTorSpecificData();
            if (info.RaceLock == "human") //special case, human lock should cover human-compatible races like elves and vampires
            {
                var race = FaceGen.GetRaceNames()[character.Race];
                return _humanCompatibleRaces.Contains(race);
            }
            else return character.Race == FaceGen.GetRaceOrDefault(info.RaceLock);
        }

        public static void LoadXML()
        {
            _itemToInfoMap.Clear();
            _runtimeDuplicatedItemIds.Clear();

            if (File.Exists(XMLPath))
            {
                XmlSerializer ser = new XmlSerializer(typeof(List<ExtendedItemObjectProperties>));
                using var stream = File.OpenRead(XMLPath);

                List<ExtendedItemObjectProperties> list = ser.Deserialize(stream) as List<ExtendedItemObjectProperties>;
                if (list != null && list.Count > 0)
                {
                    foreach (var item in list)
                    {
                        _itemToInfoMap[item.ItemStringId] = item;
                    }
                }
            }
        }
    }
}