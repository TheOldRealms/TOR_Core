using TaleWorlds.CampaignSystem;
using TaleWorlds.Localization;
using TOR_Core.Extensions;

namespace TOR_Core.CampaignMechanics.Diplomacy.Mechanics.Scoring.Alliance.Factors
{
    public class WeAreWeakFactor : IDiplomacyScoreFactor
    {
        protected static readonly TextObject _TWeakKingdom = new TextObject("{=!}Weak Kingdom");
        public void ApplyFactor(ref ExplainedNumber result, Kingdom ourKingdom, Kingdom otherKingdom, IDiplomacyScores scores)
        {
            // Weak Kingdom (Us)
            if (!ourKingdom.IsStrong())
                result.Add(scores.BelowMedianStrength, _TWeakKingdom);
        }
    }
}
