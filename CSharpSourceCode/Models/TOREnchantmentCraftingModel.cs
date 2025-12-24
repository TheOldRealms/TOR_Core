using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TOR_Core.CampaignMechanics.Crafting;
using TOR_Core.CharacterDevelopment;
using TOR_Core.Extensions;
using TOR_Core.Items;

namespace TOR_Core.Models;

public class TOREnchantmentCraftingModel : GameModel
{
    public int MaximumAmountOfEnchantments(List<Hero> heroes)
    {
        foreach (var hero in heroes)
        {
            if (hero == Hero.MainHero && Hero.MainHero.HasCareer(TORCareers.Runelord) && Hero.MainHero.HasUnlockedCareerChoiceTier(3))
            {
                return 3;
            }

            if (hero.HasAttribute("Runelord") && hero.HasAttribute("MasterCrafter"))
            {
                return 3;
            }
        }

        return 2;
    }



    public int GetEffectiveIngredientAmount(List<Hero> heroes, ItemTrait itemTrait, TorTradeGoodType ingerdient)
    {
        var explainedNumber = new ExplainedNumber(itemTrait.IngredientAmount);

        foreach (var hero in heroes)
        {
            if (hero.HasKnownEnchantmentBlueprint(itemTrait.ItemTraitStringId))
            {
                CharacterDevelopment.CareerSystem.CareerHelper.ApplyBasicCareerPassives(hero, ref explainedNumber, CharacterDevelopment.PassiveEffectType.EnchantmentCostReduction, true);
            }
        }

        return (int)explainedNumber.ResultNumber;
    }
}