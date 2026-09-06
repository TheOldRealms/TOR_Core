using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using RefiningFormula = TaleWorlds.Core.Crafting.RefiningFormula;

namespace TOR_Core.Framework
{
    /// <summary>
    /// Shared contract between CampaignMechanics/Crafting and whichever module wants to
    /// influence enchanting/smithing (Careers, chiefly) - lives here rather than inside
    /// Crafting's own folder so that neither module has to reference the other's namespace:
    /// Crafting depends on this (Framework) to consume the hooks, and the module contributing
    /// effects (today, CharacterDevelopment/CareerSystem/CraftingCareerHookRegistrations,
    /// called once from SubModule.BeginGameStart after the career registries exist) depends on
    /// this (Framework) to populate them. Nothing under CampaignMechanics/Crafting/ references
    /// CharacterDevelopment/CareerSystem for career-specific content any more (the one
    /// remaining reference, CareerHelper.ApplyBasicCareerPassives(...,
    /// PassiveEffectType.EnchantmentCostReduction, ...) in TOREnchantmentCraftingModel, is a
    /// different case - see its comment), and CraftingCareerHookRegistrations never references
    /// CampaignMechanics/Crafting either.
    /// </summary>
    public static class CraftingCareerHooks
    {
        /// <summary>Additional multiplicative cost-reduction factors for enchanting (folded in via ExplainedNumber.AddFactor), evaluated once per hero contributing to an enchantment.</summary>
        public static readonly List<Func<Hero, float>> EnchantmentCostReductionFactors = new();

        /// <summary>Additional bonus factors for enchantment-ingredient loot amount (added to the base 1.0 multiplier), evaluated for Hero.MainHero.</summary>
        public static readonly List<Func<Hero, float>> IngredientLootBonusFactors = new();

        /// <summary>Grants free beginner enchantment blueprints; called once per Hero.MainHero when the enchanter town service opens.</summary>
        public static readonly List<Action<Hero>> BeginnerBlueprintGrantors = new();

        /// <summary>Extra "can use this culture's enchanter" checks, called as (Hero.MainHero, cultureId); true from any one grants access regardless of the normal culture/companion rule.</summary>
        public static readonly List<Func<Hero, string, bool>> EnchanterAccessGrants = new();

        /// <summary>Additional energy-cost modifiers for smithing/refining/smelting, applied as (hero, runningCost) -&gt; modifiedCost, chained in registration order.</summary>
        public static readonly List<Func<Hero, int, int>> EnergyCostModifiers = new();

        /// <summary>Per-formula modifiers for the refining-formula list, applied as (hero, formula) -&gt; (possibly modified) formula, chained in registration order.</summary>
        public static readonly List<Func<Hero, RefiningFormula, RefiningFormula>> RefiningFormulaModifiers = new();
    }
}
