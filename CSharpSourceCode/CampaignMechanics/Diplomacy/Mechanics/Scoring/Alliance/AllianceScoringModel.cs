using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TOR_Core.CampaignMechanics.Diplomacy.Mechanics.Scoring.Alliance.Factors;

namespace TOR_Core.CampaignMechanics.Diplomacy.Mechanics.Scoring.Alliance
{
    public class AllianceScoringModel : AbstractScoringModel
    {
        private static readonly Lazy<AllianceScoringModel> lazyModel = new Lazy<AllianceScoringModel>(() => new AllianceScoringModel());
        public static AllianceScoringModel ScoringModelInstance { get => lazyModel.Value; }

        public AllianceScoringModel() : base(
            new AllianceScores(), 
            new List<IDiplomacyScoreFactor>
            {
                new WeAreWeakFactor(),
                new CommonEnemiesFactor(),
                new AlliedWithEnemyFactor(),
                new AlliedWithNeutral(),
                new LeaderToLeaderRelationshipFactor(),
                new TheyAreExpansionistFactor(),
                new TendencyFactor(),
                new ReligiousFactor(),
                new PlayerRelationshipWithFactionLeader()
            }) 
        {
        }

        public override bool ShouldFormBidirectional(Kingdom ourKingdom, Kingdom otherKingdom)
            => ShouldForm(ourKingdom, otherKingdom) && ShouldForm(otherKingdom, ourKingdom);

        public override bool ShouldForm(Kingdom ourKingdom, Kingdom otherKingdom)
            => GetScore(ourKingdom, otherKingdom).ResultNumber >= ScoreThreshold;

        public override ExplainedNumber GetScore(Kingdom ourKingdom, Kingdom otherKingdom)
        {
            var explainedNum = new ExplainedNumber(Scores.Base, true);

            ScoreFactors.ForEach(factor => factor.ApplyFactor(ref explainedNum, ourKingdom, otherKingdom, Scores));

            return explainedNum;
        }
    }
}