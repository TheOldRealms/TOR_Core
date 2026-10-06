using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Localization;
using TOR_Core.CampaignMechanics.SettlementPOIs;

namespace TOR_Core.Models
{
    /// <summary>
    /// Thin dispatcher — delegates TOR-prefixed location access questions to the owning
    /// <see cref="SettlementPOI"/> via <see cref="SettlementPOIRegistry"/>. Non-TOR ids
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

            var poi = SettlementPOIRegistry.Get(locationId);
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
    }
}
