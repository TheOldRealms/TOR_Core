using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem.ViewModelCollection.CharacterCreation;

namespace TOR_Core.HarmonyPatches
{
    [HarmonyPatch]
    public static class CharacterCreationPatch
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(CharacterCreationCultureStageVM), "SortCultureList")]
        public static bool DoNotSortCultures()
        {
            return false;
        }
    }
}
