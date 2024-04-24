using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core.ViewModelCollection.Information;
using TOR_Core.Extensions;

namespace TOR_Core.Models
{
    public static class ChivalryHelper
    {
        public static ChivalryLevel GetChivalryLevelForResource(float level)
        {
            ChivalryLevel result = ChivalryLevel.Uninspiring;
            
            switch (level)
            {
                case var _ when level < 250:
                {
                    result = ChivalryLevel.Unknightly;
                    break;
                }
                case var _ when level < 500:
                {
                    result = ChivalryLevel.Disappointing;
                    break;
                }
                case var _ when level > 500 && level <750:
                {
                    result = ChivalryLevel.Uninspiring;
                    break;
                }
                case var _ when level > 750 && level <1000:
                {
                    result = ChivalryLevel.Sincere;
                    break;
                }
                case var _ when level > 1000 && level < 1500:
                {
                    result = ChivalryLevel.Honorable;
                    break;
                }
                case var _ when level > 1500 && level < 2000:
                {
                    result = ChivalryLevel.PureHearted;
                    break;
                }
                case var _ when level > 2000:
                {
                    result = ChivalryLevel.TrueChivalrous;
                    break;
                }
            }
            return result;
        }

        public static float GetResourceMinimumForChivalryRank(ChivalryLevel level)
        {
            switch (level)
            {
                case ChivalryLevel.TrueChivalrous:
                    return 2000;
                    break;
                case ChivalryLevel.PureHearted:
                    return 1500;
                    break;
                case ChivalryLevel.Honorable:
                    return 1000;
                    break;
                case ChivalryLevel.Sincere:
                    return 750;
                    break;
                case ChivalryLevel.Uninspiring:
                    return 500;
                    break;
                case ChivalryLevel.Disappointing:
                    return 250;
                    break;
                case ChivalryLevel.Unknightly:
                    return 0;
                    break;
                default:
                    return 0;
            }
        }
        
        
        public static bool HasChivalryLevel(Hero hero, ChivalryLevel chivalryLevel)
        {
            return chivalryLevel == hero.GetChivalryLevel();
        }
        
        public static List<TooltipProperty> GetChivalryInfo()
        {
            var list = new List<TooltipProperty>();
            var value = Hero.MainHero.GetCustomResourceValue("Chivalry");

            var title = "Chivalry Rank";
            var chivalryLevel = GetChivalryLevelForResource(value);
            
            
            list.Add(new TooltipProperty("", "", 0, false, TooltipProperty.TooltipPropertyFlags.DefaultSeperator));
            list.Add( new TooltipProperty(title, chivalryLevel.ToString, 0, false, TooltipProperty.TooltipPropertyFlags.RundownResult));
            
            switch (chivalryLevel)
            {
                case  ChivalryLevel.Unknightly: 
                    list.Add(new TooltipProperty("Knightly wages: ", "+50%", 0, false, TooltipProperty.TooltipPropertyFlags.None));
                    list.Add(new TooltipProperty("Morale: ", "-50%", 0, false, TooltipProperty.TooltipPropertyFlags.None));
                    break;
                case  ChivalryLevel.Disappointing: 
                    list.Add(new TooltipProperty("Knightly wages: ", "+25%", 0, false, TooltipProperty.TooltipPropertyFlags.None));
                    list.Add(new TooltipProperty("Morale: ", "-25%", 0, false, TooltipProperty.TooltipPropertyFlags.None));
                    break;
                case ChivalryLevel.Uninspiring:
                    list.Add(new TooltipProperty("Knightly wages: ", "+10%", 0, false, TooltipProperty.TooltipPropertyFlags.None));
                    break;
                case ChivalryLevel.Sincere:
                    break;
                case ChivalryLevel.Honorable:
                    list.Add(new TooltipProperty("Morale: ", "+10%", 0, false, TooltipProperty.TooltipPropertyFlags.None));
                    break;
                case ChivalryLevel.PureHearted:
                    list.Add(new TooltipProperty("Morale", "+10%", 0, false, TooltipProperty.TooltipPropertyFlags.None));
                    list.Add(new TooltipProperty("Knightly wages", "-10%", 0, false, TooltipProperty.TooltipPropertyFlags.None));
                    list.Add(new TooltipProperty("Gain extra  Chivalry everyday", "5", 0, false, TooltipProperty.TooltipPropertyFlags.None));
                    break;
                case ChivalryLevel.TrueChivalrous:
                    list.Add(new TooltipProperty("Morale", "+20%", 0, false, TooltipProperty.TooltipPropertyFlags.None));
                    list.Add(new TooltipProperty("Knightly wages", "-20%", 0, false, TooltipProperty.TooltipPropertyFlags.None));
                    list.Add(new TooltipProperty("Gain extra  Chivalry everyday", "15", 0, false, TooltipProperty.TooltipPropertyFlags.None));
                    break;
            }
            list.Add( new TooltipProperty("", " ", 0, false, TooltipProperty.TooltipPropertyFlags.Cost)); //empty line
            if (chivalryLevel!=ChivalryLevel.TrueChivalrous)
            {
                list.Add( new TooltipProperty("Next Rank: ", (chivalryLevel+1).ToString(), 0, false, TooltipProperty.TooltipPropertyFlags.None));
                var result =  GetResourceMinimumForChivalryRank(((ChivalryLevel)chivalryLevel+1)) - value;
                list.Add( new TooltipProperty("Required Chivalry: ", result.ToString, 0, false, TooltipProperty.TooltipPropertyFlags.None));
            }
            
            
            
            list.Add(new TooltipProperty("", "", 0, false, TooltipProperty.TooltipPropertyFlags.DefaultSeperator));
            return list;
        }
    }
    
    public enum ChivalryLevel
    {
        TrueChivalrous = 6,
        PureHearted = 5 ,
        Honorable = 4,
        Sincere = 3 ,
        Uninspiring = 2,
        Disappointing = 1,
        Unknightly = 0,
    }
    
  
}