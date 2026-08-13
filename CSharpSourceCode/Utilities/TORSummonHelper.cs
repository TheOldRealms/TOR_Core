using System;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.LinQuick;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;
using TOR_Core.AbilitySystem;

namespace TOR_Core.Utilities
{
    public static class TORSummonHelper
    {
        /// <summary>
        /// number of agent slots kept free below the engine mission cap before summoning is blocked
        /// </summary>
        public const int AgentSafetyBuffer = 150;

        /// <summary>
        /// Minimum number of agent slots that must be available before allowing summoning.
        /// </summary>
        public const int MinSlotsForSummoning = 5;

        public const float InitialSpawnAgentCapMultiplier = 1.60f; // active agents cant go beyond 1.60 times the initial spawned agent count

        /// <summary>
        /// Minimum initial troop count before the soft summon cap is applied.
        /// Below this threshold, only the hard mission limit applies.
        /// </summary>
        public const int SoftCapThreshold = 200;

        private static int _initialSpawnedTroopCount;

        private static ActionIndexCache? _actRaiseFromGround;
        private static ActionIndexCache ActRaiseFromGround
        {
            get
            {
                if (_actRaiseFromGround == null)
                    _actRaiseFromGround = ActionIndexCache.Create("act_raisefromground");
                return _actRaiseFromGround.Value;
            }
        }
        public static void ResetInitialSpawnedTroopCount()
        {
            _initialSpawnedTroopCount = 0;
        }

        public static void RegisterInitialTroopsSpawned(int troopCount)
        {
            _initialSpawnedTroopCount += troopCount;
        }

        public static int GetCurrentActiveAgentCount()
        {
            if (Mission.Current == null) return 0;
            return Mission.Current.Agents.CountQ(a => a.IsActive());
        }

        public static int GetInitialSpawnBasedAgentLimit()
        {
            return (int)MathF.Floor(_initialSpawnedTroopCount * InitialSpawnAgentCapMultiplier);
        }

        public static int GetInitialSpawnBasedAvailableSummonSlots()
        {
            return Math.Max(0, GetInitialSpawnBasedAgentLimit() - GetCurrentActiveAgentCount());
        }

        public static AgentBuildData GetAgentBuildData(Agent caster, string summonedUnitID)
        {
            BasicCharacterObject troopCharacter = MBObjectManager.Instance.GetObject<BasicCharacterObject>(summonedUnitID);

            if (troopCharacter == null) return null;

            IAgentOriginBase troopOrigin = new SummonedAgentOrigin(caster, troopCharacter);
            var formation = caster.Team.GetFormation(FormationClass.Infantry);
            if (formation == null)
            {
                formation = caster.Formation;
            }
            AgentBuildData buildData = new AgentBuildData(troopCharacter).
                Team(caster.Team).
                Formation(formation).
                ClothingColor1(caster.Team.Color).
                ClothingColor2(caster.Team.Color2).
                Equipment(troopCharacter.FirstBattleEquipment).
                TroopOrigin(troopOrigin).
                IsReinforcement(true).
                InitialDirection(Vec2.Forward);
            return buildData;
        }

        /// <summary>
        /// gets the current number of spawned agents in the mission that have not yet been deleted, includes summoned agents and mounts
        /// </summary>
        public static int GetCurrentAgentCount()
        {
            if (Mission.Current == null) return 0;
            return Mission.Current.AllAgents.Count;
        }

        public static int GetMissionAgentLimit()
        {
            return Math.Max(0, MissionAgentSpawnLogic.MaxNumberOfAgentsForMission - AgentSafetyBuffer);
        }

        /// <summary>
        /// Gets the number of available slots for summoning new agents.
        /// Soft cap only applies when initial troop count >= SoftCapThreshold.
        /// </summary>
        public static int GetAvailableSummonSlots()
        {
            var hardAvailableSlots = Math.Max(0, GetMissionAgentLimit() - GetCurrentAgentCount());

            if (_initialSpawnedTroopCount < SoftCapThreshold)
            {
                return hardAvailableSlots;
            }

            var softAvailableSlots = GetInitialSpawnBasedAvailableSummonSlots();
            return Math.Min(hardAvailableSlots, softAvailableSlots);
        }

        /// <summary>
        /// Checks if summoning is currently possible (enough slots available).
        /// </summary>
        public static bool CanSummon()
        {
            return GetAvailableSummonSlots() >= MinSlotsForSummoning;
        }

        /// <summary>
        /// Checks if a specific number of units can be summoned.
        /// </summary>
        public static bool CanSummonCount(int count)
        {
            return GetAvailableSummonSlots() >= count;
        }

        /// <summary>
        /// Gets the number of units that can actually be summoned, limited by available slots.
        /// </summary>
        public static int GetClampedSummonCount(int desiredCount)
        {
            return Math.Min(desiredCount, GetAvailableSummonSlots());
        }

        public static Agent SpawnAgent(AgentBuildData buildData, Vec3 position, bool withAnimation = false)
        {
            Agent troop = Mission.Current.SpawnAgent(buildData, false);
            Vec3 spawnPos = position;
            if (Mission.Current.Scene.GetNavigationMeshForPosition(in position) == null || Mission.Current.Scene.GetNavigationMeshForPosition(in position) == UIntPtr.Zero)
            {
                spawnPos = Mission.Current.GetRandomPositionAroundPoint(position, 0.05f, 5f, true);
            }
            troop.TeleportToPosition(spawnPos);
            troop.FadeIn();
            troop.WieldInitialWeapons();
            troop.SetWatchState(Agent.WatchState.Alarmed);
            if (withAnimation)
            {
                troop.SetActionChannel(0, ActRaiseFromGround);
                troop.SetCurrentActionProgress(0, 0f);
                troop.SetCurrentActionSpeed(0, 1f);
            }
            return troop;
        }
    }
}