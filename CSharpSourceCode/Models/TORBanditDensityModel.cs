using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameComponents;

namespace TOR_Core.Models
{
    public class TORBanditDensityModel : DefaultBanditDensityModel
    {
        public override int NumberOfMaximumLooterParties
        {
            get
            {
                if (Campaign.Current.CampaignStartTime.ElapsedDaysUntilNow < 10)
                {
                    return 200;
                }
                else if(Campaign.Current.CampaignStartTime.ElapsedDaysUntilNow < 20)
                {
                    return 400;
                }
                else
                {
                    return 600;
                }
            }
        }

        public override int NumberOfMaximumBanditPartiesAroundEachHideout => 12;
        public override int NumberOfMaximumBanditPartiesInEachHideout => 3;
        public override int NumberOfInitialHideoutsAtEachBanditFaction => 40;
        public override int NumberOfMaximumHideoutsAtEachBanditFaction => 80;
    }
}
