using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TOR_Core.CampaignMechanics.SpellTrainers;
using TOR_Core.CharacterDevelopment;
using TOR_Core.Extensions;
using TOR_Core.Utilities;

namespace TOR_Core.CampaignMechanics.SettlementPOIs
{
    public class EmpireWizardHallPOI : SettlementPOI, ISpellTrainerHost, IEnchanterHost
    {
        public override string LocationId => "tor_wizardhall";
        public override string ParentMenuId => "town_artisan";
        public override string AnchorEntryId => "town_artisan_enchanting";
        public override bool AnchorAbove => false;

        public override string EnterLabelId => "tor_poi_wizardhall.EnterLabel";
        public override string EnterLabelDefault => "Visit the Empire Wizard Hall";

        public string SpellTrainerTemplateId => "tor_spelltrainer_empire_0";
        public string EnchanterTemplateId    => "tor_enchanter_empire_0";
        public List<string> EnchantmentSuffixes => new() { "emp_enchant" };

        public bool IsPartyEligibleForTrainer()
        {
            if (Hero.MainHero.Culture.StringId == TORConstants.Cultures.EMPIRE) return true;
            return SpellTrainerHelper.HasMatchingSpellTrainerCompanion(
                x => x.Culture.StringId == TORConstants.Cultures.EMPIRE && x.IsSpellCaster());
        }

        // Enchanter is a separate hero from the spelltrainer here, but the party-access
        // rule is the same (Empire culture or Empire spellcaster companion).
        public bool IsPartyEligibleForEnchanter() => IsPartyEligibleForTrainer();

        public override string MapIconText => "<img src=\"winds_icon_45\" extend=\"15\">";

        public override bool CheckAccess(Settlement s, Hero h, out TextObject denialReason)
        {
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
