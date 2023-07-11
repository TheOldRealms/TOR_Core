using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TOR_Core.Models
{
    public class TORMapWeatherModel : DefaultMapWeatherModel
    {
        public override AtmosphereInfo GetAtmosphereModel(Vec3 pos)
        {
            var atmo = base.GetAtmosphereModel(pos);
			ValueTuple<CampaignTime.Seasons, bool, float, float> data = GetSeasonRainAndSnowDataForOpeningMission(pos.AsVec2);
			atmo.InterpolatedAtmosphereName = GetSelectedAtmosphereId(data.Item1, data.Item2, data.Item3, data.Item4);
            return atmo;
        }

		private string GetSelectedAtmosphereId(CampaignTime.Seasons selectedSeason, bool isRaining, float rainValue, float snowValue)
		{
			string result = "semicloudy_field_battle";
			if (Settlement.CurrentSettlement != null && (Settlement.CurrentSettlement.IsFortification || Settlement.CurrentSettlement.IsVillage))
			{
				result = "semicloudy_empire";
			}
			if (selectedSeason == CampaignTime.Seasons.Winter)
			{
				if (snowValue >= 0.85f)
				{
					result = "dense_snowy";
				}
				else
				{
					result = "semi_snowy";
				}
			}
			else
			{
				if (rainValue > 0.6f)
				{
					result = "wet";
				}
				if (isRaining)
				{
					if (rainValue >= 0.85f)
					{
						result = "dense_rainy";
					}
					else
					{
						result = "semi_rainy";
					}
				}
			}
			return result;
		}

		private ValueTuple<CampaignTime.Seasons, bool, float, float> GetSeasonRainAndSnowDataForOpeningMission(Vec2 position)
		{
			CampaignTime.Seasons seasons = CampaignTime.Now.GetSeasonOfYear;
			WeatherEvent weatherEventInPosition = GetWeatherEventInPosition(position);
			float rainDensity = 0f;
			float snowDensity = 0.85f;
			bool isRaining = false;
			switch (weatherEventInPosition)
			{
				case WeatherEvent.Clear:
					if (seasons == CampaignTime.Seasons.Winter)
					{
						seasons = ((CampaignTime.Now.GetDayOfSeason > 10) ? CampaignTime.Seasons.Spring : CampaignTime.Seasons.Autumn);
					}
					break;
				case WeatherEvent.LightRain:
					if (seasons == CampaignTime.Seasons.Winter)
					{
						seasons = ((CampaignTime.Now.GetDayOfSeason > 10) ? CampaignTime.Seasons.Spring : CampaignTime.Seasons.Autumn);
					}
					rainDensity = 0.7f;
					break;
				case WeatherEvent.HeavyRain:
					if (seasons == CampaignTime.Seasons.Winter)
					{
						seasons = ((CampaignTime.Now.GetDayOfSeason > 10) ? CampaignTime.Seasons.Spring : CampaignTime.Seasons.Autumn);
					}
					isRaining = true;
					rainDensity = 0.85f + MBRandom.RandomFloatRanged(0f, 0.14999998f);
					break;
				case WeatherEvent.Snowy:
					seasons = CampaignTime.Seasons.Winter;
					rainDensity = 0.55f;
					snowDensity = 0.55f + MBRandom.RandomFloatRanged(0f, 0.3f);
					break;
				case WeatherEvent.Blizzard:
					seasons = CampaignTime.Seasons.Winter;
					rainDensity = 0.85f;
					snowDensity = 0.85f;
					break;
			}
			return new ValueTuple<CampaignTime.Seasons, bool, float, float>(seasons, isRaining, rainDensity, snowDensity);
		}
	}
}
