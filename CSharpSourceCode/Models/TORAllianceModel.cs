using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TOR_Core.Extensions;
using TOR_Core.Utilities;

namespace TOR_Core.Models
{
    public class TORAllianceModel : DefaultAllianceModel
    {
        // Scaling constants for alliance score calculations
        private const float ReligionCompatibilityWeight = 20f;
        private const float CultureCompatibilityWeight = 30f;
        private const float HostileReligionPenalty = -100f;
        private const float ChaosFactionPenalty = -1000f;
        private const float EonirDiplomacyBonus = 10f;
        private const float DistancePenaltyMultiplier = 0.3f;  // Penalty per unit distance beyond threshold
        private const float TooFarPenalty = -500f;  // Hard cutoff penalty for distant kingdoms

        private static readonly TextObject _religionCompatibilityText = new TextObject("{=TOR_Alliance_Religion}Religious compatibility");
        private static readonly TextObject _cultureCompatibilityText = new TextObject("{=TOR_Alliance_Culture}Cultural ties");
        private static readonly TextObject _chaosFactorText = new TextObject("{=TOR_Alliance_Chaos}Forces of Chaos");
        private static readonly TextObject _distanceText = new TextObject("{=TOR_Alliance_Distance}Geographic distance");
        private static readonly TextObject _honorText = new TextObject("{=TOR_Alliance_Honor}Honorable character");
        private static readonly TextObject _calculatingText = new TextObject("{=TOR_Alliance_Calculating}Strategic assessment");
        private static readonly TextObject _mercyText = new TextObject("{=TOR_Alliance_Mercy}Protective instinct");

        /// <summary>
        /// Increased from default of 2 to allow for more complex diplomatic relationships.
        /// </summary>
        public override int MaxNumberOfAlliances => 3;

        public override ExplainedNumber GetScoreOfStartingAlliance(
            Kingdom kingdomDeclaresAlliance,
            Kingdom kingdomDeclaredAlliance,
            IFaction evaluatingFaction,
            out TextObject explanationText,
            bool includeDescription = false)
        {
            // Get the base score from the default model
            var score = base.GetScoreOfStartingAlliance(
                kingdomDeclaresAlliance,
                kingdomDeclaredAlliance,
                evaluatingFaction,
                out explanationText,
                includeDescription);

            // Chaos cannot form alliances - return extremely negative score
            if (kingdomDeclaresAlliance.Culture.StringId == TORConstants.Cultures.CHAOS ||
                kingdomDeclaredAlliance.Culture.StringId == TORConstants.Cultures.CHAOS)
            {
                score.Add(ChaosFactionPenalty, _chaosFactorText);
                return score;
            }

            // DISTANCE CHECK - penalize distant alliances, hard cutoff beyond threshold
            float distance = DiplomacyHelpers.GetKingdomDistance(kingdomDeclaresAlliance, kingdomDeclaredAlliance);
            if (!DiplomacyHelpers.IsWithinAllianceDistance(kingdomDeclaresAlliance, kingdomDeclaredAlliance))
            {
                score.Add(TooFarPenalty, _distanceText);
                return score; // Too far - don't bother with other calculations
            }
            else if (distance > 200f)
            {
                // Gradual penalty for moderate distances
                float distancePenalty = (distance - 200f) * -DistancePenaltyMultiplier;
                score.Add(distancePenalty, _distanceText);
            }

            // Get the evaluating leader for personality traits
            Hero evaluatingLeader = null;
            if (evaluatingFaction is Clan evaluatingClan)
            {
                evaluatingLeader = evaluatingClan.Leader;
            }
            else if (evaluatingFaction is Kingdom evaluatingKingdom)
            {
                evaluatingLeader = evaluatingKingdom.Leader;
            }

            // PERSONALITY TRAIT MODIFIERS
            // Honor: Honorable lords value alliances (commitments, trustworthiness)
            float honorModifier = DiplomacyHelpers.GetTraitModifier(evaluatingLeader, DefaultTraits.Honor);
            // Calculating: Strategic lords evaluate alliance benefits more thoroughly
            float calculatingModifier = DiplomacyHelpers.GetTraitModifier(evaluatingLeader, DefaultTraits.Calculating);
            // Mercy: Merciful lords form protective alliances to help weaker/threatened kingdoms
            float mercyModifier = DiplomacyHelpers.GetTraitModifier(evaluatingLeader, DefaultTraits.Mercy);

            // Add religion compatibility factor
            if (DiplomacyHelpers.AreReligionsHostile(kingdomDeclaresAlliance, kingdomDeclaredAlliance))
            {
                score.Add(HostileReligionPenalty, _religionCompatibilityText);
            }
            else
            {
                float religionScore = DiplomacyHelpers.GetReligionCompatibility(kingdomDeclaresAlliance, kingdomDeclaredAlliance);
                score.Add(religionScore * ReligionCompatibilityWeight, _religionCompatibilityText);
            }

            // Add culture compatibility factor using pantheon-based relationships
            float cultureCompatibility = DiplomacyHelpers.GetCultureCompatibility(kingdomDeclaresAlliance, kingdomDeclaredAlliance);
            score.Add(cultureCompatibility * CultureCompatibilityWeight, _cultureCompatibilityText);

            // Eonir bonus
            if (kingdomDeclaresAlliance.Leader.Culture.StringId == TORConstants.Cultures.EONIR)
            {
                score.Add(EonirDiplomacyBonus);
            }

            // HONOR MODIFIER - Honorable lords are more inclined to form/value alliances
            // Dishonorable lords (-2) view alliances as merely temporary conveniences (0.25×)
            // Honorable lords (+2) deeply value alliance commitments (1.75×)
            if (honorModifier != 1f)
            {
                float honorBonus = (honorModifier - 1f) * 30f;  // -22.5 to +22.5
                score.Add(honorBonus, _honorText);
            }

            // CALCULATING MODIFIER - Strategic lords assess alliance value more thoroughly
            // Applies to strength differential: calculating lords ally with stronger kingdoms when threatened
            if (calculatingModifier != 1f && kingdomDeclaresAlliance.GetNumActiveKingdomWars() > 0)
            {
                float ourStrength = kingdomDeclaresAlliance.CurrentTotalStrength;
                float theirStrength = kingdomDeclaredAlliance.CurrentTotalStrength;
                float strengthRatio = theirStrength / Math.Max(ourStrength, 1f);

                // If ally is stronger than us, calculating lords value this more
                if (strengthRatio > 1f)
                {
                    float strategicBonus = (calculatingModifier - 1f) * strengthRatio * 15f;
                    score.Add(strategicBonus, _calculatingText);
                }
            }

            // MERCY MODIFIER - Merciful lords form protective alliances
            // If the target kingdom is under threat (at war, weaker), merciful lords want to help
            if (mercyModifier > 1f)
            {
                int targetWarCount = kingdomDeclaredAlliance.GetNumActiveKingdomWars();
                if (targetWarCount > 0)
                {
                    float protectiveBonus = (mercyModifier - 1f) * targetWarCount * 10f;
                    score.Add(protectiveBonus, _mercyText);
                }
            }

            // Alliance score is typically higher values, scale it down for chance
            score.Add(-50);
            score.AddFactor(2);

            return score;
        }
    }
}
