using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;

namespace TOR_Core.CampaignMechanics.Crafting
{
    /// <summary>
    /// Single registration entry point for the Crafting module (weapon/armor enchanting,
    /// artisan-district item duplication, related loot/models), so SubModule.cs only needs
    /// one call per lifecycle hook instead of one line per behavior/model.
    /// </summary>
    public static class CraftingModule
    {
        public static void RegisterCampaignBehaviors(CampaignGameStarter starter)
        {
            starter.AddBehavior(new EnchanterTownBehavior());
            starter.AddBehavior(new TORArtisanDistrictCampaignBehavior());
            starter.AddBehavior(new PriestBehavior());
            starter.AddBehavior(new EnchantmentIngredientLootCampaignBehavior());
            starter.AddBehavior(new LootCampaignBehavior());
        }

        public static void RegisterModels(IGameStarter gameStarterObject)
        {
            gameStarterObject.AddModel(new TORSmithingModel());
            gameStarterObject.AddModel(new TOREnchantmentIngredientsModel());
            gameStarterObject.AddModel(new TOREnchantmentCraftingModel());
        }
    }
}
