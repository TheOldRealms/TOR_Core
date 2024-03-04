using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TOR_Core.CampaignMechanics.Diplomacy.Mechanics.Scoring.Alliance.Factors
{
    public class LeaderToLeaderRelationshipFactor : IDiplomacyScoreFactor
    {
        protected static readonly TextObject _TRelationship = new TextObject("{=!}Kingdom leaders' relationship to each other");
        public void ApplyFactor(ref ExplainedNumber result, Kingdom ourKingdom, Kingdom otherKingdom, IDiplomacyScores scores)
        {
            // Relationship
            float relationMult;
            //FIXME: Should introduce a proper logic for such situations across the mod. It should only be a temporary condition, but can mess things up for good.
            if (ourKingdom.Leader is null || otherKingdom.Leader is null)
                relationMult = 0;
            else
                relationMult = MBMath.ClampFloat((float)Math.Log((ourKingdom.Leader.GetRelation(otherKingdom.Leader) + 100f) / 100f, 1.5), -1f, +1f);
            result.Add(scores.Relationship * relationMult, _TRelationship);
        }
    }
}
