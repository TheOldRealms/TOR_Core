using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Settlements;
using TOR_Core.Extensions;
using TOR_Core.Utilities;

namespace TOR_Core.Models
{
    public class TORSettlementFoodModel : DefaultSettlementFoodModel
    {
        public override ExplainedNumber CalculateTownFoodStocksChange(Town town, bool includeMarketStocks = true, bool includeDescriptions = false)
        {
            var explainedNumber = base.CalculateTownFoodStocksChange(town, includeMarketStocks, includeDescriptions);
            if (town.Settlement.IsBloodKeep()&&town.Settlement.Owner.IsVampire())
            {
                if (explainedNumber.ResultNumber<0)
                {
                    explainedNumber.Add(-explainedNumber.ResultNumber);
                }
            }

            return explainedNumber;
        }


    }
}