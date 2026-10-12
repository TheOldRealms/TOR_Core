using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TOR_Core.CharacterDevelopment;
using TOR_Core.Extensions;

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

            var race = h.CharacterObject.Race;
            if (race != FaceGen.GetRaceOrDefault("vampire") && race != FaceGen.GetRaceOrDefault("necrarch"))
            {
                denialReason = R("Rejected", "The dead within turn you away.");
                return false;
            }
            if (!h.HasCareer(TORCareers.Necrarch) && !h.HasCareer(TORCareers.MinorVampire))
            {
                denialReason = R("MustHaveCareer", "Only the undying may enter the crypt.");
                return false;
            }
            denialReason = null;
            return true;
        }
    }
}
