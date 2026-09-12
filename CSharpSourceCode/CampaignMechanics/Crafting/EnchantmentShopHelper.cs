using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Core.ImageIdentifiers;
using TaleWorlds.LinQuick;
using TaleWorlds.Localization;
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

            if (EnchantmentBlueprints.IsKnown(blueprintId) || EnchantmentHelper.IsBlueprintInInventory(blueprintId))
            {
                continue;
            }

            var eligibleHeroes = EnchantmentHelper.GetEligibleHeroesForBlueprint(blueprintId, restriction);
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

        var crCost = CalculateCustomResourceCost(blueprint.RequiredSkillValue);
        var goldCost = blueprint.Item.Value;

        var unaffordable = BuildUnaffordableText(crCost, goldCost);
        var enabled = unaffordable == null;

        SetValidItemTypeRestrictionVariable(blueprint.BlueprintId);
        GameTexts.SetVariable("CR_VALUE", crCost);
        GameTexts.SetVariable("CUSTOMRESOURCE", Hero.MainHero.GetCultureSpecificCustomResource().GetCustomResourceIconAsText());
        GameTexts.SetVariable("GOLD_VALUE", goldCost);

        var notice = unaffordable ?? BuildFutureRequirementText(blueprint);

        var hintText = string.IsNullOrEmpty(notice)
            ? new TextObject(trait.ItemTraitDescription + "\n {GOLD_VALUE}{GOLD_ICON} , {CR_VALUE}{CUSTOMRESOURCE},\n {VALIDTYPE_RESTRICTION}")
            : new TextObject("{TRAIT_EFFECT}\n\n{REQUIREMENT_TEXT}\n\n{COMPLETE_COST}");

        hintText.SetTextVariable("REQUIREMENT_TEXT", notice);
        hintText.SetTextVariable("TRAIT_EFFECT", trait.ItemTraitDescription);
        hintText.SetTextVariable("COMPLETE_COST", "{GOLD_VALUE}{GOLD_ICON} , {CR_VALUE}{CUSTOMRESOURCE}");

        return new InquiryElement(new Tuple<List<Hero>, ItemObject>(blueprint.EligibleHeroes, blueprint.Item), blueprint.Item.Name.ToString(), new ItemImageIdentifier(blueprint.Item), enabled, hintText.ToString());
    }

    /// <summary>
    /// The skill this blueprint will demand at the enchanting table, phrased as a heads-up
    /// rather than a refusal. Returns empty when the party can already execute it, so the
    /// hint stays clean for blueprints that need no warning.
    /// </summary>
    private static string BuildFutureRequirementText(PurchasableBlueprint blueprint)
    {
        var requirement = new EnchantmentHelper.BlueprintRequirement(blueprint.RequiredSkill, blueprint.RequiredSkillValue, blueprint.Restriction);
        var unmet = EnchantmentHelper.GetUnmetRequirement(blueprint.BlueprintId, requirement);

        if (unmet == null) return "";

        return TORTextHelper.GetTextObject("tor_enchantmentshop_future_requirement", "You can learn this now, but cannot enchant with it yet. {REQUIREMENT}")
            .SetTextVariable("REQUIREMENT", unmet)
            .ToString();
    }

    /// <summary>
    /// Why the player cannot afford this blueprint, or null if they can.
    /// </summary>
    private static string BuildUnaffordableText(int crCost, int goldCost)
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

        return missing.Any() ? "Not enough " + string.Join(" and ", missing) + "." : null;
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

    private static int CalculateCustomResourceCost(int skillValue)
    {
        var factor = Hero.MainHero.GetCultureSpecificCustomResource().GetCustomResourceGeneralizedFactor();
        return (int)factor * skillValue;
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
            EnchantmentBlueprints.Learn(blueprintId, candidateHero, true);
        }
        else
        {
            Hero.MainHero.PartyBelongedTo.ItemRoster.Add(new ItemRosterElement(item, 1));
            var itemAddedText = TORTextHelper.GetTextObject("tor_item_added_to_inventory_text", "{ITEM_NAME} was added to the inventory");
            itemAddedText.SetTextVariable("ITEM_NAME", item.Name);
            MBInformationManager.AddQuickInformation(itemAddedText, 0);
        }
    }

    /// <summary>
    /// Who is shown as having learned the blueprint. Storage is campaign-wide now, so this
    /// only picks a face for the notification and the learned event - it cannot lose the
    /// blueprint the way the old per-hero write could.
    /// </summary>
    /// <remarks>
    /// Prefers the main hero when several are eligible, falling back to the first eligible
    /// hero rather than returning null. The old version returned null whenever the main hero
    /// was not among 2+ eligible heroes, which silently turned a paid-for blueprint into an
    /// inventory item; that bug is gone because there is always a valid attribution.
    /// </remarks>
    private static Hero SelectRecipientHero(List<Hero> heroes)
    {
        if (heroes == null || heroes.Count == 0) return null;
        return heroes.FirstOrDefault(x => x == Hero.MainHero) ?? heroes[0];
    }

    private static void ChargeForPurchase(int skillValue, ItemObject item)
    {
        var crCost = skillValue * Hero.MainHero.GetCultureSpecificCustomResource().GetCustomResourceGeneralizedFactor();
        Hero.MainHero.AddCultureSpecificCustomResource(-crCost);
        Hero.MainHero.ChangeHeroGold(-item.Value);
    }
}
