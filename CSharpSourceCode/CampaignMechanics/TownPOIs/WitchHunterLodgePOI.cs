using TaleWorlds.Localization;

namespace TOR_Core.CampaignMechanics.TownPOIs
{
    /// <summary>
    /// Witch Hunter Lodge POI — the Order of Sigmar chapterhouses. Reachable from the
    /// <c>town_artisan</c> submenu in seven Empire towns (see
    /// <see cref="TownPOILocations.IsWitchHunterLodgeSettlement"/>). Race-locked to humans; no
    /// career gate; no NPC trainer (empty scene). Access rules enforced by
    /// <see cref="TOR_Core.Models.TORSettlementAccessModel.CanAccessWitchHunterLodge"/>.
    /// </summary>
    public class WitchHunterLodgePOI : TownPOIBehavior
    {
        protected override string LocationId => TownPOILocations.WitchHunterLodgeLocationId;
        protected override string ParentMenuId => "town_artisan";
        protected override string AnchorEntryId => "town_artisan_leave";
        protected override bool AnchorAbove => true;    // sit directly above "Leave"

        protected override string EnterLabelId => "tor_poi_witchhunter_lodge.EnterLabel";
        protected override string EnterLabelDefault => "Visit the Witch Hunter Lodge";

        protected override TextObject HideOnUndiscovered => WitchHunterLodgeReasons.Undiscovered;
        protected override TextObject HideOnSealed => WitchHunterLodgeReasons.Sealed;
    }
}
