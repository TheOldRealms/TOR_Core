using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
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
        private DefaultBattleMissionAgentSpawnLogic _spawnLogic;
        private BasicMissionTimer _aiActivationTimer;
        private bool _aiActivated;
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
            AmbushRetreatZone.ResetRegistry();
            _spawnLogic = Mission.GetMissionBehavior<DefaultBattleMissionAgentSpawnLogic>();
            _model = Campaign.Current?.Models?.GetMissionAmbushModel() ?? new TORMissionAmbushModel();
            AmbushRetreatZone.ProceduralFallbackRadius = _model.ProceduralFallbackRadius;
            Debug.Print("[AmbushMission] OnBehaviorInitialize done (spawnLogic=" + (_spawnLogic != null) + ")");
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
            // Mirror vanilla's "besieger activation" shape: defender team (our
            // AI opponent) is frozen for AiActivationDelay seconds so the player
            // gets a clean opening.
            FreezeDefenderAi();
            _aiActivationTimer = new BasicMissionTimer();

            // If the scene has no scener-authored retreat zones, drop a
            // procedural fallback at the player's current position (deployment
            // tile). Not ideal — ideally we'd pick a point opposite the enemy
            // spawn — but "retreat to your start line" is at least coherent
            // without needing scene bounds + spawn-tile introspection.
            // Dedicated ambush scenes will author their own zones and this
            // fallback never activates for them.
            if (AmbushRetreatZone.ActiveZones.Count == 0)
            {
                var mainAgent = Mission.MainAgent;
                if (mainAgent != null && mainAgent.IsActive())
                    AmbushRetreatZone.ProceduralFallbackPosition = mainAgent.Position;
            }
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
            int defenderInitial = TaleWorlds.Library.MathF.Min(besiegedTotal,
                TaleWorlds.Library.MathF.Ceiling(sum * 0.1f));
            int attackerInitial = TaleWorlds.Library.MathF.Min(besiegerTotal,
                TaleWorlds.Library.MathF.Ceiling(sum * 0.1f));

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
