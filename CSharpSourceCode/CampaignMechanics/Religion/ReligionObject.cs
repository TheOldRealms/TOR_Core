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
using TOR_Core.Extensions;

namespace TOR_Core.CampaignMechanics.Religion
{
    public class ReligionObject : MBObjectBase
    {
        public TextObject Name { get; set; }
        public TextObject LoreText { get; private set; }
        public CultureObject Culture { get; private set; }
        public List<ReligionObject> HostileReligions { get; private set; } = new List<ReligionObject>();
        public List<CharacterObject> ReligiousTroops { get; private set; } = new List<CharacterObject>();
        public Dictionary<Hero, int> InitialFollowers { get; private set; } = new Dictionary<Hero, int>();

        public static MBReadOnlyList<ReligionObject> All => MBObjectManager.Instance.GetObjectTypeList<ReligionObject>();

        public MBReadOnlyList<Hero> CurrentFollowers => new MBReadOnlyList<Hero>(Hero.AllAliveHeroes.Where(x => x.GetDominantReligion() == this).ToList());

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
                    if(child.Name == "HostileReligions")
                    {
                        foreach(XmlNode religionNode in child.ChildNodes)
                        {
                            if(religionNode.Name == "HostileReligion")
                            {
                                ReligionObject hostileReligion = MBObjectManager.Instance.ReadObjectReferenceFromXml<ReligionObject>("id", religionNode);
                                if (hostileReligion != null) HostileReligions.Add(hostileReligion);
                            }
                        }
                    }
                    if (child.Name == "Followers")
                    {
                        foreach (XmlNode followerNode in child.ChildNodes)
                        {
                            if (followerNode.Name == "FollowerHero")
                            {
                                Hero followerHero = MBObjectManager.Instance.ReadObjectReferenceFromXml<Hero>("id", followerNode);
                                int devotion = int.Parse(followerNode.Attributes.GetNamedItem("DevotionLevel").Value);
                                if (followerHero != null) InitialFollowers.Add(followerHero, devotion);
                            }
                        }
                    }
                    if (child.Name == "ReligiousTroops")
                    {
                        foreach (XmlNode troopNode in child.ChildNodes)
                        {
                            if (troopNode.Name == "ReligiousTroop")
                            {
                                CharacterObject troop = MBObjectManager.Instance.ReadObjectReferenceFromXml<CharacterObject>("id", troopNode);
                                if (troop != null) ReligiousTroops.Add(troop);
                            }
                        }
                    }
                }
            }
        }
    }
}
