using System.Collections.Generic;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CharacterCreationContent;
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
            }
        }

        // Patch PopulateGainedAttributeValues to only show bonuses from current menu and earlier menus (not future ones)
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

                    TORCommon.Log($"[TORCC] PopulateGainedAttributeValues: Current menu = {manager.CurrentMenu?.StringId}, index = {currentMenuIndex}", NLog.LogLevel.Info);

                    // Iterate through selected options, but only include current and previous menus
                    foreach (KeyValuePair<NarrativeMenu, NarrativeMenuOption> selectedOption in manager.SelectedOptions)
                    {
                        NarrativeMenu menu = selectedOption.Key;
                        NarrativeMenuOption option = selectedOption.Value;

                        int menuIndex = menuPathway.IndexOf(menu.StringId);

                        // Skip menus that come AFTER the current menu in the pathway
                        if (menuIndex > currentMenuIndex)
                        {
                            TORCommon.Log($"[TORCC]   Skipping future menu: {menu.StringId} (index {menuIndex} > current {currentMenuIndex})", NLog.LogLevel.Info);
                            continue;
                        }

                        TORCommon.Log($"[TORCC]   Including menu: {menu.StringId} (index {menuIndex} <= current {currentMenuIndex})", NLog.LogLevel.Info);

                        // Apply the logic from the original method
                        int attributeCurrent = 0;
                        int attributePrevious = 0;
                        int focusCurrent = 0;
                        int focusPrevious = 0;

                        if (menu == manager.CurrentMenu)
                            attributeCurrent = option.Args.AttributeLevelToAdd;
                        else
                            attributePrevious += option.Args.AttributeLevelToAdd;

                        if (option.Args.EffectedAttribute != null)
                        {
                            var existingTuple = affectedAttributesMap.Contains(option.Args.EffectedAttribute)
                                ? (System.Tuple<int, int>)affectedAttributesMap[option.Args.EffectedAttribute]
                                : new System.Tuple<int, int>(0, 0);

                            affectedAttributesMap[option.Args.EffectedAttribute] = new System.Tuple<int, int>(
                                existingTuple.Item1 + attributePrevious,
                                existingTuple.Item2 + attributeCurrent);
                        }

                        if (menu == manager.CurrentMenu)
                            focusCurrent = option.Args.FocusToAdd;
                        else
                            focusPrevious += option.Args.FocusToAdd;

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