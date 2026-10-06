using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TOR_Core.CharacterDevelopment;
using TOR_Core.Extensions;

namespace TOR_Core.CampaignMechanics.SettlementPOIs
{
    /// <summary>
    /// Wizard Hall POI — the Imperial Colleges of Magic. Reachable from the
    /// <c>town_artisan</c> submenu in Altdorf, Nuln, Middenheim. Human-race +
    /// Imperial-Magister-career gated.
    /// </summary>
    public class WizardHallPOI : SettlementPOI
    {
        public override string LocationId => "tor_wizardhall";
        public override string ParentMenuId => "town_artisan";
        public override string AnchorEntryId => "town_artisan_enchanting";
        public override bool AnchorAbove => false;   // sit right after "Visit the enchanter"

        public override string EnterLabelId => "tor_poi_wizardhall.EnterLabel";
        public override string EnterLabelDefault => "Visit the Wizard Hall";

        /// <summary>
        /// Where should the Imperial Magister trainer be placed in <paramref name="s"/>?
        /// The Wizard Hall POI's Location if the settlement hosts it, else <c>house_1</c>
        /// (vanilla pre-POI location that TOR trainer behaviors fall back to). Called from
        /// <see cref="TOR_Core.CampaignMechanics.Crafting.EnchanterTownBehavior"/> and
        /// <see cref="TOR_Core.CampaignMechanics.SpellTrainers.SpellTrainerInTownBehavior"/>.
        /// Lives here (not on <see cref="SettlementPOI"/>) because trainer placement is a
        /// Wizard-Hall-specific concern; a future Runehall / Engineer Hall will supply its
        /// own equivalent helper for its own trainer behaviors.
        /// </summary>
        public static string GetTrainerLocationId(Settlement s)
        {
            var poi = SettlementPOIManagerCampaignBehavior.Get("tor_wizardhall") as WizardHallPOI;
            return poi != null && poi.AppliesTo(s) ? poi.LocationId : "house_1";
        }

        public override bool CheckAccess(Settlement s, Hero h, out TextObject denialReason)
        {
            // Order: cheapest-and-broadest first — discovery → applies → siege → race → career.
            if (!IsDiscovered(s))
            {
                denialReason = R("Undiscovered", "You have not yet found this place.");
                return false;
            }
            if (!AppliesTo(s))
            {
                denialReason = R("Sealed", "This place is sealed.");
                return false;
            }
            if (s.IsUnderSiege)
            {
                denialReason = R("SiegeSealed", "The gates of the College are sealed during the siege.");
                return false;
            }
            if (h.CharacterObject.Race != FaceGen.GetRaceOrDefault("human"))
            {
                denialReason = R("Rejected", "The wards of the College turn you away.");
                return false;
            }
            if (!h.HasCareer(TORCareers.ImperialMagister))
            {
                denialReason = R("MustHaveCareer", "Only {REQUIRED_CAREER} may enter the College.")
                    .SetTextVariable("REQUIRED_CAREER", TORCareers.ImperialMagister.Name);
                return false;
            }

            denialReason = null;
            return true;
        }
    }
}
