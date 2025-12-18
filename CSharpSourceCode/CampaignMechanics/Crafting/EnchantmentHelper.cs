using HarmonyLib;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Core.ImageIdentifiers;
using TaleWorlds.LinQuick;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;
using TOR_Core.AbilitySystem.Spells;
using TOR_Core.CharacterDevelopment;
using TOR_Core.Extensions;
using TOR_Core.Items;
using TOR_Core.Utilities;

namespace TOR_Core.CampaignMechanics.Crafting;

public static class EnchantmentHelper
{
    public static ItemObject CreateEnchantedItem(ItemObject original, List<string> traits = null, string newName = null, bool playerCrafted = false, ItemModifier itemModifier = null)
    {
        var number = MBRandom.RandomInt();
        var id = "";
        if (itemModifier != null)
        {
            id = original.StringId + number + itemModifier.StringId;
        }
        else
        {
            id = original.StringId + number;
        }
        return CreateItemCopy(original, id, newName, playerCrafted, traits, itemModifier);
    }

    private static ItemObject CreateItemCopy(ItemObject copyFrom, string newId, string newName, bool playerCrafted, List<string> traits = null, ItemModifier itemModifier = null)
    {
        var newItem = new ItemObject();
        newItem.CopyPropertiesFrom(copyFrom);
        newItem.StringId = newId;
        AccessTools.Property(typeof(ItemObject), "Name").SetValue(newItem, new TextObject(newName));
        newItem.Initialize();
        if (playerCrafted)
        {
            ItemObject.InitAsPlayerCraftedItem(ref newItem);
        }
        newItem.DetermineItemCategoryForItem();
        MBObjectManager.Instance.RegisterObject(newItem);
        newItem.AfterInitialized();
        TORCampaignEvents.Instance.OnItemDuplicated(newItem, copyFrom, traits);

        return newItem;
    }



    public static void OpenEnchantmentRecipeShop(List<string> prefixList, string culture, bool blessings = false)
    {
        var blueprints = MBObjectManager.Instance.GetObjectTypeList<ItemObject>().Where(item =>
            item.IsInventoryUsable() &&
            item.GetTraits().Any(trait => trait.OnInventoryUseScript.InventoryScriptName.Contains("EnchantmentBlueprintScript")))
            .WhereQ(x => x.GetTraits().Any(x => prefixList.Any(prefix => x.ItemTraitStringId.Contains(prefix)))).ToList();

        var list = new List<ItemObject>();
        foreach (var hero in Hero.MainHero.PartyBelongedTo.GetMemberHeroes())
        {
            foreach (var item in blueprints)
            {
                foreach (var trait in item.GetTraits())
                {
                    var arguments = trait.OnInventoryUseScript.InventoryScriptArguments;

                    var traitId = arguments[0];

                    if (ItemTrait.All.All(x => x.ItemTraitStringId != traitId))
                    {
                        continue;
                    }

                    if (hero.HasKnownEnchantmentBlueprint(traitId))
                    {
                        continue;
                    }

                    var restriction = arguments.Count > 3 ? arguments[3] : null;
                    if (restriction != null)
                    {
                        var lore = LoreObject.GetAll().FirstOrDefault(x => x.ID == restriction);

                        if (lore != null)
                        {
                            if (!hero.HasKnownLore(restriction))
                            {
                                continue;
                            }

                        }
                        else
                        {
                            if (!hero.HasAttribute(restriction))
                            {
                                continue;
                            }
                        }
                    }
                    list.Add(item);
                }
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

            var included = false;

            var skills = Game.Current.DefaultSkills.GetDefaultSkills();
            skills.AddRange(TORSkills.Instance.GetTorSkills());
            var skill = skills.FirstOrDefault(x => x.StringId == arguments[1]);
            if (skill == null)
            {
                TORCommon.Log($"Failed to load skill '{arguments[1]}' for enchantment blueprint {item.StringId}. Skipping this item.", LogLevel.Error);
                continue;
            }
            var skillValue = 0;
            var hintText = new TextObject("{TRAIT_EFFECT}\n\n{REQUIREMENT_TEXT}\n\n{COMPLETE_COST}");
            var restriction = arguments.Count >= 4 ? arguments[3] : null;

            var id = arguments[0];

            var eligableHeroes = new List<Hero>();

            foreach (var hero in Hero.MainHero.PartyBelongedTo.GetMemberHeroes())
            {
                if (hero.HasKnownEnchantmentBlueprint(id))
                {
                    continue;
                }

                if (restriction != null && !(hero.HasKnownLore(restriction) || hero.HasAttribute(restriction)))
                {
                    continue;
                }

                eligableHeroes.Add(hero);
                included = true;
                break;
            }

            if (!included) continue;

            var enabled = true;

            foreach (var hero in eligableHeroes.ToList())
            {
                if (!int.TryParse(arguments[2], out skillValue) || hero.GetSkillValue(skill) < skillValue)
                {
                    hintText.SetTextVariable("REQUIREMENT_TEXT", "{B}You can't learn this. Requires : {B}" + skill.Name + " " + skillValue);
                    enabled = false;
                }
                else
                {
                    hintText.SetTextVariable("REQUIREMENT_TEXT", "");
                    enabled = true;
                }
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