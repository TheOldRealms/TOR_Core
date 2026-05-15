using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TOR_Core.AbilitySystem;
using TOR_Core.CampaignMechanics.Crafting;
using TOR_Core.CharacterDevelopment;
using TOR_Core.CharacterDevelopment.CareerSystem;
using TOR_Core.Extensions;
using TOR_Core.Items;
using TOR_Core.Utilities;

namespace TOR_Core.Models;

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



    public int GetEffectiveIngredientAmount(List<Hero> heroes, ItemTrait itemTrait, TorTradeGoodType ingerdient)
    {
        var explainedNumber = new ExplainedNumber(itemTrait.IngredientAmount);

        foreach (var hero in heroes)
        {
            if (hero.HasKnownEnchantmentBlueprint(itemTrait.ItemTraitStringId))
            {
                CharacterDevelopment.CareerSystem.CareerHelper.ApplyBasicCareerPassives(hero, ref explainedNumber, PassiveEffectType.EnchantmentCostReduction, true);

                // Greylord: For every known spell, reduce enchantment cost by 1%
                if (hero.HasCareerChoice("ForbiddenScrollsOfSapheryPassive3"))
                {
                    var choice = TORCareerChoices.GetChoice("ForbiddenScrollsOfSapheryPassive3");
                    if (choice != null)
                    {
                        var spellCount = hero.GetExtendedInfo().AllAbilities
                            .Select(AbilityFactory.GetTemplate)
                            .Count(ability => ability != null && ability.IsSpell);
                        var reductionPercent = spellCount * choice.GetPassiveValue();
                        explainedNumber.AddFactor(reductionPercent / 100f);
                    }
                }
            }
        }

        return (int)explainedNumber.ResultNumber;
    }
}