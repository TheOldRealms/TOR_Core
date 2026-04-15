using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Localization;
using TOR_Core.Extensions;
using TOR_Core.Utilities;

namespace TOR_Core.CampaignMechanics.Diplomacy
{
    public class TORKingdomDecisionPermissionModel : KingdomDecisionPermissionModel
    {
        public override bool IsPolicyDecisionAllowed(PolicyObject policy) => true;

        public override bool IsWarDecisionAllowedBetweenKingdoms(
            Kingdom kingdom1,
            Kingdom kingdom2,
            out TextObject reason)
        {
            reason = TextObject.GetEmpty();
            return true;
        }

        public override bool IsPeaceDecisionAllowedBetweenKingdoms(
            Kingdom kingdom1,
            Kingdom kingdom2,
            out TextObject reason)
        {
            reason = TextObject.GetEmpty();
            if (kingdom1.Culture.StringId == TORConstants.Cultures.CHAOS || kingdom2.Culture.StringId == TORConstants.Cultures.CHAOS) return false;
            return true;
        }

        public override bool IsAnnexationDecisionAllowed(Settlement annexedSettlement) => true;

        public override bool IsExpulsionDecisionAllowed(Clan expelledClan) => true;

        public override bool IsKingSelectionDecisionAllowed(Kingdom kingdom) => true;

        public override bool IsStartAllianceDecisionAllowedBetweenKingdoms(Kingdom kingdom1, Kingdom kingdom2, out TextObject reason)
        {
            // Chaos cannot form alliances
            if (kingdom1.Culture.StringId == TORConstants.Cultures.CHAOS || kingdom2.Culture.StringId == TORConstants.Cultures.CHAOS)
            {
                reason = TORTextHelper.GetTextObject("TOR_Alliance_Chaos_Forbidden", "The forces of Chaos cannot form alliances.");
                return false;
            }

            // Check for hostile religions
            if ((bool)(kingdom1?.Leader?.GetDominantReligion()?.HostileReligions?.Contains(kingdom2?.Leader?.GetDominantReligion())))
            {
                reason = TORTextHelper.GetTextObject("TOR_Alliance_Religion_Conflict", "The dominant religions of the two kingdoms are hostile towards each other.");
                return false;
            }
            else
            {
                reason = TextObject.GetEmpty();
                return true;
            }
        }
    }
}