using HarmonyLib;
using SandBox.Missions.MissionLogics.Arena;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TOR_Core.Extensions;
using TOR_Core.Models;
using TOR_Core.Utilities;

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

            //Agents are spawned into a fight in the background when the player speaks to the arena master and therefore the 0 index is not necessarily the player.
            var playerPracticingOffset = __instance.IsPlayerPracticing ? 1 : 0;

            // Determine which character this is
            if (__instance.IsPlayerPracticing && spawnIndex == 0)
            {
                // Player character
                character = CharacterObject.PlayerCharacter;
            }
            else if (participantCharacters != null && spawnIndex >= playerPracticingOffset && spawnIndex < participantCharacters.Count)
            {
                // AI opponent
                character = participantCharacters[spawnIndex];
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

        /// <summary>
        /// Triggers a TOR event when the player wins the arena practice fight
        /// (defeats all 30 opponents).
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch("CheckPracticeEndedForPlayer")]
        public static void CheckPracticeEndedForPlayer_Postfix(bool __result, ArenaPracticeFightMissionController __instance)
        {
            // Only trigger if practice actually ended
            if (!__result) return;

            // Check if player won (RemainingOpponentCount == 0) rather than lost
            if (__instance.RemainingOpponentCount != 0) return;

            // Verify player is still alive and active
            if (Mission.Current?.MainAgent == null || !Mission.Current.MainAgent.IsActive()) return;
            
            // Trigger the TOR event for winning a practice fight
            TORCampaignEvents.Instance?.OnPracticeFightWon(Hero.MainHero, __instance.OpponentCountBeatenByPlayer);
        }
    }
}
