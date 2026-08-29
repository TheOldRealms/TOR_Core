using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Core.ImageIdentifiers;
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
        var title = GameTexts.FindText("tor_enchantmentshop_title", shopVariation).ToString();
        var description = GameTexts.FindText("tor_enchantmentshop_description", shopVariation).ToString();

        var inquirydata = new MultiSelectionInquiryData(title, description, selectableItems, true, 1, 1, "Accept", "Cancel",
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

    private static List<InquiryElement> BuildInquiryElements(List<PurchasableBlueprint> blueprints)
    {
        var selectableItems = new List<InquiryElement>();
        foreach(var blueprint in blueprints)
        {
            if (!TryGetUsableTrait(blueprint.Item, out var trait))
            {
                continue;
            }

            selectableItems.Add(CreateInquiryElement(blueprint, trait));
        }

        return selectableItems;
    }

    private static InquiryElement CreateInquiryElement(PurchasableBlueprint blueprint, ItemTrait trait)
    {
        var item = blueprint.Item;
        var skill = blueprint.RequiredSkill;
        var skillValue = blueprint.RequiredSkillValue;
        var eligibleHeroes = blueprint.EligibleHeroes;

        var enabled = eligibleHeroes.Any(hero => hero.GetSkillValue(skill) >= skillValue);

        var hintText = new TextObject("{TRAIT_EFFECT}\n\n{REQUIREMENT_TEXT}\n\n{COMPLETE_COST}");
        hintText.SetTextVariable("REQUIREMENT_TEXT", enabled ? "" : BuildRequirementText(eligibleHeroes, skill, skillValue, blueprint.Restriction));

        var crCost = CalculateCustomResourceCost(skillValue);
        var goldCost = item.Value;
        enabled = ApplyAffordabilityCheck(hintText, enabled, crCost, goldCost);

        SetValidItemTypeRestrictionVariable(blueprint.BlueprintId);

        if (enabled)
        {
            hintText = new TextObject(trait.ItemTraitDescription + "\n {GOLD_VALUE}{GOLD_ICON} , {CR_VALUE}{CUSTOMRESOURCE},\n {VALIDTYPE_RESTRICTION}");
        }

        hintText.SetTextVariable("TRAIT_EFFECT", trait.ItemTraitDescription);
        hintText.SetTextVariable("COMPLETE_COST", "{GOLD_VALUE}{GOLD_ICON} , {CR_VALUE}{CUSTOMRESOURCE}");
        GameTexts.SetVariable("CR_VALUE", crCost);
        GameTexts.SetVariable("CUSTOMRESOURCE", Hero.MainHero.GetCultureSpecificCustomResource().GetCustomResourceIconAsText());
        GameTexts.SetVariable("GOLD_VALUE", item.Value);

        return new InquiryElement(new Tuple<List<Hero>, ItemObject>(eligibleHeroes, item), item.Name.ToString(), new ItemImageIdentifier(item), enabled, hintText.ToString());
    }

    private static bool TryGetUsableTrait(ItemObject item, out ItemTrait trait)
    {
        trait = item.GetTraits().FirstOrDefault();
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

    private static string BuildRequirementText(List<Hero> eligableHeroes, SkillObject skill, int skillValue, string restriction)
    {
        var requirementPrefix = GetRestrictionPrefix(restriction);

        if (eligableHeroes.Count == 1)
        {
            var hero = eligableHeroes[0];
            return hero == Hero.MainHero
                ? requirementPrefix + "You don't have enough " + skill.Name + ". Requires " + skillValue + "."
                : requirementPrefix + hero.Name + " doesn't have enough " + skill.Name + ". Requires " + skillValue + ".";
        }

        return requirementPrefix + "None of your eligible characters have enough " + skill.Name + ". Requires " + skillValue + ".";
    }

    private static string GetRestrictionPrefix(string restriction)
    {
        if (string.IsNullOrEmpty(restriction))
        {
            return "";
        }

        var lore = LoreObject.GetAll().FirstOrDefault(x => x.StringId == restriction);
        return lore != null
            ? "This enchantment is bound to the Lore of " + lore.Name + ". "
            : "This enchantment requires " + restriction + ". ";
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
            var missing = new List<string>();

            if (crCost >= Hero.MainHero.GetCultureSpecificCustomResourceValue())
            {
                missing.Add("{CUSTOMRESOURCE}");
            }

            if (goldCost >= Hero.MainHero.Gold)
            {
                missing.Add("{GOLD_ICON}");
            }

            if (missing.Any())
            {
                enabled = false;
                hintText.SetTextVariable("REQUIREMENT_TEXT", "Not enough " + string.Join(" and ", missing) + ".");
            }
        }

        return enabled;
    }

    private static void SetValidItemTypeRestrictionVariable(string blueprintId)
    {
        var underlyingTrait = ItemTrait.All.FirstOrDefault(x => x.ItemTraitStringId == blueprintId);
        if (underlyingTrait != null)
        {
            var typeRestriction = GameTexts.FindText("tor_enchantmentshop_restriction", underlyingTrait.ValidItemType.ToString()).ToString();
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
