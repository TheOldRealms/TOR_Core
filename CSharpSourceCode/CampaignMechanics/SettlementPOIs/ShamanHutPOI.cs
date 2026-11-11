using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TOR_Core.CampaignMechanics.Crafting;
using TOR_Core.CampaignMechanics.SpellTrainers;
using TOR_Core.CharacterDevelopment;
using TOR_Core.Extensions;
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

        public bool IsPartyEligibleForTrainer()
        {
            if (Hero.MainHero.Culture.StringId == TORConstants.Cultures.GREENSKIN) return true;
            return SpellTrainerHelper.HasMatchingSpellTrainerCompanion(
                x => x.Culture.StringId == TORConstants.Cultures.GREENSKIN && x.IsSpellCaster());
        }

        // Same hero plays both roles via template-id dedup, so eligibility is identical.
        public bool IsPartyEligibleForEnchanter() => IsPartyEligibleForTrainer();

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

        public override void RegisterDialogs(CampaignGameStarter obj)
        {
            obj.AddDialogLine("trainer_greenskin_start", "start", "choices_greenskin",
                TORTextHelper.GetText("tor_spelltrainer_greenskin_start", "Whoo'z dere? Wot you want?"),
                isGreenskinTrainer, null, 210, null);
            obj.AddDialogLine("trainer_greenskin_hub", "hub_greenskin", "choices_greenskin",
                TORTextHelper.GetText("tor_spelltrainer_greenskin_hub", "Wot else ya need? Speak up!"),
                isGreenskinTrainer, null, 200, null);

            obj.AddPlayerLine("trainer_greenskin_learnspells", "choices_greenskin", "openbook_greenskin",
                TORTextHelper.GetText("tor_spelltrainer_greenskin_open_book", "I'z here fer sum ju-ju spooks."),
                () => MobileParty.MainParty.HasSpellCasterMember() && greenskinCondition(), null, 200, null);
            obj.AddDialogLine("trainer_greenskin_afterlearnspells", "openbook_greenskin", "hub_greenskin",
                TORTextHelper.GetText("tor_spelltrainer_greenskin_close_book", "Eat dis shroom an' dance wiv me."),
                null, SpellTrainerHelper.OpenbookConsequence, 200, null);

            obj.AddPlayerLine("trainer_greenskin_playergoodbye", "choices_greenskin", "saygoodbye_greenskin",
                TORTextHelper.GetText("tor_spelltrainer_greenskin_player_goodbye", "I'z outta 'ere."),
                null, null, 200, null);
            obj.AddDialogLine("trainer_greenskin_goodbye", "saygoodbye_greenskin", "close_window",
                TORTextHelper.GetText("tor_spelltrainer_greenskin_goodbye", "I'z can feel da Gods talkin'. Get outta 'ere!"),
                isGreenskinTrainer, null, 200, null);

            // Enchanter branch — same hero as the trainer via template-id dedup,
            // attached to the Greenskin hub.
            EnchanterHelper.RegisterEnchanterBranch(obj,
                culture: TORConstants.Cultures.GREENSKIN,
                hubToken: "choices_greenskin",
                quitToken: "hub_greenskin",
                isMultiUse: true,
                enchantmentSuffixes: EnchantmentSuffixes,
                cultureAccess: IsPartyEligibleForEnchanter);

            bool isGreenskinTrainer()
            {
                if (!SpellTrainerHelper.SpellTrainerStartCondition()) return false;
                var partner = CharacterObject.OneToOneConversationCharacter;
                return partner.HeroObject != null && partner.HeroObject.Template.StringId == SpellTrainerHelper.GreenskinTrainerId;
            }

            bool greenskinCondition()
            {
                if (Hero.MainHero.HasCareer(TORCareers.OrcShaman)) return true;
                if (Hero.MainHero.PartyBelongedTo == null) return false;
                return Hero.MainHero.PartyBelongedTo.GetMemberHeroes()
                    .Any(x => x.Culture.StringId == TORConstants.Cultures.GREENSKIN && x.IsSpellCaster());
            }
        }
    }
}
