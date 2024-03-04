namespace TOR_Core.CampaignMechanics.Diplomacy.Mechanics.Scoring
{
    public interface IDiplomacyScores
    {
        int ScoreThreshold { get; }
        int Base { get; }

        int BelowMedianStrength { get; }

        int HasCommonEnemy { get; }

        int ExistingAllianceWithEnemy { get; }

        int ExistingAllianceWithNeutral { get; }

        int Relationship { get; }

        int Tendency { get; }

        int Religion { get; }

        int RelationToLeader { get; }
    }
}