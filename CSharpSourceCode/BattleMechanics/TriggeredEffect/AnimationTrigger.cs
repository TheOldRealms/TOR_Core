using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace TOR_Core.BattleMechanics.TriggeredEffect
{
    [Serializable]
    public class AnimationTrigger
    {
        [XmlAttribute(attributeName: "action_name")]
        public string ActionName { get; set; }
        [XmlAttribute(attributeName: "animation_percent_to_trigger")]
        public float AnimationPercent { get; set; }
        [XmlAttribute(attributeName: "bone_index")]
        public sbyte BoneIndex { get; set; }
        [XmlAttribute(attributeName: "triggered_effect")]
        public string TriggeredEffectId { get; set; }
    }
}
