using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Localization;
using TOR_Core.Extensions;
using TOR_Core.Utilities;

namespace TOR_Core.CampaignMechanics.SettlementPOIs
{
    /// <summary>
    /// Spike: first POI attached to a non-town settlement (Grove of the Lady shrine).
    /// Validates that <c>PlayerEncounter.EnterSettlement()</c> can prime
    /// <c>LocationEncounter</c> on shrines so our existing scene-launch path works.
    /// </summary>
    public class BretonniaForestTrainerShrinePOI : SettlementPOI, ISpellTrainerHost, IEnchanterHost
    {
        public override string LocationId => "tor_bretonnia_forest_trainer_shrine";
        public override string ParentMenuId => "shrine_menu";
        public override string AnchorEntryId => "leave";
        public override bool AnchorAbove => true;

        public override string EnterLabelId => "tor_poi_bretonnia_forest_trainer_shrine.EnterLabel";
        public override string EnterLabelDefault => "Approach the shrine...";

        // Shrines don't get a vanilla LocationEncounter — launch the scene directly.
        public override bool UsesMissionLaunch => true;

        public string SpellTrainerTemplateId => "tor_spelltrainer_bretonnia_0";
        public string EnchanterTemplateId    => "tor_spelltrainer_bretonnia_0";
        public List<string> EnchantmentSuffixes => new() { "br_enchant" };

        public override bool CheckAccess(Settlement s, Hero h, out TextObject denialReason)
        {
            if (!IsDiscovered(s)) { denialReason = R("Undiscovered", "You have not yet found this place."); return false; }
            if (!AppliesTo(s))    { denialReason = R("Sealed", "This place is sealed."); return false; }
            if (s.IsUnderSiege)   { denialReason = R("SiegeSealed", "The grove's wards close during the siege."); return false; }

            if (h.Culture.StringId != TORConstants.Cultures.BRETONNIA)
            {
                denialReason = R("WrongCulture", "The Lady's shrine is a sacred place of Bretonnia.");
                return false;
            }
            if (!h.IsSpellCaster())
            {
                denialReason = R("NotCaster", "Only those gifted with magic may approach the Lady.");
                return false;
            }
            denialReason = null;
            return true;
        }
    }
}
