using System.Collections.Generic;
using System.Reflection;
using NLog;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.ObjectSystem;
using TOR_Core.Utilities;

namespace TOR_Core.CharacterDevelopment
{
    /// <summary>
    /// Strips vanilla perks from skill trees TOR has rebuilt. Works by reflection-nulling
    /// <see cref="PerkObject.Skill"/>, which cascades through vanilla's own <c>IsTrash</c>
    /// filters: <c>Campaign.InitializeCampaignObjectsOnAfterLoad</c> drops trash from the
    /// global <c>AllPerks</c>, <c>Hero.ClearChangedPerks</c> strips them from every hero
    /// on load, and the character-developer UI iterates <c>PerkObject.All.Where(p =&gt;
    /// p.Skill == Skill)</c>. Null skill = perk vanishes everywhere, no Harmony needed.
    /// </summary>
    public static class VanillaPerkStripper
    {
        private static readonly FieldInfo SkillBackingField =
            typeof(PerkObject).GetField("<Skill>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);

        private static readonly HashSet<string> VanillaRogueryIds = new()
        {
            "RogueryNoRestForTheWicked",
            "RoguerySweetTalker",
            "RogueryTwoFaced",
            "RogueryDeepPockets",
            "RogueryInBestLight",
            "RogueryKnowHow",
            "RogueryPromises",
            "RogueryManhunter",
            "RogueryScarface",
            "RogueryWhiteLies",
            "RoguerySmugglerConnections",
            "RogueryPartnersInCrime",
            "RogueryOneOfTheFamily",
            "RoguerySaltTheEarth",
            "RogueryCarver",
            "RogueryRansomBroker",
            "RogueryArmsDealer",
            "RogueryDirtyFighting",
            "RogueryDashAndSlash",
            "RogueryFleetFooted",
            "RogueryRogueExtraordinaire",
        };

        public static void StripRogueryPerks()
        {
            int stripped = 0;
            foreach (var perk in MBObjectManager.Instance.GetObjectTypeList<PerkObject>())
            {
                if (VanillaRogueryIds.Contains(perk.StringId))
                {
                    SkillBackingField.SetValue(perk, null);
                    stripped++;
                }
            }
            TORCommon.Log(LogLevel.Info, "[VanillaPerkStripper] Stripped " + stripped + " vanilla Roguery perks.");
        }
    }
}
