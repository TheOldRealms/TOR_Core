using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.ObjectSystem;

namespace TOR_Core.CampaignMechanics.Religion
{
    [Serializable]
    public class ReligionObject
    {
        [XmlAttribute]
        public string StringId { get; set; }
        [XmlAttribute]
        public string Name { get; set; }
        [XmlAttribute]
        public string CultureId { get; set; }
        [XmlElement("HostileReligion")]
        public List<string> HostileReligionIds { get; set; } = new List<string>();
        [XmlIgnore]
        public string LoreText { get; private set; }
        [XmlIgnore]
        public CultureObject Culture { get; private set; }
        [XmlIgnore]
        public List<ReligionObject> HostileReligions { get; private set; }

        public void OnInitialize()
        {
            LoreText = GameTexts.FindText("religion_lore_text", StringId).ToString();
            Culture = MBObjectManager.Instance.GetObject<CultureObject>(x=> x.StringId == CultureId);
            HostileReligions = (from religionString in HostileReligionIds select ReligionManager.GetReligion(religionString)).ToList();
        }
    }
}
