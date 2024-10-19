using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ViewModelCollection.CharacterCreation;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade.ViewModelCollection.FaceGenerator;
using TOR_Core.CampaignMechanics.CharacterCreation;
using TOR_Core.Utilities;

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

        [HarmonyPrefix]
        [HarmonyPatch(typeof(CharacterCreationCultureStageVM), "InitializePlayersFaceKeyAccordingToCultureSelection")]
        public static bool UpdateRaceAndFaceKey(CharacterCreationCultureVM selectedCulture)
        {
            //TODO: replace these, they are copied from vanilla
            string default_elf = "<BodyProperties version='4' age='25.84' weight='0.5000' build='0.5000'  key='000BAC088000100DB976648E6774B835537D86629511323BDCB177278A84F667017776140748B49500000000000000000000000000000000000000003EFC5002'/>";
            string default_empire = "<BodyProperties version='4' age='25.84' weight='0.5000' build='0.5000'  key='000500000000000D797664884754DCBAA35E866295A0967774414A498C8336860F7776F20BA7B7A500000000000000000000000000000000000000003CFC2002'/>";
            string default_bretonnia = "<BodyProperties version='4' age='25.84' weight='0.5000' build='0.5000'  key='001CB80CC000300D7C7664876753888A7577866254C69643C4B647398C95A0370077760307A7497300000000000000000000000000000000000000003AF47002'/>";
            string default_vc = "<BodyProperties version='4' age='25.84' weight='0.5000' build='0.5000'  key='0028C80FC000100DBA756445533377873CD1833B3101B44A21C3C5347CA32C260F7776F20BBC35E8000000000000000000000000000000000000000042F41002'/>";
            string keyValue;
            
            if(selectedCulture.Culture.StringId == TORConstants.Cultures.ASRAI || selectedCulture.Culture.StringId == TORConstants.Cultures.EONIR)
            {
                keyValue = default_elf;
                CharacterObject.PlayerCharacter.Race = FaceGen.GetRaceOrDefault("elf");
            }
            else if (selectedCulture.Culture.StringId == TORConstants.Cultures.EMPIRE)
            {
                keyValue = default_empire;
                CharacterObject.PlayerCharacter.Race = FaceGen.GetRaceOrDefault("human");
            }
            else if (selectedCulture.Culture.StringId == TORConstants.Cultures.BRETONNIA || selectedCulture.Culture.StringId == TORConstants.Cultures.MOUSILLON)
            {
                keyValue = default_bretonnia;
                CharacterObject.PlayerCharacter.Race = FaceGen.GetRaceOrDefault("human");
            }
            else if (selectedCulture.Culture.StringId == TORConstants.Cultures.SYLVANIA)
            {
                keyValue = default_vc;
                CharacterObject.PlayerCharacter.Race = FaceGen.GetRaceOrDefault("human");
            }
            else
            {
                keyValue = default_empire;
                CharacterObject.PlayerCharacter.Race = FaceGen.GetRaceOrDefault("human");
            }

            CharacterObject.PlayerCharacter.Culture = selectedCulture.Culture;

            BodyProperties properties;
            if (BodyProperties.FromString(keyValue, out properties))
            {
                CharacterObject.PlayerCharacter.UpdatePlayerCharacterBodyProperties(properties, CharacterObject.PlayerCharacter.Race, CharacterObject.PlayerCharacter.IsFemale);
            }
            
            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(FaceGenVM), "UpdateRaceAndGenderBasedResources")]
        public static void PreserveRace(FaceGenVM __instance, ref int ____selectedRace)
        {
            ____selectedRace = CharacterObject.PlayerCharacter.Race;
            if (__instance.RaceSelector != null) __instance.RaceSelector.SelectedIndex = CharacterObject.PlayerCharacter.Race;
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
