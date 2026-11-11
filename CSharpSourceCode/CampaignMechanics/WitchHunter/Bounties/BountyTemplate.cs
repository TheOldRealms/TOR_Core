using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;

namespace TOR_Core.CampaignMechanics.WitchHunter.Bounties
{
    /// <summary>
    /// Parameterised shape of one bounty objective. Templates live in
    /// <see cref="BountyTemplates"/>; a <see cref="BountyIssue"/> rolls
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
        public readonly BountyTier Tier;
        public readonly BountyObjectiveType ObjectiveType;

        /// <summary>Predicate deciding whether a given killed/captured troop counts toward this bounty.</summary>
        public readonly Func<CharacterObject, bool> TargetMatches;

        /// <summary>Optional predicate for ClearHideout templates: matches the hideout
        /// settlement directly instead of iterating the defender roster. Needed
        /// because hideout defenders often use vanilla bandit cultures
        /// (e.g. <c>sea_raiders</c> for Norscan hideouts) rather than the
        /// TOR culture a template-level troop predicate would expect.
        /// If null, ClearHideout falls back to <see cref="TargetMatches"/>
        /// on the defeated roster.</summary>
        public readonly Func<Settlement, bool> HideoutMatches;

        /// <summary>Short target label used in title/log templating (e.g. "Chaos Cultists", "undead").</summary>
        public readonly string TargetLabel;

        public readonly int CountMin, CountMax;
        public readonly int GoldMin, GoldMax;
        public readonly int PrestigeMin, PrestigeMax;

        /// <summary>Chapterhouse NPC's issue brief (first thing player hears when they ask about bounties).</summary>
        public readonly string BriefKey, BriefFallback;

        /// <summary>Quest title (what shows in the quest log and in the abandon-selection inquiry).</summary>
        public readonly string TitleKey, TitleFallback;

        public BountyTemplate(
            string id,
            BountyTier tier,
            BountyObjectiveType objectiveType,
            Func<CharacterObject, bool> targetMatches,
            string targetLabel,
            int countMin, int countMax,
            int goldMin, int goldMax,
            int prestigeMin, int prestigeMax,
            string briefKey, string briefFallback,
            string titleKey, string titleFallback,
            Func<Settlement, bool> hideoutMatches = null)
        {
            Id = id;
            Tier = tier;
            ObjectiveType = objectiveType;
            TargetMatches = targetMatches;
            HideoutMatches = hideoutMatches;
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
