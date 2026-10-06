using TaleWorlds.Localization;

namespace TOR_Core.CampaignMechanics.TownPOIs
{
    /// <summary>
    /// Wizard Hall POI — the Imperial Colleges of Magic. Reachable from
    /// <c>town_artisan</c> submenu in Altdorf, Nuln, Middenheim. Human-race +
    /// Imperial-Magister-career gated (access rules enforced by
    /// <see cref="TOR_Core.Models.TORSettlementAccessModel.CanAccessWizardHall"/>).
    /// </summary>
    public class WizardHallPOI : TownPOIBehavior
    {
        protected override string LocationId => TownPOILocations.WizardHallLocationId;
        protected override string ParentMenuId => "town_artisan";
        protected override string AnchorEntryId => "town_artisan_enchanting";
        protected override bool AnchorAbove => false;   // sit right after "Visit the enchanter"

        protected override string EnterLabelId => "tor_poi_wizardhall.EnterLabel";
        protected override string EnterLabelDefault => "Visit the Wizard Hall";

        protected override TextObject HideOnUndiscovered => WizardHallReasons.Undiscovered;
        protected override TextObject HideOnSealed => WizardHallReasons.Sealed;
    }
}
