using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using TaleWorlds.Core;

namespace TOR_Core.CampaignMechanics.Religion
{
    [Serializable]
    public class ReligionObject : IDeserializationCallback
    {
        [XmlAttribute]
        public string StringId { get; set; }
        [XmlAttribute]
        public string Name { get; set; }
        [XmlAttribute]
        public string CultureId { get; set; }
        [XmlIgnore]
        public string LoreText { get; set; }

        public void OnDeserialization(object sender)
        {
            LoreText = GameTexts.FindText("religion_lore_text", StringId).ToString();
        }
    }
}
