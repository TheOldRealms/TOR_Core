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
        var blueprints = EnchantmentHelper.GetBlueprintItems(prefixList);

        var list = new List<ItemObject>();
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

            if (EnchantmentHelper.GetEligibleHeroesForBlueprint(blueprintId, requiredSkill, requiredSkillValue, restriction, false).Any())
            {
                list.Add(item);
            }
        }

        var selectableItems = new List<InquiryElement>();
        foreach (var item in list)
        {

            var trait = item.GetTraits().FirstOrDefault();
            if (trait == null)
            {
                TORCommon.Log($"Enchantment blueprint {item.StringId} has no traits. Skipping this item.", LogLevel.Error);
                continue;
            }

            if (trait.OnInventoryUseScript == null)
            {
                TORCommon.Log($"Enchantment blueprint {item.StringId} has no inventory use script. Skipping this item.", LogLevel.Error);
                continue;
            }

            var arguments = trait.OnInventoryUseScript.InventoryScriptArguments;
            if (arguments == null || arguments.Count < 3)
            {
                var argCount = arguments?.Count ?? 0;
                TORCommon.Log($"Enchantment blueprint {item.StringId} has insufficient arguments (expected at least 3, got {argCount})", LogLevel.Error);
                continue;
            }

            var hintText = new TextObject("{TRAIT_EFFECT}\n\n{REQUIREMENT_TEXT}\n\n{COMPLETE_COST}");

            if (!EnchantmentHelper.TryGetBlueprintData(item, out var id, out var skill, out var skillValue, out var restriction))
            {
                continue;
            }

            var eligableHeroes = EnchantmentHelper.GetEligibleHeroesForBlueprint(id, skill, skillValue, restriction, false);
            if (!eligableHeroes.Any())
            {
                continue;
            }

            var learnableHeroes = eligableHeroes.Where(hero => hero.GetSkillValue(skill) >= skillValue).ToList();
            var enabled = learnableHeroes.Any();

            var requirementPrefix = "";

            if (!string.IsNullOrEmpty(restriction))
            {
                var lore = LoreObject.GetAll().FirstOrDefault(x => x.StringId == restriction);
                if (lore != null)
                {
                    requirementPrefix = "This enchantment is bound to the Lore of " + lore.Name + ". ";
                }
                else
                {
                    requirementPrefix = "This enchantment requires " + restriction + ". ";
                }
            }

            if (!enabled)
            {
                if (eligableHeroes.Count == 1)
                {
                    var hero = eligableHeroes[0];
                    if (hero == Hero.MainHero)
                    {
                        hintText.SetTextVariable("REQUIREMENT_TEXT", requirementPrefix + "You don't have enough " + skill.Name + ". Requires " + skillValue + ".");
                    }
                    else
                    {
                        hintText.SetTextVariable("REQUIREMENT_TEXT", requirementPrefix + hero.Name + " doesn't have enough " + skill.Name + ". Requires " + skillValue + ".");
                    }
                }
                else
                {
                    hintText.SetTextVariable("REQUIREMENT_TEXT", requirementPrefix + "None of your eligible characters have enough " + skill.Name + ". Requires " + skillValue + ".");
                }
            }
            else
            {
                hintText.SetTextVariable("REQUIREMENT_TEXT", "");
            }

            var crCost = 0;
            var goldCost = 0;
            var cr = Hero.MainHero.GetCultureSpecificCustomResource();
            var factor = cr.GetCustomResourceGeneralizedFactor();
            crCost = (int)factor * skillValue;

            goldCost = (int)item.Value;

            if (enabled)
            {
                if (!hintText.GetVariableValue("REQUIREMENT_TEXT", out var requirementText) ||
                    requirementText != null && requirementText.ToString().IsEmpty())
                {
                    if (crCost >= Hero.MainHero.GetCultureSpecificCustomResourceValue())
                    {
                        enabled = false;

                        hintText.SetTextVariable("REQUIREMENT_TEXT", "Not enough {CUSTOMRESOURCE}");
                    }

                    if (goldCost >= Hero.MainHero.Gold)
                    {
                        enabled = false;

                        hintText.SetTextVariable("REQUIREMENT_TEXT", "Not enough {GOLD_ICON}.");
                    }
                }

            }


            var underlyingTrait = ItemTrait.All.FirstOrDefault(x => x.ItemTraitStringId == id);

            if (underlyingTrait != null)
            {
                string typeRestriction = GameTexts.FindText("tor_enchantmentshop_restriction", underlyingTrait.ValidItemType.ToString()).ToString();
                GameTexts.SetVariable("VALIDTYPE_RESTRICTION", typeRestriction);
            }



            if (enabled)
            {

                hintText = new TextObject(trait.ItemTraitDescription + "\n {GOLD_VALUE}{GOLD_ICON} , {CR_VALUE}{CUSTOMRESOURCE},\n {VALIDTYPE_RESTRICTION}");
            }
            hintText.SetTextVariable("TRAIT_EFFECT", trait.ItemTraitDescription);
            hintText.SetTextVariable("COMPLETE_COST", "{GOLD_VALUE}{GOLD_ICON} , {CR_VALUE}{CUSTOMRESOURCE}");
            GameTexts.SetVariable("CR_VALUE", crCost);
            GameTexts.SetVariable("CUSTOMRESOURCE", Hero.MainHero.GetCultureSpecificCustomResource().GetCustomResourceIconAsText());
            GameTexts.SetVariable("GOLD_VALUE", item.Value);


            selectableItems.Add(new InquiryElement(new Tuple<List<Hero>, ItemObject>(eligableHeroes, item), item.Name.ToString(), new ItemImageIdentifier(item), enabled, hintText.ToString()));
        }

        var shopvariation = "";
        if (blessings)
        {
            shopvariation = "blessings";
        }
        else
        {
            shopvariation = culture;
        }

        var title = GameTexts.FindText("tor_enchantmentshop_title", shopvariation).ToString();
        var description = GameTexts.FindText("tor_enchantmentshop_description", shopvariation).ToString();

        var inquirydata = new MultiSelectionInquiryData(title, description, selectableItems, true, 1, 1, "Accept", "Cancel",
            AddEnchantment, null, "", true);
        MBInformationManager.ShowMultiSelectionInquiry(inquirydata, true);
    }


    private static void AddEnchantment(List<InquiryElement> inquiryElements)
    {
        var element = (Tuple<List<Hero>, ItemObject>)inquiryElements.FirstOrDefault()?.Identifier;
        if (element == null) return;

        var heroes = element.Item1;
        var item = element.Item2;
        var trait = item.GetTraits().FirstOrDefault();

        var arguments = trait.OnInventoryUseScript.InventoryScriptArguments;
        var skillValue = 0;

        int.TryParse(arguments[2], out skillValue);
        var candidateHero = heroes.Count == 1 ? heroes[0] : heroes.FirstOrDefault(x => x == Hero.MainHero);

        if (candidateHero != null)
        {
            candidateHero.AddEnchantmentBlueprint(arguments[0], true); // convenience, only one character can learn it, so we instantly apply the trait
        }
        else
        {
            Hero.MainHero.PartyBelongedTo.ItemRoster.Add(new ItemRosterElement(item, 1));   // we dont know, so we just add it to the inventory
            var itemAddedText = TORTextHelper.GetTextObject("tor_item_added_to_inventory_text", "{ITEM_NAME} was added to the inventory");
            itemAddedText.SetTextVariable("ITEM_NAME", item.Name);
            MBInformationManager.AddQuickInformation(itemAddedText, 0);
        }

        var crCost = skillValue * Hero.MainHero.GetCultureSpecificCustomResource().GetCustomResourceGeneralizedFactor();
        Hero.MainHero.AddCultureSpecificCustomResource(-crCost);
        Hero.MainHero.ChangeHeroGold(-item.Value);
    }
}
