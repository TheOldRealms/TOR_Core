using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Localization;
using TOR_Core.Extensions;

namespace TOR_Core.CampaignMechanics.Diplomacy.Mechanics.Scoring.Alliance.Factors
{
    public class AlliedWithNeutral : IDiplomacyScoreFactor
    {
        protected const string SAllianceWithKingdom = "{=!}Alliance with {KINGDOM}";
        public void ApplyFactor(ref ExplainedNumber result, Kingdom ourKingdom, Kingdom otherKingdom, IDiplomacyScores scores)
        {
            // Their Alliances with Neutrals
            var alliedNeutrals = KingdomExtensions.AllActiveKingdoms
                .Where(k => k != ourKingdom
                         && k != otherKingdom
                         && FactionManager.IsAlliedWithFaction(otherKingdom, k)
                         && !FactionManager.IsAtWarAgainstFaction(ourKingdom, k));

            // FIXME: alliedNeutrals also includes common allies as it's coded... Should they be scored differently? Probable answer: YES!
            foreach (var alliedNeutral in alliedNeutrals)
                result.Add(scores.ExistingAllianceWithNeutral, new TextObject(SAllianceWithKingdom).SetTextVariable("KINGDOM", alliedNeutral.Name));
        }
    }
}
