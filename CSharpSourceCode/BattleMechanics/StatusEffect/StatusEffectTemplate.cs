using System;
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
        [XmlAttribute("change_per_tick")]
        public float ChangePerTick { get; set; } = 0;
        [XmlAttribute("duration")]
        public int BaseDuration { get; set; } = 0;
        [XmlAttribute("type")]
        public EffectType Type { get; set; } = EffectType.Invalid;
        [XmlAttribute("damage_type")]
        public DamageType DamageType { get; set; } = DamageType.Physical;
        [XmlElement]
        public AmplifierTuple DamageAmplifier { get; set; } = new AmplifierTuple();
        [XmlElement]
        public ResistanceTuple Resistance { get; set; } = new ResistanceTuple();

        public enum EffectType
        {
            HealthOverTime,
            DamageOverTime,
            DamageAmplification,
            Resistance,
            WindsRegeneration,
            CoolDownReduction,
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
    }
}
