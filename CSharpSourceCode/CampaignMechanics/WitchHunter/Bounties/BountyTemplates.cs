using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TOR_Core.Extensions;

namespace TOR_Core.CampaignMechanics.WitchHunter.Bounties
{
    /// <summary>
    /// Static registry of bounty templates for both tiers. Values anchor on the
    /// Empire Quests Mechanics spreadsheet (<c>TORTasks/witchhunter feature/Empire
    /// Quests_Mechanics.ods</c>); tune after playtest. Single dict keyed by id;
    /// per-tier views (<see cref="AllMinor"/>, <see cref="AllMajor"/>,
    /// <see cref="PickRandom"/>) filter by <see cref="BountyTemplate.Tier"/>.
    /// </summary>
    public static class BountyTemplates
    {
        private static readonly Dictionary<string, BountyTemplate> _all = new()
        {
            // ---- Minor: KillTroops
            ["minor_kill_cultists"] = new BountyTemplate(
                id: "minor_kill_cultists", tier: BountyTier.Minor,
                objectiveType: BountyObjectiveType.KillTroops,
                targetMatches: t => t?.IsCultist() == true,
                targetLabel: "Chaos Cultists",
                countMin: 15, countMax: 25,
                goldMin: 0, goldMax: 0,
                prestigeMin: 60, prestigeMax: 100,
                briefKey: "tor_wh_bounty_minor_kill_cultists_brief",
                briefFallback: "There is cult activity we have confirmed. Put {COUNT} of them to the sword and the Order will pay you in prestige.",
                titleKey: "tor_wh_bounty_minor_kill_cultists_title",
                titleFallback: "Cull the Cultists"),

            // Template id retained for save compatibility; matcher and copy
            // now target Norscan specifically. Chaos Warriors (non-Norscan
            // chaos_culture) are handled by the Major tier.
            ["minor_kill_chaos"] = new BountyTemplate(
                id: "minor_kill_chaos", tier: BountyTier.Minor,
                objectiveType: BountyObjectiveType.KillTroops,
                targetMatches: t => t?.IsNorscan() == true,
                targetLabel: "Norscan raiders",
                countMin: 30, countMax: 50,
                goldMin: 1500, goldMax: 2500,
                prestigeMin: 0, prestigeMax: 0,
                briefKey: "tor_wh_bounty_minor_kill_chaos_brief",
                briefFallback: "The Norscans raid our villages. Put {COUNT} of their heads on pikes — the Order can spare gold for that work.",
                titleKey: "tor_wh_bounty_minor_kill_chaos_title",
                titleFallback: "Thin the Norscan Hordes"),

            ["minor_kill_undead"] = new BountyTemplate(
                id: "minor_kill_undead", tier: BountyTier.Minor,
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

            // ---- Minor: CaptureTroops
            ["minor_capture_cultists"] = new BountyTemplate(
                id: "minor_capture_cultists", tier: BountyTier.Minor,
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
                id: "minor_capture_chaos", tier: BountyTier.Minor,
                objectiveType: BountyObjectiveType.CaptureTroops,
                targetMatches: t => t?.IsNorscan() == true,
                targetLabel: "Norscan raiders",
                countMin: 10, countMax: 20,
                goldMin: 2000, goldMax: 4000,
                prestigeMin: 0, prestigeMax: 0,
                briefKey: "tor_wh_bounty_minor_capture_chaos_brief",
                briefFallback: "Norscan warbands grow bold. Take {COUNT} of them alive — the Order will pay well for prisoners of their calibre.",
                titleKey: "tor_wh_bounty_minor_capture_chaos_title",
                titleFallback: "Norscan Prisoners Wanted Alive"),

            // ---- Minor: DefeatParties
            ["minor_defeat_cultists"] = new BountyTemplate(
                id: "minor_defeat_cultists", tier: BountyTier.Minor,
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
                id: "minor_defeat_chaos_raids", tier: BountyTier.Minor,
                objectiveType: BountyObjectiveType.DefeatParties,
                targetMatches: t => t?.IsNorscan() == true,
                targetLabel: "Norscan raiding parties",
                countMin: 2, countMax: 3,
                goldMin: 3000, goldMax: 4500,
                prestigeMin: 0, prestigeMax: 0,
                briefKey: "tor_wh_bounty_minor_defeat_chaos_raids_brief",
                briefFallback: "Norscans are burning villages. Break {COUNT} of their raiding parties and the Order will see you paid.",
                titleKey: "tor_wh_bounty_minor_defeat_chaos_raids_title",
                titleFallback: "Break the Norscan Raiders"),

            // ---- Minor: ClearHideout
            ["minor_clear_cultist_hideouts"] = new BountyTemplate(
                id: "minor_clear_cultist_hideouts", tier: BountyTier.Minor,
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
                id: "minor_clear_chaos_hideouts", tier: BountyTier.Minor,
                objectiveType: BountyObjectiveType.ClearHideout,
                targetMatches: t => t?.IsNorscan() == true,
                targetLabel: "Norscan hideouts",
                countMin: 2, countMax: 3,
                goldMin: 3000, goldMax: 4500,
                prestigeMin: 0, prestigeMax: 0,
                briefKey: "tor_wh_bounty_minor_clear_chaos_hideouts_brief",
                briefFallback: "Norscan bands have dug in near our settlements. Dig {COUNT} of them out.",
                titleKey: "tor_wh_bounty_minor_clear_chaos_hideouts_title",
                titleFallback: "Root Out the Norscan Hideouts"),

            // ---- Major: higher-stakes rewards, broader counts, mid-to-late-game pitch.
            // Spreadsheet reference: "Capture and return any Norscan" 20-30 → 6000-9000 gold,
            // "Defeat Chaos Raiding parties" 3-5 → 150-250 prestige, "Clear out Troll dens"
            // 1-3 → 4000-12000 gold. MVP set is three templates, one per common objective
            // shape, to prove the Major flow end-to-end.

            ["major_kill_chaos"] = new BountyTemplate(
                id: "major_kill_chaos", tier: BountyTier.Major,
                objectiveType: BountyObjectiveType.KillTroops,
                targetMatches: t => t?.IsChaosWarrior() == true,
                targetLabel: "chaos warriors",
                countMin: 60, countMax: 100,
                goldMin: 6000, goldMax: 10000,
                prestigeMin: 100, prestigeMax: 150,
                briefKey: "tor_wh_bounty_major_kill_chaos_brief",
                briefFallback: "The plate-armoured warriors of the Dark Gods march south. Put {COUNT} chaos warriors in the ground. The Order will reward you handsomely.",
                titleKey: "tor_wh_bounty_major_kill_chaos_title",
                titleFallback: "Cull the Chaos Warriors"),

            ["major_defeat_chaos_warbands"] = new BountyTemplate(
                id: "major_defeat_chaos_warbands", tier: BountyTier.Major,
                objectiveType: BountyObjectiveType.DefeatParties,
                targetMatches: t => t?.IsChaosWarrior() == true,
                targetLabel: "chaos warbands",
                countMin: 3, countMax: 5,
                goldMin: 0, goldMax: 0,
                prestigeMin: 150, prestigeMax: 250,
                briefKey: "tor_wh_bounty_major_defeat_chaos_warbands_brief",
                briefFallback: "Full warbands of chaos warriors bear down on the north. Break {COUNT} of them — not Norscan raiders, warbands led by the warriors of the Dark Gods. The Order will know your name afterwards.",
                titleKey: "tor_wh_bounty_major_defeat_chaos_warbands_title",
                titleFallback: "Break the Chaos Warbands"),

            ["major_clear_chaos_lairs"] = new BountyTemplate(
                id: "major_clear_chaos_lairs", tier: BountyTier.Major,
                objectiveType: BountyObjectiveType.ClearHideout,
                targetMatches: t => t?.IsChaosWarrior() == true,
                targetLabel: "chaos strongholds",
                countMin: 1, countMax: 3,
                goldMin: 8000, goldMax: 15000,
                prestigeMin: 0, prestigeMax: 0,
                briefKey: "tor_wh_bounty_major_clear_chaos_lairs_brief",
                briefFallback: "Fortified strongholds held by the warriors of Chaos have taken root. Burn {COUNT} of them — these are not minor cells. The Order will pay a king's ransom for the service.",
                titleKey: "tor_wh_bounty_major_clear_chaos_lairs_title",
                titleFallback: "Burn the Chaos Strongholds"),
        };

        public static BountyTemplate Get(string id) =>
            _all.TryGetValue(id, out var t) ? t : default;

        // Backwards-compat helper for the ByTier lookups used by the console
        // command and generation code; Minor-only kept to avoid a wider rename.
        public static BountyTemplate GetMinor(string id)
        {
            var t = Get(id);
            return t.Tier == BountyTier.Minor ? t : default;
        }

        public static IEnumerable<BountyTemplate> AllMinor() => _all.Values.Where(t => t.Tier == BountyTier.Minor);
        public static IEnumerable<BountyTemplate> AllMajor() => _all.Values.Where(t => t.Tier == BountyTier.Major);

        public static BountyTemplate PickRandom(BountyTier tier)
        {
            var pool = _all.Values.Where(t => t.Tier == tier).ToList();
            if (pool.Count == 0) return default;
            return pool[MBRandom.RandomInt(pool.Count)];
        }

        public static BountyTemplate PickRandomMinor() => PickRandom(BountyTier.Minor);
    }
}
