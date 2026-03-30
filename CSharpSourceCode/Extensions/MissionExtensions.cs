using HarmonyLib;
using SandBox.Missions.MissionLogics.Arena;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TOR_Core.AbilitySystem;
using TOR_Core.HarmonyPatches;
using TOR_Core.Items;
using TOR_Core.Missions;

namespace TOR_Core.Extensions
{
    public static class MissionExtensions
    {
        public static void AddMissionLogicAtIndexOf(this Mission mission, MissionLogic missionCombatantsLogic, MissionLogic torMissionCombatantsLogic)
        {

            var behaviorIndex = mission.MissionBehaviors.FindIndex(item => item.GetType() == missionCombatantsLogic.GetType());
            var logicsIndex = mission.MissionLogics.FindIndex(item => item.GetType() == missionCombatantsLogic.GetType());
            mission.RemoveMissionBehavior(missionCombatantsLogic);

            mission.AddMissionBehavior(torMissionCombatantsLogic); //TODO: Need to call this so that .mission is set on the behavior
            mission.MissionBehaviors.Remove(torMissionCombatantsLogic); //TODO: Then we remove without calling the mission.RemoveMissionBehavior, as it sets Mission to null.
            mission.MissionLogics.Remove(torMissionCombatantsLogic);

            mission.MissionBehaviors.Insert(behaviorIndex, torMissionCombatantsLogic); //TODO: And place at right location.
            mission.MissionLogics.Insert(logicsIndex, torMissionCombatantsLogic);
        }

        public static void RemoveMissionBehaviourIfNotNull(this Mission mission, MissionBehavior behavior)
        {
            if (behavior != null)
            {
                mission.RemoveMissionBehavior(behavior);
            }
        }

        public static bool IsArenaMission(this Mission mission)
        {
            return mission.GetMissionBehavior<ArenaPracticeFightMissionController>() != null || mission.CombatType == Mission.MissionCombatType.ArenaCombat || mission.Scene.GetName().ToLowerInvariant().Contains("arena");
        }

        public static bool IsDuelMission(this Mission mission)
        {
            return mission.GetMissionBehavior<DuelFightMissionController>() != null;
        }

        public static bool IsJoustMission(this Mission mission)
        {
            return mission.GetMissionBehavior<JoustFightMissionController>() != null;
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

        public static List<Team> GetEnemyTeamsOf(this Mission mission, Team team)
        {
            return mission.Teams.Where(x => x.IsEnemyOf(team)).ToList();
        }

        public static List<Team> GetAllyTeamsOf(this Mission mission, Team team)
        {
            return mission.Teams.Where(x => x.IsFriendOf(team)).ToList();
        }

        /// <summary>
        /// Adds a custom missile with the shooter's weapon damage bonus applied.
        /// This wraps AddCustomMissile and ensures the bow/weapon damage is included in the missile damage calculation.
        /// Note: Call FirearmsMissionLogic.ApplyWeaponTraitParticles() after this if particle effects are needed.
        /// </summary>
        public static Mission.Missile AddCustomMissileWithWeaponDamage(this Mission mission, Agent shooterAgent,
            MissionWeapon missileWeapon, Vec3 position, Vec3 direction, Mat3 orientation,
            float baseSpeed, float speed, bool addRigidBody, MissionObject missionObjectToIgnore = null,
            int forcedMissileIndex = -1)
        {
            MissionPatches.UseWeaponDamageForCustomMissile = true;
            try
            {
                return mission.AddCustomMissile(shooterAgent, missileWeapon, position, direction, orientation,
                    baseSpeed, speed, addRigidBody, missionObjectToIgnore, forcedMissileIndex);
            }
            finally
            {
                MissionPatches.UseWeaponDamageForCustomMissile = false;
            }
        }
    }
}