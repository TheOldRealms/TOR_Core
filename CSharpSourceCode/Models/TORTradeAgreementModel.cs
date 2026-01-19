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
    ///
    /// Score Components:
    /// 1. DISTANCE - Geographic proximity affects trade route viability
    /// 2. CULTURE - Same/compatible cultures trade more willingly
    /// 3. RELIGION - Religious compatibility affects trust
    /// 4. PERSONALITY - Leader traits affect trade attitude
    /// 5. FACTION - Special faction bonuses (e.g., Eonir merchants)
    /// </summary>
    public class TORTradeAgreementModel : DefaultTradeAgreementModel
    {
        #region Constants

        // Distance scoring
        private const float DistanceNeutralThreshold = 300f;  // No penalty below this
        private const float DistancePenaltyPerUnit = 0.2f;    // Penalty per unit beyond threshold
        private const float DistanceTooFarPenalty = -50f;     // Hard cutoff penalty

        // Culture scoring
        private const float SameCultureBonus = 15f;
        private const float CultureCompatibilityWeight = 15f;

        // Religion scoring
        private const float SameReligionBonus = 25f;
        private const float PantheonCompatibilityWeight = 20f;
        private const float ReligionCompatibilityWeight = 15f;

        // Personality scoring (base values, modified by trait level)
        private const float CalculatingTraitWeight = 10f;     // Strategic economic thinking
        private const float GenerosityTraitWeight = 8f;       // Openness to partnerships
        private const float HonorTraitWeight = 6f;            // Reliable partner preference

        // Faction bonuses
        private const float EonirTradeBonus = 10f;

        #endregion

        #region TextObjects

        private static readonly TextObject _chaosCannotTradeText = new("{=TOR_Trade_Chaos}The forces of Chaos do not engage in trade.");
        private static readonly TextObject _distanceText = new("{=TOR_Trade_Distance}Geographic distance");
        private static readonly TextObject _sameCultureText = new("{=TOR_Trade_SameCulture}Same Culture");
        private static readonly TextObject _cultureCompatibilityText = new("{=TOR_Trade_CultureCompat}Cultural Compatibility");
        private static readonly TextObject _sameReligionText = new("{=TOR_Trade_SameReligion}Same Religion");
        private static readonly TextObject _pantheonCompatibilityText = new("{=TOR_Trade_PantheonCompat}Pantheon Compatibility");
        private static readonly TextObject _religionCompatibilityText = new("{=TOR_Trade_ReligionCompat}Religious Compatibility");
        private static readonly TextObject _calculatingText = new("{=TOR_Trade_Calculating}Economic opportunity");
        private static readonly TextObject _generosityText = new("{=TOR_Trade_Generosity}Partnership openness");
        private static readonly TextObject _honorText = new("{=TOR_Trade_Honor}Reliable partner");
        private static readonly TextObject _eonirTradeText = new("{=TOR_Trade_Eonir}Eonir Trade Affinity");

        #endregion

        public override int GetMaximumTradeAgreementCount(Kingdom kingdom) => 3;

        #region Main Scoring Method

        public override float GetScoreOfStartingTradeAgreement(
            Kingdom kingdom,
            Kingdom targetKingdom,
            Clan clan,
            out TextObject explanation,
            bool includeExplanation = false)
        {
            // Get base game score
            float baseScore = base.GetScoreOfStartingTradeAgreement(
                kingdom, targetKingdom, clan, out explanation, includeExplanation);

            if (baseScore <= 0)
                return 0f;

            var score = new ExplainedNumber(baseScore, includeExplanation);
            var evaluatingLeader = clan?.Leader;

            // 1. DISTANCE - Check viability first
            float distanceScore = CalculateDistanceScore(kingdom, targetKingdom);
            if (distanceScore <= DistanceTooFarPenalty)
            {
                score.Add(distanceScore, _distanceText);
                return MBMath.ClampFloat(score.ResultNumber, 0f, 100f);
            }
            if (distanceScore != 0f)
                score.Add(distanceScore, _distanceText);

            // 2. CULTURE
            AddCultureScore(ref score, kingdom, targetKingdom);

            // 3. RELIGION
            AddReligionScore(ref score, kingdom, targetKingdom);

            // 4. PERSONALITY
            AddPersonalityScore(ref score, kingdom, targetKingdom, evaluatingLeader);

            // 5. FACTION
            AddFactionScore(ref score, kingdom);

            return MBMath.ClampFloat(score.ResultNumber, 0f, 100f);
        }

        #endregion

        #region Score Components

        /// <summary>
        /// Distance affects trade route viability.
        /// - Below 300: No penalty
        /// - 300-600: Gradual penalty (-0.2 per unit)
        /// - Above 600: Hard cutoff (-50)
        /// </summary>
        private float CalculateDistanceScore(Kingdom kingdom, Kingdom targetKingdom)
        {
            if (!DiplomacyHelpers.IsWithinTradeDistance(kingdom, targetKingdom))
                return DistanceTooFarPenalty;

            float distance = DiplomacyHelpers.GetKingdomDistance(kingdom, targetKingdom);
            if (distance <= DistanceNeutralThreshold)
                return 0f;

            return (DistanceNeutralThreshold - distance) * DistancePenaltyPerUnit;
        }

        /// <summary>
        /// Culture compatibility affects trade willingness.
        /// - Same culture: +15
        /// - Compatible cultures: Scaled by compatibility
        /// </summary>
        private void AddCultureScore(ref ExplainedNumber score, Kingdom kingdom, Kingdom targetKingdom)
        {
            var culture1 = kingdom.Culture?.StringId;
            var culture2 = targetKingdom.Culture?.StringId;

            if (string.IsNullOrEmpty(culture1) || string.IsNullOrEmpty(culture2))
                return;

            if (culture1 == culture2)
            {
                score.Add(SameCultureBonus, _sameCultureText);
                return;
            }

            float cultureCompat = ReligionObjectHelper.CalculateCultureCompatibility(culture1, culture2);
            if (cultureCompat != 0f)
                score.Add(cultureCompat * CultureCompatibilityWeight, _cultureCompatibilityText);
        }

        /// <summary>
        /// Religion compatibility affects trust for trade.
        /// - Same religion: +25
        /// - Same pantheon: Scaled by compatibility
        /// - Different pantheon: Scaled by compatibility (can be negative)
        /// </summary>
        private void AddReligionScore(ref ExplainedNumber score, Kingdom kingdom, Kingdom targetKingdom)
        {
            var religion1 = kingdom.Leader?.GetDominantReligion();
            var religion2 = targetKingdom.Leader?.GetDominantReligion();

            if (religion1 == null || religion2 == null)
                return;

            if (religion1 == religion2)
            {
                score.Add(SameReligionBonus, _sameReligionText);
                return;
            }

            float pantheonCompat = ReligionObjectHelper.GetPantheonCompatibility(religion1.Pantheon, religion2.Pantheon);
            if (pantheonCompat != 0f)
                score.Add(pantheonCompat * PantheonCompatibilityWeight, _pantheonCompatibilityText);

            float religionCompat = ReligionObjectHelper.CalculateReligionCompatibility(religion1, religion2);
            if (religionCompat != 0f)
                score.Add(religionCompat * ReligionCompatibilityWeight, _religionCompatibilityText);
        }

        /// <summary>
        /// Leader personality affects trade attitude.
        /// - Calculating: Strategic lords value economic opportunities
        /// - Generosity: Generous lords are open to partnerships
        /// - Honor: Honorable lords prefer reliable same-background partners
        /// </summary>
        private void AddPersonalityScore(ref ExplainedNumber score, Kingdom kingdom, Kingdom targetKingdom, Hero leader)
        {
            if (leader == null) return;

            // CALCULATING - Economic strategic thinking
            float calculatingMod = DiplomacyHelpers.GetTraitModifier(leader, DefaultTraits.Calculating);
            if (calculatingMod != 1f)
                score.Add((calculatingMod - 1f) * CalculatingTraitWeight, _calculatingText);

            // GENEROSITY - Openness to mutual partnerships
            float generosityMod = DiplomacyHelpers.GetTraitModifier(leader, DefaultTraits.Generosity);
            if (generosityMod != 1f)
                score.Add((generosityMod - 1f) * GenerosityTraitWeight, _generosityText);

            // HONOR - Preference for reliable partners (only applies to same background)
            bool sameBackground = kingdom.Culture?.StringId == targetKingdom.Culture?.StringId ||
                                  kingdom.Leader?.GetDominantReligion() == targetKingdom.Leader?.GetDominantReligion();
            if (sameBackground)
            {
                float honorMod = DiplomacyHelpers.GetTraitModifier(leader, DefaultTraits.Honor);
                if (honorMod != 1f)
                    score.Add((honorMod - 1f) * HonorTraitWeight, _honorText);
            }
        }

        /// <summary>
        /// Faction-specific trade bonuses.
        /// - Eonir: Natural merchants (+10)
        /// </summary>
        private void AddFactionScore(ref ExplainedNumber score, Kingdom kingdom)
        {
            if (kingdom.Culture?.StringId == TORConstants.Cultures.EONIR)
                score.Add(EonirTradeBonus, _eonirTradeText);
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

            if (!CanKingdomTrade(kingdom, out reason, includeReason))
                return false;

            if (!CanKingdomTrade(other, out reason, includeReason))
                return false;

            return base.CanMakeTradeAgreement(kingdom, other, checkOtherSideTradeSupport, out reason, includeReason);
        }

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
