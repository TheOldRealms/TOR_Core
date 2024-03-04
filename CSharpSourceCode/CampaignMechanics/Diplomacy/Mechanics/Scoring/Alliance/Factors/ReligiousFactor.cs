using TaleWorlds.CampaignSystem;
using TaleWorlds.Localization;
using TOR_Core.CampaignMechanics.Diplomacy.Mechanics.Aggression;

namespace TOR_Core.CampaignMechanics.Diplomacy.Mechanics.Scoring.Alliance.Factors
{
    public class ReligiousFactor : IDiplomacyScoreFactor
    {
        protected static readonly TextObject _TReligion = new TextObject("{=!}Religious Similarity");
        public void ApplyFactor(ref ExplainedNumber result, Kingdom ourKingdom, Kingdom otherKingdom, IDiplomacyScores scores)
        {
            // Religious cost
            var religiousAffinity = ReligiousAggressionCalculator.CalculateReligionMultiplier(ourKingdom, otherKingdom);
            result.Add(religiousAffinity * scores.Religion, _TReligion);
        }
    }
}
