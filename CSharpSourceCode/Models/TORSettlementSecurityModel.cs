using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TOR_Core.CharacterDevelopment;

namespace TOR_Core.Models
{
    public class TORSettlementSecurityModel : DefaultSettlementSecurityModel
    {
        public override ExplainedNumber CalculateSecurityChange(Town town, bool includeDescriptions = false)
        {
            var result = base.CalculateSecurityChange(town, includeDescriptions);
            // By the Road secondary: governor-role roguery perk grants
            // +SecondaryBonus (1 by default) security per day in
            // settlements they govern.
            var governor = town?.Governor;
            if (governor != null && governor.GetPerkValue(TORPerks.Roguery.ByTheRoad))
            {
                result.Add(TORPerks.Roguery.ByTheRoad.SecondaryBonus,
                    new TextObject("{=tor_perk_by_the_road_security}By the Road"));
            }
            return result;
        }
    }
}
