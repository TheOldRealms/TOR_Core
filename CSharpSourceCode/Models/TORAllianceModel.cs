using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TOR_Core.CampaignMechanics.Religion;
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

        private static readonly TextObject _religionCompatibilityText = new TextObject("{=TOR_Alliance_Religion}Religious compatibility");
        private static readonly TextObject _cultureCompatibilityText = new TextObject("{=TOR_Alliance_Culture}Cultural ties");
        private static readonly TextObject _chaosFactorText = new TextObject("{=TOR_Alliance_Chaos}Forces of Chaos");

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

            // Add religion compatibility factor
            var religion1 = kingdomDeclaresAlliance.Leader?.GetDominantReligion();
            var religion2 = kingdomDeclaredAlliance.Leader?.GetDominantReligion();

            if (religion1 != null && religion2 != null)
            {
                // Check for hostile religions - major penalty
                if (religion1.HostileReligions != null && religion1.HostileReligions.Contains(religion2))
                {
                    score.Add(HostileReligionPenalty, _religionCompatibilityText);
                }
                else
                {
                    float religionScore = ReligionObjectHelper.CalculateReligionCompatibility(religion1, religion2);
                    score.Add(religionScore * ReligionCompatibilityWeight, _religionCompatibilityText);
                }
            }

            // Add culture compatibility factor using pantheon-based relationships
            float cultureCompatibility = ReligionObjectHelper.CalculateCultureCompatibility(
                kingdomDeclaresAlliance.Culture?.StringId,
                kingdomDeclaredAlliance.Culture?.StringId);
            score.Add(cultureCompatibility * CultureCompatibilityWeight, _cultureCompatibilityText);

            return score;
        }
    }
}
