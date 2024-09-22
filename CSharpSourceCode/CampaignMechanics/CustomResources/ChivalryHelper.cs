using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core.ViewModelCollection.Information;
using TOR_Core.Extensions;

namespace TOR_Core.CampaignMechanics.CustomResources;

public static class ChivalryHelper
{
    public static ChivalryLevel GetChivalryLevelForResource(float level)
    {
        var result = ChivalryLevel.Sincere;

        switch (level)
        {
            case var _ when level < 250:
            {
                result = ChivalryLevel.Unknightly;
                break;
            }
            case var _ when level < 500:
            {
                result = ChivalryLevel.Uninspiring;
                break;
            }
            case var _ when level > 500 && level < 750:
            {
                result = ChivalryLevel.Sincere;
                break;
            }
            case var _ when level > 750 && level < 1000:
            {
                result = ChivalryLevel.Noteworthy;
                break;
            }
            case var _ when level > 1000 && level < 1500:
            {
                result = ChivalryLevel.PureHearted;
                break;
            }
            case var _ when level > 1500 && level < 2000:
            {
                result = ChivalryLevel.Honourable;
                break;
            }
            case var _ when level > 2000:
            {
                result = ChivalryLevel.Chivalrous;
                break;
            }
        }

        return result;
    }

    public static float GetResourceMinimumForChivalryRank(ChivalryLevel level)
    {
        return level switch
        {
            ChivalryLevel.Chivalrous => 2000,
            ChivalryLevel.Honourable => 1500,
            ChivalryLevel.PureHearted => 1000,
            ChivalryLevel.Noteworthy => 750,
            ChivalryLevel.Sincere => 500,
            ChivalryLevel.Uninspiring => 250,
            ChivalryLevel.Unknightly => 0,
            _ => (float)0,
        };
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
        list.Add(new TooltipProperty(title, chivalryLevel.ToString, 0, false,
            TooltipProperty.TooltipPropertyFlags.None));

        switch (chivalryLevel)
        {
            case ChivalryLevel.Unknightly:
                list.Add(new TooltipProperty("Knightly wages: ", "+75%", 0, false, TooltipProperty.TooltipPropertyFlags.None));
                list.Add(new TooltipProperty("Morale: ", "-75%", 0, false, TooltipProperty.TooltipPropertyFlags.None));
                break;
            case ChivalryLevel.Uninspiring:
                list.Add(new TooltipProperty("Knightly wages: ", "+50%", 0, false, TooltipProperty.TooltipPropertyFlags.None));
                list.Add(new TooltipProperty("Morale: ", "-50%", 0, false, TooltipProperty.TooltipPropertyFlags.None));
                break;
            case ChivalryLevel.Sincere:
                list.Add(new TooltipProperty("Knightly wages: ", "+25%", 0, false, TooltipProperty.TooltipPropertyFlags.None));
                list.Add(new TooltipProperty("Morale: ", "-20%", 0, false, TooltipProperty.TooltipPropertyFlags.None));
                break;
            case ChivalryLevel.Noteworthy:
                list.Add(new TooltipProperty("Knightly wages: ", "+10%", 0, false, TooltipProperty.TooltipPropertyFlags.None));
                break;
            case ChivalryLevel.PureHearted:
                list.Add(new TooltipProperty("Morale: ", "+10%", 0, false, TooltipProperty.TooltipPropertyFlags.None));
                break;
            case ChivalryLevel.Honourable:
                list.Add(new TooltipProperty("Morale", "+10%", 0, false, TooltipProperty.TooltipPropertyFlags.None));
                list.Add(new TooltipProperty("Knightly wages", "-10%", 0, false, TooltipProperty.TooltipPropertyFlags.None));
                list.Add(new TooltipProperty("Gain extra  Chivalry everyday", "5", 0, false, TooltipProperty.TooltipPropertyFlags.None));
                break;
            case ChivalryLevel.Chivalrous:
                list.Add(new TooltipProperty("Morale", "+20%", 0, false, TooltipProperty.TooltipPropertyFlags.None));
                list.Add(new TooltipProperty("Knightly wages", "-20%", 0, false, TooltipProperty.TooltipPropertyFlags.None));
                list.Add(new TooltipProperty("Gain extra  Chivalry everyday", "15", 0, false, TooltipProperty.TooltipPropertyFlags.None));
                break;
        }

        list.Add(new TooltipProperty("", " ", 0, false, TooltipProperty.TooltipPropertyFlags.Cost)); //empty line
        if (chivalryLevel != ChivalryLevel.Chivalrous)
        {
            list.Add(new TooltipProperty("Next Rank: ", (chivalryLevel + 1).ToString(), 0, false, TooltipProperty.TooltipPropertyFlags.None));
            var result = GetResourceMinimumForChivalryRank(chivalryLevel + 1) - value;
            list.Add(new TooltipProperty("Required Chivalry: ", result.ToString("0"), 0, false, TooltipProperty.TooltipPropertyFlags.None));
        }


        list.Add(new TooltipProperty("", "", 0, false, TooltipProperty.TooltipPropertyFlags.DefaultSeperator));
        return list;
    }
}

public enum ChivalryLevel
{
    Chivalrous = 6,
    Honourable = 5,
    PureHearted = 4,
    Noteworthy = 3,
    Sincere = 2,
    Uninspiring = 1,
    Unknightly = 0
}