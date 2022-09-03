using System.Collections.Generic;
using HarmonyLib;
using TaleWorlds.MountAndBlade;
using TOR_Core.AbilitySystem;

namespace TOR_Core.Extensions
{
    public static class MissionExtensions
    {
        public static void AddMissionLogicAtIndexOf(this Mission mission, MissionLogic missionCombatantsLogic, MissionLogic torMissionCombatantsLogic)
        {
            var behaviorIndex = mission.MissionBehaviors.FindIndex(item => item.GetType() == missionCombatantsLogic.GetType());
            var logics = Traverse.Create(mission).Field("_missionLogics").GetValue() as List<MissionLogic>;
            var logicsIndex = logics.FindIndex(item => item.GetType() == missionCombatantsLogic.GetType());
            mission.RemoveMissionBehavior(missionCombatantsLogic);

            mission.AddMissionBehavior(torMissionCombatantsLogic); //TODO: Need to call this so that .mission is set on the behavior
            mission.MissionBehaviors.Remove(torMissionCombatantsLogic); //TODO: Then we remove without calling the mission.RemoveMissionBehavior, as it sets Mission to null.
            logics.Remove(torMissionCombatantsLogic);

            mission.MissionBehaviors.Insert(behaviorIndex, torMissionCombatantsLogic); //TODO: And place at right location.
            logics.Insert(logicsIndex, torMissionCombatantsLogic);
        }

        public static void RemoveMissionBehaviourIfNotNull(this Mission mission, MissionBehavior behavior)
        {
            if (behavior != null)
            {
                mission.RemoveMissionBehavior(behavior);
            }
        }

        public static int GetArtillerySlotsLeftForTeam(this Mission mission, Team team)
        {
            int slotsLeft = 0;
            var manager = mission.GetMissionBehavior<AbilityManagerMissionLogic>();
            if (manager != null)
            {
                slotsLeft = manager.GetArtillerySlotsLeftForTeam(team);
            }

            return slotsLeft;
        }
    }
}