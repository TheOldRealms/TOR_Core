using HarmonyLib;
using SandBox.Missions.MissionLogics.Arena;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TOR_Core.Extensions;
using TOR_Core.Models;

namespace TOR_Core.HarmonyPatches
{
    [HarmonyPatch(typeof(ArenaPracticeFightMissionController))]
    public class ArenaPracticeFightMissionController_Patch
    {
        /// <summary>
        /// For non-human races (Orcs, Goblins, Dwarfs), use weapons from GetParticipantWeapons
        /// instead of the native weapon_practice_stage characters.
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch("AddRandomWeapons")]
        public static bool AddRandomWeapons_Prefix(Equipment equipment, int spawnIndex, ArenaPracticeFightMissionController __instance)
        {
            // Get the participant characters list using reflection
            var participantCharacters = (List<CharacterObject>)AccessTools.Field(typeof(ArenaPracticeFightMissionController), "_participantCharacters").GetValue(__instance);

            CharacterObject character = null;

            // Determine which character this is
            if (spawnIndex == 0)
            {
                // Player character
                character = CharacterObject.PlayerCharacter;
            }
            else if (participantCharacters != null && spawnIndex > 0 && spawnIndex <= participantCharacters.Count)
            {
                // AI opponent
                character = participantCharacters[spawnIndex - 1];
            }

            // Check if this is a non-human race
            if (character != null && (character.IsOrc() || character.IsGoblin() || character.IsDwarf()))
            {
                // Get weapons from our custom TournamentModel implementation
                var tournamentModel = Campaign.Current.Models.TournamentModel as TORTournamentModel;
                if (tournamentModel != null)
                {
                    Equipment weaponEquipment = tournamentModel.GetParticipantWeapons(character);

                    if (weaponEquipment != null)
                    {
                        // Copy weapon slots (0-4) from our custom equipment
                        for (int i = 0; i <= 4; i++)
                        {
                            EquipmentElement weaponSlot = weaponEquipment.GetEquipmentFromSlot((EquipmentIndex)i);
                            if (weaponSlot.Item != null)
                            {
                                equipment.AddEquipmentToSlotWithoutAgent((EquipmentIndex)i, weaponSlot);
                            }
                        }
                    }
                }

                // Return false to skip the original method
                return false;
            }

            // For human races, run the original method
            return true;
        }
    }
}
