using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TOR_Core.CampaignMechanics.Ambush;
using TOR_Core.Extensions;
using TOR_Core.Models;
using TOR_Core.Utilities;

namespace TOR_Core.Missions.AmbushMission
{
    /// <summary>
    /// Owns the ambush mission's end-condition priority table + the extraction
    /// bar. Full state machine documented in
    /// <c>TORTasks/ambush-feature/mission-design.md</c>.
    ///
    /// Priority order (first match wins):
    ///  1. Hero (MainAgent) inactive                               → HeroWounded
    ///  2. AttackerTeam has 0 active humans                        → Defeated
    ///  3. DefenderTeam 0 active humans + reinforcement queue empty → TotalVictory
    ///  4. Phase 2 active AND hero in zone AND bar at 100%          → Extracted
    ///  5. Phase 1 real-time elapsed past StalemateMinutes          → Stalemate
    ///
    /// Phase 1 → Phase 2 is driven by <see cref="AmbushMissionController.IsInitialPatrolDefeated"/>.
    /// The retreat zone is inert in Phase 1 (bar can't fill).
    /// </summary>
    public class AmbushEndLogic : MissionLogic
    {
        private bool _outcomeDecided;
        private bool _deploymentFinished;
        private float _diagnosticAccumulator;
        private MissionResult _result;
        private TORMissionAmbushModel _model;
        private AmbushMissionController _controller;
        private DefaultBattleMissionAgentSpawnLogic _spawnLogic;

        private float _extractionProgress;
        private float _lastReportedBucket = -1f;
        private bool _leftBehindWarningFired;
        private float _missionRealTimeSeconds;

        public override void AfterStart()
        {
            TaleWorlds.Library.Debug.Print("[AmbushEndLogic] AfterStart");
            base.AfterStart();
            _model = Campaign.Current?.Models?.GetMissionAmbushModel() ?? new TORMissionAmbushModel();
            _controller = Mission.GetMissionBehavior<AmbushMissionController>();
            _spawnLogic = Mission.GetMissionBehavior<DefaultBattleMissionAgentSpawnLogic>();
        }

        public override void OnDeploymentFinished()
        {
            base.OnDeploymentFinished();
            _deploymentFinished = true;
            TaleWorlds.Library.Debug.Print("[AmbushEndLogic] OnDeploymentFinished — end-checks armed");
        }

        public override void OnMissionTick(float dt)
        {
            base.OnMissionTick(dt);
            if (_outcomeDecided) return;
            // Don't run end-condition checks until the engine signals deployment
            // is done. Before that, teams can report 0 active human agents
            // simply because the vanilla spawn pipeline hasn't placed them yet,
            // and we'd auto-defeat on frame 1.
            if (!_deploymentFinished) return;

            _missionRealTimeSeconds += dt;
            EmitDiagnostics(dt);

            // Priority 1 — hero down. Takes precedence over everything else;
            // the "Ambush Failed" menu is specifically for this case.
            var mainAgent = Mission.MainAgent;
            if (mainAgent != null && !mainAgent.IsActive())
            {
                SetOutcome(AmbushOutcome.HeroWounded, successful: false,
                    reason: "hero inactive");
                return;
            }

            // Priority 2 — attacker wipe (hero somehow still up but no other
            // active humans; unusual but possible via routing / morale).
            if (CountActiveHumans(Mission.AttackerTeam) == 0)
            {
                SetOutcome(AmbushOutcome.Defeated, successful: false,
                    reason: "attacker team has 0 active humans");
                return;
            }

            // Priority 3 — total victory. Requires both the field clear AND
            // the reinforcement queue drained. The ReinforcementsSpawnable
            // check keeps us from firing TotalVictory while 50 queued troops
            // are seconds away from arriving.
            if (CountActiveHumans(Mission.DefenderTeam) == 0
                && !AreReinforcementsStillPending(BattleSideEnum.Defender))
            {
                SetOutcome(AmbushOutcome.TotalVictory, successful: true,
                    reason: "defender team wiped + reinforcement queue drained");
                return;
            }

            // Priority 4 — extraction bar (only during Phase 2).
            TickExtractionBar(dt, mainAgent);
            if (_outcomeDecided) return;  // bar just filled

            // Priority 5 — stalemate (Phase 1 only; Phase 2 has the bar as its
            // natural termination).
            if (_controller != null && !_controller.IsInitialPatrolDefeated
                && _missionRealTimeSeconds >= _model.StalemateMinutes * 60f)
            {
                SetOutcome(AmbushOutcome.Stalemate, successful: false,
                    reason: "Phase 1 soft-timer elapsed (" + _model.StalemateMinutes + " min)");
                return;
            }
        }

        /// <summary>
        /// Extraction bar: fills 0 → 1 while the hero is inside any retreat
        /// zone AND Phase 2 is active. Resets to 0 the moment the hero leaves.
        /// Fires one left-behind-troops warning at the configured threshold.
        /// Completes → outcome Extracted; count of attacker humans NOT in zone
        /// is handed to AmbushCampaignBehavior for post-mission wounding.
        /// </summary>
        private void TickExtractionBar(float dt, Agent mainAgent)
        {
            bool phase2 = _controller != null && _controller.IsInitialPatrolDefeated;
            bool heroInZone = phase2 && AmbushRetreatZone.IsAgentInsideAnyZone(mainAgent);

            if (!heroInZone)
            {
                if (_extractionProgress > 0f)
                {
                    _extractionProgress = 0f;
                    _lastReportedBucket = -1f;
                    _leftBehindWarningFired = false;
                    if (phase2) TORCommon.Say("You stepped out of the extraction zone — progress reset.");
                }
                return;
            }

            // Fill at 1 / ExtractionBarSeconds per second.
            _extractionProgress += dt / _model.ExtractionBarSeconds;
            if (_extractionProgress > 1f) _extractionProgress = 1f;

            // Progress-print at 20 / 40 / 60 / 80 / 100% buckets so the player
            // has some feedback without a HUD element yet.
            float bucket = (int)(_extractionProgress * 5f) / 5f;
            if (bucket > _lastReportedBucket && bucket > 0f)
            {
                _lastReportedBucket = bucket;
                int pct = (int)(_extractionProgress * 100f);
                TORCommon.Say("Extraction " + pct + "%");
            }

            // Left-behind warning (one-shot).
            if (!_leftBehindWarningFired
                && _extractionProgress >= _model.LeftBehindWarningThreshold
                && CountActiveHumansOutsideZone(Mission.AttackerTeam) > 0)
            {
                _leftBehindWarningFired = true;
                TORCommon.Say("Troops left behind will be wounded when the ambush completes.");
            }

            if (_extractionProgress >= 1f)
            {
                int leftBehind = CountActiveHumansOutsideZone(Mission.AttackerTeam);
                var behavior = Campaign.Current?.GetCampaignBehavior<AmbushCampaignBehavior>();
                if (behavior != null) behavior.ExtractionTroopsLeftBehind = leftBehind;
                TaleWorlds.Library.Debug.Print(
                    "[AmbushEndLogic] bar full — Extracted (left behind=" + leftBehind + ")");
                SetOutcome(AmbushOutcome.Extracted, successful: true,
                    reason: "extraction bar at 100%");
            }
        }

        private void EmitDiagnostics(float dt)
        {
            _diagnosticAccumulator += dt;
            if (_diagnosticAccumulator < 2f) return;
            _diagnosticAccumulator = 0f;
            int atk = CountActiveHumans(Mission.AttackerTeam);
            int def = CountActiveHumans(Mission.DefenderTeam);
            string phase = _controller != null && _controller.IsInitialPatrolDefeated ? "P2" : "P1";
            TaleWorlds.Library.Debug.Print(
                "[AmbushEndLogic] tick " + phase
                + " atk=" + atk + " def=" + def
                + " zones=" + AmbushRetreatZone.ActiveZones.Count
                + " bar=" + ((int)(_extractionProgress * 100f)) + "%"
                + " t=" + ((int)_missionRealTimeSeconds) + "s");
        }

        private bool AreReinforcementsStillPending(BattleSideEnum side)
        {
            // DefaultBattleMissionAgentSpawnLogic.IsSideDepleted(side) returns
            // true when the whole supply (initial + reinforcements) is spent.
            // While it's still false, more agents can arrive — so we should
            // NOT fire TotalVictory even if the field is momentarily empty.
            if (_spawnLogic == null) return false;
            return !_spawnLogic.IsSideDepleted(side);
        }

        private static int CountActiveHumans(Team team)
        {
            if (team == null) return 0;
            int n = 0;
            foreach (var a in team.ActiveAgents)
                if (a != null && a.IsHuman && a.IsActive()) n++;
            return n;
        }

        private static int CountActiveHumansOutsideZone(Team team)
        {
            if (team == null) return 0;
            int n = 0;
            foreach (var a in team.ActiveAgents)
            {
                if (a == null || !a.IsHuman || !a.IsActive()) continue;
                if (!AmbushRetreatZone.IsAgentInsideAnyZone(a)) n++;
            }
            return n;
        }

        public override bool MissionEnded(ref MissionResult missionResult)
        {
            if (!_outcomeDecided) return false;
            missionResult = _result;
            return true;
        }

        private void SetOutcome(AmbushOutcome outcome, bool successful, string reason)
        {
            _outcomeDecided = true;
            _result = successful
                ? MissionResult.CreateSuccessful(Mission)
                : MissionResult.CreateDefeated(Mission);
            var behavior = Campaign.Current?.GetCampaignBehavior<AmbushCampaignBehavior>();
            if (behavior != null) behavior.LastAmbushOutcome = outcome;
            TaleWorlds.Library.Debug.Print(
                "[AmbushEndLogic] outcome=" + outcome + "  successful=" + successful
                + "  (" + reason + ")");
        }
    }
}
