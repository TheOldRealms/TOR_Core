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
                explainedNumber.Add(40, new TextObject("Elven Metropolis"));
            }

            if (town.OwnerClan.IsCastleFaction() && town.IsCastle && !town.IsUnderSiege)
            {
                if (explainedNumber.ResultNumber < 100)
                {
                    explainedNumber.LimitMin(0);
                    explainedNumber.Add(100);
                }
            }

            if (town.Settlement.IsDwarfKarak())
            {
                explainedNumber.Add(75, new TextObject("Dwarf Karak"));
                if (Hero.MainHero.Culture.StringId == TORConstants.Cultures.DAWI)
                {
                    if (Hero.MainHero.HasAttribute("DwarfBrewersIII"))
                    {
                        explainedNumber.Add(25f, new TextObject("Brewers Guild"));
                    }
                    else if (Hero.MainHero.HasAttribute("DwarfBrewersII"))
                    {
                        explainedNumber.Add(15f, new TextObject("Brewers Guild"));
                    }
                    else if (Hero.MainHero.HasAttribute("DwarfBrewersI"))
                    {
                        explainedNumber.Add(10f, new TextObject("Brewers Guild"));
                    }
                }
            }

            if (town.Settlement.Owner == Hero.MainHero && town.Settlement.IsGreenskinCamp())
            {
                var shinies = town.Settlement.Stash.FirstOrDefaultQ(x => x.EquipmentElement.Item.StringId == "tor_gs_gold_pile").Amount;
                explainedNumber.Add(shinies / 20, new TextObject("Shiny Pile"));
            }

            // Squigherds bonus for greenskin-owned towns
            if (town.Settlement.OwnerClan?.Culture?.StringId == TORConstants.Cultures.GREENSKIN && town.Settlement.IsGreenskinCamp())
            {
                explainedNumber.Add(60, new TextObject("Squigherds"));
            }

            return explainedNumber;
        }


    }
}