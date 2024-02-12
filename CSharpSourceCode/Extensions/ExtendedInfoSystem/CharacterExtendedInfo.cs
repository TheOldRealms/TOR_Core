using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
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
        [XmlElement("ResourceCost")]
        public ResourceCostTuple ResourceCost = null;
    }

    [Serializable]
    public class ResourceCostTuple
    {
        [XmlAttribute]
        public string ResourceType = string.Empty;
        [XmlAttribute]
        public int UpkeepCost = 0;
        [XmlAttribute]
        public int UpgradeCost = 0;
    }

    [Serializable]
    public class ResistanceTuple
    {
        [XmlAttribute]
        public DamageType ResistedDamageType = DamageType.Invalid;
        [XmlAttribute]
        public float ReductionPercent = 0;
    }

    [Serializable]
    public class AmplifierTuple
    {
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

        public static AgentPropertyContainer InitNew()
        { 
            float[] damageProportions= new float[(int)DamageType.All+1];
            damageProportions[(int)DamageType.Physical] = 1;
            float[] damagePercentages= new float[(int)DamageType.All+1];  
            float[] resistancePercentages= new float[(int)DamageType.All+1]; 
            float[] additionalDamagePercentages= new float[(int)DamageType.All+1];
            return new AgentPropertyContainer(damageProportions, damagePercentages, resistancePercentages, additionalDamagePercentages);
        }
        
    }

    public enum PropertyMask : int
    {
        Attack = 0,
        Defense = 1,
        All = 2
    }

    [Flags]
    public enum AttackTypeMask
    {
        Ranged = 1,
        Melee = 2,
        Spell = 4,
        All = Ranged|Melee|Spell
    }
}
