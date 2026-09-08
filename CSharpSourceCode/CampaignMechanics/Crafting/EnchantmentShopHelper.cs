using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Core.ImageIdentifiers;
using TaleWorlds.LinQuick;
using TaleWorlds.Localization;
using TOR_Core.AbilitySystem.Spells;
using TOR_Core.Extensions;
using TOR_Core.Items;
using TOR_Core.Utilities;

namespace TOR_Core.CampaignMechanics.Crafting;

/// <summary>
/// The town-service enchantment shop: builds the inquiry listing eligible blueprints (via
/// <see cref="EnchantmentHelper"/>) and applies the chosen one on purchase. No blueprint
/// eligibility/data logic lives here - see <see cref="EnchantmentHelper"/> for that.
/// </summary>
public static class EnchantmentShopHelper
{
    public static void OpenEnchantmentRecipeShop(List<string> prefixList, string culture, bool blessings = false)
    {
        var purchasableBlueprints = GetPurchasableBlueprints(prefixList);
        var selectableItems = BuildInquiryElements(purchasableBlueprints);

        var shopVariation = GetShopVariation(culture, blessings);
        var title = TORTextHelper.GetText("tor_enchantmentshop_title", shopVariation, "Make your choice…");
        var description = TORTextHelper.GetText("tor_enchantmentshop_description", shopVariation, "Select an arcane scroll to study:");

        var inquirydata = new MultiSelectionInquiryData(title, description, selectableItems, true, 1, 1, TORTextHelper.GetText("tor_inquiry_accept_text", "Accept"), TORTextHelper.GetText("tor_inquiry_cancel_text", "Cancel"),
            AddEnchantment, null, "", true);
        MBInformationManager.ShowMultiSelectionInquiry(inquirydata, true);
    }

    private readonly record struct PurchasableBlueprint(ItemObject Item, string BlueprintId, SkillObject RequiredSkill, int RequiredSkillValue, string Restriction, List<Hero> EligibleHeroes);

    private static List<PurchasableBlueprint> GetPurchasableBlueprints(List<string> prefixList)
    {
        var blueprints = EnchantmentHelper.GetBlueprintItems(prefixList);

        var list = new List<PurchasableBlueprint>();
        foreach (var item in blueprints)
        {
            if (!EnchantmentHelper.TryGetBlueprintData(item, out var blueprintId, out var requiredSkill, out var requiredSkillValue, out var restriction))
            {
                continue;
            }

            if (EnchantmentHelper.IsBlueprintKnownByParty(blueprintId) || EnchantmentHelper.IsBlueprintInInventory(blueprintId))
            {
                continue;
            }

            var eligibleHeroes = EnchantmentHelper.GetEligibleHeroesForBlueprint(blueprintId, requiredSkill, requiredSkillValue, restriction, false);
            if (eligibleHeroes.Any())
            {
                list.Add(new PurchasableBlueprint(item, blueprintId, requiredSkill, requiredSkillValue, restriction, eligibleHeroes));
            }
        }

        return list;
    }

    private static List<InquiryElement> BuildInquiryElements(List<PurchasableBlueprint> blueprints) =>
        blueprints.WhereQ(b  => IsUsableTrait(b.Item)).SelectQ(CreateInquiryElement).ToListQ();

    private static InquiryElement CreateInquiryElement(PurchasableBlueprint blueprint)
    {
        var trait = blueprint.Item.GetTraits().FirstOrDefault();

        var enabled = blueprint.EligibleHeroes.Any(hero => hero.GetSkillValue(blueprint.RequiredSkill) >= blueprint.RequiredSkillValue);

        var hintText = new TextObject("{TRAIT_EFFECT}{newline}{newline}{REQUIREMENT_TEXT}{newline}{newline}{COMPLETE_COST}");
        hintText.SetTextVariable("REQUIREMENT_TEXT", enabled ? new TextObject("{=!}") : BuildRequirementText(blueprint.EligibleHeroes, blueprint.RequiredSkill, blueprint.RequiredSkillValue, blueprint.Restriction));

        var crCost = CalculateCustomResourceCost(blueprint.RequiredSkillValue);
        var goldCost = blueprint.Item.Value;
        enabled = ApplyAffordabilityCheck(hintText, enabled, crCost, goldCost);

        SetValidItemTypeRestrictionVariable(blueprint.BlueprintId);

        if (enabled)
        {
            hintText = new TextObject(trait.ItemTraitDescription + "{newline} {GOLD_VALUE}{GOLD_ICON} , {CR_VALUE}{CUSTOMRESOURCE},{newline} {VALIDTYPE_RESTRICTION}");
        }

        hintText.SetTextVariable("TRAIT_EFFECT", trait.ItemTraitDescription);
        hintText.SetTextVariable("COMPLETE_COST", "{GOLD_VALUE}{GOLD_ICON} , {CR_VALUE}{CUSTOMRESOURCE}");
        GameTexts.SetVariable("CR_VALUE", crCost);
        GameTexts.SetVariable("CUSTOMRESOURCE", Hero.MainHero.GetCultureSpecificCustomResource().GetCustomResourceIconAsText());
        GameTexts.SetVariable("GOLD_VALUE", blueprint.Item.Value);

        return new InquiryElement(new Tuple<List<Hero>, ItemObject>(blueprint.EligibleHeroes, blueprint.Item), blueprint.Item.Name.ToString(), new ItemImageIdentifier(blueprint.Item), enabled, hintText.ToString());
    }

    private static bool IsUsableTrait(ItemObject item)
    {
         var trait = item.GetTraits().FirstOrDefault();
        if (trait == null)
        {
            TORCommon.Log($"Enchantment blueprint {item.StringId} has no traits. Skipping this item.", LogLevel.Error);
            return false;
        }

        if (trait.OnInventoryUseScript == null)
        {
            TORCommon.Log($"Enchantment blueprint {item.StringId} has no inventory use script. Skipping this item.", LogLevel.Error);
            return false;
        }

        var arguments = trait.OnInventoryUseScript.InventoryScriptArguments;
        if (arguments == null || arguments.Count < 3)
        {
            var argCount = arguments?.Count ?? 0;
            TORCommon.Log($"Enchantment blueprint {item.StringId} has insufficient arguments (expected at least 3, got {argCount})", LogLevel.Error);
            return false;
        }

        return true;
    }

    private static TextObject BuildRequirementText(List<Hero> eligableHeroes, SkillObject skill, int skillValue, string restriction)
    {
        var hero = eligableHeroes.Count == 1 ? eligableHeroes[0] : null;

        var text = hero == null
            ? TORTextHelper.GetTextObject("tor_enchantmentshop_requirement_none",
                "{RESTRICTION_PREFIX}None of your eligible characters have enough {SKILL}. Requires {VALUE}.")
            : hero == Hero.MainHero
                ? TORTextHelper.GetTextObject("tor_enchantmentshop_requirement_self",
                    "{RESTRICTION_PREFIX}You don't have enough {SKILL}. Requires {VALUE}.")
                : TORTextHelper.GetTextObject("tor_enchantmentshop_requirement_hero",
                    "{RESTRICTION_PREFIX}{HERO} doesn't have enough {SKILL}. Requires {VALUE}.");

        text.SetTextVariable("RESTRICTION_PREFIX", GetRestrictionPrefix(restriction));
        text.SetTextVariable("SKILL", skill.Name);
        text.SetTextVariable("VALUE", skillValue);

        if (hero != null)
        {
            text.SetTextVariable("HERO", hero.Name);
        }

        return text;
    }

    private static TextObject GetRestrictionPrefix(string restriction)
    {
        if (string.IsNullOrEmpty(restriction))
        {
            return new TextObject("{=!}");
        }

        var lore = LoreObject.GetAll().FirstOrDefault(x => x.StringId == restriction);
        var text = lore != null
            ? TORTextHelper.GetTextObject("tor_enchantmentshop_requirement_prefix_lore",
                "This enchantment is bound to the Lore of {LORE}. ")
            : TORTextHelper.GetTextObject("tor_enchantmentshop_requirement_prefix_other",
                "This enchantment requires {RESTRICTION}. ");

        text.SetTextVariable(lore != null ? "LORE" : "RESTRICTION", lore?.Name ?? restriction);

        return text;
    }

    private static int CalculateCustomResourceCost(int skillValue)
    {
        var factor = Hero.MainHero.GetCultureSpecificCustomResource().GetCustomResourceGeneralizedFactor();
        return (int)factor * skillValue;
    }

    private static bool ApplyAffordabilityCheck(TextObject hintText, bool enabled, int crCost, int goldCost)
    {
        if (!enabled)
        {
            return false;
        }

        if (!hintText.GetVariableValue("REQUIREMENT_TEXT", out var requirementText) ||
            requirementText != null && requirementText.ToString().IsEmpty())
        {
            var missingCustomResource = crCost >= Hero.MainHero.GetCultureSpecificCustomResourceValue();
            var missingGold = goldCost >= Hero.MainHero.Gold;

            if (missingCustomResource || missingGold)
            {
                enabled = false;

                var text = missingCustomResource && missingGold
                    ? TORTextHelper.GetTextObject("tor_enchantmentshop_insufficient_both",
                        "Not enough {CUSTOMRESOURCE} and {GOLD_ICON}.")
                    : missingCustomResource
                        ? TORTextHelper.GetTextObject("tor_enchantmentshop_insufficient_customresource",
                            "Not enough {CUSTOMRESOURCE}.")
                        : TORTextHelper.GetTextObject("tor_not_enough_gold_text",
                            "Not enough gold");

                hintText.SetTextVariable("REQUIREMENT_TEXT", text);
            }
        }

        return enabled;
    }

    private static void SetValidItemTypeRestrictionVariable(string blueprintId)
    {
        var underlyingTrait = ItemTrait.All.FirstOrDefault(x => x.ItemTraitStringId == blueprintId);
        if (underlyingTrait != null)
        {
            var typeRestriction = TORTextHelper.GetText("tor_enchantmentshop_restriction", underlyingTrait.ValidItemType.ToString(), "");
            GameTexts.SetVariable("VALIDTYPE_RESTRICTION", typeRestriction);
        }
    }

    private static string GetShopVariation(string culture, bool blessings) => blessings ? "blessings" : culture;

    private static void AddEnchantment(List<InquiryElement> inquiryElements)
    {
        var element = (Tuple<List<Hero>, ItemObject>)inquiryElements.FirstOrDefault()?.Identifier;
        if (element == null) return;

        var heroes = element.Item1;
        var item = element.Item2;
        var trait = item.GetTraits().FirstOrDefault();
        var arguments = trait.OnInventoryUseScript.InventoryScriptArguments;

        int.TryParse(arguments[2], out var skillValue);

        GrantBlueprintOrAddToInventory(heroes, item, arguments[0]);
        ChargeForPurchase(skillValue, item);
    }

    private static void GrantBlueprintOrAddToInventory(List<Hero> heroes, ItemObject item, string blueprintId)
    {
        var candidateHero = SelectRecipientHero(heroes);

        if (candidateHero != null)
        {
            candidateHero.AddEnchantmentBlueprint(blueprintId, true);
        }
        else
        {
            Hero.MainHero.PartyBelongedTo.ItemRoster.Add(new ItemRosterElement(item, 1));
            var itemAddedText = TORTextHelper.GetTextObject("tor_item_added_to_inventory_text", "{ITEM_NAME} was added to the inventory");
            itemAddedText.SetTextVariable("ITEM_NAME", item.Name);
            MBInformationManager.AddQuickInformation(itemAddedText, 0);
        }
    }

    private static Hero SelectRecipientHero(List<Hero> heroes) => heroes.Count == 1 ? heroes[0] : heroes.FirstOrDefault(x => x == Hero.MainHero);

    private static void ChargeForPurchase(int skillValue, ItemObject item)
    {
        var crCost = skillValue * Hero.MainHero.GetCultureSpecificCustomResource().GetCustomResourceGeneralizedFactor();
        Hero.MainHero.AddCultureSpecificCustomResource(-crCost);
        Hero.MainHero.ChangeHeroGold(-item.Value);
    }
}
