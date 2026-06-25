using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TOR_Core.Extensions;
using TOR_Core.Utilities;

namespace TOR_Core.Models
{
    public class TORPartyDesertionModel : DefaultPartyDesertionModel
    {
        public override TroopRoster GetTroopsToDesert(MobileParty mobileParty)
        {
            //Sly : this is not what happens in practice.
            // For Greenskin player party: if they have Teef AND food AND are within party size limit, no desertion
            if (mobileParty.IsMainParty &&
                Hero.MainHero.Culture.StringId == TORConstants.Cultures.GREENSKIN)
            {
                var teef = Hero.MainHero.GetCustomResourceValue("Teef");
                var food = mobileParty.ItemRoster.TotalFood;
                var isWithinSizeLimit = mobileParty.Party.NumberOfAllMembers <= mobileParty.Party.PartySizeLimit;

                // Only prevent desertion if within size limit AND have resources
                if (teef > 0 && food > 0 && isWithinSizeLimit)
                {
                    return TroopRoster.CreateDummyTroopRoster();
                }
            }

            return base.GetTroopsToDesert(mobileParty);//Sly : the default model has no way to account for custom resources. Only starvation (morale) and wage desertion can be passed to it; custom resource desertion must be handled by us.
        }
    }
}
