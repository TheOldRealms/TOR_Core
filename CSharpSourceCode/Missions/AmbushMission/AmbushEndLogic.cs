using System.Collections.Generic;
using NLog;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
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
            base.AfterStart();
            _model = Campaign.Current?.Models?.GetMissionAmbushModel() ?? new TORMissionAmbushModel();
            _controller = Mission.GetMissionBehavior<AmbushMissionController>();
            _spawnLogic = Mission.GetMissionBehavior<DefaultBattleMissionAgentSpawnLogic>();
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
            // Don't run end-condition checks until the engine signals deployment
            // is done. Before that, teams can report 0 active human agents
            // simply because the vanilla spawn pipeline hasn't placed them yet,
            // and we'd auto-defeat on frame 1.
            if (!_deploymentFinished) return;

            _missionRealTimeSeconds += dt;

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

            // Priority 3 — total victory. The ambush mission does a single
            // custom spawn in AmbushMissionController.AfterStart and drains
            // both suppliers at that point; no reinforcement wave can
            // legitimately arrive later. Field clear = win. The vanilla
            // IsSideDepleted check (via AreReinforcementsStillPending) is
            // a scene-driven-reinforcement concept that reports pending=true
            // for our mission even with the supplier empty, which was
            // blocking this branch and routing wipes through the extraction
            // path instead.
            if (CountActiveHumans(Mission.DefenderTeam) == 0)
            {
                SetOutcome(AmbushOutcome.TotalVictory, successful: true,
                    reason: "defender team wiped");
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
            // Safety guard: if the field is already clear, the mission is a
            // TotalVictory the moment Priority 3 catches it on the next
            // check. Filling the bar and toasting "Extraction N%" in that
            // window would read as noise; reset the bar to 0 and bail so
            // the player never sees the retreat UI for what should be a
            // clean win.
            if (CountActiveHumans(Mission.DefenderTeam) == 0)
            {
                if (_extractionProgress > 0f)
                {
                    _extractionProgress = 0f;
                    _lastReportedBucket = -1f;
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
                // Promote to TotalVictory if the field cleared during the
                // bar fill. The escape-menu flow assumes surviving enemies
                // are "forming up where you struck" — opening it when the
                // defender team is empty reads as absurd to the player
                // and sends them into a menu with no sensible options.
                if (CountActiveHumans(Mission.DefenderTeam) == 0)
                {
                    SetOutcome(AmbushOutcome.TotalVictory, successful: true,
                        reason: "extraction complete and field also clear");
                    return;
                }
                int leftBehind = CountActiveHumansOutsideZone(Mission.AttackerTeam);
                var behavior = Campaign.Current?.GetCampaignBehavior<AmbushCampaignBehavior>();
                if (behavior != null) behavior.ExtractionTroopsLeftBehind = leftBehind;
                SetOutcome(AmbushOutcome.Extracted, successful: true,
                    reason: "extraction bar at 100%, left behind=" + leftBehind);
            }
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

        /// <summary>
        /// On TotalVictory, kick vanilla's post-battle state machine so loot
        /// and party cleanup actually run. Our MissionEnded wins the race
        /// against BattleEndLogic so vanilla's natural path never gets to
        /// transition MapEvent.BattleState; we do it ourselves via
        /// PlayerEncounter.SetPlayerVictorious. The post-mission cleanup
        /// flag is read by AmbushCampaignBehavior to break the chicken-and-
        /// egg between the stealth wait-menu's TimeControlMode = Stop and
        /// the TickEvent that would unstop it.
        ///
        /// Other outcomes deliberately leave PlayerEncounter alive:
        /// Extracted / HeroWounded need the encounter state intact for the
        /// Ambush Escape / Failed menus; Defeated / Stalemate use the vanilla
        /// loss flow.
        /// </summary>
        public override void OnMissionResultReady(MissionResult missionResult)
        {
            TORCommon.Log(LogLevel.Info,
                "[AmbushEndLogic] OnMissionResultReady enter"
                + " result=" + (missionResult == null ? "null"
                    : (missionResult.PlayerVictory ? "victory"
                        : missionResult.PlayerDefeated ? "defeated" : "other")));
            base.OnMissionResultReady(missionResult);
            if (missionResult == null)
            {
                TORCommon.Log(LogLevel.Info,
                    "[AmbushEndLogic] OnMissionResultReady exit (null result)");
                return;
            }
            var behavior = Campaign.Current?.GetCampaignBehavior<AmbushCampaignBehavior>();
            var outcome = behavior?.LastAmbushOutcome ?? AmbushOutcome.None;
            TORCommon.Log(LogLevel.Info,
                "[AmbushEndLogic] OnMissionResultReady outcome=" + outcome
                + " playerEncounter=" + (PlayerEncounter.Current != null));
            if (outcome == AmbushOutcome.TotalVictory)
            {
                if (PlayerEncounter.Current != null)
                {
                    TORCommon.Log(LogLevel.Info,
                        "[AmbushEndLogic] calling SetPlayerVictorious");
                    PlayerEncounter.SetPlayerVictorious();
                    TORCommon.Log(LogLevel.Info,
                        "[AmbushEndLogic] SetPlayerVictorious returned");
                }
                if (behavior != null) behavior.PendingPostAmbushCleanup = true;
            }
            TORCommon.Log(LogLevel.Info, "[AmbushEndLogic] OnMissionResultReady exit");
        }

        private void SetOutcome(AmbushOutcome outcome, bool successful, string reason)
        {
            _outcomeDecided = true;
            _result = successful
                ? MissionResult.CreateSuccessful(Mission)
                : MissionResult.CreateDefeated(Mission);
            var behavior = Campaign.Current?.GetCampaignBehavior<AmbushCampaignBehavior>();
            if (behavior != null) behavior.LastAmbushOutcome = outcome;
            TORCommon.Log(LogLevel.Info, 
                "[AmbushEndLogic] outcome=" + outcome + " successful=" + successful
                + " (" + reason + ")");
        }
    }
}
