using TaleWorlds.CampaignSystem;

namespace TOR_Core.CampaignMechanics.Diplomacy.Mechanics.Scoring
{
    public interface IDiplomacyScoreFactor
    {
        void ApplyFactor(ref ExplainedNumber result, Kingdom ourKingdom, Kingdom otherKingdom, IDiplomacyScores scores);
    }
}
