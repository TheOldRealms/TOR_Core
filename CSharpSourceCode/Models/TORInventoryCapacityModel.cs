using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Party;
using TOR_Core.CharacterDevelopment;
using TOR_Core.CharacterDevelopment.CareerSystem;
using TOR_Core.Utilities;

namespace TOR_Core.Models
{
    public class TORInventoryCapacityModel : DefaultInventoryCapacityModel
    {
        public override ExplainedNumber CalculateInventoryCapacity(MobileParty mobileParty, bool isCurrentlyAtSea, bool includeDescriptions = false, int additionalTroops = 0, int additionalSpareMounts = 0, int additionalPackAnimals = 0, bool includeFollowers = false)
        {
            var result = base.CalculateInventoryCapacity(mobileParty, includeDescriptions, isCurrentlyAtSea, additionalTroops, additionalSpareMounts, additionalPackAnimals, includeFollowers);
            if (mobileParty != null && mobileParty.HasPerk(TORPerks.GunPowder.AmmoWagons))
            {
                result.AddFactor(TORPerks.GunPowder.AmmoWagons.SecondaryBonus, TORPerks.GunPowder.AmmoWagons.Name);
            }

            if (mobileParty == MobileParty.MainParty)
            {
                CareerHelper.ApplyBasicCareerPassives(Hero.MainHero, ref result, PassiveEffectType.InventoryCapacity, true);
            }

            if (mobileParty.LeaderHero != null &&
                mobileParty.LeaderHero.Culture.StringId == TORConstants.Cultures.GREENSKIN)
            {
                result.AddFactor(0.2f);
            }

            return result;
        }
    }
}