using System;
using System.Xml.Serialization;

namespace TOR_Core.AbilitySystem
{
    [Serializable]
    public class SeekerParameters
    {
        [XmlAttribute] 
        public float Proportional = 0.5f;
        [XmlAttribute] 
        public float Derivative = 0f;
        
        [XmlAttribute]
        public float MaxDistance = float.MaxValue;
        [XmlAttribute]
        public float MinDistance = float.MinValue;

        [XmlAttribute] 
        public float DisableDistance = float.MinValue;
    }
}