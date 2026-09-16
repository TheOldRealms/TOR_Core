using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Settlements;

namespace TOR_Core.Models
{
    public class TORSettlementGarrisonModel : DefaultSettlementGarrisonModel
    {
        public override ExplainedNumber GetMaximumDailyAutoRecruitmentCount(Town town, bool includeDescriptions = false)
        {
            if (town.OwnerClan == Clan.PlayerClan)
            {
                var garrisonCapacity = Campaign.Current.Models.PartySizeLimitModel.CalculateGarrisonPartySizeLimit(town.Settlement).ResultNumber;
                return new ExplainedNumber(garrisonCapacity, includeDescriptions);
            }

            return base.GetMaximumDailyAutoRecruitmentCount(town, includeDescriptions);
        }
    }
}
