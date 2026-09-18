using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.LinQuick;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;
using TOR_Core.CharacterDevelopment;
using TOR_Core.Extensions;
using TOR_Core.Utilities;

namespace TOR_Core.CampaignMechanics.Crafting;

/// <summary>
/// Enchantment blueprint data and item creation: what blueprints exist, who in the party
/// is eligible to learn one, and building the actual enchanted <see cref="ItemObject"/>.
/// For the town-service shop UI built on top of this data, see <see cref="EnchantmentShopHelper"/>.
/// </summary>
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

    internal static List<ItemObject> GetBlueprintItems(List<string> prefixList)
    {
        return MBObjectManager.Instance.GetObjectTypeList<ItemObject>()
            .Where(item =>
                item.IsInventoryUsable() &&
                item.GetTraits().Any(trait =>
                    trait.OnInventoryUseScript != null &&
                    trait.OnInventoryUseScript.InventoryScriptName.Contains("EnchantmentBlueprintScript")))
            .WhereQ(item => item.GetTraits().Any(trait => prefixList.Any(prefix => trait.ItemTraitStringId.Contains(prefix))))
            .ToList();
    }

    internal static bool TryGetBlueprintData(ItemObject item, out string blueprintId, out SkillObject requiredSkill, out int requiredSkillValue, out string restriction)
    {
        blueprintId = null;
        requiredSkill = null;
        requiredSkillValue = 0;
        restriction = null;

        var trait = item.GetTraits().FirstOrDefault();
        if (trait?.OnInventoryUseScript == null)
        {
            return false;
        }

        var arguments = trait.OnInventoryUseScript.InventoryScriptArguments;
        if (arguments == null || arguments.Count < 3)
        {
            return false;
        }

        blueprintId = arguments[0];

        var skills = Game.Current.DefaultSkills.GetDefaultSkills();
        skills.AddRange(TORSkills.Instance.GetTorSkills());

        requiredSkill = skills.FirstOrDefault(x => x.StringId == arguments[1]);
        if (requiredSkill == null)
        {
            return false;
        }

        if (!int.TryParse(arguments[2], out requiredSkillValue))
        {
            return false;
        }

        restriction = arguments.Count > 3 ? arguments[3] : null;
        return true;
    }

    internal static bool IsBlueprintKnownByParty(string blueprintId) => Hero.MainHero.PartyBelongedTo.GetMemberHeroes().Any(hero => hero.HasKnownEnchantmentBlueprint(blueprintId));

    internal static bool IsBlueprintInInventory(string blueprintId) => Hero.MainHero.PartyBelongedTo.ItemRoster.Any(rosterElement =>
            TryGetBlueprintData(rosterElement.EquipmentElement.Item, out var inventoryBlueprintId, out _, out _, out _) &&
            inventoryBlueprintId == blueprintId);

    internal static List<Hero> GetEligibleHeroesForBlueprint(string blueprintId, SkillObject requiredSkill, int requiredSkillValue, string restriction, bool requireRequiredSkill)
    {
        var eligibleHeroes = new List<Hero>();

        foreach (var hero in Hero.MainHero.PartyBelongedTo.GetMemberHeroes())
        {
            if (hero.HasKnownEnchantmentBlueprint(blueprintId))
            {
                continue;
            }

            if (restriction != null)
            {
                var info = hero.GetExtendedInfo();
                var knowsRequiredLore = info != null && info.KnownLores.Any(lore => lore != null && lore.StringId == restriction);

                if (!knowsRequiredLore && !hero.HasAttribute(restriction))
                {
                    continue;
                }
            }

            if (requireRequiredSkill && hero.GetSkillValue(requiredSkill) < requiredSkillValue)
            {
                continue;
            }

            eligibleHeroes.Add(hero);
        }

        return eligibleHeroes;
    }

    public static bool HasAnyLearnableEnchantmentRecipe(List<string> prefixList)
    {
        foreach (var item in GetBlueprintItems(prefixList))
        {
            if (!TryGetBlueprintData(item, out var blueprintId, out var requiredSkill, out var requiredSkillValue, out var restriction))
            {
                continue;
            }

            if (IsBlueprintKnownByParty(blueprintId) || IsBlueprintInInventory(blueprintId))
            {
                continue;
            }

            if (GetEligibleHeroesForBlueprint(blueprintId, requiredSkill, requiredSkillValue, restriction, false).Any())
            {
                return true;
            }
        }

        return false;
    }
}
