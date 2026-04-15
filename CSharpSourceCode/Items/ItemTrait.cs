using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using TOR_Core.Extensions.ExtendedInfoSystem;
using static TaleWorlds.Core.ItemObject;

namespace TOR_Core.Items
{
    [Serializable]
    [XmlInclude(typeof(ItemTrait))]
    public class ItemTrait : IEquatable<ItemTrait>
    {
        private static ItemTrait _invalid;

        [XmlAttribute]
        public string ItemTraitStringId { get; set; }
        [XmlAttribute]
        public string ItemTraitName { get; set; } = "Invalid ItemTrait";
        [XmlElement]
        public string ItemTraitDescription { get; set; } = "invalid";
        [XmlElement]
        public ResistanceTuple ResistanceTuple { get; set; }
        [XmlElement]
        public AmplifierTuple AmplifierTuple { get; set; }
        [XmlElement]
        public DamageProportionTuple AdditionalDamageTuple { get; set; }
        [XmlElement("OnWeaponHitScript")]
        public WeaponScriptTuple OnWeaponHitScript { get; set; }
        [XmlElement("OnInventoryUseScript")]
        public InventoryScriptTuple OnInventoryUseScript { get; set; }
        [XmlAttribute]
        public string ImbuedStatusEffectId { get; set; } = "none";
        [XmlAttribute]
        public float ImbuedEffectChance { get; set; } = 0.25f;
        [XmlAttribute]
        public string IconName { get; set; } = "none";
        [XmlElement("WeaponParticlePreset")]
        public WeaponParticlePreset WeaponParticlePreset { get; set; }
        [XmlAttribute]
        public bool IsCraftable { get; set; } = false;
        [XmlElement]
        public ItemTraitItemType ValidItemType { get; set; } = ItemTraitItemType.Invalid;
        [XmlElement]
        public TorTradeGoodType IngredientItem { get; set; } = TorTradeGoodType.Invalid;
        [XmlElement]
        public int IngredientAmount { get; set; } = 1;
        [XmlElement]
        public StatsTuple StatsTuple { get; set; }

        private ItemTrait() { }

        public static List<ItemTrait> All => ItemTraitManager.Instance.GetItemTraits();

        public static bool IsValidFor(ItemTrait itemTrait, ItemTypeEnum itemType)
        {
            bool result = false;
            switch (itemTrait.ValidItemType)
            {
                case ItemTraitItemType.Melee:
                    result = itemType == ItemTypeEnum.OneHandedWeapon ||
                             itemType == ItemTypeEnum.TwoHandedWeapon ||
                             itemType == ItemTypeEnum.Polearm;
                    break;
                case ItemTraitItemType.Thrown:
                    result = itemType == ItemTypeEnum.Thrown;
                    break;
                case ItemTraitItemType.Ammo:
                    result = itemType == ItemTypeEnum.Bullets ||
                             itemType == ItemTypeEnum.Bolts ||
                             itemType == ItemTypeEnum.Arrows ||
                             itemType == ItemTypeEnum.Thrown;
                    break;
                case ItemTraitItemType.Ranged:
                    result = itemType == ItemTypeEnum.Thrown ||
                             itemType == ItemTypeEnum.Bow ||
                             itemType == ItemTypeEnum.Crossbow ||

                             itemType == ItemTypeEnum.Musket ||
                             itemType == ItemTypeEnum.Pistol;
                    break;
                case ItemTraitItemType.Weapon:
                    result = itemType == ItemTypeEnum.OneHandedWeapon ||
                            itemType == ItemTypeEnum.TwoHandedWeapon ||
                            itemType == ItemTypeEnum.Thrown ||
                            itemType == ItemTypeEnum.Bow ||
                            itemType == ItemTypeEnum.Crossbow ||
                            itemType == ItemTypeEnum.Polearm ||
                            itemType == ItemTypeEnum.Musket ||
                            itemType == ItemTypeEnum.Pistol;
                    break;
                case ItemTraitItemType.Shield:
                    result = itemType == ItemTypeEnum.Shield;
                    break;
                case ItemTraitItemType.Armor:
                    result = itemType == ItemTypeEnum.BodyArmor ||
                            itemType == ItemTypeEnum.Cape ||
                            itemType == ItemTypeEnum.ChestArmor ||
                            itemType == ItemTypeEnum.HandArmor ||
                            itemType == ItemTypeEnum.HeadArmor ||
                            itemType == ItemTypeEnum.LegArmor;
                    break;
            }
            return result;
        }

        public static ItemTrait Invalid
        {
            get
            {
                if (_invalid == null)
                {
                    _invalid = new ItemTrait
                    {
                        ItemTraitStringId = "invalid",
                        ItemTraitName = "Invalid ItemTrait",
                        ItemTraitDescription = "Invalid ItemTrait",
                        ResistanceTuple = new ResistanceTuple(),
                        AmplifierTuple = new AmplifierTuple(),
                        AdditionalDamageTuple = new DamageProportionTuple(),
                        OnWeaponHitScript = new WeaponScriptTuple(),
                        OnInventoryUseScript = new InventoryScriptTuple(),
                        ImbuedStatusEffectId = "none",
                        ImbuedEffectChance = 0.25f,
                        IconName = "none",
                        WeaponParticlePreset = new WeaponParticlePreset(),
                        IsCraftable = false
                    };
                }
                return _invalid;
            }
        }

        public bool Equals(ItemTrait other)
        {
            if (other == null) return false;

            return ItemTraitStringId == other.ItemTraitStringId &&
                   ItemTraitName == other.ItemTraitName &&
                   ItemTraitDescription == other.ItemTraitDescription &&
                   Equals(ResistanceTuple, other.ResistanceTuple) &&
                   Equals(AmplifierTuple, other.AmplifierTuple) &&
                   Equals(AdditionalDamageTuple, other.AdditionalDamageTuple) &&
                   Equals(OnWeaponHitScript, other.OnWeaponHitScript) &&
                   Equals(OnInventoryUseScript, other.OnInventoryUseScript) &&
                   ImbuedStatusEffectId == other.ImbuedStatusEffectId &&
                   ImbuedEffectChance.Equals(other.ImbuedEffectChance) &&
                   IconName == other.IconName &&
                   Equals(WeaponParticlePreset, other.WeaponParticlePreset) &&
                   IsCraftable == other.IsCraftable &&
                   ValidItemType == other.ValidItemType &&
                   IngredientItem == other.IngredientItem &&
                   IngredientAmount == other.IngredientAmount &&
                   Equals(StatsTuple, other.StatsTuple);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as ItemTrait);
        }

        public override int GetHashCode()
        {
            return (ItemTraitStringId, ItemTraitName, ItemTraitDescription, ResistanceTuple, AmplifierTuple, AdditionalDamageTuple, OnWeaponHitScript, OnInventoryUseScript, ImbuedStatusEffectId, ImbuedStatusEffectChance: ImbuedEffectChance, IconName, WeaponParticlePreset, IsCraftable, ValidItemType, IngredientItem, IngredientAmount, StatsTuple).GetHashCode();
        }

        public static bool operator ==(ItemTrait left, ItemTrait right)
        {
            if (ReferenceEquals(left, right)) return true;
            if (left is null || right is null) return false;
            return left.Equals(right);
        }

        public static bool operator !=(ItemTrait left, ItemTrait right)
        {
            return !(left == right);
        }
    }

    [Serializable]
    public class WeaponParticlePreset : IEquatable<WeaponParticlePreset>
    {
        [XmlAttribute]
        public string ParticlePrefab { get; set; } = "invalid";
        [XmlAttribute]
        public bool IsUniqueSingleCopy { get; set; } = false;

        public WeaponParticlePreset() { }

        public bool Equals(WeaponParticlePreset other)
        {
            if (other == null) return false;
            return ParticlePrefab == other.ParticlePrefab &&
                   IsUniqueSingleCopy == other.IsUniqueSingleCopy;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as WeaponParticlePreset);
        }

        public override int GetHashCode()
        {
            return (ParticlePrefab, IsUniqueSingleCopy).GetHashCode();
        }

        public static bool operator ==(WeaponParticlePreset left, WeaponParticlePreset right)
        {
            if (ReferenceEquals(left, right)) return true;
            if (left is null || right is null) return false;
            return left.Equals(right);
        }

        public static bool operator !=(WeaponParticlePreset left, WeaponParticlePreset right)
        {
            return !(left == right);
        }
    }

    [Serializable]
    public class WeaponScriptTuple : IEquatable<WeaponScriptTuple>
    {
        [XmlAttribute]
        public string WeaponScriptName { get; set; } = "invalid";
        [XmlArray("WeaponScriptArguments")]
        [XmlArrayItem("WeaponScriptArgument")]
        public List<string> WeaponScriptArguments { get; set; } = [];

        public WeaponScriptTuple() { }

        public bool Equals(WeaponScriptTuple other)
        {
            if (other == null) return false;
            return WeaponScriptName == other.WeaponScriptName &&
                   WeaponScriptArguments.SequenceEqual(other.WeaponScriptArguments);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as WeaponScriptTuple);
        }

        public override int GetHashCode()
        {
            return (WeaponScriptName, WeaponScriptArguments != null
                ? WeaponScriptArguments.Aggregate(0, (hash, arg) => hash ^ (arg?.GetHashCode() ?? 0))
                : 0).GetHashCode();
        }

        public static bool operator ==(WeaponScriptTuple left, WeaponScriptTuple right)
        {
            if (ReferenceEquals(left, right)) return true;
            if (left is null || right is null) return false;
            return left.Equals(right);
        }

        public static bool operator !=(WeaponScriptTuple left, WeaponScriptTuple right)
        {
            return !(left == right);
        }
    }

    [Serializable]
    public class InventoryScriptTuple : IEquatable<InventoryScriptTuple>
    {
        [XmlAttribute]
        public string InventoryScriptName { get; set; } = "invalid";
        [XmlArray("InventoryScriptArguments")]
        [XmlArrayItem("InventoryScriptArgument")]
        public List<string> InventoryScriptArguments { get; set; } = [];

        public InventoryScriptTuple() { }

        public bool Equals(InventoryScriptTuple other)
        {
            if (other == null) return false;
            return InventoryScriptName == other.InventoryScriptName &&
                   InventoryScriptArguments.SequenceEqual(other.InventoryScriptArguments);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as InventoryScriptTuple);
        }

        public override int GetHashCode()
        {
            return (InventoryScriptName, InventoryScriptArguments != null
                ? InventoryScriptArguments.Aggregate(0, (hash, arg) => hash ^ (arg?.GetHashCode() ?? 0))
                : 0).GetHashCode();
        }

        public static bool operator ==(InventoryScriptTuple left, InventoryScriptTuple right)
        {
            if (ReferenceEquals(left, right)) return true;
            if (left is null || right is null) return false;
            return left.Equals(right);
        }

        public static bool operator !=(InventoryScriptTuple left, InventoryScriptTuple right)
        {
            return !(left == right);
        }
    }

    [Serializable]
    public class StatsTuple : IEquatable<StatsTuple>
    {
        [XmlAttribute]
        public ItemTraitStatType StatType { get; set; } = ItemTraitStatType.Invalid;
        [XmlAttribute]
        public string SkillId { get; set; } = "none";
        [XmlAttribute]
        public float Value { get; set; } = 0f;

        public bool Equals(StatsTuple other)
        {
            if (other == null) return false;
            return StatType == other.StatType &&
                   SkillId == other.SkillId &&
                   Value.Equals(other.Value);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as StatsTuple);
        }

        public override int GetHashCode()
        {
            return (StatType, SkillId, Value).GetHashCode();
        }

        public static bool operator ==(StatsTuple left, StatsTuple right)
        {
            if (ReferenceEquals(left, right)) return true;
            if (left is null || right is null) return false;
            return left.Equals(right);
        }

        public static bool operator !=(StatsTuple left, StatsTuple right)
        {
            return !(left == right);
        }
    }

    [Serializable]
    public enum ItemTraitItemType
    {
        Invalid,
        Weapon,
        Armor,
        Ammo,
        Shield,
        Ranged,
        Thrown,
        Melee
    }

    [Serializable]
    public enum ItemTraitStatType
    {
        Invalid,
        HealthMax,
        HealthRegen,
        WindsOfMagicMax,
        WindsOfMagicRegen,
        PartySpeed,         //applied as a percentage
        Skill,
        ShieldHealth,
        ArmorPenetration,
        SpellRadius,
        MovementSpeed,
        CustomResourceGain,
        MissileSpeed,
        SwingSpeed,
        ReloadSpeed,        // percentage based
        ShieldDamage,       // percentage based adds shield damage as factor
        Cleave,              // allows to cleave through enemies. No values considered.
        MultiPenetration,    // Only ranged weapons. No values considered.
        ScatterShot,         // Number of projectiles for scatter shot.
        ShieldPenetration,   // Arrows can penetrate shields. No values considered.
    }
}