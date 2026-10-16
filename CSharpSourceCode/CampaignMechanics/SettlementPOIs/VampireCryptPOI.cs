using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Localization;
using TOR_Core.Utilities;

namespace TOR_Core.CampaignMechanics.SettlementPOIs
{
    public class VampireCryptPOI : SettlementPOI, ISpellTrainerHost, IEnchanterHost
    {
        public override string LocationId => "tor_vampire_crypt";
        public override string ParentMenuId => "town_artisan";
        public override string AnchorEntryId => "town_artisan_leave";
        public override bool AnchorAbove => true;

        public override string EnterLabelId => "tor_poi_vampire_crypt.EnterLabel";
        public override string EnterLabelDefault => "Visit the Vampire Crypt";

        public string SpellTrainerTemplateId => "tor_spelltrainer_vc_0";
        public string EnchanterTemplateId    => "tor_spelltrainer_vc_0";
        public List<string> EnchantmentSuffixes => new() { "vc_enchant" };

        public override bool CheckAccess(Settlement s, Hero h, out TextObject denialReason)
        {
            if (!IsDiscovered(s)) { denialReason = R("Undiscovered", "You have not yet found this place."); return false; }
            if (!AppliesTo(s))    { denialReason = R("Sealed", "This place is sealed."); return false; }
            if (s.IsUnderSiege)   { denialReason = R("SiegeSealed", "The crypt is sealed during the siege."); return false; }

            var c = h.Culture.StringId;
            if (c != TORConstants.Cultures.SYLVANIA && c != TORConstants.Cultures.MOUSILLON)
            {
                denialReason = R("WrongCulture", "The crypt opens only to those of Sylvania or Mousillon.");
                return false;
            }
            denialReason = null;
            return true;
        }
    }
}
