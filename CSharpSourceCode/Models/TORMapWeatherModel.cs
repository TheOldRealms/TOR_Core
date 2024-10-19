using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TOR_Core.Models
{
    public class TORMapWeatherModel : DefaultMapWeatherModel
    {
        public override CampaignTime WeatherUpdatePeriod => CampaignTime.Hours(0.1f);

        public override int DefaultWeatherNodeDimension => 32; //same as vanilla, can't increase without crashing
	}
}
