using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace TOR_Core.CampaignMechanics.SettlementPOIs
{
    /// <summary>
    /// Witch Hunter Lodge POI — the Order of Sigmar chapterhouses. Reachable from the
    /// <c>town_artisan</c> submenu in seven Empire towns (see
    /// <see cref="SettlementPOILocations.IsWitchHunterLodgeSettlement"/>). Race-locked to humans;
    /// no career gate; no NPC trainer (empty scene).
    /// </summary>
    public class WitchHunterLodgePOI : SettlementPOI
    {
        public override string LocationId => SettlementPOILocations.WitchHunterLodgeLocationId;
        public override string ParentMenuId => "town_artisan";
        public override string AnchorEntryId => "town_artisan_leave";
        public override bool AnchorAbove => true;    // sit directly above "Leave"

        public override string EnterLabelId => "tor_poi_witchhunter_lodge.EnterLabel";
        public override string EnterLabelDefault => "Visit the Witch Hunter Lodge";

        public override bool CheckAccess(Settlement s, Hero h, out TextObject denialReason)
        {
            if (!IsDiscovered(s))
            {
                denialReason = R("Undiscovered", "You have not yet found this place.");
                return false;
            }
            if (!SettlementPOILocations.IsWitchHunterLodgeSettlement(s))
            {
                denialReason = R("Sealed", "This place is sealed.");
                return false;
            }
            if (s.IsUnderSiege)
            {
                denialReason = R("SiegeSealed", "The lodge is closed during the siege.");
                return false;
            }
            // Race lock — humans only. No career gate (the lodge welcomes any human visitor).
            if (h.CharacterObject.Race != FaceGen.GetRaceOrDefault("human"))
            {
                denialReason = R("Rejected", "The templars turn you away.");
                return false;
            }

            denialReason = null;
            return true;
        }
    }
}
