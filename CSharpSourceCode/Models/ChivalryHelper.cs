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
                    result = ChivalryLevel.Illfated;
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
        
        public static ChivalryLevel GetChivalryLevel(Hero hero)
        {
            foreach (ChivalryLevel level in Enum.GetValues(typeof(ChivalryLevel)))
            {
                var attributeId = level.ToString();
                if (hero.HasAttribute(attributeId))
                {
                    return level;
                }
            }

            return ChivalryLevel.Uninspiring;
        }
        public static void SetChivalryLevel(Hero hero, ChivalryLevel chivalryLevel)
        {
            foreach (ChivalryLevel level in Enum.GetValues(typeof(ChivalryLevel)))
            {
                var attributeId = level.ToString();
                if (hero.HasAttribute(attributeId))
                {
                    hero.RemoveAttribute(attributeId);
                }
            }
            
            hero.AddAttribute(chivalryLevel.ToString());
        }
        
        public static bool HasChivalryLevel(Hero hero, ChivalryLevel chivalryLevel)
        {
            var id = chivalryLevel.ToString();
            return hero.HasAttribute(id);
        }
        
        public static List<TooltipProperty> GetChivalryInfo()
        {
            return new List<TooltipProperty>();
        }
    }
    
    public enum ChivalryLevel
    {
        TrueChivalrous =4,
        PureHearted = 3 ,
        Honorable = 2,
        Sincere = 1 ,
        Uninspiring = 0,
        Illfated = -1,
        Unknightly = -2,
    }
    
  
}