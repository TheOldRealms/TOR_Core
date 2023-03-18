using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;

namespace TOR_Core.CampaignMechanics.Religion
{
    public class ReligionObject : MBObjectBase
    {
        public TextObject Name { get; set; }
        public TextObject LoreText { get; private set; }
        public CultureObject Culture { get; private set; }
        public List<ReligionObject> HostileReligions { get; private set; } = new List<ReligionObject>();

        public static MBReadOnlyList<ReligionObject> All => MBObjectManager.Instance.GetObjectTypeList<ReligionObject>();

        public string EncyclopediaLink => (Campaign.Current.EncyclopediaManager.GetIdentifier(typeof(ReligionObject)) + "-" + StringId) ?? "";

        public TextObject EncyclopediaLinkWithName => HyperlinkTexts.GetSettlementHyperlinkText(EncyclopediaLink, Name);

        public override void Deserialize(MBObjectManager objectManager, XmlNode node)
        {
            base.Deserialize(objectManager, node);
            Name = new TextObject(node.Attributes.GetNamedItem("Name").Value);
            Culture = MBObjectManager.Instance.ReadObjectReferenceFromXml<CultureObject>("Culture", node);
            LoreText = GameTexts.FindText("tor_religion_description", StringId);
            if (node.HasChildNodes)
            {
                foreach (XmlNode child in node.ChildNodes)
                {
                    if(child.Name == "HostileReligion")
                    {
                        ReligionObject hostileReligion = MBObjectManager.Instance.ReadObjectReferenceFromXml<ReligionObject>("id", child);
                        if (hostileReligion != null) HostileReligions.Add(hostileReligion);
                    }
                }
            }
        }
    }
}
