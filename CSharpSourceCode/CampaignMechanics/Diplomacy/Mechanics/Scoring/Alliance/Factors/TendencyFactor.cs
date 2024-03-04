using TaleWorlds.CampaignSystem;
using TaleWorlds.Localization;

namespace TOR_Core.CampaignMechanics.Diplomacy.Mechanics.Scoring.Alliance.Factors
{
    public class TendencyFactor : IDiplomacyScoreFactor
    {
        protected static readonly TextObject _TTendency = new TextObject("{=!}Action Tendency");
        public void ApplyFactor(ref ExplainedNumber result, Kingdom ourKingdom, Kingdom otherKingdom, IDiplomacyScores scores)
        {
            // Tendency
            result.Add(scores.Tendency, _TTendency);
        }
    }
}
