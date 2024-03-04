using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Localization;

namespace TOR_Core.CampaignMechanics.Diplomacy.Mechanics.Scoring.Alliance.Factors
{
    public class CommonEnemiesFactor : IDiplomacyScoreFactor
    {
        protected const string SWarWithKingdom = "{=!}War with {KINGDOM}";
        public void ApplyFactor(ref ExplainedNumber result, Kingdom ourKingdom, Kingdom otherKingdom, IDiplomacyScores scores)
        {
            // Common Enemies
            var commonEnemies = FactionManager.GetEnemyKingdoms(ourKingdom).Intersect(FactionManager.GetEnemyKingdoms(otherKingdom));
            foreach (var commonEnemy in commonEnemies)
                result.Add(scores.HasCommonEnemy, new TextObject(SWarWithKingdom).SetTextVariable("KINGDOM", commonEnemy.Name));
        }
    }
}
