using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using TOR_Core.BattleMechanics.DamageSystem;
using TOR_Core.Extensions.ExtendedInfoSystem;
using static TOR_Core.Utilities.TORParticleSystem;

namespace TOR_Core.BattleMechanics.StatusEffect
{
    public class StatusEffectTemplate : IEquatable<StatusEffectTemplate>
    {
        [XmlAttribute("id")]
        public string Id { get; set; }
        [XmlAttribute("particle_id")]
        public string ParticleId { get; set; }
        [XmlAttribute("particle_intensity")]
        public ParticleIntensity ParticleIntensity { get; set; }
        [XmlAttribute("apply_particle_to_root_bone_only")]
        public bool ApplyToRootBoneOnly { get; set; } = false;
        [XmlAttribute("base_effect_value")]
        public float BaseEffectValue { get; set; } = 0;
        [XmlAttribute("type")]
        public EffectType Type { get; set; } = EffectType.Invalid;
        [XmlAttribute("damage_type")]
        public DamageType DamageType { get; set; } = DamageType.Physical;
        [XmlAttribute("applies_for_attack_type")]
        public AttackTypeMask AttackTypeMask { get; set; } = AttackTypeMask.Melee | AttackTypeMask.Ranged | AttackTypeMask.Spell;
        [XmlElement]
        public List<string> TemporaryAttributes { get; set; } = new List<string>();

        public enum EffectType
        {
            HealthOverTime,
            DamageOverTime,
            DamageAmplification,
            Resistance,
            TemporaryAttributeOnly,
            Invalid
        };

        public bool IsBuffEffect => Type != EffectType.Invalid && Type != EffectType.DamageOverTime;

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (obj.GetType() != typeof(StatusEffect))
            {
                return false;
            }
            return GetHashCode() == ((StatusEffect)obj).GetHashCode();
        }

        public bool Equals(StatusEffectTemplate other)
        {
            return GetHashCode() == other.GetHashCode();
        }

        public StatusEffectTemplate Clone(string newId)
        {
            return new StatusEffectTemplate()
            {
                Id = newId,
                ParticleId = ParticleId,
                ParticleIntensity = ParticleIntensity,
                ApplyToRootBoneOnly = ApplyToRootBoneOnly,
                BaseEffectValue = BaseEffectValue,
                Type = Type,
                DamageType = DamageType,
                TemporaryAttributes = TemporaryAttributes
            };
        }
    }
}
