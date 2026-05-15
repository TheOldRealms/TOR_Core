using SandBox;
using System;
using System.Collections.Generic;
using System.Linq;
using SandBox.Missions.AgentBehaviors;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.AgentOrigins;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;
using TOR_Core.CampaignMechanics.TORCustomSettlement;
using TOR_Core.Extensions;
using TOR_Core.Utilities;

namespace TOR_Core.Missions
{
    /// <summary>
    /// Mission controller for troll cave fights.
    /// Uses custom spawning for hideout-style scene and sets up player orders directly
    /// (similar to vanilla HideoutMissionController pattern).
    /// Implements IMissionAgentSpawnLogic so BattleEndLogic can query troop counts.
    /// </summary>
    public class TrollCaveMissionController : MissionLogic, IMissionAgentSpawnLogic
    {
        private const string TrollTroopId = "tor_gs_trolls";

        private static readonly string[] PlayerSpawnTags = { "sp_player", "spawnpoint_player" };
        private static readonly string[] NpcSpawnTags = { "sp_npc_idle", "sp_npc_wait", "sp_npc_sleep", "npc_common" };

        private readonly TroopRoster _selectedTroops;
        private readonly int _trollCount;
        private readonly bool _stealthMode;
        private readonly MobileParty _defenderParty;

        private const float DefeatDelaySeconds = 3f; // Delay before forcing mission end after defeat

        private List<MatrixFrame> _enemySpawnFrames = new List<MatrixFrame>();
        private int _enemySpawnIndex;
        private bool _isMissionInitialized;
        private int _spawnedPlayerTroopCount;
        private bool _playerCanLeave;
        private bool _battleResolved;
        private BattleEndLogic _battleEndLogic;
        private bool _isPlayerDefeated;
        private float _defeatStartTime;

        public TrollCaveMissionController(TroopRoster selectedTroops, int trollCount, MobileParty defenderParty, bool stealthMode = false)
        {
            _selectedTroops = selectedTroops;
            _trollCount = trollCount;
            _defenderParty = defenderParty;
            _stealthMode = stealthMode;
        }

        public override void OnBehaviorInitialize()
        {
            base.OnBehaviorInitialize();
            // Get BattleEndLogic and disable end condition checking initially (vanilla hideout pattern)
            _battleEndLogic = Mission.GetMissionBehavior<BattleEndLogic>();
            _battleEndLogic?.ChangeCanCheckForEndCondition(false);
        }

        public override void AfterStart()
        {
            Mission.SetMissionMode(MissionMode.Battle, true);
            // CRITICAL: Must be false for order UI to work - DefaultVisualOrderProvider.IsAvailable() checks this
            Mission.IsFriendlyMission = false;
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
                return;
            }

            // Check battle resolved (vanilla hideout pattern)
            if (!_battleResolved)
            {
                CheckBattleResolved();
            }

            // Handle defeat with delay - show "Battle Lost" for a few seconds before closing
            if (_isPlayerDefeated && Mission.CurrentTime - _defeatStartTime >= DefeatDelaySeconds)
            {
                HandleForcedDefeatEnd();
            }
        }

        private void CheckBattleResolved()
        {
            // Player defeated - allow retreat or wait for auto-defeat
            if (Agent.Main == null || !Agent.Main.IsActive())
            {
                if (!_isPlayerDefeated)
                {
                    _battleResolved = true;
                    _isPlayerDefeated = true;
                    _defeatStartTime = Mission.CurrentTime;
                    // Player can manually retreat, or mission will auto-end after delay
                }
            }
            // Victory - all trolls dead
            else if (GetActiveTrollCount() == 0)
            {
                _battleResolved = true;
                _playerCanLeave = true;
                _battleEndLogic?.ChangeCanCheckForEndCondition(true);
                // Set winner to player (attacker) - this enables proper loot screen
                MapEvent.PlayerMapEvent?.SetOverrideWinner(BattleSideEnum.Attacker);
            }
        }

        private void HandleForcedDefeatEnd()
        {
            // Called after DefeatDelaySeconds from player defeat
            // Force close everything in the right order

            // 1. Finish the PlayerEncounter to prevent "trapped by enemies"
            if (PlayerEncounter.Current != null)
            {
                PlayerEncounter.Finish(true);
            }

            // 2. Show defeat menu
            GameMenu.ActivateGameMenu("trollcave_defeat");

            // 3. Force mission to end
            Mission.EndMission();

            // Reset flag so this only runs once
            _isPlayerDefeated = false;
        }

        private void InitializeMission()
        {
            // Teams are created by MissionCombatantsLogic from the MapEvent
            if (Mission.PlayerTeam == null)
            {
                Mission.PlayerTeam = Mission.AttackerTeam;
            }

            // Deployment plan setup
            Mission.DeploymentPlan.MakeDefaultDeploymentPlans();

            CollectSpawnFrames();

            SpawnPlayer();
            SpawnPlayerTroops();
            SpawnTrolls();

            // Signal deployment finished
            Mission.OnDeploymentFinished();

            // Set formation orders and PlayerOwner directly (vanilla hideout pattern)
            SetupFormationOrders();
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
            if (Agent.Main != null) return;

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

            var agentBuildData = new AgentBuildData(playerCharacter)
                .Team(Mission.PlayerTeam)
                .InitialPosition(spawnFrame.origin)
                .InitialDirection(spawnFrame.rotation.f.AsVec2.Normalized())
                .NoHorses(true)
                .ClothingColor1(Mission.PlayerTeam.Color)
                .ClothingColor2(Mission.PlayerTeam.Color2)
                .TroopOrigin(new PartyAgentOrigin(PartyBase.MainParty, playerCharacter, -1, default, false))
                .Controller(AgentControllerType.Player);

            var hero = playerCharacter.HeroObject;
            if (hero?.ClanBanner != null)
            {
                agentBuildData.Banner(hero.ClanBanner);
            }

            var agent = Mission.SpawnAgent(agentBuildData);
            agent.WieldInitialWeapons(Agent.WeaponWieldActionType.InstantAfterPickUp);
        }

        private void SpawnPlayerTroops()
        {
            if (_selectedTroops == null || _selectedTroops.TotalManCount == 0) return;

            var playerSpawnFrame = GetPlayerSpawnFrame();

            foreach (var element in _selectedTroops.GetTroopRoster())
            {
                if (element.Character.IsPlayerCharacter) continue;

                for (int i = 0; i < element.Number; i++)
                {
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
                    Mission.SpawnTroop(
                        origin,
                        isPlayerSide: true,
                        hasFormation: true,
                        spawnWithHorse: false,
                        isReinforcement: false,
                        formationTroopCount: 0,
                        formationTroopIndex: 0,
                        isAlarmed: true,
                        wieldInitialWeapons: true,
                        initialPosition: spawnPos,
                        initialDirection: playerSpawnFrame.rotation.f.AsVec2.Normalized());

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

                // Use PartyAgentOrigin with defender party so deaths sync with MapEvent
                var origin = new PartyAgentOrigin(_defenderParty.Party, trollCharacter);

                var agent = Mission.SpawnTroop(origin, false, true, false, false, 0, 0, true, true,
                    spawnPos, spawnDir);

                agent.WieldInitialWeapons(Agent.WeaponWieldActionType.InstantAfterPickUp);

                var agentFlags = agent.GetAgentFlags();
                agent.SetAgentFlags((agentFlags | AgentFlag.CanGetAlarmed) & ~AgentFlag.CanRetreat);

                if (_stealthMode)
                {
                    agent.SetWatchState(Agent.WatchState.Patrolling);
                    var campaignComponent = agent.GetComponent<CampaignAgentComponent>();
                    if (campaignComponent != null)
                    {
                        var navigator = campaignComponent.CreateAgentNavigator();
                        navigator.AddBehaviorGroup<AlarmedBehaviorGroup>().AddBehavior<CautiousBehavior>();
                    }
                }
                else
                {
                    agent.SetWatchState(Agent.WatchState.Alarmed);
                }
            }
        }

        private void SetupFormationOrders()
        {
            // Set PlayerOwner on player formations so player can command them
            // Do NOT issue automatic charge orders - let player decide tactics
            foreach (var formation in Mission.PlayerTeam.FormationsIncludingEmpty)
            {
                if (formation.CountOfUnits > 0)
                {
                    formation.SetFiringOrder(FiringOrder.FiringOrderFireAtWill);
                }
                // Set PlayerOwner so player can command this formation
                formation.PlayerOwner = Agent.Main;
            }

            // Set PlayerOrderController owner and select all formations
            // Player starts with full control, no automatic orders
            Mission.PlayerTeam.PlayerOrderController.Owner = Agent.Main;
            Mission.PlayerTeam.PlayerOrderController.SelectAllFormations();

            // Set enemy formation orders - trolls charge aggressively
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
            // Allow leaving at any time (like vanilla hideouts)
            canLeave = true;
            return null;
        }

        public override void OnRetreatMission()
        {
            // Player is retreating - finish encounter and go to appropriate menu
            // This prevents "trapped by enemies" screen

            if (PlayerEncounter.Current != null)
            {
                PlayerEncounter.Finish(true);
            }

            // If player won (all trolls dead), they're just leaving after victory
            // If trolls are still alive, treat as tactical retreat
            if (GetActiveTrollCount() > 0)
            {
                // Retreating with trolls still alive - go to retreat menu
                GameMenu.ActivateGameMenu("trollcave_retreat");
            }
            // If all trolls dead, vanilla loot/prisoner screens already shown, just leave normally

            base.OnRetreatMission();
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

            // On defeat, don't end mission here - let the timer handle it after 3 seconds
            // This is handled in HandleForcedDefeatEnd called from OnMissionTick

            if (GetActiveTrollCount() == 0)
            {
                // Player victory - let vanilla handle loot/prisoners
                missionResult = MissionResult.CreateSuccessful(Mission, false);
                return true;
            }

            return false;
        }

        // IMissionAgentSpawnLogic implementation - required for SandboxGeneralsAndCaptainsAssignmentLogic
        public void StartSpawner(BattleSideEnum side) { }
        public void StopSpawner(BattleSideEnum side) { }
        public bool IsSideSpawnEnabled(BattleSideEnum side) => false;
        public bool IsSideDepleted(BattleSideEnum side) => side == BattleSideEnum.Defender ? GetActiveTrollCount() == 0 : false;
        public float GetReinforcementInterval(BattleSideEnum side = BattleSideEnum.None)
        {
            return 0;
        }

        public float GetReinforcementInterval() => 0f;
        public bool GetSpawnHorses(BattleSideEnum side) => false;
        public int GetNumberOfPlayerControllableTroops() => _spawnedPlayerTroopCount;
        public BattleSideEnum PlayerSide => BattleSideEnum.Attacker;

        public IEnumerable<IAgentOriginBase> GetAllTroopsForSide(BattleSideEnum side) => Enumerable.Empty<IAgentOriginBase>();
    }
}
