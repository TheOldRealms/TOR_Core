using HarmonyLib;
using SandBox.Tournaments.MissionLogics;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.TournamentGames;
using TaleWorlds.Core;
using TOR_Core.Extensions;
using TOR_Core.Models;

namespace TOR_Core.HarmonyPatches
{
    /// <summary>
    /// Patches tournament equipment assignment to handle race-specific equipment for greenskins.
    /// Goblins need different equipment from orcs in tournaments.
    /// </summary>
    [HarmonyPatch(typeof(TournamentFightMissionController))]
    public class TournamentFightMissionController_Patch
    {
        /// <summary>
        /// After tournament match equipment is prepared, swap equipment for any non-human ratio race to equipment relevant for their race, eg. goblin, orc, dwarf.
        /// This is analogous to ArenaPracticePatch.AddRandomWeapons_Prefix for practice fights.
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch("PrepareForMatch")]
        public static void PrepareForMatch_Postfix(TournamentFightMissionController __instance)
        {
            // Get the current match using reflection
            var matchField = AccessTools.Field(typeof(TournamentFightMissionController), "_match");
            var match = (TournamentMatch)matchField.GetValue(__instance);

            if (match == null) return;

            // Get TournamentModel
            var tournamentModel = Campaign.Current.Models.TournamentModel as TORTournamentModel;
            if (tournamentModel == null) return;

            // Check each participant
            foreach (var team in match.Teams)
            {
                foreach (var participant in team.Participants)
                {
                    if (participant.Character == null) continue;

                    //Races with racelocked gear have equipment swapped to prevent visual incongruities.
                    if (participant.Character.IsGoblin() || participant.Character.IsOrc() || participant.Character.IsDwarf())
                    {
                        Equipment racialWeapons = tournamentModel.GetParticipantWeapons(participant.Character);
                        if (racialWeapons != null)
                        {
                            // Replace weapon slots 0-4 with a racial alternative. No consideration for replacement with a similar type of equipment, eg gun replaces a bow.
                            for (int i = 0; i <= 4; i++)
                            {
                                EquipmentElement weaponSlot = racialWeapons.GetEquipmentFromSlot((EquipmentIndex)i);
                                if (weaponSlot.Item != null)
                                {
                                    participant.MatchEquipment.AddEquipmentToSlotWithoutAgent((EquipmentIndex)i, weaponSlot);
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
