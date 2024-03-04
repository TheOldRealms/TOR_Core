using System.Collections.Generic;
using TaleWorlds.CampaignSystem;

namespace TOR_Core.CampaignMechanics.Diplomacy.Mechanics.Scoring
{
    public abstract class AbstractScoringModel
    {
        public List<IDiplomacyScoreFactor> ScoreFactors { get; }
        public float ScoreThreshold { get; }

        protected IDiplomacyScores Scores { get; }

        protected AbstractScoringModel(IDiplomacyScores scores, List<IDiplomacyScoreFactor> factors)
        {
            Scores = scores;
            ScoreFactors = factors;

            ScoreThreshold = scores.ScoreThreshold;
        }

        public abstract bool ShouldFormBidirectional(Kingdom ourKingdom, Kingdom otherKingdom);

        public abstract bool ShouldForm(Kingdom ourKingdom, Kingdom otherKingdom);

        public abstract ExplainedNumber GetScore(Kingdom ourKingdom, Kingdom otherKingdom);
    }
}