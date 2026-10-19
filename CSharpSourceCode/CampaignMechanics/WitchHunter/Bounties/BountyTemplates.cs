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

            // --- Capture-and-return templates: progress ticks when the player
            // donates matching troops via the lodge's Prisoners turn-in. Any
            // lodge honours the delivery.
            ["minor_capture_cultists"] = new BountyTemplate(
                id: "minor_capture_cultists",
                objectiveType: BountyObjectiveType.CaptureTroops,
                targetMatches: t => t?.IsCultist() == true,
                targetLabel: "chaos cultists",
                countMin: 10, countMax: 20,
                goldMin: 0, goldMax: 0,
                prestigeMin: 50, prestigeMax: 100,
                briefKey: "tor_wh_bounty_minor_capture_cultists_brief",
                briefFallback: "The Order needs captives for interrogation. Bring {COUNT} living chaos cultists to the lodge — any lodge will do.",
                titleKey: "tor_wh_bounty_minor_capture_cultists_title",
                titleFallback: "Captives for the Interrogators"),

            ["minor_capture_chaos"] = new BountyTemplate(
                id: "minor_capture_chaos",
                objectiveType: BountyObjectiveType.CaptureTroops,
                targetMatches: t => t?.IsChaos() == true,
                targetLabel: "chaos warriors",
                countMin: 10, countMax: 20,
                goldMin: 2000, goldMax: 4000,
                prestigeMin: 0, prestigeMax: 0,
                briefKey: "tor_wh_bounty_minor_capture_chaos_brief",
                briefFallback: "Chaos warbands grow bold. Take {COUNT} of them alive — the Order will pay well for prisoners of their calibre.",
                titleKey: "tor_wh_bounty_minor_capture_chaos_title",
                titleFallback: "Chaos Prisoners Wanted Alive"),

            // --- Defeat-parties templates: progress ticks per defeated enemy party
            // containing matching troops. "Bands of X" semantics.
            ["minor_defeat_cultists"] = new BountyTemplate(
                id: "minor_defeat_cultists",
                objectiveType: BountyObjectiveType.DefeatParties,
                targetMatches: t => t?.IsCultist() == true,
                targetLabel: "chaos cultist bands",
                countMin: 2, countMax: 4,
                goldMin: 0, goldMax: 0,
                prestigeMin: 50, prestigeMax: 100,
                briefKey: "tor_wh_bounty_minor_defeat_cultists_brief",
                briefFallback: "Our scouts have spotted {COUNT} cultist bands on the roads. Scatter them.",
                titleKey: "tor_wh_bounty_minor_defeat_cultists_title",
                titleFallback: "Scatter the Cultist Bands"),

            ["minor_defeat_chaos_raids"] = new BountyTemplate(
                id: "minor_defeat_chaos_raids",
                objectiveType: BountyObjectiveType.DefeatParties,
                targetMatches: t => t?.IsChaos() == true,
                targetLabel: "chaos raiding parties",
                countMin: 2, countMax: 3,
                goldMin: 3000, goldMax: 4500,
                prestigeMin: 0, prestigeMax: 0,
                briefKey: "tor_wh_bounty_minor_defeat_chaos_raids_brief",
                briefFallback: "Marauders are burning villages. Break {COUNT} of their raiding parties and the Order will see you paid.",
                titleKey: "tor_wh_bounty_minor_defeat_chaos_raids_title",
                titleFallback: "Break the Chaos Raiders"),

            // --- Clear-hideout templates: progress ticks per hideout battle won
            // whose garrison contains any troop matching the predicate.
            ["minor_clear_cultist_hideouts"] = new BountyTemplate(
                id: "minor_clear_cultist_hideouts",
                objectiveType: BountyObjectiveType.ClearHideout,
                targetMatches: t => t?.IsCultist() == true,
                targetLabel: "cult lairs",
                countMin: 2, countMax: 3,
                goldMin: 0, goldMax: 0,
                prestigeMin: 80, prestigeMax: 120,
                briefKey: "tor_wh_bounty_minor_clear_cultist_hideouts_brief",
                briefFallback: "The Order has mapped {COUNT} active cult lairs. Burn them out.",
                titleKey: "tor_wh_bounty_minor_clear_cultist_hideouts_title",
                titleFallback: "Burn the Cult Lairs"),

            ["minor_clear_chaos_hideouts"] = new BountyTemplate(
                id: "minor_clear_chaos_hideouts",
                objectiveType: BountyObjectiveType.ClearHideout,
                targetMatches: t => t?.IsChaos() == true,
                targetLabel: "chaos hideouts",
                countMin: 2, countMax: 3,
                goldMin: 3000, goldMax: 4500,
                prestigeMin: 0, prestigeMax: 0,
                briefKey: "tor_wh_bounty_minor_clear_chaos_hideouts_brief",
                briefFallback: "Marauder bands have dug in near our settlements. Dig {COUNT} of them out.",
                titleKey: "tor_wh_bounty_minor_clear_chaos_hideouts_title",
                titleFallback: "Root Out the Marauder Hideouts"),
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
