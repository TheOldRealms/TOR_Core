using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TOR_Core.CampaignMechanics.Diplomacy.Mechanics.Cost;

namespace TOR_Core.CampaignMechanics.Diplomacy.Mechanics.Conditions.Alliance
{
    class HasEnoughGoldCondition : AbstractCostCondition
    {
        protected override TextObject FailedConditionText => GameTexts.FindText("str_diplomacy_notEnoughGold_text");

        protected override bool ApplyConditionInternal(Kingdom kingdom, Kingdom otherKingdom, ref TextObject textObject, bool forcePlayerCharacterCosts = false)
        {
            var hasEnoughGold = DiplomacyCostCalculator.DetermineCostForFormingAlliance(kingdom, otherKingdom, forcePlayerCharacterCosts).GoldCost.CanPayCost();
            if (!hasEnoughGold)
            {
                textObject = FailedConditionText;
            }
            return hasEnoughGold;
        }
    }
}