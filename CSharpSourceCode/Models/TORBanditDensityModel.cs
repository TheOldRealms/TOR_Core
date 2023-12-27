using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameComponents;
using TOR_Core.Utilities;

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
                    return TORConfig.NumberOfMaximumLooterPartiesEarly;
                }
                else if(Campaign.Current.CampaignStartTime.ElapsedDaysUntilNow < 20)
                {
                    return TORConfig.NumberOfMaximumLooterParties;
                }
                else
                {
                    return TORConfig.NumberOfMaximumLooterPartiesLate;
                }
            }
        }

        public override int NumberOfMaximumBanditPartiesAroundEachHideout => TORConfig.NumberOfMaximumBanditPartiesAroundEachHideout;
        public override int NumberOfMaximumBanditPartiesInEachHideout => TORConfig.NumberOfMaximumBanditPartiesInEachHideout;
        public override int NumberOfInitialHideoutsAtEachBanditFaction => TORConfig.NumberOfInitialHideoutsAtEachBanditFaction;
        public override int NumberOfMaximumHideoutsAtEachBanditFaction => TORConfig.NumberOfMaximumHideoutsAtEachBanditFaction;
    }
}
