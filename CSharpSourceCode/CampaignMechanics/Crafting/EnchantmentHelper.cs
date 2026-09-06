using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.LinQuick;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;
using TOR_Core.AbilitySystem.Spells;
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
        return GetAllBlueprintItems()
            .WhereQ(item => item.GetTraits().Any(trait => prefixList.Any(prefix => trait.ItemTraitStringId.Contains(prefix))))
            .ToList();
    }

    /// <summary>
    /// Every blueprint item in the game, unfiltered by culture prefix. The enchanting table
    /// needs requirements for blueprints from any source, not just the ones a given town's
    /// shop happens to stock.
    /// </summary>
    internal static List<ItemObject> GetAllBlueprintItems()
    {
        return MBObjectManager.Instance.GetObjectTypeList<ItemObject>()
            .Where(item =>
                item.IsInventoryUsable() &&
                item.GetTraits().Any(trait =>
                    trait.OnInventoryUseScript != null &&
                    trait.OnInventoryUseScript.InventoryScriptName.Contains("EnchantmentBlueprintScript")))
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

    internal static bool IsBlueprintInInventory(string blueprintId) => Hero.MainHero.PartyBelongedTo.ItemRoster.Any(rosterElement =>
            TryGetBlueprintData(rosterElement.EquipmentElement.Item, out var inventoryBlueprintId, out _, out _, out _) &&
            inventoryBlueprintId == blueprintId);

    /// <summary>
    /// Heroes in the party who could *acquire* <paramref name="blueprintId"/> — i.e. who
    /// satisfy its lore/attribute restriction. This is the acquisition gate and it is the
    /// only thing left that is genuinely per-hero: you still need a Runesmith in the party to
    /// read a rune manuscript, you simply no longer need to keep them afterwards.
    ///
    /// Deliberately does <b>not</b> check the required skill. Skill is now a crafting-time
    /// requirement (<see cref="GetUnmetRequirement"/>), not a purchase-time toll, so being
    /// short of it must not stop you buying the manuscript.
    /// </summary>
    internal static List<Hero> GetEligibleHeroesForBlueprint(string blueprintId, string restriction)
    {
        if (EnchantmentBlueprints.IsKnown(blueprintId))
        {
            return [];
        }

        var eligibleHeroes = new List<Hero>();

        foreach (var hero in Hero.MainHero.PartyBelongedTo.GetMemberHeroes())
        {
            if (!SatisfiesRestriction(hero, restriction))
            {
                continue;
            }

            eligibleHeroes.Add(hero);
        }

        return eligibleHeroes;
    }

    /// <summary>
    /// True if <paramref name="hero"/> meets a blueprint's lore/attribute restriction. A null
    /// or empty restriction means the blueprint is unrestricted.
    /// </summary>
    internal static bool SatisfiesRestriction(Hero hero, string restriction)
    {
        if (string.IsNullOrEmpty(restriction)) return true;

        var info = hero.GetExtendedInfo();
        var knowsRequiredLore = info != null && info.KnownLores.Any(lore => lore != null && lore.StringId == restriction);

        return knowsRequiredLore || hero.HasAttribute(restriction);
    }

    public static bool HasAnyLearnableEnchantmentRecipe(List<string> prefixList)
    {
        foreach (var item in GetBlueprintItems(prefixList))
        {
            if (!TryGetBlueprintData(item, out var blueprintId, out _, out _, out var restriction))
            {
                continue;
            }

            if (EnchantmentBlueprints.IsKnown(blueprintId) || IsBlueprintInInventory(blueprintId))
            {
                continue;
            }

            if (GetEligibleHeroesForBlueprint(blueprintId, restriction).Any())
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// What a blueprint demands of whoever executes it: a skill threshold and, optionally, a
    /// lore/attribute restriction. Both are checked at the enchanting table.
    /// </summary>
    internal readonly record struct BlueprintRequirement(SkillObject RequiredSkill, int RequiredSkillValue, string Restriction);

    /// <summary>
    /// Every blueprint's crafting requirements, keyed by trait id.
    ///
    /// Built by scanning the whole item list, so call it once per table population rather
    /// than once per trait. Deliberately uncached: <see cref="ItemObject"/> instances are
    /// re-registered per campaign, so a static cache would go stale across a load.
    /// </summary>
    internal static Dictionary<string, BlueprintRequirement> GetBlueprintRequirements()
    {
        var requirements = new Dictionary<string, BlueprintRequirement>();

        foreach (var item in GetAllBlueprintItems())
        {
            if (!TryGetBlueprintData(item, out var blueprintId, out var requiredSkill, out var requiredSkillValue, out var restriction))
            {
                continue;
            }

            requirements[blueprintId] = new BlueprintRequirement(requiredSkill, requiredSkillValue, restriction);
        }

        return requirements;
    }

    /// <summary>
    /// Why the party cannot craft <paramref name="blueprintId"/> right now, or null if it
    /// can. Knowing a blueprint is necessary but no longer sufficient — this is the
    /// crafting-time gate that replaced the old purchase-time skill toll.
    /// </summary>
    /// <remarks>
    /// Resolved <b>best-in-party</b>: one hero must clear the restriction and the skill
    /// threshold together. Splitting them across two heroes does not count — a scholar who
    /// knows the lore cannot lend it to a smith who has the hands.
    ///
    /// NOTE FOR REVIEW — whose skill, and consistency with cost reduction. Career cost
    /// reduction is now main-hero-only (your call), while this gate is best-in-party. That is
    /// the inconsistency the proposal flagged: "consistency between the cost-reduction rule
    /// and the skill rule matters more than which one is picked." Best-in-party is used here
    /// because main-hero-only would make a hired Runesmith unable to do the one job you hired
    /// them for. Say which way you want the two unified.
    /// </remarks>
    internal static string GetUnmetRequirement(string blueprintId, BlueprintRequirement requirement)
    {
        var heroes = MobileParty.MainParty?.GetMemberHeroes();
        if (heroes == null || heroes.Count == 0) return null;

        var restrictionSatisfiedBy = heroes.WhereQ(hero => SatisfiesRestriction(hero, requirement.Restriction)).ToListQ();

        if (restrictionSatisfiedBy.Count == 0)
        {
            return BuildRestrictionRequirementText(requirement.Restriction);
        }

        if (requirement.RequiredSkill == null) return null;

        if (restrictionSatisfiedBy.Any(hero => hero.GetSkillValue(requirement.RequiredSkill) >= requirement.RequiredSkillValue))
        {
            return null;
        }

        return TORTextHelper.GetTextObject("tor_enchanting_requires_skill", "Requires {SKILL} {VALUE}.")
            .SetTextVariable("SKILL", requirement.RequiredSkill.Name)
            .SetTextVariable("VALUE", requirement.RequiredSkillValue)
            .ToString();
    }

    private static string BuildRestrictionRequirementText(string restriction)
    {
        var lore = LoreObject.GetAll().FirstOrDefault(x => x.StringId == restriction);

        return lore != null
            ? TORTextHelper.GetTextObject("tor_enchanting_requires_lore", "Requires a character who knows the Lore of {LORE}.")
                .SetTextVariable("LORE", lore.Name).ToString()
            : TORTextHelper.GetTextObject("tor_enchanting_requires_attribute", "Requires a character with {ATTRIBUTE}.")
                .SetTextVariable("ATTRIBUTE", restriction).ToString();
    }
}
