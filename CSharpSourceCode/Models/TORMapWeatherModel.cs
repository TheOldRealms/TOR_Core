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
		private readonly WeatherEvent[] _torWeatherDataCache = new WeatherEvent[1024];
		private readonly float _harshWeatherTreshhold = 0.95f;
		private readonly float _mildWeatherTreshhold = 0.85f;

		public override AtmosphereInfo GetAtmosphereModel(Vec3 pos)
        {
            var atmo = base.GetAtmosphereModel(pos);
			ValueTuple<CampaignTime.Seasons, bool, float, float> data = GetSeasonRainAndSnowDataForOpeningMission(pos.AsVec2);
			atmo.InterpolatedAtmosphereName = GetSelectedAtmosphereId(data.Item1, data.Item2, data.Item3, data.Item4);
			atmo.TimeInfo.Season = (int)data.Item1;
            return atmo;
        }

        public override WeatherEvent UpdateWeatherForPosition(Vec2 position, CampaignTime ct)
        {
			bool isWinter = CampaignTime.Now.GetSeasonOfYear == CampaignTime.Seasons.Winter;
			WeatherEvent weather = WeatherEvent.Clear;
			var rng = MBRandom.RandomFloatRanged(0f, 1f);
			if (rng > _harshWeatherTreshhold)
            {
				if (isWinter)
				{
					weather = SetWeatherForPosition(position, WeatherEvent.Blizzard);
				}
				else
				{
					weather = SetWeatherForPosition(position, WeatherEvent.HeavyRain);
				}
			}
            else if(rng > _mildWeatherTreshhold)
            {
				if (isWinter)
				{
					weather = SetWeatherForPosition(position, WeatherEvent.Snowy);
				}
				else
				{
					weather = SetWeatherForPosition(position, WeatherEvent.LightRain);
				}
			}
            else
            {
				weather = SetWeatherForPosition(position, WeatherEvent.Clear);
			}
			return weather;
        }

		private WeatherEvent SetWeatherForPosition(in Vec2 position, WeatherEvent weather)
		{
			int xIndex;
			int yIndex;
			GetNodePositionForWeather(position, out xIndex, out yIndex);
			_torWeatherDataCache[yIndex * DefaultWeatherNodeDimension + xIndex] = weather;
			return _torWeatherDataCache[yIndex * DefaultWeatherNodeDimension + xIndex];
		}

		public override WeatherEvent GetWeatherEventInPosition(Vec2 pos)
        {
			int xIndex;
			int yIndex;
			GetNodePositionForWeather(pos, out xIndex, out yIndex);
			return _torWeatherDataCache[yIndex * DefaultWeatherNodeDimension + xIndex];
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
			WeatherEvent weatherEventInPosition = GetWeatherEventFromSurroundingNodes(position);
			float rainDensity = 0f;
			float snowDensity = 0.85f;
			bool isRaining = false;
			switch (weatherEventInPosition)
			{
				case WeatherEvent.LightRain:
					rainDensity = 0.7f;
					break;
				case WeatherEvent.HeavyRain:
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

		private Vec2 GetNodePositionForWeather(Vec2 pos, out int xIndex, out int yIndex)
		{
			if (Campaign.Current.MapSceneWrapper != null)
			{
				Vec2 terrainSize = Campaign.Current.MapSceneWrapper.GetTerrainSize();
				float xSize = terrainSize.X / DefaultWeatherNodeDimension;
				float ySize = terrainSize.Y / DefaultWeatherNodeDimension;
				xIndex = (int)(pos.x / xSize);
				yIndex = (int)(pos.y / ySize);
				float a = xIndex * xSize;
				float b = yIndex * ySize;
				return new Vec2(a, b);
			}
			xIndex = 0;
			yIndex = 0;
			return Vec2.Zero;
		}

		private WeatherEvent GetWeatherEventFromSurroundingNodes(Vec2 position)
        {
			WeatherEvent weather = WeatherEvent.Clear;
			int xIndex;
			int yIndex;
			GetNodePositionForWeather(position, out xIndex, out yIndex);

			int xMin = Math.Max(0, xIndex - 1);
			int xMax = Math.Min(xIndex + 1, DefaultWeatherNodeDimension);
			int yMin = Math.Max(0, yIndex - 1);
			int yMax = Math.Min(yIndex + 1, DefaultWeatherNodeDimension);

			for (int x = xMin; x <= xMax; x++)
            {
				for(int y = yMin; y <= yMax; y++)
                {
					weather = _torWeatherDataCache[y * DefaultWeatherNodeDimension + x];
					if (weather != WeatherEvent.Clear) return weather;
				}
            }
			return weather;
		}
	}
}
