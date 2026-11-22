using NLog;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Missions.MissionLogics;
using TaleWorlds.MountAndBlade.Missions.Objectives;
using TOR_Core.CampaignMechanics.Ambush;
using TOR_Core.Utilities;

namespace TOR_Core.Missions.AmbushMission
{
    /// <summary>
    /// Mission logic for the ambusher-side ambush (player strikes an AI
    /// party from stealth). Owns the outcome priority table, the Phase 1
    /// to Phase 2 objective transition, and the extraction-bar fill.
    /// Role-specific SetOutcome flags come from <see cref="AmbushMissionLogicBase.WriteRoleFlags"/>.
    /// </summary>
    /// <remarks>
    /// Outcome priority: hero inactive, attacker wiped, defender wiped + reinforcements drained, extraction complete, stalemate timer.
    /// </remarks>
    public class PlayerAmbushMissionLogic : AmbushMissionLogicBase
    {
        private AmbushMissionController _controller;

        private float _extractionProgress;
        private bool _leftBehindWarningFired;
        private float _missionRealTimeSeconds;

        private bool _objectiveIsPhase2;
        private int _lastRemainingShown = int.MinValue;
        private int _lastTotalPoolShown = int.MinValue;
        private bool _lastInZoneShown;

        protected override string LogTag => "[PlayerAmbushMission]";

        protected override void OnAfterStarted()
        {
            _controller = Mission.GetMissionBehavior<AmbushMissionController>();
            StartPhase1Objective();
        }

        protected override void OnMissionTickReady(float dt)
        {
            _missionRealTimeSeconds += dt;

            // Priority 1: hero down.
            var mainAgent = Mission.MainAgent;
            if (mainAgent != null && !mainAgent.IsActive())
            {
                SetOutcome(AmbushResolution.AmbusherDefeated,
                    successful: false, leaderDown: true,
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

        protected override void WriteRoleFlags(AmbushCampaignBehavior behavior, bool leaderDown)
        {
            behavior.PlayerWasAmbusher = true;
            behavior.LastAmbusherLeaderDown = leaderDown;
            behavior.LastVictimLeaderDown = false;
        }

        protected override void OnOutcomeSet(AmbushResolution resolution, bool successful, string reason,
                                              AmbushCampaignBehavior behavior)
        {
            // Stamp SetPlayerVictorious at decision time; vanilla's OnBattleOver reads WinningSide synchronously.
            if (resolution == AmbushResolution.AmbusherTotalVictory
                && PlayerEncounter.Current != null)
            {
                PlayerEncounter.SetPlayerVictorious();
                if (behavior != null) behavior.PendingPostAmbushCleanup = true;
                TORCommon.Log(LogLevel.Info,
                    LogTag + " SetPlayerVictorious stamped at SetOutcome"
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
        }

        public override void OnMissionResultReady(MissionResult missionResult)
        {
            base.OnMissionResultReady(missionResult);
            // Victory override moved to OnOutcomeSet so it lands before vanilla's iteration races in.
        }

        /// <summary>
        /// Fill the extraction bar while the hero stands inside a retreat zone during Phase 2.
        /// </summary>
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

        /// <summary>
        /// Count the live human agents on the team that are outside every troop-extraction zone.
        /// </summary>
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

        /// <summary>
        /// Start the "defeat the patrol" objective; description is updated per tick with the live count.
        /// </summary>
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

        /// <summary>
        /// Start the Phase 2 "escape the ambush" objective and light the retreat-zone markers.
        /// </summary>
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

        /// <summary>
        /// Handle Phase 1 to Phase 2 transition and update objective description only when inputs change.
        /// </summary>
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

        /// <summary>
        /// Phase 2 progress bar fill based on the cached in-zone flag.
        /// </summary>
        private MissionObjectiveProgressInfo GetPhase2Progress()
        {
            if (!_lastInZoneShown) return default;
            return new MissionObjectiveProgressInfo
            {
                RequiredProgressAmount = 100,
                CurrentProgressAmount = (int)(_extractionProgress * 100f),
            };
        }
    }
}
