using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TOR_Core.Extensions;
using TOR_Core.Models;

namespace TOR_Core.Missions.AmbushMission
{
    /// <summary>
    /// Battle setup for the ambush mission. Clones the useful parts of vanilla
    /// SallyOutMissionController (reinforcement timer, attacker-activation
    /// deployment delay, troop-ratio balancing) and skips the castle-gate code
    /// that doesn't apply to a field ambush.
    ///
    /// All tune-ables pulled from <see cref="TORMissionAmbushModel"/>.
    /// </summary>
    public class AmbushMissionController : MissionLogic
    {
        // Grace window before we decide the scene has no authored retreat zone
        // and set a procedural fallback. ScriptComponentBehavior.OnInit on
        // AmbushRetreatZone isn't guaranteed to run before OnDeploymentFinished,
        // so checking immediately races with the engine and can turn a scene
        // that DOES have an authored zone into "fallback at player position"
        // (= instant extraction victory as soon as the player stands still).
        private const float FallbackGraceSeconds = 2f;

        private DefaultBattleMissionAgentSpawnLogic _spawnLogic;
        private BasicMissionTimer _aiActivationTimer;
        private BasicMissionTimer _fallbackGraceTimer;
        private bool _aiActivated;
        private bool _fallbackResolved;
        private TORMissionAmbushModel _model;
        private readonly int _playerTroopCount;
        private readonly int _targetTroopCount;

        public AmbushMissionController(int playerTroopCount, int targetTroopCount)
        {
            _playerTroopCount = playerTroopCount;
            _targetTroopCount = targetTroopCount;
        }

        public override void OnBehaviorInitialize()
        {
            Debug.Print("[AmbushMission] OnBehaviorInitialize enter");
            base.OnBehaviorInitialize();
            // DO NOT call AmbushRetreatZone.ResetRegistry() here. Scene scripts
            // (including AmbushRetreatZone.OnInit) fire during scene load, which
            // happens before MissionLogic.OnBehaviorInitialize. Clearing the
            // registry at this point wipes the zone that just registered —
            // every subsequent IsAgentInsideAnyZone check falls through to the
            // procedural fallback. Reset is done by the campaign behavior
            // immediately before MissionState.OpenNew so stale zones from any
            // prior scene are cleared BEFORE this scene's scripts fire.
            _spawnLogic = Mission.GetMissionBehavior<DefaultBattleMissionAgentSpawnLogic>();
            _model = Campaign.Current?.Models?.GetMissionAmbushModel() ?? new TORMissionAmbushModel();
            AmbushRetreatZone.ProceduralFallbackRadius = _model.ProceduralFallbackRadius;
            Debug.Print("[AmbushMission] OnBehaviorInitialize done (spawnLogic=" + (_spawnLogic != null) + ", activeZones=" + AmbushRetreatZone.ActiveZones.Count + ")");
        }

        public override void AfterStart()
        {
            Debug.Print("[AmbushMission] AfterStart enter");
            base.AfterStart();
            SetupInitialSpawn();
            Debug.Print("[AmbushMission] AfterStart done");
        }

        public override void OnDeploymentFinished()
        {
            Debug.Print("[AmbushMission] OnDeploymentFinished enter");
            base.OnDeploymentFinished();

            // Teleport the just-spawned initial batch onto the scener-tagged
            // positions. Attacker (player) → `ambush_attacker` points (cover /
            // overlook). Defender (ambushed patrol) → `dynamic_patrol_area_tag`
            // points (the road / clearing). Reinforcement spawns are untouched
            // — they stay on the sp_*_reinforcement children of sp_battle_set,
            // which is the user's intended "current spawners become reinforcement
            // spawners" model.
            TeleportTeamToTag(Mission.AttackerTeam, "ambush_attacker");
            TeleportTeamToTag(Mission.DefenderTeam, "dynamic_patrol_area_tag");

            // Mirror vanilla's "besieger activation" shape: defender team (our
            // AI opponent) is frozen for AiActivationDelay seconds so the player
            // gets a clean opening.
            FreezeDefenderAi();
            _aiActivationTimer = new BasicMissionTimer();

            // Start the grace clock for the procedural-fallback decision. The
            // actual check happens in OnMissionTick once script OnInits have
            // reliably fired — doing it here races with the script lifecycle
            // and plants the fallback on the player, which auto-triggers
            // extraction. See FallbackGraceSeconds.
            _fallbackGraceTimer = new BasicMissionTimer();
        }

        private void TeleportTeamToTag(Team team, string tag)
        {
            if (team == null) return;
            var entities = Mission.Scene.FindEntitiesWithTag(tag).ToList();
            if (entities.Count == 0)
            {
                Debug.Print("[AmbushMission] TeleportTeamToTag: no entities with tag " + tag);
                return;
            }
            // Collect world-space positions once.
            var positions = new List<Vec3>(entities.Count);
            foreach (var e in entities)
            {
                positions.Add(e.GetGlobalFrame().origin);
            }
            // Shuffle so repeat ambushes don't always deposit the same unit at
            // the same point.
            for (int i = positions.Count - 1; i > 0; i--)
            {
                int j = MBRandom.RandomInt(i + 1);
                (positions[i], positions[j]) = (positions[j], positions[i]);
            }
            // Round-robin across positions when there are more agents than tags.
            int idx = 0;
            foreach (var agent in team.ActiveAgents.ToList())
            {
                if (agent == null || !agent.IsActive() || !agent.IsHuman) continue;
                agent.TeleportToPosition(positions[idx % positions.Count]);
                idx++;
            }
            Debug.Print("[AmbushMission] TeleportTeamToTag(" + tag + "): placed " + idx + " agents across " + positions.Count + " tagged points");
        }

        public override void OnMissionTick(float dt)
        {
            base.OnMissionTick(dt);
            if (!_aiActivated && _aiActivationTimer != null
                && _aiActivationTimer.ElapsedTime >= _model.AiActivationDelay)
            {
                UnfreezeDefenderAi();
                _aiActivated = true;
            }
            if (!_fallbackResolved && _fallbackGraceTimer != null
                && _fallbackGraceTimer.ElapsedTime >= FallbackGraceSeconds)
            {
                _fallbackResolved = true;
                if (AmbushRetreatZone.ActiveZones.Count == 0)
                {
                    var mainAgent = Mission.MainAgent;
                    if (mainAgent != null && mainAgent.IsActive())
                    {
                        AmbushRetreatZone.ProceduralFallbackPosition = mainAgent.Position;
                        Debug.Print("[AmbushMission] No authored zones after grace; procedural fallback at " + mainAgent.Position);
                    }
                }
                else
                {
                    Debug.Print("[AmbushMission] Authored zones present: " + AmbushRetreatZone.ActiveZones.Count + "; procedural fallback skipped.");
                }
            }
        }

        private void SetupInitialSpawn()
        {
            Debug.Print("[AmbushMission] SetupInitialSpawn (playerTroops=" + _playerTroopCount + " targetTroops=" + _targetTroopCount + ")");
            if (_spawnLogic == null)
            {
                Debug.Print("[AmbushMission] SetupInitialSpawn: _spawnLogic is null, aborting");
                return;
            }

            // Mirror vanilla's AdjustTotalTroopCounts shape: clamp both sides
            // against the battle-size budget split, then rebalance so one side's
            // roster doesn't dwarf the other's spawn allocation.
            int besiegedTotal = _targetTroopCount;
            int besiegerTotal = _playerTroopCount;

            float ratio = _model.TargetTotalTroopRatio;
            float inverse = 1f - ratio;
            int besiegedCap = (int)(_spawnLogic.BattleSize * ratio);
            int besiegerCap = (int)(_spawnLogic.BattleSize * inverse);
            if (besiegedTotal > besiegedCap) besiegedTotal = besiegedCap;
            if (besiegerTotal > besiegerCap) besiegerTotal = besiegerCap;

            int sum = besiegedTotal + besiegerTotal;
            // Defender (ambushed patrol): the entire visible patrol spawns upfront.
            // Fiction is "the patrol you're ambushing" — a 10% scout wave feels
            // like 3 lost scouts, not the actual force. Reinforcements then come
            // in as "second patrol responding to the alarm" waves on top of this.
            // There are 54 dynamic_patrol_area_tag points to receive them.
            int defenderInitial = besiegedTotal;
            // Attacker (player ambush party): entire force spawns upfront. There
            // are no player reinforcements — the ambush is a fixed-roster strike,
            // intended to be preselected like TrollCave later. Cap only against
            // the battle-size slice we already clamped above.
            int attackerInitial = besiegerTotal;

            _spawnLogic.SetSpawnHorses(BattleSideEnum.Defender, spawnHorses: true);
            _spawnLogic.SetSpawnHorses(BattleSideEnum.Attacker, spawnHorses: false);

            var spawnSettings = new MissionSpawnSettings(
                MissionSpawnSettings.InitialSpawnMethod.FreeAllocation,
                MissionSpawnSettings.ReinforcementTimingMethod.CustomTimer,
                MissionSpawnSettings.ReinforcementSpawnMethod.Fixed,
                0f, 0f, 0f, 0f, 0,
                defenderReinforcementBatchPercentage: _model.ReinforcementBatchFraction,
                attackerReinforcementBatchPercentage: _model.PlayerReinforcementBatchFraction);

            _spawnLogic.InitWithSinglePhase(
                besiegedTotal, besiegerTotal,
                defenderInitial, attackerInitial,
                spawnDefenders: false, spawnAttackers: false,
                in spawnSettings);

            _spawnLogic.SetCustomReinforcementSpawnTimer(new AmbushReinforcementSpawnTimer(_model));
        }

        private void FreezeDefenderAi()
        {
            if (Mission.DefenderTeam == null) return;
            foreach (var formation in Mission.DefenderTeam.FormationsIncludingSpecialAndEmpty)
            {
                formation.SetMovementOrder(MovementOrder.MovementOrderStop);
                formation.SetFiringOrder(FiringOrder.FiringOrderHoldYourFire);
                formation.SetControlledByAI(isControlledByAI: false);
            }
        }

        private void UnfreezeDefenderAi()
        {
            if (Mission.DefenderTeam == null) return;
            foreach (var formation in Mission.DefenderTeam.FormationsIncludingSpecialAndEmpty)
            {
                formation.SetControlledByAI(isControlledByAI: true);
            }
        }
    }
}
