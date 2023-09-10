using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Localization;
using TOR_Core.Extensions;

namespace TOR_Core.Models
{
    public class TORSettlementLoyaltyModel : DefaultSettlementLoyaltyModel
    {
        public override ExplainedNumber CalculateLoyaltyChange(Town town, bool includeDescriptions = false)
        { 
            var modifiedExplainedNumberNumber= base.CalculateLoyaltyChange(town, includeDescriptions); 
            GetTORSpecialSettlementLoyalityChange(ref modifiedExplainedNumberNumber, town);
            return modifiedExplainedNumberNumber;
        }
        
        
        private static void GetTORSpecialSettlementLoyalityChange(
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
        }
    }
}