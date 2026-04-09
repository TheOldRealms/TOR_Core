using System.Linq;
using TaleWorlds.CampaignSystem;
using TOR_Core.CampaignMechanics.Crafting;
using TOR_Core.Models;

namespace TOR_Core.Extensions
{
    public static class GameModelsExtensions
    {
        public static TORAbilityModel GetAbilityModel(this GameModels models)
        {
            return models.GetGameModels().OfType<TORAbilityModel>().LastOrDefault();
        }

        public static TORFaithModel GetFaithModel(this GameModels models)
        {
            return models.GetGameModels().OfType<TORFaithModel>().LastOrDefault();
        }

        public static TORCustomResourceModel GetCustomResourceModel(this GameModels models)
        {
            return models.GetGameModels().OfType<TORCustomResourceModel>().LastOrDefault();
        }

        public static TOREnchantmentIngredientsModel GetEnchantmentIngredientModel(this GameModels models)
        {
            return models.GetGameModels().OfType<TOREnchantmentIngredientsModel>().LastOrDefault();
        }

        public static TORCompanionTrainingModel GetCompanionTrainingModel(this GameModels models)
        {
            return models.GetGameModels().OfType<TORCompanionTrainingModel>().LastOrDefault();
        }

        public static TORHiringCompatibilityModel GetHiringCompatibilityModel(this GameModels models)
        {
            return models.GetGameModels().OfType<TORCompanionHiringCompatibilityModel>().LastOrDefault();
        }

        public static TORReinforcementRestrictionModel GetReinforcementRestrictionModel(this GameModels models)
        {
            return models.GetGameModels().OfType<TORReinforcementRestrictionModel>().LastOrDefault();
        }

        public static TORSiegeEngineCalculationModel GetSiegeEngineCalculationModel(this GameModels models)
        {
            return models.GetGameModels().OfType<TORSiegeEngineCalculationModel>().LastOrDefault();
        }

        public static TORSmithingModel GetSmithingModel(this GameModels models)
        {
            return models.GetGameModels().OfType<TORSmithingModel>().LastOrDefault();
        }
    }
}