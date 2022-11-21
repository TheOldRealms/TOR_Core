using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Mono.CompilerServices.SymbolWriter;
using TOR_Core.BattleMechanics.DamageSystem;

namespace TOR_Core.Extensions.ExtendedInfoSystem
{
    /// <summary>
    /// Contains Tow data of single unit or character template. 
    /// </summary>
    public class CharacterExtendedInfo
    {
        [XmlAttribute("id")]
        public string CharacterStringId;
        [XmlArray("Abilities")]
        public List<string> Abilities = new List<string>();
        [XmlArray("Attributes")]
        public List<string> CharacterAttributes = new List<string>();
        [XmlArray("DamageProportions")]
        public List<DamageProportionTuple> DamageProportions = new List<DamageProportionTuple>();
        [XmlArray("Resistances")]
        public List<ResistanceTuple> Resistances = new List<ResistanceTuple>();
        [XmlArray("DamageAmplifiers")]
        public List<AmplifierTuple> DamageAmplifiers = new List<AmplifierTuple>();
    }

    [Serializable]
    public class ResistanceTuple
    {
        [XmlAttribute]
        public AttackTypeMask AttackTypeMask = AttackTypeMask.All;
        [XmlAttribute]
        public DamageType ResistedDamageType = DamageType.Invalid;
        [XmlAttribute]
        public float ReductionPercent = 0;
    }

    [Serializable]
    public class AmplifierTuple
    {
        [XmlAttribute]
        public AttackTypeMask AttackTypeMask = AttackTypeMask.All;
        [XmlAttribute]
        public DamageType AmplifiedDamageType = DamageType.Invalid;
        [XmlAttribute]
        public float DamageAmplifier = 0;
    }

    [Serializable]
    public class DamageProportionTuple
    {
        [XmlAttribute]
        public DamageType DamageType = DamageType.Invalid;
        [XmlAttribute]
        public float Percent = 1;
        public DamageProportionTuple()
        {
        }
        public DamageProportionTuple(DamageType damageType, float percent)
        {
            DamageType = damageType;
            Percent = percent;
        }
    }

    /// <summary>
    /// Contains summarized Agent properties for Damage and Resistances. Cannot be changed.
    /// </summary>
    public struct AgentPropertyContainer
    {
        public readonly float[] DamageProportions;
        public readonly float[] DamagePercentages;
        public readonly float[] ResistancePercentages;
        public readonly float[] AdditionalDamagePercentages;
        public AgentPropertyContainer(float[] damageProportions, float[] damagePercentages, float[] resistancePercentages, float[] additionalDamagePercentages)
        {
            DamageProportions = damageProportions;
            DamagePercentages = damagePercentages;
            ResistancePercentages = resistancePercentages;
            AdditionalDamagePercentages = additionalDamagePercentages;
        }
    }

    public enum PropertyMask : int
    {
        Attack = 0,
        Defense = 1,
        All = 2
    }
    public enum  AttackTypeMask
    {
        None = 0,
        Melee = 1,
        Range = 2,
        Spell = 3,
        All=4
    }
}
