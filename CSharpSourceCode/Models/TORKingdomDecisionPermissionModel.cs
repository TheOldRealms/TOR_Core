using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Localization;

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
            reason = TextObject.Empty;
            return true;
        }

        public override bool IsPeaceDecisionAllowedBetweenKingdoms(
            Kingdom kingdom1,
            Kingdom kingdom2,
            out TextObject reason)
        {
            reason = TextObject.Empty;
            return true;
        }

        public override bool IsAnnexationDecisionAllowed(Settlement annexedSettlement) => true;

        public override bool IsExpulsionDecisionAllowed(Clan expelledClan) => true;

        public override bool IsKingSelectionDecisionAllowed(Kingdom kingdom) => true;
    }
}
