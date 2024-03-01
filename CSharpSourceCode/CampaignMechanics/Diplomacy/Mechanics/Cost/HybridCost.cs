using System.Collections.Generic;
using System.Linq;

namespace TOR_Core.CampaignMechanics.Diplomacy.Mechanics.Cost
{
    class HybridCost : DiplomacyCost
    {
        public InfluenceCost InfluenceCost { get; }
        public GoldCost GoldCost { get; }
        public List<KingdomWalletCost> KingdomWalletCosts { get; }

        public HybridCost(InfluenceCost influenceCost, GoldCost goldCost) : this(influenceCost, goldCost, new List<KingdomWalletCost>()) { }
        public HybridCost(InfluenceCost influenceCost, GoldCost goldCost, KingdomWalletCost kingdomWalletCost) : this(influenceCost, goldCost, new List<KingdomWalletCost>() { kingdomWalletCost }) { }
        public HybridCost(InfluenceCost influenceCost, GoldCost goldCost, List<KingdomWalletCost> kingdomWalletCosts) : base(new List<IDiplomacyCost>())
        {
            DiplomacyCosts.AddRange(kingdomWalletCosts);
            DiplomacyCosts.Add(goldCost);
            DiplomacyCosts.Add(influenceCost);

            InfluenceCost = influenceCost;
            GoldCost = goldCost;
            KingdomWalletCosts = kingdomWalletCosts;
        }

        public override bool CanPayCost()
        {
            return _diplomacyCosts.All(x => x.CanPayCost());
        }
    }
}