using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Localization;
using TOR_Core.Utilities;

namespace TOR_Core.CampaignMechanics.SettlementPOIs
{
    /// <summary>
    /// Dwarf Engineers' Hall. Reuses the <c>tor_dawi_engineers_guild_npc_0</c> template
    /// so the existing OathGoldBehavior Engineer-guild dialog tree fires on contact
    /// without forking. Known issue (same as RunehallPOI): OathGoldBehavior also places
    /// that template at <c>house_2</c>, so engineer-hall-equipped towns show the NPC
    /// at two locations. Parked for a follow-up.
    /// </summary>
    public class DwarfEngineerHallPOI : SettlementPOI, ISpellTrainerHost
    {
        public override string LocationId => "tor_dwarf_engineer_hall";
        public override string ParentMenuId => "town_artisan";
        public override string AnchorEntryId => "town_artisan_leave";
        public override bool AnchorAbove => true;

        public override string EnterLabelId => "tor_poi_dwarf_engineer_hall.EnterLabel";
        public override string EnterLabelDefault => "Visit the Engineers' Hall";

        public string SpellTrainerTemplateId => "tor_dawi_engineers_guild_npc_0";

        // Dwarves have no spellcasters, so no companion fallback — main-hero culture only.
        public bool IsPartyEligibleForTrainer() =>
            Hero.MainHero.Culture.StringId == TORConstants.Cultures.DAWI;

        public override bool CheckAccess(Settlement s, Hero h, out TextObject denialReason)
        {
            if (!IsDiscovered(s)) { denialReason = R("Undiscovered", "You have not yet found this place."); return false; }
            if (!AppliesTo(s))    { denialReason = R("Sealed", "This place is sealed."); return false; }
            if (s.IsUnderSiege)   { denialReason = R("SiegeSealed", "The hold's gates are shut while under siege."); return false; }

            if (h.Culture.StringId != TORConstants.Cultures.DAWI)
            {
                denialReason = R("WrongCulture", "The Engineers' Hall is open to the dawi alone.");
                return false;
            }
            denialReason = null;
            return true;
        }
    }
}
