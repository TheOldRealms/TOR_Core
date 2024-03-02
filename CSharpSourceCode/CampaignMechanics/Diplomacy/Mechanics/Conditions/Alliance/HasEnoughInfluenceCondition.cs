using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TOR_Core.CampaignMechanics.Diplomacy.Mechanics.Cost;

namespace TOR_Core.CampaignMechanics.Diplomacy.Mechanics.Conditions.Alliance
{
    class HasEnoughInfluenceCondition : AbstractCostCondition
    {
        protected override bool ApplyConditionInternal(Kingdom kingdom, Kingdom otherKingdom, ref TextObject textObject, bool forcePlayerCharacterCosts = false)
        {
            var hasEnoughInfluence = DiplomacyCostCalculator.DetermineCostForFormingAlliance(kingdom, otherKingdom, forcePlayerCharacterCosts).InfluenceCost.CanPayCost();
            if (!hasEnoughInfluence)
            {
                textObject = GameTexts.FindText("str_diplomacy_notEnoughInfluence_text");
            }

            return hasEnoughInfluence;
        }
    }
}