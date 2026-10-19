using System;
using TaleWorlds.CampaignSystem;

namespace TOR_Core.CampaignMechanics.WitchHunter.Bounties
{
    /// <summary>
    /// Parameterised shape of one bounty objective. Templates live in
    /// <see cref="BountyTemplates"/>; a <see cref="MinorBountyIssue"/> rolls
    /// specific counts and rewards from the template's ranges at generation.
    ///
    /// <para>The <see cref="TargetMatches"/> predicate is deliberately a
    /// delegate rather than a culture / troop-id string, so a template can
    /// span either specific troop templates ("tor_chaos_acolyte") or whole
    /// categories ("any chaos troop", "any undead"). Keeps the templates
    /// table expressive without a new DSL.</para>
    /// </summary>
    public readonly struct BountyTemplate
    {
        public readonly string Id;
        public readonly BountyObjectiveType ObjectiveType;

        /// <summary>Predicate deciding whether a given killed/captured troop counts toward this bounty.</summary>
        public readonly Func<CharacterObject, bool> TargetMatches;

        /// <summary>Short target label used in title/log templating (e.g. "Chaos Cultists", "undead").</summary>
        public readonly string TargetLabel;

        public readonly int CountMin, CountMax;
        public readonly int GoldMin, GoldMax;
        public readonly int PrestigeMin, PrestigeMax;

        /// <summary>Lodge NPC's issue brief (first thing player hears when they ask about bounties).</summary>
        public readonly string BriefKey, BriefFallback;

        /// <summary>Quest title (what shows in the quest log and in the abandon-selection inquiry).</summary>
        public readonly string TitleKey, TitleFallback;

        public BountyTemplate(
            string id,
            BountyObjectiveType objectiveType,
            Func<CharacterObject, bool> targetMatches,
            string targetLabel,
            int countMin, int countMax,
            int goldMin, int goldMax,
            int prestigeMin, int prestigeMax,
            string briefKey, string briefFallback,
            string titleKey, string titleFallback)
        {
            Id = id;
            ObjectiveType = objectiveType;
            TargetMatches = targetMatches;
            TargetLabel = targetLabel;
            CountMin = countMin;
            CountMax = countMax;
            GoldMin = goldMin;
            GoldMax = goldMax;
            PrestigeMin = prestigeMin;
            PrestigeMax = prestigeMax;
            BriefKey = briefKey;
            BriefFallback = briefFallback;
            TitleKey = titleKey;
            TitleFallback = titleFallback;
        }

        public bool IsEmpty => Id == null;
    }
}
