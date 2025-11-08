using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.LinQuick;
using TaleWorlds.Localization;
using TOR_Core.Extensions;
using TOR_Core.Utilities;

namespace TOR_Core.Models
{
    public class TORSettlementLoyaltyModel : DefaultSettlementLoyaltyModel
    {
        public override ExplainedNumber CalculateLoyaltyChange(Town town, bool includeDescriptions = false)
        { 
            var modifiedExplainedNumberNumber= base.CalculateLoyaltyChange(town, includeDescriptions); 
            GetTORSpecialSettlementLoyaltyChange(ref modifiedExplainedNumberNumber, town);
            return modifiedExplainedNumberNumber;
        }
        
        
        private static void GetTORSpecialSettlementLoyaltyChange(
            ref ExplainedNumber explainedNumber, Town town)
        {
            if (town.Culture.StringId == "blooddragons")
            {
                if (town.Settlement.Owner.IsVampire())
                {
                    explainedNumber.Add(5,new TextObject("vampire effect"), new TextObject("variable test"));
                }
                else
                {
                    explainedNumber.Add(-10,new TextObject("not vampire"), new TextObject("variable test"));
                }
            }

            if (Hero.MainHero.Culture.StringId == TORConstants.Cultures.DAWI)
            {
                if (town.Settlement.IsDwarfKarak())
                {
                    var description = new TextObject("Brewers Guild");
                    if (Hero.MainHero.HasAttribute("DwarfBrewersIII"))
                    {
                        explainedNumber.Add(2,description);
                    }
                    else if (Hero.MainHero.HasAttribute("DwarfBrewersII"))
                    {
                        explainedNumber.Add(1,description);
                    }
                        
                }
            }

            if (Hero.MainHero.Culture.StringId == TORConstants.Cultures.GREENSKIN)
            {
                if (town.Settlement.Owner == Hero.MainHero && town.Settlement.IsGreenskinCamp())
                {
                    float shinies = town.Settlement.Stash.FirstOrDefaultQ(x=> x.EquipmentElement.Item.StringId == "tor_gs_gold_pile" ).Amount;
                    explainedNumber.Add(shinies/500, new TextObject("Shiny Pile"));
                }
            }
            
        }
    }
}