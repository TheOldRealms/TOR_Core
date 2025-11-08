using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Localization;
using TaleWorlds.TwoDimension;
using TOR_Core.Extensions;

namespace TOR_Core.CampaignMechanics.CustomResources;

public class ForestHarmonyHelper
{
   
    private static readonly int MinimumBound = 750;
    private static readonly int MinimumHarmony = 1500;

    public static readonly float HealthDebuffUnBound = -0.35f;
    public static readonly float HealthDebuffBound = -0.15f;
    
    public static readonly float HealthRegDebuffUnBound = -0.25f;
    public static readonly float HealthRegDebuffBound = -0.12f;
    
    public static readonly float WindsDebuffUnbound = -0.50f;
    public static readonly float WindsDebuffBound = -0.25f;

    
    public static ForestHarmonyLevel GetForestHarmonyLevelForResource(float level)
    {
        var result = ForestHarmonyLevel.Unbound;

        switch (level)
        {
            case var _ when level < MinimumBound:
            {
                result = ForestHarmonyLevel.Unbound;
                break;
            }
            case var _ when level > MinimumBound &&  level < MinimumHarmony:
            {
                result = ForestHarmonyLevel.Bound;
                break;
            }
            case var _ when level > MinimumHarmony:
            {
                result = ForestHarmonyLevel.Harmony;
                break;
            }
        }

        return result;
    }
    
    public static bool HasForestBindingLevel(Hero hero, ForestHarmonyLevel harmonyLevel)
    {
        return harmonyLevel == hero.GetForestHarmonyLevel();
    }

    private static new List<TooltipProperty> GetForestSymbolText(string ForestSymbol)
    {
        var list = new List<TooltipProperty>();

        var hasTitle = GameTexts.TryGetText("tor_treesymbol_title",  out var symbolTitle, ForestSymbol);
        
        var hasText = GameTexts.TryGetText("tor_treesymbol_description", out var symbolText, ForestSymbol);

        if (hasTitle && hasText)
        {
            list.Add(new TooltipProperty("", "", 0, false, TooltipProperty.TooltipPropertyFlags.DefaultSeperator));
            list.Add(new TooltipProperty("Current active symbol: ", symbolTitle.ToString, 0, false, TooltipProperty.TooltipPropertyFlags.None));
            list.Add(new TooltipProperty("", symbolText.ToString, 0, false, TooltipProperty.TooltipPropertyFlags.MultiLine));
        }

        return list;
    }

    public static List<TooltipProperty> GetForestHarmonyInfo()
    {
        var list = new List<TooltipProperty>();
        var value = Hero.MainHero.GetCustomResourceValue("ForestBinding");
        var forestBindingLevel = Hero.MainHero.GetForestHarmonyLevel();


        
        if(Hero.MainHero.HasAttribute("WEKithbandSymbol"))
        {
            list.AddRange(GetForestSymbolText("WEKithbandSymbol"));
        }
        
        if(Hero.MainHero.HasAttribute("WEWardancerSymbol"))
        {
            list.AddRange(GetForestSymbolText("WEWardancerSymbol"));
        }
        
        if(Hero.MainHero.HasAttribute("WETreekinSymbol"))
        {
            list.AddRange(GetForestSymbolText("WETreekinSymbol"));
        }
        
        if(Hero.MainHero.HasAttribute("WEOrionSymbol"))
        {
            list.AddRange(GetForestSymbolText("WEOrionSymbol"));
        }
        
        if(Hero.MainHero.HasAttribute("WEArielSymbol"))
        {
            list.AddRange(GetForestSymbolText("WEArielSymbol"));
        }
        
        if(Hero.MainHero.HasAttribute("WEDurthuSymbol"))
        {
            list.AddRange(GetForestSymbolText("WEDurthuSymbol"));
        }
        
        if(Hero.MainHero.HasAttribute("WEWandererSymbol"))
        {
            list.AddRange(GetForestSymbolText("WEWandererSymbol"));
        }

        var text = forestBindingLevel.ToString();
        var title = "Forest Harmony: "+text;


        list.Add(new TooltipProperty("", "", 0, false, TooltipProperty.TooltipPropertyFlags.DefaultSeperator));
        list.Add(new TooltipProperty(title, "", 0, false,
            TooltipProperty.TooltipPropertyFlags.RundownResult));
        
        if (!Hero.MainHero.HasAttribute("WEWandererSymbol"))
        {
            switch (forestBindingLevel)
            {
                case ForestHarmonyLevel.Unbound:
                    list.Add(new TooltipProperty("Maximum health reduced: ", HealthDebuffUnBound.ToString("0%"), 0, false, TooltipProperty.TooltipPropertyFlags.None));
                    list.Add(new TooltipProperty("Health regeneration reduced: ", HealthRegDebuffUnBound.ToString("0%"), 0, false, TooltipProperty.TooltipPropertyFlags.None));
                    list.Add(new TooltipProperty("Winds regeneration reduced: ", WindsDebuffUnbound.ToString("0%"), 0, false, TooltipProperty.TooltipPropertyFlags.None));
                    break;
                case ForestHarmonyLevel.Bound:
                    list.Add(new TooltipProperty("Maximum health reduced: ", HealthDebuffBound.ToString("0%"), 0, false, TooltipProperty.TooltipPropertyFlags.None));
                    list.Add(new TooltipProperty("Health regeneration reduced: ", HealthDebuffBound.ToString("0%"), 0, false, TooltipProperty.TooltipPropertyFlags.None));
                    list.Add(new TooltipProperty("Winds regeneration reduced: ", WindsDebuffBound.ToString("0%"), 0, false, TooltipProperty.TooltipPropertyFlags.None));
                    break;
                case ForestHarmonyLevel.Harmony:
                    list.Add(new TooltipProperty("You are one with the forest", "", 0, false, TooltipProperty.TooltipPropertyFlags.None));
                    break;
            }
        }


        list.Add(new TooltipProperty("", " ", 0, false, TooltipProperty.TooltipPropertyFlags.Cost)); //empty line
        if (forestBindingLevel != ForestHarmonyLevel.Harmony)
        {
            list.Add(new TooltipProperty("Next Rank: ", (forestBindingLevel + 1).ToString(), 0, false, TooltipProperty.TooltipPropertyFlags.None));//the enum conversion to string won't support localization ids because it needs to pass through a textObject first
            var result = GetResourceMinimumForForestBindingRank(forestBindingLevel + 1) - value;
            list.Add(new TooltipProperty("Required Forest Harmony: ", result.ToString("0"), 0, false, TooltipProperty.TooltipPropertyFlags.None));
        }


        list.Add(new TooltipProperty("", "", 0, false, TooltipProperty.TooltipPropertyFlags.DefaultSeperator));
        return list;
    }
    
    public static float GetResourceMinimumForForestBindingRank(ForestHarmonyLevel level)
    {
        switch (level)
        {
            case ForestHarmonyLevel.Harmony:
                return MinimumHarmony;
            case ForestHarmonyLevel.Bound:
                return MinimumBound;
            default:
                return 0;
        }
    }

    public static TextObject TreeSymbolText(string attributeId)
    {
        switch (attributeId)
        {
            case  "WEKithbandSymbol": return GameTexts.FindText("tor_treesymbol_title", "WEKithbandSymbol");
            case  "WEWardancerSymbol": return GameTexts.FindText("tor_treesymbol_title", "WEWardancerSymbol");
            case  "WETreekinSymbol": return GameTexts.FindText("tor_treesymbol_title", "WETreekinSymbol");
            case  "WEOrionSymbol": return GameTexts.FindText("tor_treesymbol_title", "WEOrionSymbol");
            case  "WEArielSymbol": return GameTexts.FindText("tor_treesymbol_title", "WEArielSymbol");
            case  "WEDurthuSymbol": return GameTexts.FindText("tor_treesymbol_title", "WEDurthuSymbol");
            case  "WEWandererSymbol": return GameTexts.FindText("tor_treesymbol_title", "WEWandererSymbol");
        }

        return new TextObject("Failed to find symbol's associated text : " + attributeId);
    }

    public static float HarmonySymbolPenalty()
    {
        if (Hero.MainHero.HasAttribute("WEWardancerSymbol") || Hero.MainHero.HasAttribute("WEKithbandSymbol")) return 0.25f;
        if (Hero.MainHero.HasAttribute("WEWandererSymbol")) return 0.5f;

        return 0f;
    }
}

public enum ForestHarmonyLevel
{
    Harmony = 2,
    Bound = 1,
    Unbound = 0
}