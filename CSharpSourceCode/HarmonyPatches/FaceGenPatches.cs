using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.MountAndBlade.ViewModelCollection.FaceGenerator;
using TOR_Core.CampaignMechanics.CharacterCreation;
using TOR_Core.Extensions;
using TOR_Core.Utilities;

namespace TOR_Core.HarmonyPatches
{
    [HarmonyPatch]
    public static class FaceGenPatches
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(FaceGenVM), "get_CanChangeRace")]
        public static void OverrideCanChangeRace(ref bool __result)
        {
            // Override the CanChangeRace property based on config
            // When AllowFreeRaceSelection is false, hide the race selector
            if (!TORConfig.AllowFreeRaceSelection)
            {
                __result = false;
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(FaceGenVM), "get_CanChangeGender")]
        public static void OverrideCanChangeGender(ref bool __result)
        {
            // Orcs and Dwarfs cannot change gender
            var playerCharacter = CharacterObject.PlayerCharacter;
            if (playerCharacter.IsOrc() || playerCharacter.IsDwarf())
            {
                __result = false;
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(FaceGenVM), "UpdateRaceAndGenderBasedResources")]
        public static void PreserveRace(FaceGenVM __instance, ref int ____selectedRace)
        {
            if (!TORConfig.AllowFreeRaceSelection)
            {
                // Lock race to player's character race (release mode)
                ____selectedRace = CharacterObject.PlayerCharacter.Race;
                if (__instance.RaceSelector != null)
                    __instance.RaceSelector.SelectedIndex = CharacterObject.PlayerCharacter.Race;
            }
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