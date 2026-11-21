using HarmonyLib;
using SandBox.Tournaments.MissionLogics;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.TournamentGames;
using TaleWorlds.Core;
using TOR_Core.CharacterDevelopment;
using TOR_Core.Extensions;
using TOR_Core.Models;

namespace TOR_Core.HarmonyPatches
{
    /// <summary>
    /// Doubles the tournament max bet when the player holds HighRoller.
    /// <para><b>Problem:</b> <c>GetMaximumBet</c> lives on a non-virtual
    /// public method of <c>MissionLogic</c> and is not reachable through
    /// <c>AddModel</c>. The vanilla hardcode targets stripped vanilla perks.</para>
    /// <para><b>Fix:</b> prefix replace the whole method and key on
    /// <c>TORPerks.Roguery.HighRoller</c>. PrimaryBonus folds into
    /// <c>(1 + bonus)</c> so the cap doubles at 1.0.</para>
    /// </summary>
    [HarmonyPatch(typeof(TournamentBehavior), nameof(TournamentBehavior.GetMaximumBet))]
    public static class TournamentBehavior_GetMaximumBet_Patch
    {
        /// <summary>Returns the final bet cap and skips the vanilla body.</summary>
        public static bool Prefix(ref int __result)
        {
            int num = 150;
            if (Hero.MainHero != null
                && Hero.MainHero.GetPerkValue(TORPerks.Roguery.HighRoller))
            {
                num = (int)(num * (1f + TORPerks.Roguery.HighRoller.PrimaryBonus));
            }
            __result = num;
            return false;
        }
    }

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
