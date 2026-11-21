using System.Collections.Generic;
using NLog;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Missions.MissionLogics;
using TaleWorlds.MountAndBlade.Missions.Objectives;
using TOR_Core.CampaignMechanics.Ambush;
using TOR_Core.Extensions;
using TOR_Core.Models;
using TOR_Core.Utilities;

namespace TOR_Core.Missions.AmbushMission
{
    /// <summary>End-condition priority table and extraction bar for the ambusher-side mission.</summary>
    /// <remarks>
    /// Priority: hero inactive, attacker wiped, defender wiped + reinforcements drained, extraction complete, stalemate timer.
    /// </remarks>
    public class AmbushEndLogic : MissionLogic
    {
        private bool _outcomeDecided;
        private bool _deploymentFinished;
        private MissionResult _result;
        private TORMissionAmbushModel _model;
        private AmbushMissionController _controller;
        private AmbushReinforcementMissionLogic _reinforcementLogic;

        private float _extractionProgress;
        private bool _leftBehindWarningFired;
        private float _missionRealTimeSeconds;

        private MissionObjectiveLogic _objectiveLogic;
        private AmbushObjective _objective;
        private bool _objectiveIsPhase2;
        private int _lastRemainingShown = int.MinValue;
        private int _lastTotalPoolShown = int.MinValue;
        private bool _lastInZoneShown;

        public override void AfterStart()
        {
            base.AfterStart();
            _model = Campaign.Current?.Models?.GetMissionAmbushModel() ?? new TORMissionAmbushModel();
            _controller = Mission.GetMissionBehavior<AmbushMissionController>();
            _reinforcementLogic = Mission.GetMissionBehavior<AmbushReinforcementMissionLogic>();
            _objectiveLogic = Mission.GetMissionBehavior<MissionObjectiveLogic>();
            StartPhase1Objective();
        }

        public override void OnDeploymentFinished()
        {
            base.OnDeploymentFinished();
            _deploymentFinished = true;
        }

        public override void OnMissionTick(float dt)
        {
            base.OnMissionTick(dt);
            if (_outcomeDecided) return;
            // Skip checks until deployment finishes; otherwise frame-1 would auto-defeat.
            if (!_deploymentFinished) return;

            _missionRealTimeSeconds += dt;

            // Priority 1: hero down.
            var mainAgent = Mission.MainAgent;
            if (mainAgent != null && !mainAgent.IsActive())
            {
                SetOutcome(AmbushResolution.AmbusherDefeated,
                    successful: false, ambusherLeaderDown: true,
                    reason: "hero inactive");
                return;
            }

            // Priority 2: attacker wiped.
            if (CountActiveHumans(Mission.AttackerTeam) == 0)
            {
                SetOutcome(AmbushResolution.AmbusherDefeated, successful: false,
                    reason: "attacker team has 0 active humans");
                return;
            }

            // Priority 3: total victory. Wait for the reinforcement pool to drain before calling it.
            if (CountActiveHumans(Mission.DefenderTeam) == 0
                && !AreReinforcementsStillPending())
            {
                SetOutcome(AmbushResolution.AmbusherTotalVictory, successful: true,
                    reason: "defender team wiped + reinforcement pool depleted");
                return;
            }

            // Priority 4: extraction bar (Phase 2 only).
            TickExtractionBar(dt, mainAgent);
            if (_outcomeDecided) return;

            RefreshObjective(mainAgent);

            // Priority 5: Phase 1 stalemate timer.
            if (_controller != null && !_controller.IsInitialPatrolDefeated
                && _missionRealTimeSeconds >= _model.StalemateMinutes * 60f)
            {
                SetOutcome(AmbushResolution.Stalemate, successful: false,
                    reason: "Phase 1 soft-timer elapsed (" + _model.StalemateMinutes + " min)");
                return;
            }
        }

        /// <summary>Fill the extraction bar while the hero stands inside a retreat zone during Phase 2.</summary>
        private void TickExtractionBar(float dt, Agent mainAgent)
        {
            // Suppress the bar in the frame before TotalVictory fires.
            if (CountActiveHumans(Mission.DefenderTeam) == 0
                && !AreReinforcementsStillPending())
            {
                if (_extractionProgress > 0f)
                {
                    _extractionProgress = 0f;
                    _leftBehindWarningFired = false;
                }
                return;
            }

            bool phase2 = _controller != null && _controller.IsInitialPatrolDefeated;
            bool heroInZone = phase2 && AmbushRetreatZone.IsAgentInsideAnyZone(mainAgent);

            if (!heroInZone)
            {
                if (_extractionProgress > 0f)
                {
                    _extractionProgress = 0f;
                    _leftBehindWarningFired = false;
                }
                return;
            }

            // Fill at 1 / ExtractionBarSeconds per second.
            _extractionProgress += dt / _model.ExtractionBarSeconds;
            if (_extractionProgress > 1f) _extractionProgress = 1f;

            // One-shot left-behind warning.
            if (!_leftBehindWarningFired
                && _extractionProgress >= _model.LeftBehindWarningThreshold
                && CountActiveHumansOutsideZone(Mission.AttackerTeam) > 0)
            {
                _leftBehindWarningFired = true;
                MBInformationManager.AddQuickInformation(
                    new TextObject("{=tor_ambush_left_behind}Troops left behind will be wounded when the ambush completes."));
            }

            if (_extractionProgress >= 1f)
            {
                // Promote to TotalVictory only if the field is clear AND the pool is exhausted.
                if (CountActiveHumans(Mission.DefenderTeam) == 0
                    && !AreReinforcementsStillPending())
                {
                    SetOutcome(AmbushResolution.AmbusherTotalVictory, successful: true,
                        reason: "extraction complete and field also clear");
                    return;
                }
                int leftBehind = CountActiveHumansOutsideZone(Mission.AttackerTeam);
                var behavior = Campaign.Current?.GetCampaignBehavior<AmbushCampaignBehavior>();
                if (behavior != null) behavior.ExtractionTroopsLeftBehind = leftBehind;
                SetOutcome(AmbushResolution.AmbusherPartialVictory, successful: true,
                    reason: "extraction bar at 100%, left behind=" + leftBehind);
            }
        }

        /// <summary>True while the reinforcement wave timer may still deliver another defender batch.</summary>
        private bool AreReinforcementsStillPending()
        {
            if (_reinforcementLogic == null) return false;
            return !_reinforcementLogic.IsPoolDepleted;
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
                // Troop extraction uses the zone's larger troop radius; scener-tuned per zone.
                if (!AmbushRetreatZone.IsAgentInsideAnyTroopExtractionZone(a)) n++;
            }
            return n;
        }

        /// <summary>Start the "defeat the patrol" objective; description is updated per tick with the live count.</summary>
        private void StartPhase1Objective()
        {
            if (_objectiveLogic == null) return;
            _objective = new AmbushObjective(
                Mission, "tor_ambush_phase1",
                new TextObject("{=tor_ambush_obj_phase1_name}Ambush the Patrol"),
                new TextObject("{=tor_ambush_obj_phase1_desc_init}Defeat the enemy patrol."));
            _objective.SetProgressCallback(() => default);
            _objectiveLogic.StartObjective(_objective);
            _objectiveIsPhase2 = false;
            _lastRemainingShown = int.MinValue;
            _lastInZoneShown = false;
            _lastTotalPoolShown = int.MinValue;
        }

        /// <summary>Start the Phase 2 "escape the ambush" objective and light the retreat-zone markers.</summary>
        private void StartPhase2Objective()
        {
            if (_objectiveLogic == null) return;
            _objective = new AmbushObjective(
                Mission, "tor_ambush_phase2",
                new TextObject("{=tor_ambush_obj_phase2_name}Escape the Ambush"),
                new TextObject("{=tor_ambush_obj_phase2_desc_init}Retreat to the Extraction Point or fight off the reinforcements."));
            _objective.SetProgressCallback(GetPhase2Progress);
            // Use vanilla MissionObjectiveMarkers (world-anchored projection via objective targets).
            _objective.AddTarget(new AmbushRetreatZoneObjectiveTarget());
            _objectiveLogic.StartObjective(_objective);
            _objectiveIsPhase2 = true;
            _lastInZoneShown = false;
            _lastTotalPoolShown = int.MinValue;
            AmbushRetreatZone.ShowRuntimeMarker = true;
        }

        /// <summary>Handle Phase 1 to Phase 2 transition and update objective description only when inputs change.</summary>
        private void RefreshObjective(Agent mainAgent)
        {
            if (_objective == null || _controller == null) return;
            bool phase2 = _controller.IsInitialPatrolDefeated;
            if (phase2 && !_objectiveIsPhase2)
            {
                StartPhase2Objective();
            }
            if (!_objectiveIsPhase2)
            {
                int remaining = CountActiveHumans(Mission.DefenderTeam);
                if (remaining == _lastRemainingShown) return;
                _lastRemainingShown = remaining;
                var desc = new TextObject("{=tor_ambush_obj_phase1_desc}Defeat the enemy patrol. ({N} remaining)");
                desc.SetTextVariable("N", remaining);
                _objective.SetDescription(desc);
                return;
            }
            bool inZone = AmbushRetreatZone.IsAgentInsideAnyZone(mainAgent);
            int pool = _reinforcementLogic?.RemainingPoolCount ?? 0;
            int onField = CountActiveHumans(Mission.DefenderTeam);
            int total = pool + onField;
            if (inZone == _lastInZoneShown && total == _lastTotalPoolShown) return;
            _lastInZoneShown = inZone;
            _lastTotalPoolShown = total;
            TextObject phase2Desc;
            if (inZone)
            {
                phase2Desc = new TextObject("{=tor_ambush_obj_phase2_desc_extracting}Extracting...");
            }
            else
            {
                phase2Desc = new TextObject("{=tor_ambush_obj_phase2_desc_press}Retreat to the Extraction Point or fight off the reinforcements. ({N} remaining)");
                phase2Desc.SetTextVariable("N", total);
            }
            _objective.SetDescription(phase2Desc);
        }

        /// <summary>Phase 2 progress bar fill based on the cached in-zone flag.</summary>
        private MissionObjectiveProgressInfo GetPhase2Progress()
        {
            if (!_lastInZoneShown) return default;
            return new MissionObjectiveProgressInfo
            {
                RequiredProgressAmount = 100,
                CurrentProgressAmount = (int)(_extractionProgress * 100f),
            };
        }

        public override bool MissionEnded(ref MissionResult missionResult)
        {
            if (!_outcomeDecided) return false;
            missionResult = _result;
            return true;
        }

        public override void OnMissionResultReady(MissionResult missionResult)
        {
            base.OnMissionResultReady(missionResult);
            // Victory override moved to SetOutcome so it lands before vanilla's iteration races in.
        }

        private void SetOutcome(AmbushResolution resolution, bool successful, string reason,
                                 bool ambusherLeaderDown = false)
        {
            _outcomeDecided = true;
            _result = successful
                ? MissionResult.CreateSuccessful(Mission)
                : MissionResult.CreateDefeated(Mission);
            var behavior = Campaign.Current?.GetCampaignBehavior<AmbushCampaignBehavior>();
            if (behavior != null)
            {
                behavior.LastResolution = resolution;
                behavior.PlayerWasAmbusher = true;
                behavior.LastAmbusherLeaderDown = ambusherLeaderDown;
                behavior.LastVictimLeaderDown = false;
            }

            // Stamp SetPlayerVictorious at decision time; vanilla's OnBattleOver reads WinningSide synchronously.
            if (resolution == AmbushResolution.AmbusherTotalVictory
                && PlayerEncounter.Current != null)
            {
                PlayerEncounter.SetPlayerVictorious();
                if (behavior != null) behavior.PendingPostAmbushCleanup = true;
                TORCommon.Log(LogLevel.Info,
                    "[AmbushEndLogic] SetPlayerVictorious stamped at SetOutcome"
                    + " resolution=" + resolution
                    + " WinningSide=" + PlayerEncounter.WinningSide
                    + " enc.PlayerSide="
                      + (PlayerEncounter.Current?.PlayerSide.ToString() ?? "n/a")
                    + " Mission.PlayerTeam.Side="
                      + (Mission.PlayerTeam?.Side.ToString() ?? "null"));
            }

            // Extraction path: pre-activate the escape menu and tear down immediately to beat the scoreboard.
            if (resolution == AmbushResolution.AmbusherPartialVictory && behavior != null)
            {
                behavior.BeginExtractionTransition(behavior.ExtractionTroopsLeftBehind);
                Mission.EndMission();
            }

            TORCommon.Log(LogLevel.Info,
                "[AmbushEndLogic] resolution=" + resolution + " successful=" + successful
                + " (" + reason + ")");
        }
    }
}
