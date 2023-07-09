using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.Library;

namespace TOR_Core.Models
{
    public class TORMapWeatherModel : DefaultMapWeatherModel
    {
        public override AtmosphereInfo GetAtmosphereModel(Vec3 pos)
        {
            var atmo = base.GetAtmosphereModel(pos);
            atmo.TimeInfo.Season = (int)CampaignTime.Now.GetSeasonOfYear;
            return atmo;
        }
    }
}
