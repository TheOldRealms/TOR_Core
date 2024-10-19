using System;
using System.Xml.Serialization;
using TOR_Core.Extensions.ExtendedInfoSystem;

namespace TOR_Core.Items
{
    [Serializable]
    public class ItemTrait : IEquatable<ItemTrait>
    {
        [XmlAttribute]
        public string ItemTraitName { get; set; }
        [XmlElement]
        public string ItemTraitDescription { get; set; }
        [XmlElement]
        public ResistanceTuple ResistanceTuple { get; set; }
        [XmlElement]
        public AmplifierTuple AmplifierTuple { get; set; }
        [XmlElement]
        public DamageProportionTuple AdditionalDamageTuple { get; set; }
        [XmlElement]
        public SkillTuple SkillTuple { get; set; }
        [XmlAttribute]
        public string OnHitScriptName { get; set; } = "none";
        [XmlAttribute]
        public string ImbuedStatusEffectId { get; set; } = "none";
        [XmlAttribute]
        public float ImbuedStatusEffectChance { get; set; } = 0.25f;
        [XmlAttribute]
        public string IconName { get; set; } = "none";
        [XmlElement]
        public WeaponParticlePreset WeaponParticlePreset { get; set; }

        public bool Equals(ItemTrait other)
        {
            return ItemTraitName == other.ItemTraitName &&
                ItemTraitDescription == other.ItemTraitDescription &&
                OnHitScriptName == other.OnHitScriptName &&
                ImbuedStatusEffectId == other.ImbuedStatusEffectId &&
                WeaponParticlePreset.ParticlePrefab == other.WeaponParticlePreset.ParticlePrefab &&
                WeaponParticlePreset.IsUniqueSingleCopy == other.WeaponParticlePreset.IsUniqueSingleCopy;
        }
    }

    [Serializable]
    public class WeaponParticlePreset
    {
        [XmlAttribute]
        public string ParticlePrefab { get; set; } = "invalid";
        [XmlAttribute]
        public bool IsUniqueSingleCopy { get; set; } = false;
    }

    [Serializable]
    public class SkillTuple
    {
        [XmlAttribute]
        public bool IsAbility { get; set; } = false;
        [XmlAttribute]
        public string SkillId { get; set; }
        [XmlAttribute]
        public float SkillExp { get; set; } = 0;
        [XmlAttribute]
        public float LearningTime { get; set; } = 1;
        
    }
}