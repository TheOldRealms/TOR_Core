using Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Party;
using TOR_Core.CharacterDevelopment;

namespace TOR_Core.Models
{
    public class TORInventoryCapacityModel : DefaultInventoryCapacityModel
    {
        public override ExplainedNumber CalculateInventoryCapacity(MobileParty mobileParty, bool includeDescriptions = false, int additionalTroops = 0, int additionalSpareMounts = 0, int additionalPackAnimals = 0, bool includeFollowers = false)
        {
            var result = base.CalculateInventoryCapacity(mobileParty, includeDescriptions, additionalTroops, additionalSpareMounts, additionalPackAnimals, includeFollowers);
            if(mobileParty != null && mobileParty.HasPerk(TORPerks.GunPowder.AmmoWagons))
            {
                result.AddFactor(TORPerks.GunPowder.AmmoWagons.SecondaryBonus, TORPerks.GunPowder.AmmoWagons.Name);
            }
            return result;
        }
    }
}
