using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TOR_Core.Extensions;
using TOR_Core.Utilities;

namespace TOR_Core.Models;

public class TORVillageProductionCalculatorModel : DefaultVillageProductionCalculatorModel
{
    public override ExplainedNumber CalculateDailyProductionAmount(Village village, ItemObject item)
    {
        var value = base.CalculateDailyProductionAmount(village, item);

        if (village.Settlement.Culture.StringId == TORConstants.Cultures.DAWI)
        {
            if (item.ItemCategory == DefaultItemCategories.Iron || item.ItemCategory == DefaultItemCategories.Silver || item.ItemCategory == DefaultItemCategories.Salt)
            {
                var bonus = 1f;
                if (Hero.MainHero.HasAttribute(CharacterAttributes.GUILD_MINERS_3))
                {
                    bonus = 1.25f;
                }
                else if (Hero.MainHero.HasAttribute(CharacterAttributes.GUILD_MINERS_2))
                {
                    bonus = 1.1f;
                }

                value.AddFactor(bonus, new TextObject("Dwarf Mining Bonus"));
            }


        }


        return value;
    }
}