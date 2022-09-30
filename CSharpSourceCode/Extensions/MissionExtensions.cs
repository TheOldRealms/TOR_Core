using TaleWorlds.MountAndBlade;
using TOR_Core.AbilitySystem;

namespace TOR_Core.Extensions
{
    public static class MissionExtensions
    {
        
        public static bool IsPlayerInSpellCasterMode(this Mission mission)
        {
            var abilityManagerMissionLogic = mission.GetMissionBehavior<AbilityManagerMissionLogic>();
            return abilityManagerMissionLogic.CurrentState == AbilityModeState.Casting ||
                   abilityManagerMissionLogic.CurrentState == AbilityModeState.Casting;
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
