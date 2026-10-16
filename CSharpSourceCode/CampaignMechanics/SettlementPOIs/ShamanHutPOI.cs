using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Localization;
using TOR_Core.Utilities;

namespace TOR_Core.CampaignMechanics.SettlementPOIs
{
    public class ShamanHutPOI : SettlementPOI, ISpellTrainerHost, IEnchanterHost
    {
        public override string LocationId => "tor_shaman_hut";
        public override string ParentMenuId => "town_artisan";
        public override string AnchorEntryId => "town_artisan_leave";
        public override bool AnchorAbove => true;

        public override string EnterLabelId => "tor_poi_shaman_hut.EnterLabel";
        public override string EnterLabelDefault => "Visit the Shaman Hut";

        public string SpellTrainerTemplateId => "tor_spelltrainer_greenskins_0";
        public string EnchanterTemplateId    => "tor_spelltrainer_greenskins_0";
        public List<string> EnchantmentSuffixes => new() { "gs_enchant" };

        public override bool CheckAccess(Settlement s, Hero h, out TextObject denialReason)
        {
            if (!IsDiscovered(s)) { denialReason = R("Undiscovered", "You have not yet found this place."); return false; }
            if (!AppliesTo(s))    { denialReason = R("Sealed", "This place is sealed."); return false; }
            if (s.IsUnderSiege)   { denialReason = R("SiegeSealed", "The shaman da hut iz closed. Bosses fightin'!"); return false; }

            if (h.Culture.StringId != TORConstants.Cultures.GREENSKIN)
            {
                denialReason = R("WrongCulture", "Da shaman don't see yer kind.");
                return false;
            }
            denialReason = null;
            return true;
        }
    }
}
