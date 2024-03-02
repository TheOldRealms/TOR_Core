namespace TOR_Core.CampaignMechanics.Diplomacy.Mechanics.Scoring
{
    internal abstract partial class AbstractScoringModel<T> where T : AbstractScoringModel<T>, new()
    {
        public interface IDiplomacyScores
        {
            int Base { get; }

            int BelowMedianStrength { get; }

            int HasCommonEnemy { get; }

            int ExistingAllianceWithEnemy { get; }

            int ExistingAllianceWithNeutral { get; }

            int NonAggressionPactWithEnemy { get; }

            int NonAggressionPactWithNeutral { get; }

            int Relationship { get; }

            int Tendency { get; }

            int LeaderClanMarriage { get; }
        }
    }
}