using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core.ViewModelCollection.Information;
using TOR_Core.Extensions;

namespace TOR_Core.CampaignMechanics.CustomResources;

public class ForestBindingHelper
{
   
    private static readonly int MinimumBound = 750;
    private static readonly int MinimumSymbiosis = 1500;

    public static readonly float HealthDebuffUnBound = -0.35f;
    public static readonly float HealthDebuffBound = -0.15f;
    
    public static readonly float HealthRegDebuffUnBound = -0.35f;
    public static readonly float HealthRegDebuffBound = -0.15f;
    
    public static ForestBindingLevel GetForestBindingLevelForResource(float level)
    {
        var result = ForestBindingLevel.Unbound;

        switch (level)
        {
 
            case var _ when level < MinimumBound:
            {
                result = ForestBindingLevel.Unbound;
                break;
            }
            case var _ when level > MinimumBound &&  level < MinimumSymbiosis:
            {
                result = ForestBindingLevel.Bound;
                break;
            }
            case var _ when level > MinimumSymbiosis:
            {
                result = ForestBindingLevel.Symbiosis;
                break;
            }
        }

        return result;
    }
    
    public static bool HasForestBindingLevel(Hero hero, ForestBindingLevel chivalryLevel)
    {
        return chivalryLevel == hero.GetForestBindingLevel();
    }
    
    public static List<TooltipProperty> GetForestBindingInfo()
    {
        var list = new List<TooltipProperty>();
        var value = Hero.MainHero.GetCustomResourceValue("ForestBinding");

        var title = "Forest Binding";
        var forestBindingLevel = Hero.MainHero.GetForestBindingLevel();


        list.Add(new TooltipProperty("", "", 0, false, TooltipProperty.TooltipPropertyFlags.DefaultSeperator));
        list.Add(new TooltipProperty(title, forestBindingLevel.ToString, 0, false,
            TooltipProperty.TooltipPropertyFlags.RundownResult));

        switch (forestBindingLevel)
        {
            case ForestBindingLevel.Unbound:
                list.Add(new TooltipProperty("Maximum health reduced: ", HealthDebuffUnBound.ToString("0%"), 0, false, TooltipProperty.TooltipPropertyFlags.None));
                list.Add(new TooltipProperty("Maximum health regeneration reduced: ", "-50%", 0, false, TooltipProperty.TooltipPropertyFlags.None));
                list.Add(new TooltipProperty("Winds regeneration reduced: ", "-50%", 0, false, TooltipProperty.TooltipPropertyFlags.None));
                break;
            case ForestBindingLevel.Bound:
                list.Add(new TooltipProperty("Maximum health reduced: ", HealthDebuffBound.ToString("0%"), 0, false, TooltipProperty.TooltipPropertyFlags.None));
                list.Add(new TooltipProperty("Maximum health regeneration reduced: ", "-25%", 0, false, TooltipProperty.TooltipPropertyFlags.None));
                list.Add(new TooltipProperty("Winds regeneration reduced: ", "-25%", 0, false, TooltipProperty.TooltipPropertyFlags.None));
                break;
            case ForestBindingLevel.Symbiosis:
                list.Add(new TooltipProperty("You are one with the forest", "", 0, false, TooltipProperty.TooltipPropertyFlags.None));
                break;
        }

        list.Add(new TooltipProperty("", " ", 0, false, TooltipProperty.TooltipPropertyFlags.Cost)); //empty line
        if (forestBindingLevel != ForestBindingLevel.Symbiosis)
        {
            list.Add(new TooltipProperty("Next Rank: ", (forestBindingLevel + 1).ToString(), 0, false, TooltipProperty.TooltipPropertyFlags.None));
            var result = GetResourceMinimumForForestBindingRank(forestBindingLevel + 1) - value;
            list.Add(new TooltipProperty("Required Forest binding: ", result.ToString("0"), 0, false, TooltipProperty.TooltipPropertyFlags.None));
        }


        list.Add(new TooltipProperty("", "", 0, false, TooltipProperty.TooltipPropertyFlags.DefaultSeperator));
        return list;
    }
    
    
    public static float GetResourceMinimumForForestBindingRank(ForestBindingLevel level)
    {
        switch (level)
        {
            case ForestBindingLevel.Symbiosis:
                return MinimumSymbiosis;
            case ForestBindingLevel.Bound:
                return MinimumBound;
            default:
                return 0;
        }
    }
}




public enum ForestBindingLevel
{
    Symbiosis = 2,
    Bound = 1,
    Unbound = 0
}