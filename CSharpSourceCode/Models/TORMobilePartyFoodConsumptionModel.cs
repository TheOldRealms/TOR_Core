using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Party;

namespace TOR_Core.Models
{
    public class TORMobilePartyFoodConsumptionModel : DefaultMobilePartyFoodConsumptionModel
    {
        public override ExplainedNumber CalculateDailyBaseFoodConsumptionf(MobileParty party, bool includeDescription = false)
        {
            int num = party.Party.NumberOfAllMembers + party.Party.NumberOfPrisoners / 2;
            num = ((num < 1) ? 1 : num);
            return new ExplainedNumber(-num / NumberOfMenOnMapToEatOneFood, includeDescription, null);
        }
    }
}
