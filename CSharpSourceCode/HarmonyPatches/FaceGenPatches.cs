using HarmonyLib;
using TaleWorlds.MountAndBlade.ViewModelCollection.FaceGenerator;
using TOR_Core.CampaignMechanics.CharacterCreation;
using TOR_Core.Utilities;

namespace TOR_Core.HarmonyPatches
{
    [HarmonyPatch]
    public static class FaceGenPatches
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(FaceGenVM), "UpdateRaceAndGenderBasedResources")]
        public static void PreserveRace(FaceGenVM __instance, ref int ____selectedRace)
        {
            // IMPORTANT! Uncomment for release
            //    ____selectedRace = CharacterObject.PlayerCharacter.Race;
            //  if (__instance.RaceSelector != null) __instance.RaceSelector.SelectedIndex = CharacterObject.PlayerCharacter.Race;
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
                    item.ImagePath = TORPaths.NormalizeAssetPath("FaceGen\\Beard\\" + name);
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

                    item.ImagePath = TORPaths.NormalizeAssetPath(hairIconName);
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