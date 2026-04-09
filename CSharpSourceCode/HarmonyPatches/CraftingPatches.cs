using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.ViewModelCollection.WeaponCrafting.WeaponDesign;
using TaleWorlds.Core;
using TOR_Core.CampaignMechanics.Crafting;
using TOR_Core.Extensions;
using TOR_Core.Models;

namespace TOR_Core.HarmonyPatches
{
    [HarmonyPatch]
    public static class CraftingPatches
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(WeaponClassSelectionPopupVM), MethodType.Constructor, typeof(ICraftingCampaignBehavior), typeof(List<CraftingTemplate>), typeof(Action<int>), typeof(Func<CraftingTemplate, int>))]
        public static void FilterCategories(ICraftingCampaignBehavior craftingBehavior, List<CraftingTemplate> templatesList, Action<int> onSelect, Func<CraftingTemplate, int> getUnlockedPiecesCount)
        {
            var backup = templatesList.ToList();
            templatesList.Clear();
            if (TORSmithingModel.ValidPlayerCraftingTemplates.Count > 0)
            {
                templatesList.AddRange(TORSmithingModel.ValidPlayerCraftingTemplates);
            }
            else
            {
                templatesList.AddRange(backup.Where(x => !TORSmithingModel.HiddenCraftingTemplateIds.Contains(x.StringId)));
            }
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
                //replace with the call to ValidTemplate
                codes.Insert(replaceIndex, new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(CraftingPatches), nameof(CraftingPatches.ValidTemplate))));
            }
            return codes.AsEnumerable();
        }

        /// <summary>
        /// Prevents the game from selecting a crafting order using an npc weapon template.
        /// </summary>
        /// <remarks>
        /// CraftingCampaignBehavior (which has calls leading to this) is added to the game starter before the smithing model, but both of them are loaded before the NewGamePartialFollowUp which the behavior has an event registered with. It's therefore safe to validate through the crafting model when this is called through CreateTownOrder.
        /// </remarks>
        /// <returns>A valid crafting template in the context of TOR</returns>
        public static CraftingTemplate ValidTemplate()
        {
            if (!TORSmithingModel.templatesValidated)
            {
                Campaign.Current.Models.GetSmithingModel().ValidateHiddenCraftingTemplates();
            }
            CraftingTemplate restrictedRandom = TORSmithingModel.ValidPlayerCraftingTemplates.GetRandomElement();
            //TORCommon.Log("Valid Template chose : " + restrictedRandom.StringId, NLog.LogLevel.Info);
            return restrictedRandom;
        }
    }
}