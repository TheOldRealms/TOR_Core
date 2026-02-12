using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.AgentOrigins;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;
using TOR_Core.Extensions;
using TOR_Core.Utilities;

namespace TOR_Core.Missions
{
    public class TrollCaveMissionController : MissionLogic, IMissionAgentSpawnLogic
    {
        private const string TrollTroopId = "tor_gs_trolls";

        private static readonly string[] PlayerSpawnTags = { "sp_player", "spawnpoint_player" };
        private static readonly string[] NpcSpawnTags = { "sp_npc_idle", "sp_npc_wait", "sp_npc_sleep", "npc_common" };

        private readonly TroopRoster _selectedTroops;
        private readonly int _trollCount;
        private readonly Action<bool> _onMissionEnd;

        private List<MatrixFrame> _enemySpawnFrames = new List<MatrixFrame>();
        private int _enemySpawnIndex;
        private bool _isMissionInitialized;
        private int _spawnedTrollCount;
        private int _spawnedPlayerTroopCount;

        public TrollCaveMissionController(TroopRoster selectedTroops, int trollCount, Action<bool> onMissionEnd)
        {
            _selectedTroops = selectedTroops;
            _trollCount = trollCount;
            _onMissionEnd = onMissionEnd;
        }

        public override void AfterStart()
        {
            Mission.SetMissionMode(MissionMode.Battle, true);
            Mission.IsInventoryAccessible = false;
            Mission.IsQuestScreenAccessible = false;
            Mission.DoesMissionRequireCivilianEquipment = false;
        }

        public override void OnMissionTick(float dt)
        {
            if (!_isMissionInitialized)
            {
                InitializeMission();
                _isMissionInitialized = true;
            }
        }

        private void InitializeMission()
        {
            CollectSpawnFrames();
            SpawnPlayer();
            SpawnPlayerTroops();
            SpawnTrolls();
            SetupFormations();
        }

        private void CollectSpawnFrames()
        {
            _enemySpawnFrames.Clear();

            foreach (var tag in NpcSpawnTags)
            {
                var entities = Mission.Scene.FindEntitiesWithTag(tag).ToList();
                foreach (var entity in entities)
                {
                    var frame = entity.GetGlobalFrame();
                    frame.rotation.OrthonormalizeAccordingToForwardAndKeepUpAsZAxis();
                    _enemySpawnFrames.Add(frame);
                }
            }
        }

        private MatrixFrame GetPlayerSpawnFrame()
        {
            foreach (var tag in PlayerSpawnTags)
            {
                var entity = Mission.Scene.FindEntityWithTag(tag);
                if (entity != null)
                {
                    var frame = entity.GetGlobalFrame();
                    frame.rotation.OrthonormalizeAccordingToForwardAndKeepUpAsZAxis();
                    return frame;
                }
            }
            return MatrixFrame.Identity;
        }

        private MatrixFrame GetNextEnemySpawnFrame()
        {
            if (_enemySpawnFrames.Count == 0)
                return MatrixFrame.Identity;

            var frame = _enemySpawnFrames[_enemySpawnIndex % _enemySpawnFrames.Count];
            _enemySpawnIndex++;
            return frame;
        }

        private void SpawnPlayer()
        {
            var spawnFrame = GetPlayerSpawnFrame();
            if (spawnFrame == MatrixFrame.Identity)
            {
                spawnFrame.origin = new Vec3(0, 0, 0);
                spawnFrame.rotation = Mat3.Identity;
            }

            using (new TWSharedMutexReadLock(Scene.PhysicsAndRayCastLock))
            {
                spawnFrame.origin.z = Mission.Scene.GetGroundHeightAtPosition(spawnFrame.origin, BodyFlags.CommonCollisionExcludeFlags);
            }

            var playerCharacter = CharacterObject.PlayerCharacter;
            var origin = new PartyAgentOrigin(PartyBase.MainParty, playerCharacter, -1, default, false);

            // Spawn player on Attacker side (like vanilla hideout)
            var agent = Mission.SpawnTroop(origin, true, true, false, false, 0, 0, true, true, true,
                spawnFrame.origin, spawnFrame.rotation.f.AsVec2.Normalized());

            agent.Controller = AgentControllerType.Player;
            agent.WieldInitialWeapons(Agent.WeaponWieldActionType.InstantAfterPickUp);
        }

        private void SpawnPlayerTroops()
        {
            if (_selectedTroops == null || _selectedTroops.TotalManCount == 0) return;

            var playerSpawnFrame = GetPlayerSpawnFrame();

            foreach (var element in _selectedTroops.GetTroopRoster())
            {
                for (int i = 0; i < element.Number; i++)
                {
                    // Spawn near player with small offset
                    var offset = new Vec3(
                        MBRandom.RandomFloat * 4f - 2f,
                        MBRandom.RandomFloat * 4f - 2f,
                        0f);

                    var spawnPos = playerSpawnFrame.origin + offset;

                    using (new TWSharedMutexReadLock(Scene.PhysicsAndRayCastLock))
                    {
                        spawnPos.z = Mission.Scene.GetGroundHeightAtPosition(spawnPos, BodyFlags.CommonCollisionExcludeFlags);
                    }

                    var origin = new PartyAgentOrigin(PartyBase.MainParty, element.Character);

                    // Spawn on Attacker side (player's side)
                    var agent = Mission.SpawnTroop(origin, true, true, false, false, 0, 0, true, true, false,
                        spawnPos, playerSpawnFrame.rotation.f.AsVec2.Normalized());

                    agent.WieldInitialWeapons(Agent.WeaponWieldActionType.InstantAfterPickUp);
                    _spawnedPlayerTroopCount++;
                }
            }
        }

        private void SpawnTrolls()
        {
            var trollCharacter = MBObjectManager.Instance.GetObject<CharacterObject>(TrollTroopId);
            if (trollCharacter == null) return;

            for (int i = 0; i < _trollCount; i++)
            {
                var frame = GetNextEnemySpawnFrame();
                Vec3 spawnPos;
                Vec2 spawnDir;

                if (frame != MatrixFrame.Identity)
                {
                    spawnPos = frame.origin;
                    spawnDir = frame.rotation.f.AsVec2.Normalized();
                }
                else if (Agent.Main != null)
                {
                    // Fallback: spawn away from player
                    spawnPos = Agent.Main.Position + new Vec3(
                        MBRandom.RandomFloat * 20f - 10f,
                        MBRandom.RandomFloat * 20f - 10f,
                        0f);
                    spawnDir = Vec2.Forward;
                }
                else
                {
                    continue;
                }

                using (new TWSharedMutexReadLock(Scene.PhysicsAndRayCastLock))
                {
                    spawnPos.z = Mission.Scene.GetGroundHeightAtPosition(spawnPos, BodyFlags.CommonCollisionExcludeFlags);
                }

                var origin = new SimpleAgentOrigin(trollCharacter);

                // Spawn on Defender side (enemy side)
                var agent = Mission.SpawnTroop(origin, false, false, false, false, 0, 0, false, false, false,
                    spawnPos, spawnDir);

                agent.WieldInitialWeapons(Agent.WeaponWieldActionType.InstantAfterPickUp);
                agent.SetWatchState(Agent.WatchState.Alarmed);

                _spawnedTrollCount++;
            }
        }

        private void SetupFormations()
        {
            // Setup player team formations for command
            foreach (var formation in Mission.AttackerTeam.FormationsIncludingEmpty)
            {
                if (formation.CountOfUnits > 0)
                {
                    formation.SetMovementOrder(MovementOrder.MovementOrderCharge);
                    formation.SetFiringOrder(FiringOrder.FiringOrderFireAtWill);

                    if (Mission.AttackerTeam == Mission.PlayerTeam)
                    {
                        formation.PlayerOwner = Agent.Main;
                    }
                }
            }

            // Setup enemy formations
            foreach (var formation in Mission.DefenderTeam.FormationsIncludingEmpty)
            {
                if (formation.CountOfUnits > 0)
                {
                    formation.SetMovementOrder(MovementOrder.MovementOrderCharge);
                    formation.SetFiringOrder(FiringOrder.FiringOrderFireAtWill);
                }
            }
        }

        public override InquiryData OnEndMissionRequest(out bool canLeave)
        {
            canLeave = Mission.MissionResult != null && Mission.MissionResult.BattleResolved;
            if (!canLeave)
            {
                MBInformationManager.AddQuickInformation(
                    TORTextHelper.GetTextObject("tor_trollcave_cannot_leave", "You cannot leave until the trolls are dealt with!"));
            }
            return null;
        }

        private int GetActiveTrollCount()
        {
            int count = 0;
            foreach (var agent in Mission.Agents)
            {
                if (agent.IsHuman && agent.IsActive() && agent.Team == Mission.DefenderTeam)
                    count++;
            }
            return count;
        }

        public override bool MissionEnded(ref MissionResult missionResult)
        {
            if (!_isMissionInitialized) return false;

            // Player died
            if (Agent.Main == null || !Agent.Main.IsActive())
            {
                missionResult = MissionResult.CreateDefeated(Mission);
                _onMissionEnd?.Invoke(false);
                return true;
            }

            // All trolls dead - victory
            if (GetActiveTrollCount() == 0)
            {
                missionResult = MissionResult.CreateSuccessful(Mission, false);
                _onMissionEnd?.Invoke(true);
                return true;
            }

            return false;
        }

        // IMissionAgentSpawnLogic implementation
        public void StartSpawner(BattleSideEnum side) { }
        public void StopSpawner(BattleSideEnum side) { }
        public bool IsSideSpawnEnabled(BattleSideEnum side) => false;
        public bool IsSideDepleted(BattleSideEnum side) => side == BattleSideEnum.Defender ? GetActiveTrollCount() == 0 : false;
        public float GetReinforcementInterval() => 0f;
        public bool GetSpawnHorses(BattleSideEnum side) => false;
        public int GetNumberOfPlayerControllableTroops() => _spawnedPlayerTroopCount;
        public IEnumerable<IAgentOriginBase> GetAllTroopsForSide(BattleSideEnum side) => Enumerable.Empty<IAgentOriginBase>();
    }
}
