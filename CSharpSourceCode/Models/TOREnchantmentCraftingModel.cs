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
        foreach (var hero in heroes)
        {
            // True Transmutation perk: 2 enchantments, 3 if dwarf
            if (hero == Hero.MainHero && Hero.MainHero.GetPerkValue(TORPerks.Spellcraft.TrueTransmutation))
            {
                bool isDwarf = Hero.MainHero.Culture?.StringId == TORConstants.Cultures.DAWI;
                return isDwarf ? 3 : 2;
            }

            // Miracle perk: 2 blessings
            if (hero == Hero.MainHero && Hero.MainHero.GetPerkValue(TORPerks.Faith.Miracle))
            {
                return 2;
            }

            // Dwarfs get 2 enchantments by default
            if (hero == Hero.MainHero && Hero.MainHero.Culture?.StringId == TORConstants.Cultures.DAWI)
            {
                return 2;
            }
        }

        return 1;
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