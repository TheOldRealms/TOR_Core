using System;

using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;

namespace TOR_Core.CampaignMechanics.Diplomacy.Mechanics.Cost
{
    public sealed class InfluenceCost : AbstractDiplomacyCost
    {
        private readonly Clan _clan;

        public Clan Clan => _clan;

        public InfluenceCost(Clan clan, float value) : base(value)
        {
            _clan = clan;
        }

        public override void ApplyCost()
        {
            _clan.Influence = MBMath.ClampFloat(_clan.Influence - Math.Max(Value, 0f), 0f, float.MaxValue);
        }

        public override bool CanPayCost()
        {
            return _clan.Influence >= Value;
        }
    }
}