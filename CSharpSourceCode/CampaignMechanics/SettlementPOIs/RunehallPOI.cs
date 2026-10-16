using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Localization;
using TOR_Core.Utilities;

namespace TOR_Core.CampaignMechanics.SettlementPOIs
{
    /// <summary>
    /// Dwarf Runehall. Reuses the <c>tor_dawi_runelord_trainer_0</c> template so the
    /// existing OathGoldBehavior Runesmith-guild dialog tree fires on contact without
    /// forking the (sizeable) dialog. Known issue: OathGoldBehavior also places this
    /// template at <c>house_1</c>, so Runehall-equipped towns will show the Runesmith
    /// at two locations. Parked for a follow-up — makes dialog work today.
    /// </summary>
    public class RunehallPOI : SettlementPOI, ISpellTrainerHost
    {
        public override string LocationId => "tor_runehall";
        public override string ParentMenuId => "town_artisan";
        public override string AnchorEntryId => "town_artisan_leave";
        public override bool AnchorAbove => true;

        public override string EnterLabelId => "tor_poi_runehall.EnterLabel";
        public override string EnterLabelDefault => "Visit the Runehall";

        public string SpellTrainerTemplateId => "tor_dawi_runelord_trainer_0";

        public override bool CheckAccess(Settlement s, Hero h, out TextObject denialReason)
        {
            if (!IsDiscovered(s)) { denialReason = R("Undiscovered", "You have not yet found this place."); return false; }
            if (!AppliesTo(s))    { denialReason = R("Sealed", "This place is sealed."); return false; }
            if (s.IsUnderSiege)   { denialReason = R("SiegeSealed", "The hold's gates are shut while under siege."); return false; }

            if (h.Culture.StringId != TORConstants.Cultures.DAWI)
            {
                denialReason = R("WrongCulture", "The Runehall is open to the dawi alone.");
                return false;
            }
            denialReason = null;
            return true;
        }
    }
}
