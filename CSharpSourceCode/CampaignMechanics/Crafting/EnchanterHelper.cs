using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Core.ImageIdentifiers;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;
using TaleWorlds.TwoDimension;
using TOR_Core.Extensions;
using TOR_Core.Framework;
using TOR_Core.Utilities;
using static TOR_Core.Utilities.TORConstants;

namespace TOR_Core.CampaignMechanics.Crafting
{
    /// <summary>
    /// Shared enchanter-dialog infrastructure. Owns the full list of enchanter
    /// templates, the shared player-progression flags (persisted by
    /// <see cref="EnchanterProgressBehavior"/>), the stateless utility consequences
    /// (shop open, donation flow, manual grant, blueprint grant), and the
    /// parameterized <see cref="RegisterEnchanterBranch"/> that each caller — POI
    /// or legacy behavior — invokes with its culture / hub / quit / suffixes.
    /// </summary>
    public static class EnchanterHelper
    {
        // ------- Save-persisted progression flags (synced by EnchanterProgressBehavior)
        public static bool SpokeToEnchanter;
        public static bool LearnedEnchantment;

        // ------- All enchanter template ids (POI + legacy OathGold / EonirFavor)
        public static readonly HashSet<string> AllEnchanterTemplates = new()
        {
            "tor_enchanter_empire_0",
            "tor_spelltrainer_vc_0",
            "tor_spelltrainer_bretonnia_0",
            "tor_spelltrainer_woodelves_0",
            "tor_eonir_spellsinger_envoy_0",
            "tor_dawi_runelord_trainer_0",
            "tor_spelltrainer_greenskins_0",
        };

        private static readonly List<string> CareersEligibleForFreebee = new()
        {
            "GrailDamsel", "ImperialMagister", "Spellsinger", "Greylord", "Necromancer", "Runelord", "OrcShaman"
        };

        public static bool IsEnchanter(Hero hero) =>
            hero?.Template?.StringId != null && AllEnchanterTemplates.Contains(hero.Template.StringId);

        public static bool EnchanterCareer() =>
            CareersEligibleForFreebee.Contains(Hero.MainHero.GetCareer().StringId);

        public static bool FullfillsFreeBeeCondition() => !LearnedEnchantment && EnchanterCareer();

        public static void OpenEnchantmentShop(List<string> suffixes, string culture) =>
            EnchantmentShopHelper.OpenEnchantmentRecipeShop(suffixes, culture, false);

        public static bool HasAnyViableEnchanterCharacter(List<string> suffixes) =>
            EnchantmentHelper.HasAnyLearnableEnchantmentRecipe(suffixes);

        public static void AddBeginnerBlueprintsForEnchantment()
        {
            foreach (var grantor in CraftingCareerHooks.BeginnerBlueprintGrantors)
                grantor(Hero.MainHero);
        }

        public static void SpokeFirstTime()
        {
            SpokeToEnchanter = true;
            AddManual();
        }

        public static void AddManual()
        {
            var itemId = GetManual();
            if (itemId == "") return;
            var item = MBObjectManager.Instance.GetObject<ItemObject>(itemId);
            Hero.MainHero.PartyBelongedTo.ItemRoster.AddToCounts(item, 1);
        }

        public static bool HasManual()
        {
            var itemId = GetManual();
            if (itemId == "") return false;
            return Hero.MainHero.PartyBelongedTo.ItemRoster.Any(x => x.EquipmentElement.Item.StringId == itemId);
        }

        public static string GetManual()
        {
            switch (Hero.MainHero.Culture.StringId)
            {
                case TORConstants.Cultures.EMPIRE: return "tor_book_learn_enchanting";
                case TORConstants.Cultures.DAWI:   return "tor_book_learn_runecraft";
                default:
                    TORCommon.Log("ENCHANTMENT: could not find manual item for" + Hero.MainHero.Culture.StringId, LogLevel.Warn);
                    return "tor_book_learn_enchanting";
            }
        }

        public static bool ConditionForDonation()
        {
            var roster = Hero.MainHero.PartyBelongedTo.Party.ItemRoster;
            foreach (var elem in roster)
            {
                var item = elem.EquipmentElement.Item;
                if (item.IsCraftedByPlayer) continue;
                if (item.HasAnyTrait())
                    if (item.IsArmor() || item.IsWeapon() || item.IsShield())
                        return true;
            }
            return false;
        }

        public static void DonationMode(bool customResourceExchange)
        {
            var roster = Hero.MainHero.PartyBelongedTo.Party.ItemRoster.ToMBList();
            if (customResourceExchange) roster = roster.FindAll(x => !x.EquipmentElement.Item.IsCraftedByPlayer).ToMBList();

            var selectableItems = new List<InquiryElement>();
            foreach (var elem in roster)
            {
                var equipmentElement = elem.EquipmentElement;
                var item = equipmentElement.Item;
                if (item.HasAnyTrait() && (item.IsWeapon() || item.IsArmor() || item.IsShield()))
                    selectableItems.Add(new InquiryElement(equipmentElement, item.Name.ToString(), new ItemImageIdentifier(item)));
            }

            GameTexts.SetVariable("CUSTOMRESOURCE_ICON", Hero.MainHero.GetCultureSpecificCustomResource().GetCustomResourceIconAsText());

            // Nothing calls DonationMode(false) today; the disintegrate strings exist so the branch is
            // localized while it survives. Deleting the branch and its strings is Codesmells work.
            var title = customResourceExchange
                ? TORTextHelper.GetTextObject("tor_enchant_prompt_donate_items", "title", "Donate items")
                : TORTextHelper.GetTextObject("tor_enchant_prompt_disintegrate", "title", "Disenchant items");
            var text = customResourceExchange
                ? TORTextHelper.GetTextObject("tor_enchant_prompt_donate_items", "text", "Select the items you want to donate. You will obtain {CUSTOMRESOURCE_ICON} from this action. ")
                : TORTextHelper.GetTextObject("tor_enchant_prompt_disintegrate", "text", "Select the items you want to disenchant. Their ingredients will be returned to you.");

            var inquirydata = new MultiSelectionInquiryData(title.ToString(), text.ToString(), selectableItems, true, 1, 15,
                TORTextHelper.GetText("tor_inquiry_accept_text", "Accept"),
                TORTextHelper.GetText("tor_inquiry_cancel_text", "Cancel"),
                items => Disenchant(items, customResourceExchange), null);
            MBInformationManager.ShowMultiSelectionInquiry(inquirydata, true);
        }

        private static void Disenchant(List<InquiryElement> inquiryElements, bool customResourceExchange)
        {
            var model = Campaign.Current.Models.GetEnchantmentIngredientModel();
            var resources = new int[Enum.GetValues(typeof(TorTradeGoodType)).Length];
            var customResources = 0;

            foreach (var element in inquiryElements)
            {
                var customResourceElem = 0;
                var customResourceFactor = 1 / 3f;
                var equipmentElement = (EquipmentElement)element.Identifier;
                var item = equipmentElement.Item;

                if (!item.IsCraftedByPlayer)
                {
                    var adjusted = item.Value / 200;
                    customResourceElem += MBMath.ClampIndex(adjusted, 50, 500);
                }

                var traits = item.GetTraits();
                if (!traits.Any()) return;

                Hero.MainHero.PartyBelongedTo.ItemRoster.AddToCounts(equipmentElement, -1);

                foreach (var trait in item.GetTraits())
                    foreach (TorTradeGoodType type in Enum.GetValues(typeof(TorTradeGoodType)))
                    {
                        if (trait.IngredientItem != type) continue;
                        if (trait.IngredientAmount > 0)
                        {
                            var factor = model.GetPercentOfIngredientForRecycleItems(type, item);
                            resources[(int)type] += (int)Mathf.Max(1, trait.IngredientAmount * factor);
                            customResourceElem += model.GetCustomResourceValueForIngredient(type) * trait.IngredientAmount;
                            customResourceFactor += customResourceFactor;
                        }
                    }

                customResourceElem = (int)(customResourceElem * customResourceFactor);
                customResources += customResourceElem;
            }

            if (customResourceExchange)
            {
                var resultValue = customResources;
                var gainedText = TORTextHelper.GetTextObject("tor_gained_resource_notification_text", "Gained {AMOUNT}{RESOURCE_ICON}");
                gainedText.SetTextVariable("AMOUNT", resultValue);
                gainedText.SetTextVariable("RESOURCE_ICON", Hero.MainHero.GetCultureSpecificCustomResource().GetCustomResourceIconAsText());
                MBInformationManager.AddQuickInformation(gainedText, 2000, Hero.MainHero.CharacterObject);
                Hero.MainHero.AddCultureSpecificCustomResource(resultValue);
                return;
            }

            var gainedItems = new List<TextObject>();
            foreach (TorTradeGoodType type in Enum.GetValues(typeof(TorTradeGoodType)))
            {
                var ingredient = TorEnchantingIngredients.GetItemObjectForIngredient(type);
                if (ingredient == null) continue;
                Hero.MainHero.PartyBelongedTo.ItemRoster.Add(new ItemRosterElement(ingredient, resources[(int)type]));
                if (resources[(int)type] > 0)
                {
                    gainedItems.Add(TORTextHelper.GetTextObject("tor_gained_item_amount_text", "{AMOUNT} {ITEM_NAME}")
                        .SetTextVariable("AMOUNT", resources[(int)type])
                        .SetTextVariable("ITEM_NAME", ingredient.Name));
                }
            }
            var gainedItemsText = TORTextHelper.GetTextObject("tor_gained_items_notification_text", "Gained {ITEMS}");
            gainedItemsText.SetTextVariable("ITEMS", GameTexts.GameTextHelper.MergeTextObjectsWithComma(gainedItems, false));
            MBInformationManager.AddQuickInformation(gainedItemsText, 2000, Hero.MainHero.CharacterObject);
        }

        /// <summary>
        /// Register the full enchanter dialog branch for one culture. Called once per
        /// culture by its owning POI (or legacy behavior for DAWI/EONIR) at session
        /// launch. The branch registers ~15 dialog lines on the given hub + quit tokens.
        ///
        /// <paramref name="isMultiUse"/> toggles between the two shapes:
        /// <list type="bullet">
        ///   <item>true — enchanter shares a hero with the spelltrainer; dialog
        ///   enters via a player line on the trainer's existing hub.</item>
        ///   <item>false — enchanter has their own standalone dialog (Empire).</item>
        /// </list>
        ///
        /// <paramref name="cultureAccess"/> is the per-culture party-eligibility
        /// predicate — for POIs this is <c>poi.IsPartyEligibleForEnchanter</c>; for
        /// DAWI/EONIR the legacy behavior passes its own predicate.
        /// </summary>
        public static void RegisterEnchanterBranch(
            CampaignGameStarter obj,
            string culture,
            string hubToken,
            string quitToken,
            bool isMultiUse,
            List<string> enchantmentSuffixes,
            Func<bool> cultureAccess)
        {
            if (isMultiUse)
            {
                obj.AddPlayerLine("enchanter_start_p" + culture, hubToken, "enchanter_start" + culture,
                    TORTextHelper.GetText("tor_enchanter_start_p", culture, "I wish to learn about enchantment.", true),
                    () => EnchanterCondition(culture) && cultureAccess(), null, 210);

                obj.AddDialogLine("enchanter_start_enchanter" + culture, "enchanter_start" + culture,
                    "enchanter_hub_intro" + culture, TORTextHelper.GetText("tor_enchanter_start_enchanter", culture, "Welcome, fellow practitioner of the arts.", true),
                    () => EnchanterCareer() && cultureAccess(), null, 200);

                obj.AddDialogLine("enchanter_start" + culture, "enchanter_start" + culture,
                    "enchanter_hub_intro" + culture, TORTextHelper.GetText("tor_enchanter_dialog_start", culture, "Greetings, visitor. How may I assist you?", true),
                    () => EnchanterCondition(culture) && cultureAccess(), null, 200);
            }
            else
            {
                obj.AddDialogLine("enchanter_start_quit" + culture, "start", "close_window",
                    TORTextHelper.GetText("tor_enchanter_dialog_start_quit", culture, "You are not of my culture. Begone.", true),
                    () => IsDirectEnchanterBlocked(culture, cultureAccess), null, 200);

                obj.AddDialogLine("enchanter_start_2" + culture, "start", "enchanter_hub_intro" + culture,
                    TORTextHelper.GetText("tor_enchanter_dialog_start_2", culture, "Ah, a fellow practitioner! You are most welcome here.", true),
                    () => EnchanterCondition(culture) && cultureAccess() && EnchanterCareer(), null, 200);

                obj.AddDialogLine("enchanter_start_1" + culture, "start", "enchanter_hub_intro" + culture,
                    TORTextHelper.GetText("tor_enchanter_dialog_start_1", culture, "Welcome. You have proven yourself worthy of my attention.", true),
                    () => EnchanterCondition(culture) && cultureAccess(), null, 200);
            }

            // enchantment hub
            obj.AddDialogLine("enchanter_hub_intro" + culture, "enchanter_hub_intro" + culture,
                "enchantment_hub" + culture, TORTextHelper.GetText("tor_enchanter_hub_intro", culture, "So, what brings you here?", true), null, null, 200);

            obj.AddDialogLine("enchanter_hub_reintro" + culture, "enchanter_hub_reintro" + culture,
                "enchantment_hub" + culture, TORTextHelper.GetText("tor_enchanter_hub_reintro", culture, "Is there anything else?", true), null, null, 200);

            obj.AddPlayerLine("enchanter_hub_info_p" + culture, "enchantment_hub" + culture,
                "enchanter_info_hub_intro" + culture, TORTextHelper.GetText("tor_enchanter_hub_info_p", culture, "Can you teach me about enchantment?", true),
                () => !LearnedEnchantment, null, 200);
            obj.AddPlayerLine("enchanter_hub_blueprints_p" + culture, "enchantment_hub" + culture,
                "enchanter_blueprints" + culture, TORTextHelper.GetText("tor_enchanter_hub_blueprints_p", culture, "I would like to learn new enchantments.", true),
                () => SpokeToEnchanter, null, 200);
            obj.AddPlayerLine("enchanter_hub_donate_items_p" + culture, "enchantment_hub" + culture,
                "enchanter_donate_items_1" + culture, TORTextHelper.GetText("tor_enchanter_hub_donate_items_p", culture, "I have magical items I no longer need.", true),
                () => SpokeToEnchanter, null, 200);
            obj.AddPlayerLine("enchanter_hub_open_enchanter_p" + culture, "enchantment_hub" + culture,
                "enchanter_hub_reintro" + culture, TORTextHelper.GetText("tor_enchanter_hub_open_enchanter_p", culture, "May I use your enchanting facilities?", true),
                () => SpokeToEnchanter, () => EnchantingScreen.Open(), 200);

            obj.AddPlayerLine("enchanter_hub_return_p" + culture, "enchantment_hub" + culture, quitToken,
                TORTextHelper.GetText("tor_enchanter_hub_return_p", culture, "I must go. Farewell.", true), null, null, 200);

            // buy enchanting blueprints
            obj.AddDialogLine("enchanter_blueprints" + culture, "enchanter_blueprints" + culture,
                "enchanter_hub_reintro" + culture, TORTextHelper.GetText("tor_enchanter_blueprints", culture, "Let me show you what I have available.", true),
                () => HasAnyViableEnchanterCharacter(enchantmentSuffixes), () => OpenEnchantmentShop(enchantmentSuffixes, culture), 200);

            obj.AddDialogLine("enchanter_blueprints_decline" + culture, "enchanter_blueprints" + culture,
                "enchanter_hub_reintro" + culture, TORTextHelper.GetText("tor_enchanter_blueprints_decline", culture, "I'm afraid you have no one capable of learning these arts.", true), null, null, 200);

            // donation
            obj.AddDialogLine("enchanter_donate_items_1" + culture, "enchanter_donate_items_1" + culture,
                "enchanter_gift_items_hub" + culture, TORTextHelper.GetText("tor_enchanter_donate_items_1", culture, "I am interested in magical items from your travels. What do you have?", true),
                ConditionForDonation, null, 200);

            obj.AddDialogLine("enchanter_donate_items_1_negative" + culture, "enchanter_donate_items_1" + culture,
                "enchanter_hub_reintro" + culture, TORTextHelper.GetText("tor_enchanter_donate_items_1_negative", culture, "You have nothing of interest to me at this time.", true), null, null, 200);

            obj.AddPlayerLine("enchanter_donate_items_hub_p" + culture, "enchanter_gift_items_hub" + culture,
                "enchanter_donate_items_2" + culture, TORTextHelper.GetText("tor_enchanter_donate_items_hub_p", culture, "Let me provide you these items…", true), null,
                () => DonationMode(true), 200);

            obj.AddPlayerLine("enchanter_gift_items_hub_return_p" + culture, "enchanter_gift_items_hub" + culture,
                "enchanter_hub_reintro" + culture, TORTextHelper.GetText("tor_enchanter_gift_items_hub_return_p", culture, "I need to think about this.", true), null, null, 200);

            obj.AddDialogLine("enchanter_donate_items_2" + culture, "enchanter_donate_items_2" + culture,
                "enchanter_hub_reintro" + culture, TORTextHelper.GetText("tor_enchanter_donate_items_2", culture, "Which items do you wish to donate?", true), null, null, 200);

            // enchantment info hub
            obj.AddDialogLine("enchanter_info_hub_intro_enchanter_first_visit" + culture,
                "enchanter_info_hub_intro" + culture, "enchanter_hub_reintro" + culture,
                TORTextHelper.GetText("tor_enchanter_info_hub_intro_enchanter_first_visit", culture, "Of course! Take this, it will help you get started.", true),
                FullfillsFreeBeeCondition, () => { LearnedEnchantment = true; SpokeFirstTime(); AddBeginnerBlueprintsForEnchantment(); }, 200);

            obj.AddDialogLine("enchanter_info_hub_intro_first_visit" + culture,
                "enchanter_info_hub_intro" + culture, "enchanter_hub_reintro" + culture,
                TORTextHelper.GetText("tor_enchanter_info_hub_intro_first_visit", culture, "Of course! Here, study this manual.", true),
                () => !SpokeToEnchanter, () => SpokeFirstTime(), 200);

            obj.AddDialogLine("enchanter_info_hub_intro_visit" + culture,
                "enchanter_info_hub_intro" + culture, "enchanter_hub_reintro" + culture,
                TORTextHelper.GetText("tor_enchanter_info_hub_intro_first_visit", culture, "Of course! Here, study this manual.", true),
                () => !HasManual(), () => SpokeFirstTime(), 200);

            obj.AddDialogLine("enchanter_info_hub_intro_read_book" + culture,
                "enchanter_info_hub_intro" + culture, "enchanter_hub_reintro" + culture,
                TORTextHelper.GetText("tor_enchanter_info_hub_intro_read_book", culture, "Just read the manual I gave you.", true), null,
                () => SpokeFirstTime(), 200);
        }

        private static bool EnchanterCondition(string culture)
        {
            var partner = CharacterObject.OneToOneConversationCharacter;
            if (partner?.HeroObject == null) return false;
            if (partner.Culture.StringId != culture) return false;
            if (!SettlementMatchesEnchanterCulture(culture)) return false;
            return partner.HeroObject.IsEnchanter();
        }

        private static bool IsDirectEnchanterBlocked(string culture, Func<bool> cultureAccess)
        {
            var partner = CharacterObject.OneToOneConversationCharacter;
            if (partner?.HeroObject == null || partner.HeroObject.IsSpellTrainer()) return false;
            return partner.HeroObject.IsEnchanter() && partner.Culture.StringId == culture && !cultureAccess();
        }

        private static bool SettlementMatchesEnchanterCulture(string culture)
        {
            var settlementCulture = Settlement.CurrentSettlement?.Culture?.StringId;
            if (settlementCulture == null) return false;
            return SharesVcEnchanterBranch(culture, settlementCulture);
        }

        private static bool SharesVcEnchanterBranch(string requestedCulture, string actualCulture)
        {
            if (requestedCulture == actualCulture) return true;
            return (requestedCulture == TORConstants.Cultures.SYLVANIA && actualCulture == TORConstants.Cultures.MOUSILLON)
                || (requestedCulture == TORConstants.Cultures.MOUSILLON && actualCulture == TORConstants.Cultures.SYLVANIA);
        }
    }
}
