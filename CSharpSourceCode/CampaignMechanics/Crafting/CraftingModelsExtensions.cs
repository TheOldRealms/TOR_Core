using System.Linq;
using TaleWorlds.CampaignSystem;
using TOR_Core.CampaignMechanics.Crafting.Models;

namespace TOR_Core.CampaignMechanics.Crafting
{
    /// <summary>
    /// Shortcuts to Campaign.Current.Models.GetGameModels().OfType&lt;T&gt;() for the models
    /// this module owns - colocated here rather than in the shared Extensions/GameModelsExtensions
    /// so that Framework layer doesn't need to know this module's concrete model types.
    /// </summary>
    public static class CraftingModelsExtensions
    {
        public static TORSmithingModel GetSmithingModel(this GameModels models)
        {
            return models.GetGameModels().OfType<TORSmithingModel>().LastOrDefault();
        }

        public static TOREnchantmentIngredientsModel GetEnchantmentIngredientModel(this GameModels models)
        {
            return models.GetGameModels().OfType<TOREnchantmentIngredientsModel>().LastOrDefault();
        }
    }
}
