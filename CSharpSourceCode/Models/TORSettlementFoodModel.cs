using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.LinQuick;
using TaleWorlds.Localization;
using TOR_Core.Extensions;
using TOR_Core.Utilities;

namespace TOR_Core.Models
{
    public class TORSettlementFoodModel : DefaultSettlementFoodModel
    {
        public override ExplainedNumber CalculateTownFoodStocksChange(Town town, bool includeMarketStocks = true, bool includeDescriptions = false)
        {
            base.CalculateTownFoodStocksChange(town, includeMarketStocks, includeDescriptions);
            var explainedNumber = base.CalculateTownFoodStocksChange(town, includeMarketStocks, includeDescriptions);


            if (town.StringId == "town_comp_LL1")
            {
                explainedNumber.Add(40,new TextObject("Elven Metropolis"));
            }
            
            if (town.OwnerClan.IsCastleFaction() && town.IsCastle && !town.IsUnderSiege)
            {
                if (explainedNumber.ResultNumber < 100)
                {
                    explainedNumber.LimitMin(0);
                    explainedNumber.Add(100);
                }
            }

            return explainedNumber;
        }


    }
}