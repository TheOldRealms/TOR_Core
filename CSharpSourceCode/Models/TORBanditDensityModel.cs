using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameComponents;
using TOR_Core.Utilities;

namespace TOR_Core.Models
{
    public class TORBanditDensityModel : DefaultBanditDensityModel
    {
        // 21 days per season, 4 seasons
        public const int DAYS_IN_YEAR = 84;
        public override int NumberOfMaximumLooterParties
        {
            get
            {
                if (Campaign.Current.CampaignStartTime.ElapsedDaysUntilNow < TORConfig.YearsToEndEarlyCampaign * DAYS_IN_YEAR)
                {
                    return TORConfig.NumberOfMaximumLooterPartiesEarly;
                }
                else if(Campaign.Current.CampaignStartTime.ElapsedDaysUntilNow < TORConfig.YearsToEndMidCampaign * DAYS_IN_YEAR)
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
