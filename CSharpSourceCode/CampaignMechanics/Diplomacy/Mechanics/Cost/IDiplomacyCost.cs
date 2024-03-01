namespace TOR_Core.CampaignMechanics.Diplomacy.Mechanics.Cost
{
    public interface IDiplomacyCost
    {
        void ApplyCost();
        bool CanPayCost();
    }
}