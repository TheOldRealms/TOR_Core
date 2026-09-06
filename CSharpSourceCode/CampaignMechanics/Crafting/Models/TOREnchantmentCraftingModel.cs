using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TOR_Core.CharacterDevelopment;
using TOR_Core.CharacterDevelopment.CareerSystem;
using TOR_Core.Framework;
using TOR_Core.Items;
using TOR_Core.Utilities;

namespace TOR_Core.CampaignMechanics.Crafting.Models;

public class TOREnchantmentCraftingModel : GameModel
{
    public int MaximumAmountOfEnchantments(List<Hero> heroes)
    {
        //Sly : the arguments do not permit a differentiation between enchantments and blessings. If this is desired, the method will need a change in implementation to be achieved.
        var value = 1;
        foreach (var hero in heroes)
        {
            if (value == 3) return value;

            bool isDwarf = hero.Culture?.StringId == TORConstants.Cultures.DAWI;

            // True Transmutation perk: 2 enchantments, 3 if dwarf
            if (hero.GetPerkValue(TORPerks.Spellcraft.TrueTransmutation))
            {
                value = Math.Max(value, isDwarf ?  3 : 2);
            }

            // Miracle perk: 2 blessings
            //Sly : I'm considering blessings and enchantments equivalent until the desired usage is clarified at which point this method can be updated and this comment removed.
            else if (hero.GetPerkValue(TORPerks.Faith.Miracle))
            {
                value = Math.Max(value, isDwarf ?  3 : 2);
            }

            // Dwarfs get 2 enchantments by default
            else if (isDwarf)
            {
                return Math.Max(value, 2);
            }
        }

        return value;
    }



    /// <summary>
    /// Ingredient cost after career discounts.
    /// </summary>
    public int GetEffectiveIngredientAmount(ItemTrait itemTrait, TorTradeGoodType ingredient)
    {
        var explainedNumber = new ExplainedNumber(itemTrait.IngredientAmount);
        var hero = Hero.MainHero;

        if (hero != null && EnchantmentBlueprints.IsKnown(itemTrait.ItemTraitStringId))
        {
            // Note: this call is a different kind of coupling than the module-specific
            // hooks below - PassiveEffectType is a generic, career-agnostic dispatch
            // mechanism (CareerHelper doesn't know "Grail Damsel"/"Necrarch"/"Runelord" by
            // name, it just applies every choice tagged EnchantmentCostReduction), it's
            // just currently misplaced under CharacterDevelopment/CareerSystem alongside
            // genuinely Careers-specific content. Left as-is; properly fixing it means
            // relocating CareerHelper/PassiveEffectType to Framework, a separate, larger
            // move with a much bigger blast radius (CareerHelper is used everywhere).
            CharacterDevelopment.CareerSystem.CareerHelper.ApplyBasicCareerPassives(hero, ref explainedNumber, PassiveEffectType.EnchantmentCostReduction, true);

            foreach (var factor in CraftingCareerHooks.EnchantmentCostReductionFactors)
            {
                explainedNumber.AddFactor(factor(hero));
            }
        }

        return (int)explainedNumber.ResultNumber;
    }
}