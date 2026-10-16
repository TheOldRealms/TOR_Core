using System;
using System.Xml.Serialization;

namespace TOR_Core.BattleMechanics.TriggeredEffect
{
    [Serializable]
    public class AnimationTrigger
    {
        [XmlAttribute(attributeName: "character_id")]
        public string CharacterId { get; set; }
        [XmlAttribute(attributeName: "triggered_effect")]
        public string TriggeredEffectId { get; set; }
    }
}