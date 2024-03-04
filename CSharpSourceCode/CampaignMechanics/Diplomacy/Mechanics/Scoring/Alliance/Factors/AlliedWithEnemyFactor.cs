using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Localization;
using TOR_Core.Extensions;

namespace TOR_Core.CampaignMechanics.Diplomacy.Mechanics.Scoring.Alliance.Factors
{
    public class AlliedWithEnemyFactor : IDiplomacyScoreFactor
    {
        protected const string SAllianceWithKingdom = "{=!}Alliance with {KINGDOM}";
        public void ApplyFactor(ref ExplainedNumber result, Kingdom ourKingdom, Kingdom otherKingdom, IDiplomacyScores scores)
        {
            // Their Alliances with Enemies
            var alliedEnemies = KingdomExtensions.AllActiveKingdoms
                .Where(k => k != ourKingdom
                         && k != otherKingdom
                         && FactionManager.IsAlliedWithFaction(otherKingdom, k)
                         && FactionManager.IsAtWarAgainstFaction(ourKingdom, k));

            foreach (var alliedEnemy in alliedEnemies)
                result.Add(scores.ExistingAllianceWithEnemy, new TextObject(SAllianceWithKingdom).SetTextVariable("KINGDOM", alliedEnemy.Name));
        }
    }
}
