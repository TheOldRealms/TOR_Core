using System;
using System.Xml.Serialization;

namespace TOR_Core.BattleMechanics.TriggeredEffect
{
    [Serializable]
    public class AnimationTrigger
    {
        [XmlAttribute(attributeName: "race_id")]
        public string RaceId { get; set; }
        [XmlAttribute(attributeName: "triggered_effect")]
        public string TriggeredEffectId { get; set; }
    }
}