using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TOR_Core.CampaignMechanics.Religion;
using TOR_Core.Extensions;

namespace TOR_Core.Models
{
    /// <summary>
    /// Shared helper methods for diplomacy calculations (war, alliance, trade).
    /// Centralizes distance calculations and personality trait modifiers.
    /// </summary>
    public static class DiplomacyHelpers
    {
        // Distance thresholds for different diplomatic actions
        public const float MaxWarDistance = 350f;
        public const float MaxAllianceDistance = 500f;
        public const float MaxTradeDistance = 600f;

        // Personality trait modifier step (trait level -2 to +2 maps to 0.25 to 1.75)
        private const float TraitModifierStep = 0.375f;

        #region Distance Calculations

        /// <summary>
        /// Gets the approximate distance between two kingdoms based on their mid settlements.
        /// </summary>
        public static float GetKingdomDistance(Kingdom kingdom1, Kingdom kingdom2)
        {
            if (kingdom1?.FactionMidSettlement == null || kingdom2?.FactionMidSettlement == null)
                return float.MaxValue;

            return kingdom1.FactionMidSettlement.Position.Distance(kingdom2.FactionMidSettlement.Position);
        }

        /// <summary>
        /// Checks if two kingdoms are within war declaration distance.
        /// </summary>
        public static bool IsWithinWarDistance(Kingdom kingdom1, Kingdom kingdom2)
        {
            return GetKingdomDistance(kingdom1, kingdom2) <= MaxWarDistance;
        }

        /// <summary>
        /// Checks if two kingdoms are within alliance distance.
        /// </summary>
        public static bool IsWithinAllianceDistance(Kingdom kingdom1, Kingdom kingdom2)
        {
            return GetKingdomDistance(kingdom1, kingdom2) <= MaxAllianceDistance;
        }

        /// <summary>
        /// Checks if two kingdoms are within trade agreement distance.
        /// </summary>
        public static bool IsWithinTradeDistance(Kingdom kingdom1, Kingdom kingdom2)
        {
            return GetKingdomDistance(kingdom1, kingdom2) <= MaxTradeDistance;
        }

        #endregion

        #region Trait Modifiers

        /// <summary>
        /// Gets a trait-based modifier for diplomacy scoring.
        /// Maps trait level (-2 to +2) to multiplier (0.25 to 1.75).
        /// Higher trait level = higher modifier.
        /// </summary>
        public static float GetTraitModifier(Hero leader, TraitObject trait)
        {
            if (leader == null || trait == null) return 1f;
            int traitLevel = leader.GetTraitLevel(trait);
            return 1f + (traitLevel * TraitModifierStep);
        }

        /// <summary>
        /// Gets an inverse trait-based modifier for diplomacy scoring.
        /// Maps trait level (-2 to +2) to multiplier (1.75 to 0.25).
        /// Higher trait level = lower modifier.
        /// </summary>
        public static float GetInverseTraitModifier(Hero leader, TraitObject trait)
        {
            if (leader == null || trait == null) return 1f;
            int traitLevel = leader.GetTraitLevel(trait);
            return 1f - (traitLevel * TraitModifierStep);
        }

        #endregion

        #region Pantheon Helpers

        /// <summary>
        /// Gets the dominant pantheon for a kingdom based on its leader's religion or culture.
        /// </summary>
        public static Pantheon GetKingdomPantheon(Kingdom kingdom)
        {
            if (kingdom == null) return Pantheon.Human;

            var leaderReligion = kingdom.Leader?.GetDominantReligion();
            if (leaderReligion != null)
                return leaderReligion.Pantheon;

            return ReligionObjectHelper.GetPantheon(kingdom.Culture?.StringId);
        }

        #endregion
    }
}
