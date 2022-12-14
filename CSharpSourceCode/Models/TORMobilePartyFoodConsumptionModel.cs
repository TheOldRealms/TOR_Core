using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.LinQuick;
using TOR_Core.Extensions;

namespace TOR_Core.Models
{
    public class TORMobilePartyFoodConsumptionModel : DefaultMobilePartyFoodConsumptionModel
    {
        public override ExplainedNumber CalculateDailyBaseFoodConsumptionf(MobileParty party, bool includeDescription = false)
        {
            var eatingMemberRoster = party.Party.MemberRoster.GetTroopRoster().WhereQ(x => !x.Character.IsUndead());
            int eatingMemberNum = 0;
            foreach(var item in eatingMemberRoster)
            {
                eatingMemberNum += item.Number;
            }

            var eatingPrisonerRoster = party.Party.PrisonRoster.GetTroopRoster().WhereQ(x => !x.Character.IsUndead());
            int eatingPrisonerNum = 0;
            foreach (var item in eatingPrisonerRoster)
            {
                eatingPrisonerNum += item.Number;
            }

            float num = eatingMemberNum + eatingPrisonerNum / 2;

            num = ((num < 1) ? 1 : num);
            float resultNumber = -num / (float)NumberOfMenOnMapToEatOneFood;
            return new ExplainedNumber(resultNumber, includeDescription, null);
        }
    }
}
