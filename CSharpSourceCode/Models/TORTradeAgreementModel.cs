using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TOR_Core.CampaignMechanics.Religion;
using TOR_Core.Extensions;
using TOR_Core.Utilities;

namespace TOR_Core.Models
{
    /// <summary>
    /// TOR Trade Agreement Model - Determines AI willingness to form trade agreements.
    /// Trade agreements provide a diplomatic penalty when declaring war on trade partners.
    /// </summary>
    public class TORTradeAgreementModel : DefaultTradeAgreementModel
    {
        // Culture scoring
        private const float SameCultureBonus = 15f;
        private const float CultureCompatibilityWeight = 15f;

        // Religion scoring
        private const float SameReligionBonus = 25f;
        private const float HostileReligionPenalty = -30f;
        private const float PantheonCompatibilityWeight = 10f;
        private const float ReligionCompatibilityWeight = 15f;

        // Personality trait weights
        private const float CalculatingTraitWeight = 10f;
        private const float GenerosityTraitWeight = 8f;
        private const float HonorTraitWeight = 6f;

        // Faction-specific bonuses
        private const float EonirTradeBonus = 10f;

        // Note: Distance thresholds are in DiplomacyHelpers (MaxTradeDistance = 600)

        public override int GetMaximumTradeAgreementCount(Kingdom kingdom) => 3;

        /// <summary>
        /// Calculates TOR custom trade agreement scoring factors.
        /// Used for AI decision making on trade agreements.
        /// </summary>
        public override float GetScoreOfStartingTradeAgreement(
            Kingdom kingdom,
            Kingdom targetKingdom,
            Clan clan,
            out TextObject explanation,
            bool includeExplanation = false)
        {
            float baseScore = base.GetScoreOfStartingTradeAgreement(
                kingdom, targetKingdom, clan, out explanation, includeExplanation);

            if (baseScore <= 0)
                return 0f;

            // DISTANCE CHECK FIRST - prevent trade with distant kingdoms
            if (!DiplomacyHelpers.IsWithinTradeDistance(kingdom, targetKingdom))
                return 0f; // Too far - don't even consider this trade agreement

            var leader = clan?.Leader;

            // Get trait modifiers for the evaluating clan's leader
            float calculatingModifier = DiplomacyHelpers.GetTraitModifier(leader, DefaultTraits.Calculating);
            float generosityModifier = DiplomacyHelpers.GetTraitModifier(leader, DefaultTraits.Generosity);
            float honorModifier = DiplomacyHelpers.GetTraitModifier(leader, DefaultTraits.Honor);

            // Individual scoring factors
            float distanceScore = CalculateDistanceScore(kingdom, targetKingdom);
            float cultureScore = CalculateCultureScore(kingdom, targetKingdom);
            float religionScore = CalculateReligionScore(kingdom, targetKingdom);
            float factionScore = CalculateFactionScore(kingdom);

            // Personality scoring
            // Calculating: Strategic lords value trade for economic benefit
            float calculatingScore = CalculateCalculatingScore(calculatingModifier);
            // Generosity: Generous lords are more open to partnerships
            float generosityScore = CalculateGenerosityScore(generosityModifier);
            // Honor: Honorable lords prefer trading with same background (culture/religion)
            float honorScore = CalculateHonorScore(kingdom, targetKingdom, honorModifier);

            float totalScore = baseScore
                             + distanceScore
                             + cultureScore
                             + religionScore
                             + factionScore
                             + calculatingScore
                             + generosityScore
                             + honorScore;

            return MBMath.ClampFloat(totalScore, 0f, 100f);
        }

        #region Distance

        /// <summary>
        /// Calculates distance score for trade agreements.
        /// Closer kingdoms are better trade partners (shorter routes).
        /// Returns: 0 (close) to -60 (far but within range)
        /// </summary>
        private float CalculateDistanceScore(Kingdom kingdom, Kingdom targetKingdom)
        {
            float distance = DiplomacyHelpers.GetKingdomDistance(kingdom, targetKingdom);

            // No penalty for close kingdoms (below 300)
            if (distance <= 300f)
                return 0f;

            // Gradual penalty: -0.2 per unit beyond 300
            return (300f - distance) * 0.2f;
        }

        #endregion

        #region Culture

        /// <summary>
        /// Calculates culture compatibility score for trade.
        /// Same culture or compatible cultures trade more willingly.
        /// Returns: -15 to +15
        /// </summary>
        private float CalculateCultureScore(Kingdom kingdom, Kingdom targetKingdom)
        {
            if (DiplomacyHelpers.AreSameCulture(kingdom, targetKingdom))
                return SameCultureBonus;

            float compatibility = DiplomacyHelpers.GetCultureCompatibility(kingdom, targetKingdom);
            return compatibility * CultureCompatibilityWeight;
        }

        #endregion

        #region Religion

        /// <summary>
        /// Calculates religion compatibility score for trade.
        /// Same religion or compatible pantheons increase trust.
        /// Hostile religions severely penalize trade willingness.
        /// Returns: -30 to +25
        /// </summary>
        private float CalculateReligionScore(Kingdom kingdom, Kingdom targetKingdom)
        {
            // Same religion - strong bonus
            if (DiplomacyHelpers.AreSameReligion(kingdom, targetKingdom))
                return SameReligionBonus;

            // Hostile religions - major penalty
            if (DiplomacyHelpers.AreReligionsHostile(kingdom, targetKingdom))
                return HostileReligionPenalty;

            // Otherwise use compatibility
            float pantheonCompat = DiplomacyHelpers.GetPantheonCompatibility(kingdom, targetKingdom);
            float religionCompat = DiplomacyHelpers.GetReligionCompatibility(kingdom, targetKingdom);

            return pantheonCompat * PantheonCompatibilityWeight + religionCompat * ReligionCompatibilityWeight;
        }

        #endregion

        #region Faction

        /// <summary>
        /// Calculates faction-specific trade bonuses.
        /// Some factions are naturally more inclined to trade.
        /// </summary>
        private float CalculateFactionScore(Kingdom kingdom)
        {
            // Eonir are natural merchants
            if (kingdom.Culture?.StringId == TORConstants.Cultures.EONIR)
                return EonirTradeBonus;

            return 0f;
        }

        #endregion

        #region Personality

        /// <summary>
        /// Calculating lords value trade for strategic economic benefit.
        /// Returns: -7.5 to +7.5
        /// </summary>
        private float CalculateCalculatingScore(float calculatingModifier)
        {
            return (calculatingModifier - 1f) * CalculatingTraitWeight;
        }

        /// <summary>
        /// Generous lords are more open to mutual partnerships.
        /// Returns: -6 to +6
        /// </summary>
        private float CalculateGenerosityScore(float generosityModifier)
        {
            return (generosityModifier - 1f) * GenerosityTraitWeight;
        }

        /// <summary>
        /// Honorable lords prefer trading with same background (culture/religion).
        /// Only applies if kingdoms share culture or religion.
        /// Returns: 0, or -4.5 to +4.5
        /// </summary>
        private float CalculateHonorScore(Kingdom kingdom, Kingdom targetKingdom, float honorModifier)
        {
            bool sameBackground = DiplomacyHelpers.AreSameCulture(kingdom, targetKingdom) ||
                                  DiplomacyHelpers.AreSameReligion(kingdom, targetKingdom);

            if (!sameBackground)
                return 0f;

            return (honorModifier - 1f) * HonorTraitWeight;
        }

        #endregion

        #region Lore Restrictions

        public override bool CanMakeTradeAgreement(
            Kingdom kingdom,
            Kingdom other,
            bool checkOtherSideTradeSupport,
            out TextObject reason,
            bool includeReason = false)
        {
            reason = includeReason ? TextObject.GetEmpty() : null;

            // Check lore restrictions
            if (!CanKingdomTrade(kingdom, out reason, includeReason))
                return false;

            if (!CanKingdomTrade(other, out reason, includeReason))
                return false;

            return base.CanMakeTradeAgreement(kingdom, other, checkOtherSideTradeSupport, out reason, includeReason);
        }

        private static readonly TextObject _chaosCannotTradeText = new("{=TOR_Trade_Chaos}The forces of Chaos do not engage in trade.");

        /// <summary>
        /// Checks lore restrictions on trade capability.
        /// - Chaos: Cannot trade
        /// </summary>
        private bool CanKingdomTrade(Kingdom kingdom, out TextObject reason, bool includeReason)
        {
            reason = includeReason ? TextObject.GetEmpty() : null;

            var pantheon = DiplomacyHelpers.GetKingdomPantheon(kingdom);
            if (pantheon == Pantheon.Chaos)
            {
                reason = _chaosCannotTradeText;
                return false;
            }

            return true;
        }

        #endregion
    }
}
