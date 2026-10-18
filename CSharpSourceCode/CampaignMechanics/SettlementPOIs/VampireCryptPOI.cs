using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TOR_Core.AbilitySystem.Spells;
using TOR_Core.CampaignMechanics.Crafting;
using TOR_Core.CampaignMechanics.CustomResources;
using TOR_Core.CampaignMechanics.SpellTrainers;
using TOR_Core.CharacterDevelopment;
using TOR_Core.Extensions;
using TOR_Core.Utilities;

namespace TOR_Core.CampaignMechanics.SettlementPOIs
{
    public class VampireCryptPOI : SettlementPOI, ISpellTrainerHost, IEnchanterHost
    {
        public override string LocationId => "tor_vampire_crypt";
        public override string ParentMenuId => "town_artisan";
        public override string AnchorEntryId => "town_artisan_leave";
        public override bool AnchorAbove => true;

        // Attach to BOTH the town artisan submenu (Sylvania/Mousillon towns) and the
        // castle courtyard submenu (Necrarch tower castle_MT1 and future castles).
        // AppliesTo gates per-settlement — the castle entry only appears where the
        // scene override is set up in tor_settlements.xml.
        public override IEnumerable<ParentMenuAttachment> MenuAttachments => new[]
        {
            new ParentMenuAttachment("town_artisan",      "town_artisan_leave",     true),
            new ParentMenuAttachment("castle_courtyard",  "castle_courtyard_leave", true),
        };

        public override string EnterLabelId => "tor_poi_vampire_crypt.EnterLabel";
        public override string EnterLabelDefault => "Visit the Vampire Crypt";

        public string SpellTrainerTemplateId => "tor_spelltrainer_vc_0";
        public string EnchanterTemplateId    => "tor_spelltrainer_vc_0";
        public List<string> EnchantmentSuffixes => new() { "vc_enchant" };

        // Same template fills both roles; template-id dedup in the manager keeps it one hero.
        public override void EnsurePlaced(Settlement s) => PlaceResident(s, SpellTrainerTemplateId);

        public bool IsPartyEligibleForTrainer()
        {
            if (IsVcCulture(Hero.MainHero.Culture.StringId)) return true;
            return SpellTrainerHelper.HasMatchingSpellTrainerCompanion(x => IsVcCulture(x.Culture.StringId) && x.IsSpellCaster());
        }

        // Same hero plays both roles via template-id dedup, so eligibility is identical.
        public bool IsPartyEligibleForEnchanter() => IsPartyEligibleForTrainer();

        private static bool IsVcCulture(string cultureId) =>
            cultureId == TORConstants.Cultures.SYLVANIA || cultureId == TORConstants.Cultures.MOUSILLON;

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

        public override void RegisterDialogs(CampaignGameStarter obj)
        {
            obj.AddDialogLine("trainer_vampire_start", "start", "vampire_choices",
                TORTextHelper.GetText("tor_spelltrainer_vampire_start", "Ah, a creature of the night graces my presence. What do you seek?"),
                isVampireTrainer, null, 200, null);
            obj.AddDialogLine("trainer_vampire_hub", "hub_vampire", "vampire_choices",
                TORTextHelper.GetText("tor_spelltrainer_vampire_hub", "What else do you require?"),
                isVampireTrainer, null, 200, null);

            obj.AddPlayerLine("trainer_vampire_learnspells", "vampire_choices", "openbook_vampire",
                TORTextHelper.GetText("tor_spelltrainer_vampire_open_book", "I wish to study the dark arts."),
                () => MobileParty.MainParty.HasSpellCasterMember() && vampireCondition(), null, 200, null);
            obj.AddPlayerLine("trainer_vampire_scrollShop", "vampire_choices", "hub_vampire",
                TORTextHelper.GetText("tor_spelltrainer_vampire_scrolls", "Show me your forbidden tomes."),
                null, SpellTrainerHelper.OpenScrollShop, 200, null);
            obj.AddDialogLine("trainer_vampire_afterlearnspells", "openbook_vampire", "hub_vampire",
                TORTextHelper.GetText("tor_spelltrainer_vampire_close_book", "The shadows reveal their secrets to you."),
                null, SpellTrainerHelper.OpenbookConsequence, 200, null);

            obj.AddPlayerLine("trainer_vampire_learn_lore", "vampire_choices", "vampire_dark_energy_request",
                TORTextHelper.GetText("tor_spelltrainer_magictest_vc_vampire_player_specialize_lore",
                    "I can feel my dark power continuing to grow, teach me more lest I find myself a new 'tutor'. Hurry on to find your Grimoire, lest I grow thirsty in your absence..."),
                VampireCasterReachedNewRankCondition, null, 200);

            obj.AddDialogLine("trainer_vampire_tier_limit", "vampire_dark_energy_request", "hub_vampire",
                TORTextHelper.GetText("tor_spelltrainer_vampire_tier_limit",
                    "You have learned all that your current mastery allows. Return when you have advanced further in your dark arts."),
                HasReachedNecrarchTierLimit, null, 210);
            obj.AddDialogLine("trainer_vampire_dark_energy_request", "vampire_dark_energy_request", "vampire_dark_energy_response",
                TORTextHelper.GetText("tor_spelltrainer_vampire_dark_energy_request",
                    "Even if you have the everliving-gift, the access to forbidden knowledge is restricted by my master. Provide us a gift and I am not speaking of gold, " +
                    "I need you to pay with the essence of Life. Then my masters can give you access... ({DARK_ENERGY_COST}{DARKENERGYICON})"),
                SetDarkEnergyCostVariable, null, 200);
            obj.AddPlayerLine("vampire_dark_energy_accept", "vampire_dark_energy_response", "vampire_lore_prompt",
                "{PAY_DARK_ENERGY_FOR_LORE}", HasEnoughDarkEnergy, null, 200);
            obj.AddPlayerLine("vampire_dark_energy_decline", "vampire_dark_energy_response", "vampire_dark_energy_declined",
                TORTextHelper.GetText("tor_spelltrainer_vampire_decline_price", "I can't provide you this gift"), null,
                null, 200);

            obj.AddDialogLine("trainer_vampire_lore_prompt", "vampire_lore_prompt", "vampire_lore_chosen",
                TORTextHelper.GetText("tor_dialog_ellipsis", "…"),
                null, chooseVampireLoreConsequence, 200);
            obj.AddDialogLine("trainer_vampire_lore_chosen", "vampire_lore_chosen", "hub_vampire",
                TORTextHelper.GetText("tor_spelltrainer_vampire_lore_learned", "The dark knowledge is now yours. Use it wisely... or not."),
                null, null, 200);
            obj.AddDialogLine("trainer_vampire_dark_energy_declined", "vampire_dark_energy_declined", "hub_vampire",
                TORTextHelper.GetText("tor_spelltrainer_vampire_shame", "A shame. What else can I help you with?"),
                null, null, 200);

            obj.AddPlayerLine("trainer_vampire_playergoodbye", "vampire_choices", "saygoodbye_vampire",
                TORTextHelper.GetText("tor_spelltrainer_vampire_player_goodbye", "I shall take my leave."),
                null, null, 100, null);
            obj.AddDialogLine("trainer_vampire_goodbye", "saygoodbye_vampire", "close_window",
                TORTextHelper.GetText("tor_spelltrainer_vampire_goodbye", "Until the next moonrise…"),
                isVampireTrainer, null, 200, null);

            // Enchanter branches — same hero as the trainer via template-id dedup.
            // Registered for BOTH SYLVANIA and MOUSILLON since the shared VC trainer
            // can spawn under either culture (SharesVcEnchanterBranch unites them).
            EnchanterHelper.RegisterEnchanterBranch(obj,
                culture: TORConstants.Cultures.SYLVANIA,
                hubToken: "vampire_choices",
                quitToken: "hub_vampire",
                isMultiUse: true,
                enchantmentSuffixes: EnchantmentSuffixes,
                cultureAccess: IsPartyEligibleForEnchanter);
            EnchanterHelper.RegisterEnchanterBranch(obj,
                culture: TORConstants.Cultures.MOUSILLON,
                hubToken: "vampire_choices",
                quitToken: "hub_vampire",
                isMultiUse: true,
                enchantmentSuffixes: EnchantmentSuffixes,
                cultureAccess: IsPartyEligibleForEnchanter);

            bool isVampireTrainer()
            {
                if (!SpellTrainerHelper.SpellTrainerStartCondition()) return false;
                var partner = CharacterObject.OneToOneConversationCharacter;
                return partner.HeroObject != null && partner.HeroObject.Template.StringId == SpellTrainerHelper.VampireTrainerId;
            }

            bool vampireCondition()
            {
                if (Hero.MainHero.IsVampire()) return true;
                if (Hero.MainHero.PartyBelongedTo == null) return false;
                return Hero.MainHero.PartyBelongedTo.GetMemberHeroes().Any(x =>
                    (x.Culture.StringId == TORConstants.Cultures.SYLVANIA || x.Culture.StringId == TORConstants.Cultures.MOUSILLON) && x.IsSpellCaster());
            }

            bool HasReachedNecrarchTierLimit()
            {
                if (!Hero.MainHero.HasCareer(TORCareers.Necrarch)) return false;
                int loreCount = Hero.MainHero.GetExtendedInfo().KnownLores.Count;
                return loreCount >= GetNecrarchMaxLores();
            }

            bool SetDarkEnergyCostVariable()
            {
                MBTextManager.SetTextVariable("DARK_ENERGY_COST", SpellTrainerHelper.DarkEnergyLoreCost);
                MBTextManager.SetTextVariable("DARKENERGYICON",
                    CustomResourceManager.GetResourceObject("DarkEnergy").GetCustomResourceIconAsText());
                return true;
            }

            bool HasEnoughDarkEnergy()
            {
                MBTextManager.SetTextVariable("DARK_ENERGY_COST", SpellTrainerHelper.DarkEnergyLoreCost);
                MBTextManager.SetTextVariable("DARKENERGYICON",
                    CustomResourceManager.GetResourceObject("DarkEnergy").GetCustomResourceIconAsText());
                string text = TORTextHelper.GetText("tor_spelltrainer_vampire_agree_price",
                    "Take my gift. Now give me what I demand! (Pay {DARK_ENERGY_COST}{DARKENERGYICON})");
                MBTextManager.SetTextVariable("PAY_DARK_ENERGY_FOR_LORE", text);
                return Hero.MainHero.GetCustomResourceValue("DarkEnergy") >= SpellTrainerHelper.DarkEnergyLoreCost;
            }

            bool VampireCasterReachedNewRankCondition()
            {
                if (!Hero.MainHero.HasCareer(TORCareers.Necrarch) && !Hero.MainHero.HasCareer(TORCareers.MinorVampire)) return false;

                MBTextManager.SetTextVariable("DARKENERGYICON",
                    CustomResourceManager.GetResourceObject("DarkEnergy").GetCustomResourceIconAsText());

                int loreCount = Hero.MainHero.GetExtendedInfo().KnownLores.Count;

                if (Hero.MainHero.HasCareer(TORCareers.Necrarch))
                {
                    return loreCount < 8;
                }
                if (Hero.MainHero.HasCareer(TORCareers.MinorVampire))
                {
                    if (Hero.MainHero.HasUnlockedCareerChoiceTier(2)) return loreCount < 3;
                }
                return false;
            }

            void chooseVampireLoreConsequence()
            {
                if (Hero.MainHero.HasCareer(TORCareers.MinorVampire)) showMinorVampireLoreChoice();
                else if (Hero.MainHero.HasCareer(TORCareers.Necrarch)) showNecrarchLoreChoice();
            }

            void showMinorVampireLoreChoice()
            {
                var list = new List<InquiryElement>();
                foreach (var item in LoreObject.GetAll())
                {
                    if (item.StringId != "DarkMagic" && item.StringId != "LoreOfDeath") continue;
                    if (Hero.MainHero.GetExtendedInfo().HasKnownLore(item.StringId)) continue;
                    list.Add(new InquiryElement(item, item.Name, null));
                }
                var inquirydata = new MultiSelectionInquiryData(
                    TORTextHelper.GetText("tor_vampire_lore_choice_label", "Choose Your Dark Path"),
                    TORTextHelper.GetText("tor_vampire_lore_choice_description", "This choice is final. You may only master one of these dark arts."),
                    list, true, 1, 1,
                    TORTextHelper.GetText("tor_inquiry_confirm_text", "Confirm"),
                    TORTextHelper.GetText("tor_inquiry_cancel_text", "Cancel"),
                    SpellTrainerHelper.OnChooseLore, SpellTrainerHelper.OnCancelLore);
                MBInformationManager.ShowMultiSelectionInquiry(inquirydata, true);
            }

            void showNecrarchLoreChoice()
            {
                var list = new List<InquiryElement>();
                var model = Campaign.Current.Models.GetAbilityModel();
                foreach (var item in LoreObject.GetAll())
                {
                    if (item.StringId == "MinorMagic" || Hero.MainHero.GetExtendedInfo().HasKnownLore(item.StringId)) continue;
                    if (!model.IsValidLoreForHero(Hero.MainHero, item)) continue;
                    list.Add(new InquiryElement(item, item.Name, null));
                }
                int maxLores = GetNecrarchMaxLores();
                int currentLores = Hero.MainHero.GetExtendedInfo().KnownLores.Count;
                MBTextManager.SetTextVariable("REMAINING_LORES", maxLores - currentLores);
                var description = TORTextHelper.GetText("tor_necrarch_lore_choice_description", "You may learn {REMAINING_LORES} more lores at your current level of mastery.");
                var inquirydata = new MultiSelectionInquiryData(
                    TORTextHelper.GetText("tor_spell_learn_label", "Choose Lore"),
                    description,
                    list, true, 1, 1,
                    TORTextHelper.GetText("tor_inquiry_confirm_text", "Confirm"),
                    TORTextHelper.GetText("tor_inquiry_cancel_text", "Cancel"),
                    SpellTrainerHelper.OnChooseLore, SpellTrainerHelper.OnCancelLore);
                MBInformationManager.ShowMultiSelectionInquiry(inquirydata, true);
            }

            int GetNecrarchMaxLores()
            {
                if (Hero.MainHero.HasUnlockedCareerChoiceTier(3)) return 8;
                if (Hero.MainHero.HasUnlockedCareerChoiceTier(2)) return 5;
                return 3;
            }
        }
    }
}
