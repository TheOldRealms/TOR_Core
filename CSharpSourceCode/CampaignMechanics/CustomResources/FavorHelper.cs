using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core.ViewModelCollection.Information;
using TOR_Core.Extensions;

namespace TOR_Core.CampaignMechanics.CustomResources;

public static class FavorHelper
{
    public static List<TooltipProperty> GetFavorInfo()
    {
        var list = new List<TooltipProperty>();
        if (Hero.MainHero.GetCustomResourceValue("Prestige") > 0)
        {
            var prestige = CustomResourceManager.GetResourceObject("Prestige");
            list.Add(new TooltipProperty(prestige.GetCustomResourceIconAsText(), Hero.MainHero.GetCustomResourceValue(prestige.StringId).ToString,3, false, TooltipProperty.TooltipPropertyFlags.Title));
        }

        return list;
    }
}