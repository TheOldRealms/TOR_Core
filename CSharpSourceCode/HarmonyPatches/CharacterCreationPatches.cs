using System.Collections.Generic;
using HarmonyLib;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CharacterCreationContent;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.CampaignSystem.ViewModelCollection.CharacterCreation;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TOR_Core.Utilities;

namespace TOR_Core.HarmonyPatches
{
    [HarmonyPatch]
    public static class CharacterCreationPatches
    {
        // Maintain order of races added to the faction list.
        internal static readonly Dictionary<NarrativeMenuOptionArgs, TextObject> CustomPositiveEffects = new Dictionary<NarrativeMenuOptionArgs, TextObject>();

        // Flag to track if we should jump to Stage 3 (Profession) when entering narrative stage
        internal static bool ShouldJumpToProfessionStage = false;

        [HarmonyPrefix]
        [HarmonyPatch(typeof(CharacterCreationCultureStageVM), "SortCultureList")]
        public static bool DoNotSortCultures()
        {
            return false;
        }

        // Patch to override positive effect text for character creation options
        [HarmonyPatch(typeof(NarrativeMenuOptionArgs), "PositiveEffectText", MethodType.Getter)]
        public class PositiveEffectTextPatch
        {
            static bool Prefix(NarrativeMenuOptionArgs __instance, ref TextObject __result)
            {
                // Check if this args instance has custom positive effect text
                if (CustomPositiveEffects.TryGetValue(__instance, out TextObject customText))
                {
                    __result = customText;
                    return false; // Skip original method
                }

                // No custom text - return empty instead of showing "0 unspent" text
                __result = TextObject.GetEmpty();
                return false; // Skip original method
            }
        }

        // Patch CharacterCreationManager.StartNarrativeStage to jump to Stage 3 (Profession) when flag is set
        [HarmonyPatch(typeof(CharacterCreationManager), "StartNarrativeStage")]
        public class StartNarrativeStagePatch
        {
            static void Postfix(CharacterCreationManager __instance)
            {
                // Check if we should jump to profession menu (Stage 3)
                if (ShouldJumpToProfessionStage)
                {
                    TORCommon.Log($"[TORCC] StartNarrativeStage called - jumping to profession menu (tor_profession_menu)", NLog.LogLevel.Info);

                    // Get the profession menu
                    NarrativeMenu professionMenu = __instance.GetNarrativeMenuWithId("tor_profession_menu");

                    if (professionMenu != null)
                    {
                        // Use reflection to set CurrentMenu to profession menu
                        try
                        {
                            var currentMenuProperty = typeof(CharacterCreationManager).GetProperty("CurrentMenu");
                            if (currentMenuProperty != null)
                            {
                                currentMenuProperty.SetValue(__instance, professionMenu);
                                TORCommon.Log($"[TORCC] Successfully set CurrentMenu to tor_profession_menu", NLog.LogLevel.Info);
                            }
                        }
                        catch (System.Exception ex)
                        {
                            TORCommon.Log($"[TORCC] Failed to set CurrentMenu: {ex.Message}", NLog.LogLevel.Error);
                        }
                    }
                    else
                    {
                        TORCommon.Log($"[TORCC] Could not find tor_profession_menu!", NLog.LogLevel.Error);
                    }

                    // Reset the flag after using it
                    ShouldJumpToProfessionStage = false;
                }
                else
                {
                    var menuCharacters = __instance.CurrentMenu.Characters;
                    var playerCharacter = menuCharacters.FirstOrDefault();
                    playerCharacter?.SetEquipment(new MBEquipmentRoster());
                }
            }
        }

        /// <summary>
        /// Patches PopulateGainedAttributeValues to implement layer-by-layer bonus display during character creation navigation.
        /// When navigating backward through narrative stages, only shows bonuses from the current menu and earlier menus in the pathway.
        /// This prevents "future" selections (e.g., Stage 3 when viewing Stage 2) from being displayed, providing clear visual feedback
        /// of what bonuses apply at each stage. Uses pathway-based filtering: start -> tor_origin_menu -> tor_growth_menu -> tor_profession_menu.
        /// Selections remain intact in SelectedOptions, but are filtered from display calculations based on menu position in pathway.
        /// ALSO handles final review stage: applies specialization bonuses/penalties from Stage 4 to show correct final values.
        /// </summary>
        [HarmonyPatch(typeof(TaleWorlds.CampaignSystem.ViewModelCollection.CharacterCreation.CharacterCreationGainedPropertiesVM), "PopulateGainedAttributeValues")]
        public class PopulateGainedAttributeValuesPatch
        {
            static bool Prefix(TaleWorlds.CampaignSystem.ViewModelCollection.CharacterCreation.CharacterCreationGainedPropertiesVM __instance)
            {
                try
                {
                    // Get private fields via reflection
                    var managerField = typeof(TaleWorlds.CampaignSystem.ViewModelCollection.CharacterCreation.CharacterCreationGainedPropertiesVM)
                        .GetField("_characterCreationManager", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    var affectedAttributesMapField = typeof(TaleWorlds.CampaignSystem.ViewModelCollection.CharacterCreation.CharacterCreationGainedPropertiesVM)
                        .GetField("_affectedAttributesMap", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    var affectedSkillMapField = typeof(TaleWorlds.CampaignSystem.ViewModelCollection.CharacterCreation.CharacterCreationGainedPropertiesVM)
                        .GetField("_affectedSkillMap", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                    if (managerField == null || affectedAttributesMapField == null || affectedSkillMapField == null)
                        return true; // Fall back to original method

                    var manager = (CharacterCreationManager)managerField.GetValue(__instance);
                    var affectedAttributesMap = (System.Collections.IDictionary)affectedAttributesMapField.GetValue(__instance);
                    var affectedSkillMap = (System.Collections.IDictionary)affectedSkillMapField.GetValue(__instance);

                    if (manager == null) return true;

                    // Build the menu pathway: start -> stage1 -> stage2 -> stage3
                    List<string> menuPathway = new List<string> { "start", "tor_origin_menu", "tor_growth_menu", "tor_profession_menu" };

                    // Find current menu index in pathway
                    int currentMenuIndex = menuPathway.IndexOf(manager.CurrentMenu?.StringId);

                    // Log current menu for debugging
                    string currentMenuId = manager.CurrentMenu?.StringId ?? "null";

                    // Check if we're past narrative stages (Stage 4+ including final review)
                    // This flag is set when entering Stage 4 and reset when going back to stages 1-3
                    // So if it's true, we're either on Stage 4 or final review (not navigating back to 1-3)
                    bool isFinalStage = TOR_Core.CampaignMechanics.CharacterCreation.TORCharacterCreationContentHandler.IsPastNarrativeStages;

                    TORCommon.Log($"[TORCC] PopulateGainedAttributeValues: CurrentMenu = '{currentMenuId}', Index = {currentMenuIndex}, IsPastNarrativeStages = {isFinalStage}", NLog.LogLevel.Info);

                    // Iterate through selected options, but only include current and previous menus
                    foreach (KeyValuePair<NarrativeMenu, NarrativeMenuOption> selectedOption in manager.SelectedOptions)
                    {
                        NarrativeMenu menu = selectedOption.Key;
                        NarrativeMenuOption option = selectedOption.Value;

                        int menuIndex = menuPathway.IndexOf(menu.StringId);

                        // Skip menus that come AFTER the current menu in the pathway (unless we're on final stage)
                        if (!isFinalStage && menuIndex > currentMenuIndex)
                        {
                            continue;
                        }

                        // Apply the logic from the original method
                        int attributeCurrent = 0;
                        int attributePrevious = 0;
                        int focusCurrent = 0;
                        int focusPrevious = 0;

                        // On final stage, ALL bonuses from narrative stages should show as "previous" (dark green)
                        // Otherwise, check if this menu is the current menu to determine "current" vs "previous"
                        if (isFinalStage)
                        {
                            // Everything is "previous" on final stage
                            attributePrevious += option.Args.AttributeLevelToAdd;
                        }
                        else if (menu == manager.CurrentMenu)
                        {
                            attributeCurrent = option.Args.AttributeLevelToAdd;
                        }
                        else
                        {
                            attributePrevious += option.Args.AttributeLevelToAdd;
                        }

                        if (option.Args.EffectedAttribute != null)
                        {
                            var existingTuple = affectedAttributesMap.Contains(option.Args.EffectedAttribute)
                                ? (System.Tuple<int, int>)affectedAttributesMap[option.Args.EffectedAttribute]
                                : new System.Tuple<int, int>(0, 0);

                            affectedAttributesMap[option.Args.EffectedAttribute] = new System.Tuple<int, int>(
                                existingTuple.Item1 + attributePrevious,
                                existingTuple.Item2 + attributeCurrent);
                        }

                        // Same logic for focus points
                        if (isFinalStage)
                        {
                            focusPrevious += option.Args.FocusToAdd;
                        }
                        else if (menu == manager.CurrentMenu)
                        {
                            focusCurrent = option.Args.FocusToAdd;
                        }
                        else
                        {
                            focusPrevious += option.Args.FocusToAdd;
                        }

                        foreach (SkillObject skill in option.Args.AffectedSkills)
                        {
                            var existingTuple = affectedSkillMap.Contains(skill)
                                ? (System.Tuple<int, int>)affectedSkillMap[skill]
                                : new System.Tuple<int, int>(0, 0);

                            affectedSkillMap[skill] = new System.Tuple<int, int>(
                                existingTuple.Item1 + focusPrevious,
                                existingTuple.Item2 + focusCurrent);
                        }
                    }

                    // NOTE: We do NOT need to apply specialization bonuses to the display here
                    // because they are already applied to Hero.MainHero stats by ApplySpecializationBonuses()
                    // The final review screen will read from the actual Hero stats, not from SelectedOptions

                    return false; // Skip original method
                }
                catch (System.Exception ex)
                {
                    TORCommon.Log($"[TORCC] PopulateGainedAttributeValues patch failed: {ex.Message}", NLog.LogLevel.Error);
                    return true; // Fall back to original method
                }
            }

        }
    }
}