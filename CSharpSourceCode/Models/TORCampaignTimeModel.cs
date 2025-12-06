using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.GameComponents;

namespace TOR_Core.Models
{
    class TORCampaignTimeModel : DefaultCampaignTimeModel
	{
		public override CampaignTime CampaignStartTime
		{
			get
			{
                return CampaignTime.Years(2502f) + CampaignTime.Weeks(4f) + CampaignTime.Days(5f) + CampaignTime.Hours(12f);
			}
		}

		public override int SunRise
		{
			get
			{
				return 4;
			}
		}

		public override int SunSet
		{
			get
			{
				return 20;
			}
		}

		public override long TimeTicksPerMillisecond
		{
			get
			{
				return 10L;
			}
		}

		public override int MillisecondInSecond
		{
			get
			{
				return 1000;
			}
		}

		public override int SecondsInMinute
		{
			get
			{
				return 60;
			}
		}

		public override int MinutesInHour
		{
			get
			{
				return 60;
			}
		}

		public override int HoursInDay
		{
			get
			{
				return 24;
			}
		}

		public override int DaysInWeek
		{
			get
			{
				if (Campaign.Current.Options.AccelerationMode != GameAccelerationMode.Fast)
				{
					return 7;
				}
				return 3;
			}
		}

		public override int WeeksInSeason
		{
			get
			{
				if (Campaign.Current.Options.AccelerationMode != GameAccelerationMode.Fast)
				{
					return 3;
				}
				return 2;
			}
		}

		public override int SeasonsInYear
		{
			get
			{
				return 4;
			}
		}
	}
}
