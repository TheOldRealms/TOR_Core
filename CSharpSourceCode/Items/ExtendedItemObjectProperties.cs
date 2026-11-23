using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using TOR_Core.BattleMechanics.DamageSystem;
using TOR_Core.Extensions.ExtendedInfoSystem;

namespace TOR_Core.Items
{
    [Serializable]
    public enum HideHandType
    {
        None = 0,
        Right = 1,
        Left = 2,
        Both = 3
    }

    [Serializable]
    public enum WeaponGovernance
    {
        OneHanded = 0,
        Berserk = 1
    }

    [Serializable]
    public class ExtendedItemObjectProperties
    {
        [XmlAttribute]
        public string ItemStringId;
        [XmlAttribute]
        public string RaceLock = "human";
        [XmlElement("HideHand")]
        public HideHandType HideHand = HideHandType.None;
        [XmlElement("ForearmMesh")]
        public string ForearmMesh = "";
        [XmlElement("GovernedBy")]
        public WeaponGovernance GovernedBy = WeaponGovernance.OneHanded;
        [XmlElement]
        public string Description = "";
        [XmlArray("DamageProportions")]
        public List<DamageProportionTuple> DamageProportions = [];
        [XmlArray("ItemTraits")]
        [XmlArrayItem("ItemTrait")]
        public List<string> ItemTraits = [];

        public ExtendedItemObjectProperties() { }

        private ExtendedItemObjectProperties(string id, DamageType defaultDamageType = DamageType.Physical)
        {
            ItemStringId = id;
            DamageProportionTuple proportionTuple = new()
            {
                DamageType = defaultDamageType,
                Percent = 1f
            };
            DamageProportions.Add(proportionTuple);
        }
        public static ExtendedItemObjectProperties CreateDefault(string id)
        {
            return new ExtendedItemObjectProperties(id);
        }

        public ExtendedItemObjectProperties Clone()
        {
            var prop = new ExtendedItemObjectProperties
            {
                ItemStringId = ItemStringId,
                Description = Description,
                RaceLock = RaceLock,
                HideHand = HideHand,
                ForearmMesh = ForearmMesh,
                GovernedBy = GovernedBy,
                ItemTraits = []
            };
            foreach (var trait in ItemTraits)
            {
                prop.ItemTraits.Add(trait);
            }
            prop.DamageProportions = [.. DamageProportions];
            return prop;
        }
    }
}