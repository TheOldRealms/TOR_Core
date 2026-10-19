using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TOR_Core.Extensions;

namespace TOR_Core.CampaignMechanics.WitchHunter.Bounties
{
    /// <summary>
    /// Static registry of bounty templates. Values anchor on the Empire Quests
    /// Mechanics spreadsheet (<c>TORTasks/witchhunter feature/Empire Quests_Mechanics.ods</c>);
    /// tune after playtest. Phase B ships only Minor templates with the
    /// <see cref="BountyObjectiveType.KillTroops"/> objective. Phase C adds
    /// capture / clear-hideout / defeat-parties and the Major templates.
    /// </summary>
    public static class BountyTemplates
    {
        private static readonly Dictionary<string, BountyTemplate> _minor = new()
        {
            ["minor_kill_cultists"] = new BountyTemplate(
                id: "minor_kill_cultists",
                objectiveType: BountyObjectiveType.KillTroops,
                // IsCultist() = culture == CHAOS_CULTIST. Narrower than IsChaos.
                targetMatches: t => t?.IsCultist() == true,
                targetLabel: "Chaos Cultists",
                countMin: 15, countMax: 25,
                goldMin: 0, goldMax: 0,
                prestigeMin: 60, prestigeMax: 100,
                briefKey: "tor_wh_bounty_minor_kill_cultists_brief",
                briefFallback: "There is cult activity we have confirmed. Put {COUNT} of them to the sword and the Order will pay you in prestige.",
                titleKey: "tor_wh_bounty_minor_kill_cultists_title",
                titleFallback: "Cull the Cultists"),

            ["minor_kill_chaos"] = new BountyTemplate(
                id: "minor_kill_chaos",
                objectiveType: BountyObjectiveType.KillTroops,
                // IsChaos covers CHAOS and CHAOS_CULTIST; this template is deliberately
                // broader than the cultist-only one above.
                targetMatches: t => t?.IsChaos() == true,
                targetLabel: "chaos marauders",
                countMin: 30, countMax: 50,
                goldMin: 1500, goldMax: 2500,
                prestigeMin: 0, prestigeMax: 0,
                briefKey: "tor_wh_bounty_minor_kill_chaos_brief",
                briefFallback: "The Marauders raid our villages. Put {COUNT} of their heads on pikes — the Order can spare gold for that work.",
                titleKey: "tor_wh_bounty_minor_kill_chaos_title",
                titleFallback: "Thin the Marauder Hordes"),

            ["minor_kill_undead"] = new BountyTemplate(
                id: "minor_kill_undead",
                objectiveType: BountyObjectiveType.KillTroops,
                targetMatches: t => t?.IsUndead() == true,
                targetLabel: "undead",
                countMin: 15, countMax: 25,
                goldMin: 0, goldMax: 0,
                prestigeMin: 60, prestigeMax: 100,
                briefKey: "tor_wh_bounty_minor_kill_undead_brief",
                briefFallback: "Undead walk the lands near our chapterhouse. Burn {COUNT} of them and return — this is the Order's work.",
                titleKey: "tor_wh_bounty_minor_kill_undead_title",
                titleFallback: "Cleanse the Walking Dead"),
        };

        public static BountyTemplate GetMinor(string id) =>
            _minor.TryGetValue(id, out var t) ? t : default;

        public static IEnumerable<BountyTemplate> AllMinor() => _minor.Values;

        public static BountyTemplate PickRandomMinor()
        {
            if (_minor.Count == 0) return default;
            var keys = _minor.Keys.ToList();
            return _minor[keys[MBRandom.RandomInt(keys.Count)]];
        }
    }
}
