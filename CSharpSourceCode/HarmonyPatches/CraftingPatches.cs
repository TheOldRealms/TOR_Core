using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.ViewModelCollection.WeaponCrafting.WeaponDesign;
using TaleWorlds.Core;
using TOR_Core.CampaignMechanics.Crafting;
using TOR_Core.Utilities;

namespace TOR_Core.HarmonyPatches
{
    public enum TemplateWeaponCategory
    {
        Sword,          // 1H/2H swords, rapiers
        Axe,            // 1H/2H axes
        Mace,           // 1H/2H maces, hammers
        Polearm,        // Polearms, spears, staves, scythes, lances
        OrcWeapon,      // All orc-specific templates
        Hidden          // Monster/troll/dual-wield
    }

    [HarmonyPatch]
    public static class CraftingPatches
    {
        public static List<string> HiddenCraftingTemplateIds => ["tor_large_monster_weapon_template", "tor_dual_wield_mainhand", "tor_trolltwohandedmace"];

        private static readonly Dictionary<string, TemplateWeaponCategory> TemplateCategoryMap = new()
        {
            // Orc templates
            { "tor_orc_twohandedsword_template", TemplateWeaponCategory.OrcWeapon },
            { "tor_orc_axe_template", TemplateWeaponCategory.OrcWeapon },
            { "tor_orc_sword_template", TemplateWeaponCategory.OrcWeapon },
            { "tor_orc_mace_template", TemplateWeaponCategory.OrcWeapon },
            { "tor_orc_polearm_template", TemplateWeaponCategory.OrcWeapon },

            // Hidden templates (already filtered)
            { "tor_large_monster_weapon_template", TemplateWeaponCategory.Hidden },
            { "tor_dual_wield_mainhand", TemplateWeaponCategory.Hidden },
            { "tor_trolltwohandedmace", TemplateWeaponCategory.Hidden },

            // Sword templates
            { "tor_sword_template", TemplateWeaponCategory.Sword },
            { "tor_twohandedswords_template", TemplateWeaponCategory.Sword },
            { "tor_rapier_template", TemplateWeaponCategory.Sword },

            // Axe templates
            { "tor_axe_template", TemplateWeaponCategory.Axe },
            { "tor_dwarven_axe_template", TemplateWeaponCategory.Axe },
            { "tor_twohandedaxe", TemplateWeaponCategory.Axe },

            // Mace templates
            { "tor_mace_template", TemplateWeaponCategory.Mace },
            { "tor_twohandedmace", TemplateWeaponCategory.Mace },

            // Polearm templates
            { "tor_polearm_template", TemplateWeaponCategory.Polearm },
            { "tor_staff_template", TemplateWeaponCategory.Polearm },
            { "tor_scythe_template", TemplateWeaponCategory.Polearm },
            { "tor_lance_template", TemplateWeaponCategory.Polearm }
        };

        private static readonly Dictionary<string, HashSet<TemplateWeaponCategory>> CultureAllowedCategories = new()
        {
            // Dwarfs - Axes, Hammers, Maces
            { TORConstants.Cultures.DAWI, new HashSet<TemplateWeaponCategory>
                { TemplateWeaponCategory.Axe, TemplateWeaponCategory.Mace } },

            // Wood Elves - Swords, Daggers, Polearms
            { TORConstants.Cultures.ASRAI, new HashSet<TemplateWeaponCategory>
                { TemplateWeaponCategory.Sword, TemplateWeaponCategory.Polearm } },

            // High Elves - Swords, Daggers, Polearms
            { TORConstants.Cultures.EONIR, new HashSet<TemplateWeaponCategory>
                { TemplateWeaponCategory.Sword, TemplateWeaponCategory.Polearm } },

            // Greenskins - All weapons INCLUDING Orc weapons
            { TORConstants.Cultures.GREENSKIN, new HashSet<TemplateWeaponCategory>
                { TemplateWeaponCategory.Sword, TemplateWeaponCategory.Axe,
                  TemplateWeaponCategory.Mace, TemplateWeaponCategory.Polearm,
                  TemplateWeaponCategory.OrcWeapon } },

            // Empire - All except Orc weapons
            { TORConstants.Cultures.EMPIRE, new HashSet<TemplateWeaponCategory>
                { TemplateWeaponCategory.Sword, TemplateWeaponCategory.Axe,
                  TemplateWeaponCategory.Mace, TemplateWeaponCategory.Polearm } },

            // Bretonnia - All except Orc weapons
            { TORConstants.Cultures.BRETONNIA, new HashSet<TemplateWeaponCategory>
                { TemplateWeaponCategory.Sword, TemplateWeaponCategory.Axe,
                  TemplateWeaponCategory.Mace, TemplateWeaponCategory.Polearm } }

            // Other cultures default to Empire rules (handled in ValidTemplate method)
        };

        [HarmonyPrefix]
        [HarmonyPatch(typeof(WeaponClassSelectionPopupVM), MethodType.Constructor, typeof(ICraftingCampaignBehavior), typeof(List<CraftingTemplate>), typeof(Action<int>), typeof(Func<CraftingTemplate, int>))]
        public static void FilterCategories(ICraftingCampaignBehavior craftingBehavior, List<CraftingTemplate> templatesList, Action<int> onSelect, Func<CraftingTemplate, int> getUnlockedPiecesCount)
        {
            var backup = templatesList.ToList();
            templatesList.Clear();
            templatesList.AddRange(backup.Where(x => !HiddenCraftingTemplateIds.Contains(x.StringId)));
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(CampaignGameStarter), "UnregisterNonReadyObjects")]
        public static void BeforeUnregisterNonReadyObjects()
        {
            var behavior = Campaign.Current.GetCampaignBehavior<TORArtisanDistrictCampaignBehavior>();
            behavior?.InitializeSavedCraftedItems();
        }

        /// <summary>
        /// Filters out crafting categories for npc-specific weapons when generating smithing orders on the daily tick.
        /// </summary>
        [HarmonyPatch((typeof(CraftingCampaignBehavior)), "CreateTownOrder")]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> CreateTownOrderPatch(IEnumerable<CodeInstruction> instructions, ILGenerator ilGenerator)
        {
            var codes = new List<CodeInstruction>(instructions);
            int replaceIndex = -1;

            for (var i = 0; i < codes.Count; i++)
            {
                /* div
                 * stloc (the target piece tier)
                 * call (get crafting templates)
                 * call (get random)
                 * stloc (the selected crafting template)
                */
                if (codes[i].opcode == OpCodes.Div)
                {
                    replaceIndex = i + 2;
                    break;
                }
            }

            if (replaceIndex < 0) { throw new ArgumentException("Didn't find CreateTownOrder division instruction for removing problematic crafting orders."); }
            else
            {
                //remove the CraftingTemplate.All and GetRandom calls
                codes.RemoveRange(replaceIndex, 2);
                //replace with ldarg.1 (Hero orderOwner) and call to ValidTemplate(Hero)
                codes.Insert(replaceIndex, new CodeInstruction(OpCodes.Ldarg_1));
                codes.Insert(replaceIndex + 1, new CodeInstruction(OpCodes.Call,
                    AccessTools.Method(typeof(CraftingPatches), nameof(CraftingPatches.ValidTemplate), new[] { typeof(Hero) })));
            }
            return codes.AsEnumerable();
        }

        /// <summary>
        /// Prevents the game from selecting a crafting order using an npc weapon template or culture-inappropriate weapon.
        /// </summary>
        /// <param name="orderOwner">The hero who owns the crafting order (determines settlement culture)</param>
        /// <returns>A valid crafting template in the context of TOR that matches the settlement's culture preferences</returns>
        public static CraftingTemplate ValidTemplate(Hero orderOwner)
        {
            // Safety check: if hero or settlement is null, fall back to existing behavior
            if (orderOwner?.CurrentSettlement?.Town == null)
            {
                CraftingTemplate fallbackTemplate = CraftingTemplate.All.GetRandomElementWithPredicate(
                    x => !HiddenCraftingTemplateIds.Contains(x.StringId));
                if (fallbackTemplate == null)
                {
                    throw new Exception("CraftingPatches.ValidTemplate selected a null template (fallback mode).");
                }
                return fallbackTemplate;
            }

            Town town = orderOwner.CurrentSettlement.Town;
            string cultureId = town.Culture.StringId;

            // Get allowed categories for this culture (default to Empire if not defined)
            HashSet<TemplateWeaponCategory> allowedCategories =
                CultureAllowedCategories.TryGetValue(cultureId, out var categories)
                    ? categories
                    : CultureAllowedCategories[TORConstants.Cultures.EMPIRE];

            // Filter templates: not hidden AND matches culture weapon preferences
            CraftingTemplate selectedTemplate = CraftingTemplate.All.GetRandomElementWithPredicate(template =>
            {
                // First check: exclude hidden templates (existing logic)
                if (HiddenCraftingTemplateIds.Contains(template.StringId))
                    return false;

                // Second check: culture-specific filtering
                if (TemplateCategoryMap.TryGetValue(template.StringId, out var category))
                {
                    return allowedCategories.Contains(category);
                }

                // Unknown template - allow by default (future-proofing)
                return true;
            });

            if (selectedTemplate == null)
            {
                throw new Exception($"[TOR] No valid crafting templates found for culture: {cultureId} in settlement: {town.Name}");
            }

            //TORCommon.Log($"Valid Template chose: {selectedTemplate.StringId} for culture {cultureId} in {town.Name}", NLog.LogLevel.Info);
            return selectedTemplate;
        }
    }
}