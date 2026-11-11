using System;
using System.Linq;
using System.Xml.Serialization;

namespace TOR_Core.BattleMechanics.TriggeredEffect
{
    [Serializable]
    public class AnimationProgressTrigger
    {
        [XmlAttribute(attributeName: "race_id")]
        public string RaceId { get; set; }

        [XmlAttribute(attributeName: "triggered_effect")]
        public string TriggeredEffectId { get; set; }

        [XmlAttribute(attributeName: "action_ids")]
        public string ActionIds { get; set; }

        [XmlAttribute(attributeName: "trigger_progress")]
        public float TriggerProgress { get; set; }

        [XmlAttribute(attributeName: "bone_index")]
        public sbyte BoneIndex { get; set; }

        [XmlAttribute(attributeName: "min_progress_on_interrupt")]
        public float MinProgressOnInterrupt { get; set; } = -1f;

        [XmlIgnore]
        public string[] ActionIdList => ActionIds?.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).ToArray() ?? new string[0];
    }
}
