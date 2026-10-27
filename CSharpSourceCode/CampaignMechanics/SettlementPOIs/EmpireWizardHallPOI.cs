using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TOR_Core.CampaignMechanics.Crafting;
using TOR_Core.CampaignMechanics.SpellTrainers;
using TOR_Core.Extensions;
using TOR_Core.Utilities;

namespace TOR_Core.CampaignMechanics.SettlementPOIs
{
    public class EmpireWizardHallPOI : SettlementPOI, ISpellTrainerHost, IEnchanterHost, ISkillTrainerHost
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

        // Two distinct heroes: the magister trainer and a separate enchanter.
        public override void EnsurePlaced(Settlement s)
        {
            PlaceResident(s, SpellTrainerTemplateId);
            PlaceResident(s, EnchanterTemplateId);
        }

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
            // Any Imperial — plus Bretonnian spellcasters (damsels / prophetesses visiting
            // the Colleges in a Lady-blessed capacity). Everyone else is denied.
            bool allowed = h.Culture.StringId == TORConstants.Cultures.EMPIRE
                        || (h.Culture.StringId == TORConstants.Cultures.BRETONNIA && h.IsSpellCaster());
            if (!allowed)
            {
                denialReason = R("WrongCulture", "The College of Magic is open to those of the Empire — and the Lady's blessed.");
                return false;
            }

            denialReason = null;
            return true;
        }

        public override void RegisterDialogs(CampaignGameStarter starter)
        {
            // Empire's enchanter is a separate hero from the spelltrainer and gets its
            // own standalone dialog (isMultiUse=false → attaches to the generic "start"
            // hub). The spelltrainer side runs through SpellTrainerHelper.RegisterCommonDialogs.
            EnchanterHelper.RegisterEnchanterBranch(starter,
                culture: TORConstants.Cultures.EMPIRE,
                hubToken: "start",
                quitToken: "close_window",
                isMultiUse: false,
                enchantmentSuffixes: EnchantmentSuffixes,
                cultureAccess: IsPartyEligibleForEnchanter);
        }

        // Companion Spellcraft training plugs into the shared spelltrainer hub
        // ("choices"/"start") registered by SpellTrainerHelper.RegisterCommonDialogs.
        // TrainerCondition filters by partner.Template so this line only shows
        // when the player is talking to the Empire Magister specifically.
        public IEnumerable<SkillTrainerConfig> GetSkillTrainers()
        {
            yield return new SkillTrainerConfig(
                templateId:   SpellTrainerTemplateId,
                dialogSuffix: "EmpireMagister",
                skillId:      "Spellcraft",
                hubState:     "choices",
                reintroState: "start",
                restrictions: new[] { "SpellCaster" });
        }
    }
}
