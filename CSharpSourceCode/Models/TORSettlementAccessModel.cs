using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Localization;
using TOR_Core.CampaignMechanics.SettlementPOIs;
using TOR_Core.CharacterDevelopment;

namespace TOR_Core.Models
{
    /// <summary>
    /// Thin dispatcher — delegates TOR-prefixed location access questions to the owning
    /// <see cref="SettlementPOI"/> via <see cref="SettlementPOIManagerCampaignBehavior"/>. Non-TOR ids
    /// delegate to the vanilla base model unchanged.
    ///
    /// <para>Access rules themselves live in each POI's <see cref="SettlementPOI.CheckAccess"/>
    /// override, not here. Adding a new POI requires no changes to this file.</para>
    /// </summary>
    public class TORSettlementAccessModel : DefaultSettlementAccessModel
    {
        public override bool CanMainHeroAccessLocation(Settlement settlement, string locationId,
                                                       out bool disableOption, out TextObject disabledText)
        {
            if (locationId == null || !locationId.StartsWith("tor_"))
                return base.CanMainHeroAccessLocation(settlement, locationId, out disableOption, out disabledText);

            var poi = SettlementPOIManagerCampaignBehavior.Get(locationId);
            if (poi == null)
            {
                // Unknown tor_-prefixed id — fail-closed. Any menu option that pointed at
                // this id without a matching registered POI is broken configuration; the
                // fallback keeps the game runnable without obscuring the bug.
                disableOption = true;
                disabledText = new TextObject("This place is sealed.");
                return false;
            }

            bool allowed = poi.CheckAccess(settlement, Hero.MainHero, out disabledText);
            disableOption = !allowed;
            return allowed;
        }

        /// <summary>Disguise primary (Roguery 175): "You can trade in towns
        /// while disguised." Vanilla's <c>CanMainHeroTrade</c> is a private
        /// helper gated on <c>DefaultPerks.Roguery.SmugglerConnections</c>,
        /// which TOR strips. Intercept the public dispatcher for the Trade
        /// case: let base compute the vanilla result first, then flip the
        /// disabled flag when the ONLY possible reason for vanilla's
        /// disabled-true in the non-village branch is the stripped-perk
        /// gate. Village branch has its own non-disguise disables (war,
        /// stock, gold) so it's skipped.</summary>
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
