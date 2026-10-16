using Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.LinQuick;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;
using TOR_Core.AbilitySystem.SpellBook;
using TOR_Core.AbilitySystem.Spells;
using TOR_Core.CampaignMechanics.CustomResources;
using TOR_Core.CharacterDevelopment;
using TOR_Core.CharacterDevelopment.CareerSystem;
using TOR_Core.Extensions;
using TOR_Core.Quests;
using TOR_Core.Utilities;
using static TOR_Core.Utilities.TORConstants;

namespace TOR_Core.CampaignMechanics.SpellTrainers
{
    /// <summary>
    /// Shared trainer-dialog infrastructure. Owns the five trainer template ids,
    /// the cross-trainer access rules (main-hero and party-companion), the
    /// stateless utility consequences (scroll shop, spellbook open, lore choice),
    /// and the "common" trainer dialogs that apply to multiple cultures
    /// (magic-test flow, lore specialisation, generic greeting/goodbye).
    ///
    /// Per-trainer dialog trees (Prophetess / Spellsinger / Vampire / Greenskin)
    /// live on the respective <see cref="SettlementPOIs.SettlementPOI"/> subclasses
    /// via their <c>RegisterDialogs</c> override. The POI manager drives both this
    /// helper and each POI's override at session launch.
    /// </summary>
    public static class SpellTrainerHelper
    {
        // ------- Template ids (keep in sync with ModuleData/tor_npccharacters)
        public const string EmpireTrainerId     = "tor_spelltrainer_empire_0";
        public const string VampireTrainerId    = "tor_spelltrainer_vc_0";
        public const string ProphetessTrainerId = "tor_spelltrainer_bretonnia_0";
        public const string WoodelfTrainerId    = "tor_spelltrainer_woodelves_0";
        public const string GreenskinTrainerId  = "tor_spelltrainer_greenskins_0";

        public const int DarkEnergyLoreCost = 2000;
        private const float TestSuccessChance = 1f;

        // Magic-test flow carries a short-lived per-conversation flag. Static is fine
        // because at most one trainer conversation is active at a time.
        private static string _testResult = "";

        // ------- Template-based identity

        public static bool IsSpellTrainer(Hero hero)
        {
            var t = hero?.Template?.StringId;
            return t == EmpireTrainerId
                || t == VampireTrainerId
                || t == ProphetessTrainerId
                || t == WoodelfTrainerId
                || t == GreenskinTrainerId;
        }

        public static bool IsVcTrainerCulture(string cultureId)
        {
            return cultureId == TORConstants.Cultures.SYLVANIA || cultureId == TORConstants.Cultures.MOUSILLON;
        }

        // ------- Access rules (main hero OR party-companion)

        public static bool HasMatchingSpellTrainerCompanion(Func<Hero, bool> predicate)
        {
            if (Hero.MainHero.PartyBelongedTo == null) return false;
            return Hero.MainHero.PartyBelongedTo.GetMemberHeroes().Any(x => x != Hero.MainHero && predicate(x));
        }

        public static bool HasEmpireTrainerAccess()
        {
            if (Hero.MainHero.Culture.StringId == TORConstants.Cultures.EMPIRE) return true;
            return HasMatchingSpellTrainerCompanion(x => x.Culture.StringId == TORConstants.Cultures.EMPIRE && x.IsSpellCaster());
        }

        public static bool HasVampireTrainerAccess()
        {
            if (IsVcTrainerCulture(Hero.MainHero.Culture.StringId)) return true;
            return HasMatchingSpellTrainerCompanion(x => IsVcTrainerCulture(x.Culture.StringId) && x.IsSpellCaster());
        }

        public static bool HasBretonnianTrainerAccess()
        {
            if (Hero.MainHero.Culture.StringId == TORConstants.Cultures.BRETONNIA) return true;
            return HasMatchingSpellTrainerCompanion(x => x.Culture.StringId == TORConstants.Cultures.BRETONNIA && x.IsSpellCaster());
        }

        public static bool HasSpellsingerTrainerAccess()
        {
            if (Hero.MainHero.HasCareer(TORCareers.Spellsinger)) return true;
            if (Hero.MainHero.HasCareer(TORCareers.Warden)) return true;
            if (Hero.MainHero.Culture.StringId == TORConstants.Cultures.ASRAI &&
                Hero.MainHero.IsSpellCaster() &&
                Hero.MainHero.GetKnownLoreCount() > 0)
            {
                return true;
            }
            return HasMatchingSpellTrainerCompanion(x => x.Culture.StringId == TORConstants.Cultures.ASRAI && x.IsSpellCaster());
        }

        public static bool HasGreenskinTrainerAccess()
        {
            if (Hero.MainHero.Culture.StringId == TORConstants.Cultures.GREENSKIN) return true;
            return HasMatchingSpellTrainerCompanion(x => x.Culture.StringId == TORConstants.Cultures.GREENSKIN && x.IsSpellCaster());
        }

        public static bool CanAccessSpellTrainer(Hero trainer)
        {
            var settlementCulture = Settlement.CurrentSettlement?.Culture?.StringId;
            var templateId = trainer.Template?.StringId;
            if (settlementCulture == null || templateId == null) return false;

            if (templateId == EmpireTrainerId)     return settlementCulture == TORConstants.Cultures.EMPIRE    && HasEmpireTrainerAccess();
            if (templateId == VampireTrainerId)    return IsVcTrainerCulture(settlementCulture)                && HasVampireTrainerAccess();
            if (templateId == ProphetessTrainerId) return settlementCulture == TORConstants.Cultures.BRETONNIA && HasBretonnianTrainerAccess();
            if (templateId == WoodelfTrainerId)    return settlementCulture == TORConstants.Cultures.ASRAI     && HasSpellsingerTrainerAccess();
            if (templateId == GreenskinTrainerId)  return settlementCulture == TORConstants.Cultures.GREENSKIN && HasGreenskinTrainerAccess();
            return false;
        }

        public static bool IsBlockedSpellTrainerConversation()
        {
            var partner = CharacterObject.OneToOneConversationCharacter;
            return partner?.HeroObject != null && partner.HeroObject.IsSpellTrainer() && !CanAccessSpellTrainer(partner.HeroObject);
        }

        public static bool SpellTrainerStartCondition()
        {
            var partner = CharacterObject.OneToOneConversationCharacter;
            if (partner?.HeroObject == null || !partner.HeroObject.IsSpellTrainer()) return false;
            return CanAccessSpellTrainer(partner.HeroObject);
        }

        // ------- Stateless consequences used across trainer dialogs

        public static void OpenScrollShop()
        {
            var roster = new ItemRoster();
            MBObjectManager.Instance.GetObjectTypeList<ItemObject>()
                .Where(item => item.IsInventoryUsable() && item.GetTraits().Any(trait => trait.OnInventoryUseScript.InventoryScriptName.Contains("SkillBookScript")))
                .ToList()
                .ForEach(item => roster.Add(new ItemRosterElement(item, MBRandom.RandomInt(1, 5))));
            InventoryScreenHelper.OpenScreenAsTrade(roster, Settlement.CurrentSettlement.Town);
        }

        public static void OpenbookConsequence()
        {
            var state = Game.Current.GameStateManager.CreateState<SpellBookState>();
            state.IsTrainerMode = true;
            state.TrainerCulture = CharacterObject.OneToOneConversationCharacter.Culture.StringId;
            Game.Current.GameStateManager.PushState(state);
        }

        public static void OnChooseLore(List<InquiryElement> obj)
        {
            var choice = obj[0].Identifier as LoreObject;
            var info = Hero.MainHero.GetExtendedInfo();
            if (choice != null)
            {
                Hero.MainHero.AddKnownLore(choice.StringId);
                var choiceText = new TextObject(choice.Name);
                if (info.SpellCastingLevel < SpellCastingLevel.Entry) Hero.MainHero.SetSpellCastingLevel(SpellCastingLevel.Entry);
                MBInformationManager.AddQuickInformation(new TextObject(TORTextHelper.GetText("tor_magic_lore_prompt_notification", "Successfully learned lore: ") + choiceText));
                TORQuestHelper.GetCurrentActiveIfExists<SpecializeLoreQuest>()?.CompleteQuestWithSuccess();
                if (Hero.MainHero.IsVampire())
                {
                    Hero.MainHero.AddCustomResource("DarkEnergy", -DarkEnergyLoreCost);
                }
            }
            InformationManager.HideInquiry();
        }

        public static void OnCancelLore(List<InquiryElement> obj) => InformationManager.HideInquiry();

        // ------- Magic-test flow (used by Empire + Sylvania/Mousillon generic greet)

        public static bool MagicTestCondition()
        {
            var partner = CharacterObject.OneToOneConversationCharacter;
            var culture = partner.Culture.StringId;
            if (partner.Culture.StringId is TORConstants.Cultures.ASRAI or TORConstants.Cultures.BRETONNIA) return false;
            if (!CareerHelper.IsMagicCapableCareer(Hero.MainHero.GetCareer())) return false;

            bool flag = !Hero.MainHero.IsVampire()
                     && !Hero.MainHero.IsSpellCaster()
                     && !Hero.MainHero.HasAttribute(CharacterAttributes.PRIEST)
                     && _testResult == "";
            if (flag)
            {
                string text;
                switch (culture)
                {
                    case "empire":
                        text = TORTextHelper.GetText("tor_spelltrainer_magictest_empire", "I have come seeking knowledge, I wish to learn the arcane arts. Can you help me?");
                        break;
                    case TORConstants.Cultures.SYLVANIA:
                    case "mousillon":
                        text = TORTextHelper.GetText("tor_spelltrainer_magictest_vc", "I need the power to escape death, to rule over this world as something more. Can you teach me the ways of your power?");
                        break;
                    default:
                        text = "You shouldn't see this.";
                        break;
                }
                MBTextManager.SetTextVariable("TEST_QUESTION", text);
            }
            return flag;
        }

        public static bool FillTextForTestPrompt()
        {
            string text;
            var culture = Hero.OneToOneConversationHero.Culture.StringId;
            switch (culture)
            {
                case TORConstants.Cultures.EMPIRE:
                    text = TORTextHelper.GetText("tor_spelltrainer_magictest_begin_empire", "Hmm. To understand the Winds of magic you must have the aethyric senses. Let me perform an experiment on you to determine your potential...");
                    break;
                case TORConstants.Cultures.SYLVANIA:
                case TORConstants.Cultures.MOUSILLON:
                    text = TORTextHelper.GetText("tor_spelltrainer_magictest_begin_vc", "I can sense the you might have some grasp on the Winds of magic. Let me subject you to an examination to see your potential...");
                    break;
                default:
                    text = "You shouldn't see this.";
                    break;
            }
            MBTextManager.SetTextVariable("TEST_PROMPT", text);
            return true;
        }

        public static void DetermineTestOutcome()
        {
            var rng = MBRandom.RandomFloatRanged(0, 1);
            _testResult = rng <= TestSuccessChance ? "success" : "failure";
        }

        public static bool TestResultCondition()
        {
            string result = "Error.";
            if (_testResult == "success")
            {
                result = TORTextHelper.GetText("tor_spelltrainer_magictest_result_success", "Hmm...interesting. It would seem you do have an aptitude, perhaps even potential.");
                Hero.MainHero.AddAttribute(CharacterAttributes.ABILITY_USER);
                Hero.MainHero.AddAttribute(CharacterAttributes.SPELLCASTER);
                Hero.MainHero.AddKnownLore("MinorMagic");
                Hero.MainHero.SetSpellCastingLevel(SpellCastingLevel.Minor);
                var quest = TORQuestHelper.GetNewSpecializeLoreQuest(true);
                if (quest != null) quest.StartQuest();
            }
            else if (_testResult == "failure")
            {
                result = TORTextHelper.GetText("tor_spelltrainer_magictest_result_failure", "Pah, it is beyond you. Begone before you waste more of my time.");
            }
            MBTextManager.SetTextVariable("TEST_RESULT", result);
            return true;
        }

        // ------- Lore-specialisation flow (post-test: Empire college / Sylvania grimoire)

        public static bool SpecializeLoreCondition()
        {
            var quest = TORQuestHelper.GetCurrentActiveIfExists<SpecializeLoreQuest>();
            var partnerCulture = CharacterObject.OneToOneConversationCharacter.Culture.StringId;
            if (Hero.MainHero.IsVampire() && partnerCulture == "empire") return false;
            var info = Hero.MainHero.GetExtendedInfo();
            var possibleLores = new List<LoreObject>();
            foreach (var item in LoreObject.GetAll())
            {
                if (item.StringId != "MinorMagic" &&
                    !item.DisabledForCultures.Contains(partnerCulture) &&
                    !info.HasKnownLore(item.StringId) &&
                    !(item.IsRestrictedToVampires && !Hero.MainHero.IsVampire())) possibleLores.Add(item);
            }
            bool flag = false;
            if (quest != null && possibleLores.Count > 0)
            {
                flag = info.KnownLores.Count == 1 && info.KnownLores[0].StringId == "MinorMagic" && quest.Task1Complete;
                if (!flag && Hero.MainHero.IsVampire() && quest.Task1Complete) flag = true;
            }
            else if (Hero.MainHero.IsVampire() && possibleLores.Count > 0 && info.SpellCastingLevel == SpellCastingLevel.Master) flag = true;
            if (flag)
            {
                string text = "";
                var culture = Hero.OneToOneConversationHero.Culture.StringId;
                switch (culture)
                {
                    case "empire":
                        text = TORTextHelper.GetText("tor_spelltrainer_magictest_empire_player_specialize_lore", "I am ready to join the colleges and become a true wizard of the Empire.");
                        break;
                    case TORConstants.Cultures.SYLVANIA:
                        text = TORTextHelper.GetText("tor_spelltrainer_magictest_vc_player_specialize_lore", "My Lord, I beseech you to teach me your dark magic. I have learned all that I can on my own and would be a most loyal apprentice to you.");
                        if (Hero.MainHero.IsVampire())
                        {
                            text = TORTextHelper.GetText("tor_spelltrainer_magictest_vc_vampire_player_specialize_lore", "I can feel my dark power continuing to grow, teach me more lest I find myself a new 'tutor'. Hurry on to find your Grimoire, lest I grow thirsty in your absence...");
                        }
                        break;
                    default:
                        text = "You shouldn't see this.";
                        break;
                }
                MBTextManager.SetTextVariable("SPECALIZE_QUESTION", text);
            }
            return flag;
        }

        public static bool FillChooseLoreText()
        {
            string text = "";
            var culture = Hero.OneToOneConversationHero.Culture.StringId;
            switch (culture)
            {
                case "empire":
                    text = TORTextHelper.GetText("tor_spelltrainer_magictest_empire_specialize_lore", "You have proven yourself and have a strong aptitude for the wind of magic, which college will you be joining? You can only dedicate yourself to one, choose wisely.");
                    break;
                case TORConstants.Cultures.SYLVANIA:
                    text = TORTextHelper.GetText("tor_spelltrainer_magictest_vc_specialize_lore", "You have potential Dark One, serve me and I will teach you all that I know in time. Come let me consult my grimoire.");
                    if (Hero.MainHero.IsVampire()) text = TORTextHelper.GetText("tor_spelltrainer_magictest_vc_vampire_specialize_lore", "Yes sire! Right away my good and merciful lord.");
                    break;
                default:
                    text = "You shouldn't see this.";
                    break;
            }
            MBTextManager.SetTextVariable("SPECIALIZE_PROMPT", text);
            return true;
        }

        public static void ChooseLoreConsequence()
        {
            var list = new List<InquiryElement>();
            var lores = LoreObject.GetAll();
            var model = Campaign.Current.Models.GetAbilityModel();
            foreach (var item in lores)
            {
                if (item.StringId == "MinorMagic" || Hero.MainHero.GetExtendedInfo().HasKnownLore(item.StringId)) continue;
                if (!model.IsValidLoreForHero(Hero.MainHero, item)) continue;
                list.Add(new InquiryElement(item, item.Name, null));
            }
            var inquirydata = new MultiSelectionInquiryData(
                TORTextHelper.GetText("tor_spell_learn_label", "Choose Lore"),
                TORTextHelper.GetText("tor_spell_learn_descr", "Choose a lore to specialize in."),
                list, true, 1, 1,
                TORTextHelper.GetText("tor_inquiry_confirm_text", "Confirm"),
                TORTextHelper.GetText("tor_inquiry_cancel_text", "Cancel"),
                OnChooseLore, OnCancelLore);
            MBInformationManager.ShowMultiSelectionInquiry(inquirydata, true);
        }

        // ------- Common dialog registration (lines shared across trainers)

        /// <summary>
        /// Dialogs that fire for every spell-trainer conversation that passes
        /// <see cref="SpellTrainerStartCondition"/>: generic greet / goodbye, magic-test
        /// entry, scroll shop, "learn spells" fallback, lore specialisation.
        /// Called ONCE at session launch by SettlementPOIManagerCampaignBehavior,
        /// alongside each POI's own <c>RegisterDialogs</c> override.
        /// </summary>
        public static void RegisterCommonDialogs(CampaignGameStarter obj)
        {
            obj.AddDialogLine("trainer_start", "start", "choices", TORTextHelper.GetTextForNative("tor_spelltrainer_start", "Do I know you? What do you need, be quick I am a busy."), SpellTrainerStartCondition, null, 200, null);
            obj.AddDialogLine("trainer_start_rejected", "start", "close_window", TORTextHelper.GetTextForNative("tor_spelltrainer_start_rejected", "I have nothing to teach you. Begone."), IsBlockedSpellTrainerConversation, null, 250, null);
            obj.AddPlayerLine("trainer_test", "choices", "magictest", "{TEST_QUESTION}", MagicTestCondition, null, 200, null);
            obj.AddDialogLine("trainer_testoutcome", "magictest", "testoutcome", "{TEST_PROMPT}", FillTextForTestPrompt, DetermineTestOutcome, 200, null);
            obj.AddDialogLine("trainer_testresult", "testoutcome", "start", "{TEST_RESULT}", TestResultCondition, null, 200, null);

            obj.AddPlayerLine("trainer_learnspells", "choices", "openbook", TORTextHelper.GetTextForNative("tor_spelltrainer_open_book", "I have come seeking further knowledge."),
                () => !MobileParty.MainParty.GetSpellCasterMemberHeroes().IsEmpty() && MobileParty.MainParty.GetSpellCasterMemberHeroes().Any(x => x.Culture.StringId != TORConstants.Cultures.DAWI), null, 200, null);
            obj.AddPlayerLine("trainer_scrollShop", "choices", "start", TORTextHelper.GetTextForNative("tor_spelltrainer_scrolls", "Do you sell any scrolls?"), null, OpenScrollShop, 200, null);

            obj.AddDialogLine("trainer_afterlearnspells", "openbook", "start",
                TORTextHelper.GetText("tor_spelltrainer_close_book", "Hmm, come then. I will teach you what I can."), null, OpenbookConsequence, 200, null);
            obj.AddPlayerLine("trainer_specialize", "choices", "specializelore", "{SPECALIZE_QUESTION}", SpecializeLoreCondition, null, 200, null);
            obj.AddDialogLine("trainer_chooselore", "specializelore", "start", "{SPECIALIZE_PROMPT}.", FillChooseLoreText, ChooseLoreConsequence, 200, null);

            obj.AddPlayerLine("trainer_playergoodbye", "choices", "saygoodbye", TORTextHelper.GetTextForNative("tor_spelltrainer_player_goodbye", "Farewell Magister."), null, null, 200, null);
            obj.AddDialogLine("trainer_goodbye", "saygoodbye", "close_window", TORTextHelper.GetTextForNative("tor_spelltrainer_goodbye", "Hmm, yes. Farewell."), null, null, 200, null);
        }
    }
}
