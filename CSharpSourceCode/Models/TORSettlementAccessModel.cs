using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Localization;
using TOR_Core.CampaignMechanics.SettlementPOIs;
using TOR_Core.CharacterDevelopment;

namespace TOR_Core.Models
{
    /// <summary>Thin dispatcher: TOR-prefixed location ids delegate to their owning <see cref="SettlementPOI"/>; non-TOR ids pass to vanilla.</summary>
    public class TORSettlementAccessModel : DefaultSettlementAccessModel
    {
        /// <summary>Access check for TOR POI ids. Unknown tor_-prefixed ids fail-closed with a generic sealed message.</summary>
        public override bool CanMainHeroAccessLocation(Settlement settlement, string locationId,
                                                       out bool disableOption, out TextObject disabledText)
        {
            if (locationId == null || !locationId.StartsWith("tor_"))
                return base.CanMainHeroAccessLocation(settlement, locationId, out disableOption, out disabledText);

            var poi = SettlementPOIManagerCampaignBehavior.Get(locationId);
            if (poi == null)
            {
                // Unknown tor_-prefixed id, fail-closed.
                disableOption = true;
                disabledText = new TextObject("This place is sealed.");
                return false;
            }

            bool allowed = poi.CheckAccess(settlement, Hero.MainHero, out disabledText);
            disableOption = !allowed;
            return allowed;
        }

        /// <summary>Settlement-action gate. Disguise perk lets a disguised hero trade in towns despite vanilla's stripped-perk block.</summary>
        public override bool CanMainHeroDoSettlementAction(Settlement settlement,
            SettlementAction settlementAction, out bool disableOption, out TextObject disabledText)
        {
            bool allowed = base.CanMainHeroDoSettlementAction(settlement, settlementAction,
                out disableOption, out disabledText);

            if (!allowed
                && settlementAction == SettlementAction.Trade
                && settlement != null && !settlement.IsVillage
                && Campaign.Current != null && Campaign.Current.IsMainHeroDisguised
                && Hero.MainHero != null
                && Hero.MainHero.GetPerkValue(TORPerks.Roguery.Disguise))
            {
                disableOption = false;
                disabledText = null;
                return true;
            }

            return allowed;
        }
    }
}
