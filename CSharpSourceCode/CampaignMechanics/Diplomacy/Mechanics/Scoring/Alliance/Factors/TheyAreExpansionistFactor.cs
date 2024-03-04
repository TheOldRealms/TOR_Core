using TaleWorlds.CampaignSystem;
using TaleWorlds.Localization;
using TOR_Core.Extensions;

namespace TOR_Core.CampaignMechanics.Diplomacy.Mechanics.Scoring.Alliance.Factors
{
    public class TheyAreExpansionistFactor : IDiplomacyScoreFactor
    {
        protected static readonly TextObject _TExpansionism = new TextObject("{=!}Expansionism");
        public void ApplyFactor(ref ExplainedNumber result, Kingdom ourKingdom, Kingdom otherKingdom, IDiplomacyScores scores)
        {
            // Expansionism (Them)
            var expansionismPenalty = otherKingdom.GetExpansionismDiplomaticPenalty();

            if (expansionismPenalty < 0)
                result.Add(expansionismPenalty, _TExpansionism);
        }
    }
}
