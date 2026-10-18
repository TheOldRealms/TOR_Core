using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.LinQuick;
using TaleWorlds.Localization;
using TOR_Core.AbilitySystem.Spells;
using TOR_Core.CampaignMechanics.Crafting;
using TOR_Core.CampaignMechanics.SpellTrainers;
using TOR_Core.CharacterDevelopment;
using TOR_Core.CharacterDevelopment.CareerSystem;
using TOR_Core.Extensions;
using TOR_Core.Utilities;
using static TOR_Core.Utilities.TORConstants;

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

        // Same template fills both roles; template-id dedup in the manager keeps it one hero.
        public override void EnsurePlaced(Settlement s) => PlaceResident(s, SpellTrainerTemplateId);

        public bool IsPartyEligibleForTrainer()
        {
            if (Hero.MainHero.HasCareer(TORCareers.Spellsinger)) return true;
            if (Hero.MainHero.HasCareer(TORCareers.Warden)) return true;
            if (Hero.MainHero.Culture.StringId == TORConstants.Cultures.ASRAI &&
                Hero.MainHero.IsSpellCaster() &&
                Hero.MainHero.GetKnownLoreCount() > 0)
            {
                return true;
            }
            return SpellTrainerHelper.HasMatchingSpellTrainerCompanion(
                x => x.Culture.StringId == TORConstants.Cultures.ASRAI && x.IsSpellCaster());
        }

        // Same hero plays both roles via template-id dedup, so eligibility is identical.
        // Note the legacy EnchanterTownBehavior rule was slightly tighter (no career-only
        // paths); we unify on the trainer rule here. Audit if enchanter-specific access
        // ever needs to differ.
        public bool IsPartyEligibleForEnchanter() => IsPartyEligibleForTrainer();

        public override bool CheckAccess(Settlement s, Hero h, out TextObject denialReason)
        {
            if (!IsDiscovered(s)) { denialReason = R("Undiscovered", "You have not yet found this place."); return false; }
            if (!AppliesTo(s))    { denialReason = R("Sealed", "This place is sealed."); return false; }
            if (s.IsUnderSiege)   { denialReason = R("SiegeSealed", "The grove's wards close during the siege."); return false; }

            var c = h.Culture.StringId;
            if (c != TORConstants.Cultures.ASRAI && c != TORConstants.Cultures.EONIR)
            {
                denialReason = R("WrongCulture", "The grove opens only to the elves of Athel Loren and Laurelorn.");
                return false;
            }
            denialReason = null;
            return true;
        }

        public override void RegisterDialogs(CampaignGameStarter obj)
        {
            var spellweaverLores = LoreObject.GetAll().Where(x => x.StringId == "HighMagic" || x.StringId == "DarkMagic").ToList();

            obj.AddDialogLine("trainer_spellsinger_start", "start", "choices_spellsinger", TORTextHelper.GetTextForNative("tor_spelltrainer_spellsinger_start", "I welcome you, child of Athel Loren."), isSpellsingerTrainer, null, 200, null);
            obj.AddDialogLine("trainer_spellsinger_start", "hub_spellsinger", "choices_spellsinger", TORTextHelper.GetTextForNative("tor_spelltrainer_spellsinger_reintro", "Is there more you seek? Speak your desires."), isSpellsingerTrainer, null, 200, null);

            obj.AddPlayerLine("trainer_spellsinger_learnmagic", "choices_spellsinger", "trainer_spellsinger_learnmagic_answer", TORTextHelper.GetTextForNative("tor_spelltrainer_spellsinger_learnmagic", "Greetings wise Spellsinger. I am seeking the capabilities of performing magic."), () => Hero.MainHero.HasCareer(TORCareers.Warden) && !Hero.MainHero.IsSpellCaster(), null, 200, null);
            obj.AddPlayerLine("trainer_spellsinger_learnmagic_second_lore", "choices_spellsinger", "trainer_spellsinger_learnmagic_answer_secondlore", TORTextHelper.GetTextForNative("tor_spelltrainer_spellsinger_learn_magic_warden_second_lore", "I want to enhance my magic capabilities."), () => Hero.MainHero.HasCareer(TORCareers.Warden) &&
                Hero.MainHero.HasUnlockedCareerChoiceTier(3) && Hero.MainHero.GetSkillValue(TORSkills.Spellcraft) > 200, null, 200, null);
            obj.AddDialogLine("trainer_spellsinger_learnmagic_answer_secondlore", "trainer_spellsinger_learnmagic_answer_secondlore", "trainer_spellsinger_learnmagic_secondlore__answer_player", TORTextHelper.GetTextForNative("tor_spelltrainer_spellsinger_learn_magic_warden_second_lore_answer", "You truly choose the path of Ariel. But the forest demands another tribute. Pay it and I will teach you more"), isSpellsingerTrainer, null, 200, null);

            obj.AddPlayerLine("trainer_spellsinger_learnmagic_answer_player_secondlore_agree", "trainer_spellsinger_learnmagic_secondlore__answer_player", "hub_spellsinger", TORTextHelper.GetTextForNative("tor_spelltrainer_spellsinger_learn_magic_warden_agree", "Yes I am."), null, learnMagicWardenSecondLore, 200, null);
            obj.AddPlayerLine("trainer_spellsinger_learnmagic_answer_player_secondlore_decline", "trainer_spellsinger_learnmagic_secondlore__answer_player", "hub_spellsinger", TORTextHelper.GetTextForNative("tor_spelltrainer_spellsinger_learn_magic_warden_decline", "I have to think about this."), null, null, 200, null);

            obj.AddDialogLine("trainer_spellsinger_learnmagic_answer", "trainer_spellsinger_learnmagic_answer", "trainer_spellsinger_learnmagic_answer_player", TORTextHelper.GetTextForNative("tor_spelltrainer_spellsinger_learn_magic_warden_answer", "Hm. You are not a spellsinger, yet I see your potential. If you are willing to show me how much you are bound to the forest. I am willing to teach you the first steps of the path of Ariel"), isSpellsingerTrainer, null, 200, null);
            obj.AddPlayerLine("trainer_spellsinger_learnmagic_answer_player_agree", "trainer_spellsinger_learnmagic_answer_player", "hub_spellsinger", TORTextHelper.GetTextForNative("tor_spelltrainer_spellsinger_learn_magic_warden_agree", "Yes I am."), null, learnMagicWarden, 200, null);
            obj.AddPlayerLine("trainer_spellsinger_learnmagic_answer_player_decline", "trainer_spellsinger_learnmagic_answer_player", "hub_spellsinger", TORTextHelper.GetTextForNative("tor_spelltrainer_spellsinger_learn_magic_warden_decline", "I have to think about this."), null, null, 200, null);

            obj.AddPlayerLine("trainer_spellsinger_learnspells", "choices_spellsinger", "openbook_spellsinger", TORTextHelper.GetTextForNative("tor_spelltrainer_spellsinger_open_book", "I seek further knowledge of Athel Loren's Magic."), () => MobileParty.MainParty.HasSpellCasterMember() && spellsingerCondition(), null, 200, null);
            obj.AddPlayerLine("trainer_spellsinger_spellweaver", "choices_spellsinger", "spellweaver_choice_dialog", TORTextHelper.GetTextForNative("tor_spelltrainer_spellweaver", "I want to become a spellweaver."), () => Hero.MainHero.HasCareer(TORCareers.Spellsinger) && spellsingerCondition() && SpellweaverCondition(), null, 200, null);
            obj.AddPlayerLine("trainer_spellsinger_spellweaver", "choices_spellsinger", "spellweaver_companion_choice_dialog",
                TORTextHelper.GetText("tor_spelltrainer_spellweaver_companion", "My companion is ready to become a spellweaver."),
                () => MobileParty.MainParty.HasSpellCasterMember() && spellsingerCondition() && MobileParty.MainParty.GetMemberHeroes()
                          .AnyQ(x => x.IsSpellCaster() && x.CharacterObject.IsElf() && x != Hero.MainHero && !(x.HasKnownLore("DarkMagic") || x.HasKnownLore("HighMagic"))) &&
                Hero.MainHero.HasUnlockedCareerChoiceTier(3), null, 200, null);

            obj.AddPlayerLine("trainer_spellsinger_learnlore", "choices_spellsinger", "choices_spellsinger", TORTextHelper.GetTextForNative("tor_spelltrainer_spellsinger_learnlore", "Teach me one of Ariel's many pathways."), () => MobileParty.MainParty.HasSpellCasterMember() && spellsingerCondition() && SpellsingerAdditonalLoreCondition(), AdditionalLoresPrompt, 200, null);

            obj.AddDialogLine("trainer_spellsinger_weaver", "spellweaver_choice_dialog", "spellweaver_choice_player", TORTextHelper.GetTextForNative("tor_spelltrainer_spellsinger_weaver_choice", "A spellsinger can pick either the pathway of the Darkweaver or the one of the Highweaver. Choose wisely."), isSpellsingerTrainer, null, 200, null);
            obj.AddPlayerLine("spellweaver_choice_player", "spellweaver_choice_player", "choices_spellsinger", TORTextHelper.GetTextForNative("tor_spelltrainer_spellweaver_choice", "Let me choose."), () => MobileParty.MainParty.HasSpellCasterMember() && spellsingerCondition(), spellweaverPrompt, 200, null);

            obj.AddDialogLine("spellweaver_companion_choice_dialog", "spellweaver_companion_choice_dialog", "spellweaver_choice_lores_companion", TORTextHelper.GetTextForNative("tor_spelltrainer_spellsinger_companion_choice", "A Highweaver, or Darkweaver... a tough choice"), isSpellsingerTrainer, null, 200, null);
            obj.AddPlayerLine("spellweaver_choice_lores_companion", "spellweaver_choice_lores_companion", "choices_spellsinger", TORTextHelper.GetTextForNative("tor_spelltrainer_spellsinger_companion_choose", "Let me choose."), () => MobileParty.MainParty.HasSpellCasterMember() && spellsingerCondition(), spellweaverCompanionPrompt, 200, null);

            obj.AddDialogLine("trainer_spellsinger_weaver", "spellweaver_choice_dialog", "close_window", TORTextHelper.GetTextForNative("tor_spelltrainer_spellsinger_goodbye", "May She guide you on all your paths through her garden."), isSpellsingerTrainer, null, 200, null);

            obj.AddPlayerLine("trainer_spellsinger_playergoodbye", "choices_spellsinger", "saygoodbye", TORTextHelper.GetTextForNative("tor_spelltrainer_spellsinger_player_goodbye", "Ariel be with you."), null, null, 200, null);
            obj.AddDialogLine("trainer_spellsinger_goodbye", "saygoodbye", "close_window", TORTextHelper.GetTextForNative("tor_spelltrainer_spellsinger_goodbye", "May She guide you on all your paths through her garden."), isSpellsingerTrainer, null, 200, null);
            obj.AddDialogLine("trainer_spellsinger_afterlearnspells", "openbook_spellsinger", "hub_spellsinger", TORTextHelper.GetTextForNative("tor_spelltrainer_spellsinger_close_book", "A new facet of Ariel's infinite knowledge."), null, SpellTrainerHelper.OpenbookConsequence, 200, null);

            // Enchanter branch — same hero as the trainer via template-id dedup,
            // attached to the Spellsinger hub.
            EnchanterHelper.RegisterEnchanterBranch(obj,
                culture: TORConstants.Cultures.ASRAI,
                hubToken: "choices_spellsinger",
                quitToken: "hub_spellsinger",
                isMultiUse: true,
                enchantmentSuffixes: EnchantmentSuffixes,
                cultureAccess: IsPartyEligibleForEnchanter);

            bool isSpellsingerTrainer()
            {
                if (!SpellTrainerHelper.SpellTrainerStartCondition()) return false;
                var partner = CharacterObject.OneToOneConversationCharacter;
                return partner.HeroObject != null && partner.HeroObject.Template.StringId == SpellTrainerHelper.WoodelfTrainerId;
            }

            bool spellsingerCondition()
            {
                if (Hero.MainHero.HasCareer(TORCareers.Spellsinger)) return true;
                if (Hero.MainHero.Culture.StringId == TORConstants.Cultures.ASRAI &&
                    Hero.MainHero.IsSpellCaster() &&
                    Hero.MainHero.GetKnownLoreCount() > 0) return true;
                if (Hero.MainHero.PartyBelongedTo != null &&
                    Hero.MainHero.PartyBelongedTo.GetMemberHeroes().Any(x => x.Culture.StringId == TORConstants.Cultures.ASRAI && x.IsSpellCaster())) return true;
                return false;
            }

            void learnMagicWarden()
            {
                Hero.MainHero.AddKnownLore("LoreOfLife");
                Hero.MainHero.AddCultureSpecificCustomResource(-2500);
                Hero.MainHero.AddAttribute(CharacterAttributes.SPELLCASTER);
                Hero.MainHero.SetSpellCastingLevel(SpellCastingLevel.Entry);
                if (Hero.MainHero.GetSkillValue(TORSkills.Spellcraft) < 25)
                {
                    Hero.MainHero.SetSkillValue(TORSkills.Spellcraft, 25);
                }
            }

            void learnMagicWardenSecondLore()
            {
                Hero.MainHero.AddKnownLore("LoreOfBeasts");
                Hero.MainHero.AddCultureSpecificCustomResource(-2500);
            }

            bool SpellsingerAdditonalLoreCondition()
            {
                if (!Hero.MainHero.HasCareer(TORCareers.Spellsinger)) return false;
                var additionalLoreCount = Hero.MainHero.GetKnownLoreCount();
                if (Hero.MainHero.HasKnownLore("HighMagic")) additionalLoreCount--;
                if (Hero.MainHero.HasKnownLore("DarkMagic")) additionalLoreCount--;
                if (Hero.MainHero.HasKnownLore("LoreOfLife")) additionalLoreCount--;
                if (Hero.MainHero.HasKnownLore("LoreOfBeasts")) additionalLoreCount--;
                if (Hero.MainHero.HasUnlockedCareerChoiceTier(3)) return true;
                if (Hero.MainHero.HasUnlockedCareerChoiceTier(2)) return additionalLoreCount < 3;
                return additionalLoreCount < 2;
            }

            void AdditionalLoresPrompt()
            {
                var list = new List<InquiryElement>();
                var additionalLores = LoreObject.GetAll()
                    .WhereQ(x => !x.DisabledForCultures.Contains(TORConstants.Cultures.ASRAI))
                    .WhereQ(x => x.StringId != "DarkMagic" && x.StringId != "HighMagic")
                    .ToList();
                foreach (var item in additionalLores)
                {
                    if (Hero.MainHero.HasKnownLore(item.StringId)) continue;
                    list.Add(new InquiryElement(item, item.Name, null));
                }
                var inquirydata = new MultiSelectionInquiryData(
                    TORTextHelper.GetText("tor_spell_learn_label", "Choose Lore"),
                    TORTextHelper.GetText("tor_spell_learn_descr", "Choose a lore to specialize in."),
                    list, true, 1, 1,
                    TORTextHelper.GetText("tor_inquiry_confirm_text", "Confirm"),
                    TORTextHelper.GetText("tor_inquiry_cancel_text", "Cancel"),
                    data => { SpellTrainerHelper.OnChooseLore(data); Hero.MainHero.AddCultureSpecificCustomResource(-1000); },
                    null);
                MBInformationManager.ShowMultiSelectionInquiry(inquirydata, true);
            }

            bool SpellweaverCondition()
            {
                if (Hero.MainHero.HasKnownLore("HighMagic") || Hero.MainHero.HasKnownLore("DarkMagic")) return false;
                return Hero.MainHero.HasUnlockedCareerChoiceTier(2);
            }

            void spellweaverCompanionPrompt()
            {
                var list = new List<InquiryElement>();
                var heroes = (from hero in Hero.MainHero.PartyBelongedTo.GetMemberHeroes()
                              where hero != Hero.MainHero
                              where hero.IsSpellCaster()
                              where hero.CharacterObject.IsElf()
                              where !(hero.HasKnownLore("DarkMagic") || hero.HasKnownLore("HighMagic"))
                              select hero).ToList();
                foreach (var hero in heroes)
                {
                    list.Add(new InquiryElement(hero, hero.FirstName.ToString(), null));
                }
                var inquirydata = new MultiSelectionInquiryData(
                    TORTextHelper.GetText("tor_choose_companion_spellcaster_label", "Choose Companion"),
                    TORTextHelper.GetText("tor_choose_companion_spellcaster_descr", "Which companion should become a Spellcaster?"),
                    list, true, 1, 1,
                    TORTextHelper.GetText("tor_inquiry_confirm_text", "Confirm"),
                    TORTextHelper.GetText("tor_inquiry_cancel_text", "Cancel"),
                    OnChooseCompanion, null);
                MBInformationManager.ShowMultiSelectionInquiry(inquirydata, true);
            }

            void OnChooseCompanion(List<InquiryElement> inquiryElements)
            {
                spellweaverCompanionChooseLore((Hero)inquiryElements[0].Identifier);
            }

            void spellweaverCompanionChooseLore(Hero hero)
            {
                var list = new List<InquiryElement>();
                foreach (var item in spellweaverLores)
                {
                    list.Add(new InquiryElement(item, item.Name, null));
                }
                var inquirydata = new MultiSelectionInquiryData(
                    TORTextHelper.GetText("tor_magic_lore_prompt_companion_lore", "Choose Lore for your companion"),
                    TORTextHelper.GetText("tor_spell_learn_descr", "Choose a lore to specialize in."),
                    list, true, 1, 1,
                    TORTextHelper.GetText("tor_inquiry_confirm_text", "Confirm"),
                    TORTextHelper.GetText("tor_inquiry_cancel_text", "Cancel"),
                    args => OnChooseCompanionLore(hero, args),
                    SpellTrainerHelper.OnCancelLore);
                MBInformationManager.ShowMultiSelectionInquiry(inquirydata, true);
            }

            void OnChooseCompanionLore(Hero hero, List<InquiryElement> picked)
            {
                var lore = (LoreObject)picked[0].Identifier;
                hero.AddKnownLore(lore.StringId);
                InformationManager.HideInquiry();
            }

            void spellweaverPrompt()
            {
                var list = new List<InquiryElement>();
                foreach (var item in spellweaverLores)
                {
                    list.Add(new InquiryElement(item, item.Name, null));
                }
                var inquirydata = new MultiSelectionInquiryData(
                    TORTextHelper.GetText("tor_spell_learn_label", "Choose Lore"),
                    TORTextHelper.GetText("tor_spell_learn_descr", "Choose a lore to specialize in."),
                    list, true, 1, 1,
                    TORTextHelper.GetText("tor_inquiry_confirm_text", "Confirm"),
                    TORTextHelper.GetText("tor_inquiry_cancel_text", "Cancel"),
                    SpellTrainerHelper.OnChooseLore, SpellTrainerHelper.OnCancelLore);
                MBInformationManager.ShowMultiSelectionInquiry(inquirydata, true);
            }
        }
    }
}
