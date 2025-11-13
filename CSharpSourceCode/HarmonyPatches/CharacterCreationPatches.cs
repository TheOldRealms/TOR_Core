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
    }
}
