using System;
using System.Collections.Generic;
using System.Linq;
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

        // Economic benefit weights
        private const float ComplementaryResourceWeight = 3f;   // Per unique resource they have that we lack
        private const float FoodScarcityBonusWeight = 10f;      // Bonus if we lack food and they have it
        private const float ProsperityDifferenceWeight = 0.002f; // Slight bonus for trading with prosperous kingdoms

        // Faction-specific bonuses
        private const float EonirTradeBonus = 10f;
        
        private const float WastelandBonus = 20f;
        
        private const float MontfortBonus = 20f;

        // Resource categories for trade value
        private static readonly HashSet<string> FoodResources = new()
        {
            "grain_farm", "cattle_farm", "sheep_farm", "swine_farm",
            "fisherman", "date_farm", "olive_trees"
        };

        private static readonly HashSet<string> LuxuryResources = new()
        {
            "silver_mine", "vineyard", "silk", "silkworm_farm", "fur_trader"
        };

        private static readonly HashSet<string> MilitaryResources = new()
        {
            "iron_mine", "europe_horse_ranch", "steppe_horse_ranch",
            "desert_horse_ranch", "lumberjack"
        };

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
            float loreConsiderations = CalculateLoreConsiderations(kingdom, targetKingdom);
            // Economic: What trade benefits can we gain? (affected by Calculating trait)
            float economicScore = CalculateEconomicBenefitScore(kingdom, targetKingdom) * calculatingModifier;
            // War alternative: Calculating lords consider if war would be more profitable
            float warAlternativePenalty = CalculateWarAlternativePenalty(kingdom, targetKingdom, calculatingModifier);

            // Personality scoring
            float calculatingScore = CalculateCalculatingScore(calculatingModifier);
            float generosityScore = CalculateGenerosityScore(generosityModifier);
            float honorScore = CalculateHonorScore(kingdom, targetKingdom, honorModifier);

            float totalScore = baseScore
                             + distanceScore
                             + cultureScore
                             + religionScore
                             + loreConsiderations
                             + economicScore
                             + warAlternativePenalty
                             + calculatingScore
                             + generosityScore
                             + honorScore;

            return MBMath.ClampFloat(totalScore, 0f, 100f);
        }

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

        /// <summary>
        /// Calculates faction-specific trade bonuses.
        /// Some factions are naturally more inclined to trade.
        /// </summary>
        private float CalculateLoreConsiderations(Kingdom kingdom, Kingdom targetKingdom)
        {
            // Eonir are natural merchants
            if (kingdom.Culture?.StringId == TORConstants.Cultures.EONIR)
                return EonirTradeBonus;
            
            // Marienburg is the biggest harbor in world - there are goods that nobody can aquire
            if (targetKingdom.StringId == TORConstants.Factions.WASTELAND)
                return WastelandBonus;
            
            // Montfort like to trade with humans
            if (kingdom.StringId == TORConstants.Factions.MONTFORT && DiplomacyHelpers.GetKingdomPantheon(targetKingdom) == Pantheon.Human)
                return MontfortBonus;

            return 0f;
        }

        /// <summary>
        /// Calculates economic benefit of trading with target kingdom.
        /// Considers: complementary resources, food scarcity, prosperity.
        /// Returns: 0 to ~30 (higher = more beneficial trade partner)
        /// </summary>
        private float CalculateEconomicBenefitScore(Kingdom kingdom, Kingdom targetKingdom)
        {
            if (kingdom.Settlements == null || targetKingdom.Settlements == null)
                return 0f;

            // Gather our resources
            HashSet<string> ourResources = new();
            bool weHaveFood = false;
            float ourProsperity = 0f;

            foreach (var settlement in kingdom.Settlements)
            {
                if (settlement.IsVillage && settlement.Village?.VillageType != null)
                {
                    string resourceId = settlement.Village.VillageType.StringId;
                    ourResources.Add(resourceId);
                    if (FoodResources.Contains(resourceId))
                        weHaveFood = true;
                }
                else if (settlement.IsTown && settlement.Town != null)
                {
                    ourProsperity += settlement.Town.Prosperity;
                }
            }

            // Gather their resources
            HashSet<string> theirResources = new();
            bool theyHaveFood = false;
            float theirProsperity = 0f;

            foreach (var settlement in targetKingdom.Settlements)
            {
                if (settlement.IsVillage && settlement.Village?.VillageType != null)
                {
                    string resourceId = settlement.Village.VillageType.StringId;
                    theirResources.Add(resourceId);
                    if (FoodResources.Contains(resourceId))
                        theyHaveFood = true;
                }
                else if (settlement.IsTown && settlement.Town != null)
                {
                    theirProsperity += settlement.Town.Prosperity;
                }
            }

            float score = 0f;

            // Complementary resources - resources they have that we lack
            int complementaryCount = 0;
            foreach (var resource in theirResources)
            {
                if (!ourResources.Contains(resource))
                {
                    complementaryCount++;
                    // Bonus for luxury and military resources
                    if (LuxuryResources.Contains(resource) || MilitaryResources.Contains(resource))
                        complementaryCount++; // Double count valuable resources
                }
            }
            score += complementaryCount * ComplementaryResourceWeight;

            // Food scarcity bonus - if we lack food and they have it
            if (!weHaveFood && theyHaveFood)
                score += FoodScarcityBonusWeight;

            // Prosperity difference - slight bonus for trading with prosperous kingdoms
            if (theirProsperity > ourProsperity)
            {
                float prosperityDiff = theirProsperity - ourProsperity;
                score += prosperityDiff * ProsperityDifferenceWeight;
            }

            return score;
        }

        /// <summary>
        /// Calculating lords consider if war would be more profitable than trade.
        /// Uses actual war scoring to determine if conquest is preferable.
        /// Returns: 0 (war not attractive) to -30 (war much better option)
        /// </summary>
        private float CalculateWarAlternativePenalty(Kingdom kingdom, Kingdom targetKingdom, float calculatingModifier)
        {
            // Only calculating lords think this way
            if (calculatingModifier <= 1f)
                return 0f;

            // Already at war - trade not relevant
            if (kingdom.IsAtWarWith(targetKingdom))
                return 0f;

            // Get the actual war score using the diplomacy model
            var diplomacyModel = Campaign.Current?.Models?.DiplomacyModel as TORDiplomacyModel;
            if (diplomacyModel == null)
                return 0f;

            float warScore = diplomacyModel.GetScoreOfDeclaringWar(kingdom, targetKingdom, kingdom.RulingClan, out _);

            // If war score is negative or low, trade is the better option
            if (warScore <= 0f)
                return 0f;

            // War looks attractive - penalty scales with how good war looks
            // Normalize war score (typically ranges from 0 to ~50000 for very attractive wars)
            float normalizedWarScore = Math.Min(warScore / 10000f, 3f); // Cap at 3x multiplier

            // Scale by how calculating the lord is
            float penalty = -normalizedWarScore * 10f * (calculatingModifier - 1f);

            return Math.Max(penalty, -30f); // Cap at -30
        }

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

        private static readonly TextObject _chaosCannotTradeText = new("{=TOR_Trade_Chaos}The forces of Chaos do not engage in trade.");
        private static readonly TextObject _greenskinCannotTradeText = new("{=TOR_Trade_Greenskin}Greenskins do not understand the concept of trade.");

        /// <summary>
        /// Checks if two kingdoms can form a trade agreement.
        /// Lore restrictions: Chaos and Greenskins cannot trade.
        /// </summary>
        public override bool CanMakeTradeAgreement(
            Kingdom kingdom,
            Kingdom other,
            bool checkOtherSideTradeSupport,
            out TextObject reason,
            bool includeReason = false)
        {
            reason = includeReason ? TextObject.GetEmpty() : null;

            // Check lore restrictions for both kingdoms
            var ourPantheon = DiplomacyHelpers.GetKingdomPantheon(kingdom);
            var theirPantheon = DiplomacyHelpers.GetKingdomPantheon(other);

            if (ourPantheon == Pantheon.Chaos || theirPantheon == Pantheon.Chaos)
            {
                reason = _chaosCannotTradeText;
                return false;
            }

            if (ourPantheon == Pantheon.Greenskin || theirPantheon == Pantheon.Greenskin)
            {
                reason = _greenskinCannotTradeText;
                return false;
            }

            return base.CanMakeTradeAgreement(kingdom, other, checkOtherSideTradeSupport, out reason, includeReason);
        }
    }
}
