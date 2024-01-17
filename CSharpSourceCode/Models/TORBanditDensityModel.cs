using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameComponents;
using TOR_Core.Utilities;

namespace TOR_Core.Models
{
    public class TORBanditDensityModel : DefaultBanditDensityModel
    {
        // Usually it takes about 70 days or so to get yourself on your foot... very player style dependent
        public const int EARLY_GAME_END_YEAR = 1;
        // Usually late game means starting your own kingdom and that usually happens when the player is in their 30s, 10yrs put the player right in that range
        public const int MID_GAME_END_YEAR = 10;

        // 21 days per season, 4 seasons
        public const int DAYS_IN_YEAR = 84;
        public override int NumberOfMaximumLooterParties
        {
            get
            {
                if (Campaign.Current.CampaignStartTime.ElapsedDaysUntilNow < EARLY_GAME_END_YEAR * DAYS_IN_YEAR)
                {
                    return TORConfig.NumberOfMaximumLooterPartiesEarly;
                }
                else if(Campaign.Current.CampaignStartTime.ElapsedDaysUntilNow < MID_GAME_END_YEAR * DAYS_IN_YEAR)
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
