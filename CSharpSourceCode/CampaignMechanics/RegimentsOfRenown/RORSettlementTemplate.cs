using System;
using System.Xml.Serialization;

namespace TOR_Core.CampaignMechanics.RegimentsOfRenown
{
    [Serializable]
    public class RORSettlementTemplate
    {
        [XmlAttribute]
        public string SettlementId = "invalid";
        [XmlAttribute]
        public string BaseTroopId = "invalid";
        [XmlAttribute]
        public string MenuHeaderText = "invalid";
        [XmlAttribute]
        public string MenuBackgroundImageId = "invalid";
        [XmlAttribute]
        public string RegimentName = "invalid";
        [XmlAttribute]
        public string RegimentHQName = "invalid";
    }
}
