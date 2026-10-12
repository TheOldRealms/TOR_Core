using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TOR_Core.CharacterDevelopment;
using TOR_Core.Extensions;

namespace TOR_Core.CampaignMechanics.SettlementPOIs
{
    public class SpellsingersGrovePOI : SettlementPOI, ISpellTrainerHost, IEnchanterHost
    {
        public override string LocationId => "tor_spellsingers_grove";
        public override string ParentMenuId => "town_artisan";
        public override string AnchorEntryId => "town_artisan_leave";
        public override bool AnchorAbove => true;

        public override string EnterLabelId => "tor_poi_spellsingers_grove.EnterLabel";
        public override string EnterLabelDefault => "Visit the Spellsingers' Grove";

        public string SpellTrainerTemplateId => "tor_spelltrainer_woodelves_0";
        public string EnchanterTemplateId    => "tor_spelltrainer_woodelves_0";
        public List<string> EnchantmentSuffixes => new() { "we_enchant", "asrai_enchant" };

        public override bool CheckAccess(Settlement s, Hero h, out TextObject denialReason)
        {
            if (!IsDiscovered(s)) { denialReason = R("Undiscovered", "You have not yet found this place."); return false; }
            if (!AppliesTo(s))    { denialReason = R("Sealed", "This place is sealed."); return false; }
            if (s.IsUnderSiege)   { denialReason = R("SiegeSealed", "The grove's wards close during the siege."); return false; }

            var race = h.CharacterObject.Race;
            if (race != FaceGen.GetRaceOrDefault("elf") && race != FaceGen.GetRaceOrDefault("orion"))
            {
                denialReason = R("Rejected", "The grove does not open for your kind.");
                return false;
            }
            if (!h.HasCareer(TORCareers.Spellsinger) && !h.HasCareer(TORCareers.Waywatcher))
            {
                denialReason = R("MustHaveCareer", "Only the children of Ariel may enter the grove.");
                return false;
            }
            denialReason = null;
            return true;
        }
    }
}
