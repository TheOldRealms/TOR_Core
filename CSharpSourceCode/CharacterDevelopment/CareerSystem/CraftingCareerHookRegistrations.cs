using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TOR_Core.CampaignMechanics.Crafting;
using TOR_Core.AbilitySystem;
using TOR_Core.Extensions;
using TOR_Core.Framework;
using TOR_Core.Utilities;
using RefiningFormula = TaleWorlds.Core.Crafting.RefiningFormula;

namespace TOR_Core.CharacterDevelopment.CareerSystem
{
    /// <summary>
    /// The one place CharacterDevelopment/CareerSystem is allowed to know about
    /// CampaignMechanics/Crafting: pushes each career's enchanting/smithing-specific effects
    /// into Crafting's CraftingCareerHooks extension points, so Crafting's own code never
    /// references a Career/CareerChoice by name. Called once from SubModule.BeginGameStart,
    /// after TORCareers/TORCareerChoices exist (the closures below resolve lazily at call
    /// time regardless, so exact ordering doesn't actually matter).
    /// </summary>
    public static class CraftingCareerHookRegistrations
    {
        public static void RegisterAll()
        {
            RegisterGreyLordEnchantmentCostReduction();
            RegisterOrcShamanIngredientLootBonus();
            RegisterBeginnerBlueprintGrants();
            RegisterSpellsingerEnchanterAccess();
            RegisterRunelordSmithingBonuses();
        }

        /// <summary>
        /// Grey Lord's "ForbiddenScrollsOfSapheryPassive3": -1% enchantment cost per known spell.
        /// Tagged PassiveEffectType.Special in GreyLordCareerChoices.cs (opts out of the generic
        /// CareerHelper dispatch since it needs a per-hero-computed multiplier, not a flat value).
        /// </summary>
        private static void RegisterGreyLordEnchantmentCostReduction()
        {
            CraftingCareerHooks.EnchantmentCostReductionFactors.Add(hero =>
            {
                if (!hero.HasCareerChoice("ForbiddenScrollsOfSapheryPassive3")) return 0f;

                var choice = TORCareerChoices.GetChoice("ForbiddenScrollsOfSapheryPassive3");
                if (choice == null) return 0f;

                var spellCount = hero.GetExtendedInfo().AllAbilities
                    .Select(AbilityFactory.GetTemplate)
                    .Count(ability => ability != null && ability.IsSpell);

                return spellCount * choice.GetPassiveValue() / 100f;
            });
        }

        /// <summary>
        /// Orc Shaman's "BonesAnFirepitzPassive3": +25% enchantment-ingredient loot. Also tagged
        /// PassiveEffectType.Special (evaluated as an additive bonus factor on Hero.MainHero
        /// specifically, not the generic per-choice dispatch).
        /// </summary>
        private static void RegisterOrcShamanIngredientLootBonus()
        {
            CraftingCareerHooks.IngredientLootBonusFactors.Add(hero =>
            {
                if (!hero.HasCareerChoice("BonesAnFirepitzPassive3")) return 0f;

                var choice = TORCareerChoices.GetChoice("BonesAnFirepitzPassive3");
                return choice?.GetPassiveValue() ?? 0f;
            });
        }

        /// <summary>
        /// Free beginner enchantment blueprints granted just by having picked a relevant career -
        /// Imperial Magister (per known Lore), Grail Damsel (Lore of Life), Runelord (rune stone).
        /// </summary>
        private static void RegisterBeginnerBlueprintGrants()
        {
            CraftingCareerHooks.BeginnerBlueprintGrantors.Add(hero =>
            {
                if (hero.IsSpellCaster() && hero.GetCareer() == TORCareers.ImperialMagister)
                {
                    if (hero.HasKnownLore("LoreOfDeath")) EnchantmentBlueprints.Learn("emp_enchant_shyish_whisper", hero, true);
                    if (hero.HasKnownLore("LoreOfMetal")) EnchantmentBlueprints.Learn("emp_enchant_chamon_whisper", hero, true);
                    if (hero.HasKnownLore("LoreOfLight")) EnchantmentBlueprints.Learn("emp_enchant_hysh_whisper", hero, true);
                    if (hero.HasKnownLore("LoreOfHeavens")) EnchantmentBlueprints.Learn("emp_enchant_azyr_whisper", hero, true);
                    if (hero.HasKnownLore("LoreOfBeasts")) EnchantmentBlueprints.Learn("emp_enchant_ghur_whisper", hero, true);
                    if (hero.HasKnownLore("LoreOfLife")) EnchantmentBlueprints.Learn("emp_enchant_ghyran_whisper", hero, true);
                    if (hero.HasKnownLore("LoreOfFire")) EnchantmentBlueprints.Learn("emp_enchant_aqshy_whisper", hero, true);
                }

                if (hero.IsSpellCaster() && hero.HasCareer(TORCareers.GrailDamsel))
                    if (hero.HasKnownLore("LoreOfLife"))
                        EnchantmentBlueprints.Learn("emp_enchant_ghyran_whisper", hero, true);

                if (hero.HasCareer(TORCareers.Runelord)) EnchantmentBlueprints.Learn("dw_rune_stone", hero, true);
            });
        }

        /// <summary>
        /// Spellsinger gets Asrai-culture enchanter access regardless of the hero's own culture
        /// or a matching companion (the normal rule Crafting itself enforces).
        /// </summary>
        private static void RegisterSpellsingerEnchanterAccess()
        {
            CraftingCareerHooks.EnchanterAccessGrants.Add((hero, culture) =>
                culture == TORConstants.Cultures.ASRAI && hero.HasCareer(TORCareers.Spellsinger));
        }

        /// <summary>
        /// Runelord: -40% smithing energy cost with ForgefireBurningPassive3; ForgefireBurningPassive1
        /// grants +1 Charcoal from refining, ForgefireBurningPassive2 doubles Iron refining output.
        /// </summary>
        private static void RegisterRunelordSmithingBonuses()
        {
            CraftingCareerHooks.EnergyCostModifiers.Add((hero, value) =>
            {
                if (hero.HasCareer(TORCareers.Runelord) && Hero.MainHero.HasCareerChoice("ForgefireBurningPassive3"))
                {
                    var reduction = value * 0.4f;
                    value -= (int)MathF.Round(reduction);
                }
                return value;
            });

            CraftingCareerHooks.RefiningFormulaModifiers.Add((hero, formula) =>
            {
                if (!hero.HasCareer(TORCareers.Runelord)) return formula;

                if (hero.HasCareerChoice("ForgefireBurningPassive1") && formula.Output == CraftingMaterials.Charcoal)
                {
                    return new RefiningFormula(formula.Input1, formula.Input1Count, formula.Input2, formula.Input2Count, formula.Output, formula.OutputCount + 1);
                }
                if (hero.HasCareerChoice("ForgefireBurningPassive2") && formula.Output is CraftingMaterials.Iron1 or CraftingMaterials.Iron2 or CraftingMaterials.Iron3 or CraftingMaterials.Iron4)
                {
                    return new RefiningFormula(formula.Input1, formula.Input1Count, formula.Input2, formula.Input2Count, formula.Output, formula.OutputCount * 2);
                }
                return formula;
            });
        }
    }
}
