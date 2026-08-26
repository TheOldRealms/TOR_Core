using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
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
        private static MBReadOnlyList<ReligionObject> _all;
        public TextObject Name { get; set; }
        public TextObject DeityName { get; set; }
        public TextObject LoreText { get; private set; }
        public TextObject BlessingEffectDescription { get; private set; }
        public TextObject BlessingEffectName { get; private set; }
        public CultureObject Culture { get; private set; }
        public List<ReligionObject> HostileReligions { get; private set; } = [];
        public List<CharacterObject> ReligiousTroops { get; private set; } = [];
        public List<CharacterObject> EliteUnits { get; private set; } = [];
        public List<ItemObject> ReligiousArtifacts { get; private set; } = [];
        public List<string> InitialClans { get; private set; } = [];
        public Pantheon Pantheon { get; private set; }

        public static MBReadOnlyList<ReligionObject> All => _all ?? [];
        public static void FillAll() => _all = MBObjectManager.Instance.GetObjectTypeList<ReligionObject>();

        public MBReadOnlyList<Hero> CurrentFollowers => new(Hero.AllAliveHeroes.Where(x => x.GetDominantReligion() == this).ToList());

        public string EncyclopediaLink => (Campaign.Current.EncyclopediaManager.GetIdentifier(typeof(ReligionObject)) + "-" + StringId) ?? "";

        public TextObject EncyclopediaLinkWithName => HyperlinkTexts.GetSettlementHyperlinkText(EncyclopediaLink, Name);

        /// <summary>
        /// Gets the hostility factor between this religion and another.
        /// Returns a negative value if the other religion is in the HostileReligions list.
        /// This is meant to be added on top of Pantheon compatibility calculations.
        /// </summary>
        /// <param name="other">The other religion to check hostility against</param>
        /// <returns>-0.5 if hostile, 0 otherwise</returns>
        public float GetHostilityFactor(ReligionObject other)
        {
            if (other == null) return 0f;
            return HostileReligions.Contains(other) ? -0.5f : 0f;
        }

        public override void Deserialize(MBObjectManager objectManager, XmlNode node)
        {
            base.Deserialize(objectManager, node);
            Name = new TextObject(node.Attributes.GetNamedItem("Name").Value);
            DeityName = new TextObject(node.Attributes.GetNamedItem("DeityName").Value);
            Culture = MBObjectManager.Instance.ReadObjectReferenceFromXml<CultureObject>("Culture", node);
            Pantheon = (Pantheon)Enum.Parse(typeof(Pantheon), node.Attributes.GetNamedItem("Pantheon").Value);
            LoreText = GameTexts.FindText("tor_religion_description", StringId);

            if (GameTexts.TryGetText("tor_religion_blessing_name", out var blessingName, this.StringId))
                BlessingEffectName = blessingName;
            if (GameTexts.TryGetText("tor_religion_blessing_effect_description", out var blessingDescription, this.StringId))
                BlessingEffectDescription = blessingDescription;

            if (node.HasChildNodes)
            {
                foreach (XmlNode child in node.ChildNodes)
                {
                    if (child.Name == "HostileReligions")
                    {
                        foreach (XmlNode religionNode in child.ChildNodes)
                        {
                            if (religionNode.Name == "HostileReligion")
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
                            if (followerNode.Name == "FollowerClan")
                            {
                                var id = followerNode.Attributes.GetNamedItem("stringId").Value;
                                if (!string.IsNullOrWhiteSpace(id) && !InitialClans.Contains(id)) InitialClans.Add(id);
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
                    if (child.Name == "EliteUnits")
                    {
                        foreach (XmlNode unitNode in child.ChildNodes)
                        {
                            if (unitNode.Name == "EliteUnit")
                            {
                                CharacterObject unit = MBObjectManager.Instance.ReadObjectReferenceFromXml<CharacterObject>("id", unitNode);
                                if (unit != null) EliteUnits.Add(unit);
                            }
                        }
                    }
                    if (child.Name == "ReligiousArtifacts")
                    {
                        foreach (XmlNode artifactNode in child.ChildNodes)
                        {
                            if (artifactNode.Name == "ArtifactItem")
                            {
                                ItemObject item = MBObjectManager.Instance.ReadObjectReferenceFromXml<ItemObject>("id", artifactNode);
                                if (item != null) ReligiousArtifacts.Add(item);
                            }
                        }
                    }
                }
            }
        }
    }

    public enum DevotionLevel
    {
        None,
        Follower,
        Devoted,
        Fanatic
    }

    /// <summary>
    /// Represents the broad pantheon/faction grouping for diplomatic compatibility.
    /// Used to determine cultural and religious alignment between factions.
    /// </summary>
    public enum Pantheon
    {
        /// <summary>Human gods: Sigmar, Ulric, Morr, Taal, Lady of the Lake, etc.</summary>
        Human,
        /// <summary>Elven gods: Isha, Kurnous, Lileath, Khaine, etc.</summary>
        Elven,
        /// <summary>Dwarven Ancestor Gods: Grungni, Valaya, Grimnir</summary>
        Dwarven,
        /// <summary>Undead worship: Nagash, dark necromancy</summary>
        Undead,
        /// <summary>Greenskin gods: Gork and Mork</summary>
        Greenskin,
        /// <summary>Chaos gods: Khorne, Nurgle, Tzeentch, Slaanesh</summary>
        Chaos
    }
}