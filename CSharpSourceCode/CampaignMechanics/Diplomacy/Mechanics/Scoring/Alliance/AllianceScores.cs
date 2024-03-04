namespace TOR_Core.CampaignMechanics.Diplomacy.Mechanics.Scoring.Alliance
{
    public class AllianceScores : IDiplomacyScores
    {
        public int ScoreThreshold => 100;

        public int Base => 20;

        public int BelowMedianStrength => 50;

        public int HasCommonEnemy => 50;

        public int ExistingAllianceWithEnemy => -1000;

        public int ExistingAllianceWithNeutral => -50;

        public int Relationship => 50;

        public int Tendency => 0;

        public int Religion => 100;

        public int RelationToLeader => 50;
    }
}