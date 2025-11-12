using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core.ViewModelCollection.Information;
using TOR_Core.Extensions;

namespace TOR_Core.CampaignMechanics.CustomResources;

public static class TeefHelper
{
    public static WaaaghLevel GetWaaaghLevelForResource(float level)
    {
        return level switch
        {
            <= 100 => WaaaghLevel.InternalFightin,
            <= 300 => WaaaghLevel.PettySquabblin,
            <= 500 => WaaaghLevel.EreWeGo,
            _ => WaaaghLevel.WAAAGH
        };
    }

    public static float GetResourceMinimumForWaaaghRank(WaaaghLevel level)
    {
        return level switch
        {
            WaaaghLevel.WAAAGH => 501,
            WaaaghLevel.EreWeGo => 301,
            WaaaghLevel.PettySquabblin => 101,
            WaaaghLevel.InternalFightin => 0,
            _ => 0
        };
    }

    public static List<TooltipProperty> GetTeefInfo()
    {
        var list = new List<TooltipProperty>();
        var waaaghValue = Hero.MainHero.GetCustomResourceValue("Waaagh");

        var title = "Waaagh State";
        var waaaghLevel = GetWaaaghLevelForResource(waaaghValue);

        list.Add(new TooltipProperty("", "", 0, false, TooltipProperty.TooltipPropertyFlags.DefaultSeperator));

        // Display current Waaagh level
        var waaaghResource = CustomResourceManager.GetResourceObject("Waaagh");
        if (waaaghResource != null)
        {
            list.Add(new TooltipProperty("Current Waaagh", waaaghValue.ToString("0"), 0, false, TooltipProperty.TooltipPropertyFlags.None));
        }

        var stateName = waaaghLevel switch
        {
            WaaaghLevel.InternalFightin => "Internal Fightin'",
            WaaaghLevel.PettySquabblin => "Petty Squabblin'",
            WaaaghLevel.EreWeGo => "'Ere We Go!",
            WaaaghLevel.WAAAGH => "WAAAGH!!!!",
            _ => "Unknown"
        };

        list.Add(new TooltipProperty(title, stateName, 0, false, TooltipProperty.TooltipPropertyFlags.None));

        switch (waaaghLevel)
        {
            case WaaaghLevel.InternalFightin:
                list.Add(new TooltipProperty("Description", "Da Boys uv da mob are demoralized. They 'ave no gits to focus on an' resort to fightin' each other.", 0, false, TooltipProperty.TooltipPropertyFlags.MultiLine));
                list.Add(new TooltipProperty("", " ", 0, false, TooltipProperty.TooltipPropertyFlags.Cost));
                list.Add(new TooltipProperty("Morale", "-40", 0, false, TooltipProperty.TooltipPropertyFlags.None));
                list.Add(new TooltipProperty("Damage Dealt", "-20%", 0, false, TooltipProperty.TooltipPropertyFlags.None));
                list.Add(new TooltipProperty("Food Consumed", "-60%", 0, false, TooltipProperty.TooltipPropertyFlags.None));
                break;
            case WaaaghLevel.PettySquabblin:
                list.Add(new TooltipProperty("Description", "Da mob found sum gits to bash but smaller scraps are still occurin' among da tribe. Da Boys will soon start gettin' restless again.", 0, false, TooltipProperty.TooltipPropertyFlags.MultiLine));
                list.Add(new TooltipProperty("", " ", 0, false, TooltipProperty.TooltipPropertyFlags.Cost));
                list.Add(new TooltipProperty("Morale", "-20", 0, false, TooltipProperty.TooltipPropertyFlags.None));
                list.Add(new TooltipProperty("Damage Dealt", "-10%", 0, false, TooltipProperty.TooltipPropertyFlags.None));
                list.Add(new TooltipProperty("Food Consumed", "-30%", 0, false, TooltipProperty.TooltipPropertyFlags.None));
                list.Add(new TooltipProperty("Daily Wounded", "Smaller chance", 0, false, TooltipProperty.TooltipPropertyFlags.None));
                break;
            case WaaaghLevel.EreWeGo:
                list.Add(new TooltipProperty("Description", "Da recent exploits uv your mob 'ave been 'eard in other tribes as well. Greenskins from other tribes start gatherin', an' your Boys are preparin' fer a proppa big scrap.", 0, false, TooltipProperty.TooltipPropertyFlags.MultiLine));
                list.Add(new TooltipProperty("", " ", 0, false, TooltipProperty.TooltipPropertyFlags.Cost));
                list.Add(new TooltipProperty("Damage Dealt", "+10%", 0, false, TooltipProperty.TooltipPropertyFlags.None));
                list.Add(new TooltipProperty("Food Consumed", "+25%", 0, false, TooltipProperty.TooltipPropertyFlags.None));
                list.Add(new TooltipProperty("Party Size", "+60", 0, false, TooltipProperty.TooltipPropertyFlags.None));
                list.Add(new TooltipProperty("Daily Recruitment", "Small chance (T1-3)", 0, false, TooltipProperty.TooltipPropertyFlags.None));
                break;
            case WaaaghLevel.WAAAGH:
                list.Add(new TooltipProperty("Description", "Now da Boys are proppa eager an' killy! Wez gonna show all dem humies an' stunties an' all da uva gits too! DIS IZ WAAAAGH!!!", 0, false, TooltipProperty.TooltipPropertyFlags.MultiLine));
                list.Add(new TooltipProperty("", " ", 0, false, TooltipProperty.TooltipPropertyFlags.Cost));
                list.Add(new TooltipProperty("Damage Dealt", "+20%", 0, false, TooltipProperty.TooltipPropertyFlags.None));
                list.Add(new TooltipProperty("Food Consumed", "+100%", 0, false, TooltipProperty.TooltipPropertyFlags.None));
                list.Add(new TooltipProperty("Party Size", "+120", 0, false, TooltipProperty.TooltipPropertyFlags.None));
                list.Add(new TooltipProperty("Daily Recruitment", "Big chance (T1-3)", 0, false, TooltipProperty.TooltipPropertyFlags.None));
                break;
        }

        list.Add(new TooltipProperty("", " ", 0, false, TooltipProperty.TooltipPropertyFlags.Cost));

        if (waaaghLevel != WaaaghLevel.WAAAGH)
        {
            var nextLevel = waaaghLevel + 1;
            var nextStateName = nextLevel switch
            {
                WaaaghLevel.PettySquabblin => "Petty Squabblin'",
                WaaaghLevel.EreWeGo => "'Ere We Go!",
                WaaaghLevel.WAAAGH => "WAAAGH!!!!",
                _ => "Unknown"
            };

            list.Add(new TooltipProperty("Next State", nextStateName, 0, false, TooltipProperty.TooltipPropertyFlags.None));
            var required = GetResourceMinimumForWaaaghRank(nextLevel) - waaaghValue;
            list.Add(new TooltipProperty("Required Waaagh", required.ToString("0"), 0, false, TooltipProperty.TooltipPropertyFlags.None));
        }

        list.Add(new TooltipProperty("", "", 0, false, TooltipProperty.TooltipPropertyFlags.DefaultSeperator));
        return list;
    }
}

public enum WaaaghLevel
{
    WAAAGH = 3,
    EreWeGo = 2,
    PettySquabblin = 1,
    InternalFightin = 0
}