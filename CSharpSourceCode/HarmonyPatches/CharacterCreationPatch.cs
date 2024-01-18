using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem.ViewModelCollection.CharacterCreation;
using TaleWorlds.MountAndBlade.ViewModelCollection.FaceGenerator;
using TOR_Core.CampaignMechanics.CharacterCreation;

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

        [HarmonyPostfix]
        [HarmonyPatch(typeof(FaceGenVM), "UpdateRaceAndGenderBasedResources")]
        public static void ReplaceImages(FaceGenVM __instance)
        {
            int selectedRace = __instance.RaceSelector == null ? 0 : __instance.RaceSelector.SelectedIndex;

            foreach (var item in __instance.BeardTypes)
            {
                string name = FaceGenHelper.GetBeardName(item.Index, selectedRace, __instance.SelectedGender);
                if (!string.IsNullOrEmpty(name))
                {
                    item.ImagePath = "FaceGen\\Beard\\" + name;
                }
            }

            foreach (var item in __instance.HairTypes)
            {
                string name = FaceGenHelper.GetHairName(item.Index, selectedRace, __instance.SelectedGender);
                if (!string.IsNullOrEmpty(name))
                {
                    string gender = (__instance.SelectedGender == 1) ? "Female" : "Male";
                    string hairIconName = string.Concat(new object[]
                    {
                        "FaceGen\\Hair\\",
                        gender,
                        "\\",
                        name
                    });

                    item.ImagePath = hairIconName;
                }
            }

            /* -- NO CUSTOM TATTOOS YET
            foreach (var item in __instance.TaintTypes)
            {
                string name = FaceGenHelper.GetTattooName(item.Index, selectedRace, __instance.SelectedGender);
                if (!string.IsNullOrEmpty(name))
                {
                    string gender = (__instance.SelectedGender == 1) ? "Female" : "Male";
                    string tattooIconName = string.Concat(new object[]
                    {
                        "FaceGen\\Tattoo\\",
                        gender,
                        "\\",
                        name
                    });
                    item.ImagePath = tattooIconName;
                }
            }
            */
        }
    }
}
