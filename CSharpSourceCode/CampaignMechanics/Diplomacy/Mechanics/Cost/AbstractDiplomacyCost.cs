
namespace TOR_Core.CampaignMechanics.Diplomacy.Mechanics.Cost
{
    public abstract class AbstractDiplomacyCost : IDiplomacyCost
    {
        public float Value { get; }

        protected AbstractDiplomacyCost(float value)
        {
            Value = value;
        }

        public abstract void ApplyCost();
        public abstract bool CanPayCost();
    }
}