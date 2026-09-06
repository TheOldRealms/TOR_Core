using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TOR_Core.CampaignMechanics.Crafting.Models;
using TOR_Core.Framework;

namespace TOR_Core.CampaignMechanics.Crafting
{
    /// <summary>
    /// Registration entry point for the Crafting module (weapon/armor enchanting,
    /// artisan-district item duplication, related loot/models), so SubModule.cs only needs
    /// one call per lifecycle hook instead of one line per behavior/model.
    /// </summary>
    [TORModule]
    public class CraftingModule : ITORModule
    {
        public void OnSubModuleLoad() { }

        public void RegisterCampaignBehaviors(CampaignGameStarter starter)
        {
            starter.AddBehavior(new EnchanterTownBehavior());
            starter.AddBehavior(new TORArtisanDistrictCampaignBehavior());
            starter.AddBehavior(new PriestBehavior());
            starter.AddBehavior(new EnchantmentIngredientLootCampaignBehavior());
            starter.AddBehavior(new LootCampaignBehavior());
        }

        public void RegisterModels(IGameStarter gameStarterObject)
        {
            gameStarterObject.AddModel(new TORSmithingModel());
            gameStarterObject.AddModel(new TOREnchantmentIngredientsModel());
            gameStarterObject.AddModel(new TOREnchantmentCraftingModel());
        }

        public void RegisterMissionBehaviors(Mission mission) { }

        public void RegisterGameObjectTypes(Game game) { }
    }
}
