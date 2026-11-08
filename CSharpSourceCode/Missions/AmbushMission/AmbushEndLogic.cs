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
        private AmbushReinforcementMissionLogic _reinforcementLogic;

        private float _extractionProgress;
        private float _lastReportedBucket = -1f;
        private bool _leftBehindWarningFired;
        private float _missionRealTimeSeconds;

        public override void AfterStart()
        {
            base.AfterStart();
            _model = Campaign.Current?.Models?.GetMissionAmbushModel() ?? new TORMissionAmbushModel();
            _controller = Mission.GetMissionBehavior<AmbushMissionController>();
            _reinforcementLogic = Mission.GetMissionBehavior<AmbushReinforcementMissionLogic>();
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

            // Priority 3 — total victory. The custom spawn now does a
            // partial drain (capped at InitialOnFieldCap per side) so the
            // supplier legitimately holds reinforcement origins past the
            // initial wave. Firing TotalVictory the frame the initial
            // patrol drops would end the mission before a single wave
            // arrives; the player clears 15 troops in Phase 1 and the
            // "you win" fires before reinforcements can even appear.
            // Gate on the spawn-logic's pool-depleted flag so the mission
            // only completes once the authored reinforcement schedule is
            // fully consumed AND the field is clear.
            if (CountActiveHumans(Mission.DefenderTeam) == 0
                && !AreReinforcementsStillPending())
            {
                SetOutcome(AmbushOutcome.TotalVictory, successful: true,
                    reason: "defender team wiped + reinforcement pool depleted");
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
            // Safety guard: when the field is clear AND no more
            // reinforcements are pending, Priority 3 will fire TotalVictory
            // next tick. Suppress the extraction UI during that short
            // window. When the field is momentarily empty between waves
            // but reinforcements are still pending, keep the bar filling —
            // extracting early is a valid choice.
            if (CountActiveHumans(Mission.DefenderTeam) == 0
                && !AreReinforcementsStillPending())
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
                // Promote to TotalVictory only if the field is fully clear
                // AND the reinforcement pool is exhausted. The escape-menu
                // flow assumes surviving enemies are "forming up where you
                // struck"; with reinforcements still pending that fiction
                // holds even when the current wave is dead between arrivals.
                if (CountActiveHumans(Mission.DefenderTeam) == 0
                    && !AreReinforcementsStillPending())
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

        /// <summary>
        /// True while the reinforcement wave timer may still deliver
        /// another defender batch. Reads directly from the authoritative
        /// pool-depleted flag on
        /// <see cref="AmbushReinforcementMissionLogic"/>, so we're not
        /// coupled to vanilla's phase bookkeeping for the gate. Attacker
        /// side has no wave schedule (one-shot ambusher commitment);
        /// callers only need the defender answer, so this is parameter-free.
        /// </summary>
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
                    // Diagnostic (task #82): dump the state the scoreboard
                    // VM will read when it eventually renders. If
                    // WinningSide here is None or opposite the player
                    // side, something downstream clears the override
                    // before the end-screen draws.
                    // mapEvent field is private, but the two top-level
                    // static accessors go through it: PlayerEncounter
                    // .Battle and PlayerEncounter.BattleState. Both
                    // return the current state we need to verify.
                    var battle = PlayerEncounter.Battle;
                    TORCommon.Log(LogLevel.Info,
                        "[AmbushEndLogic] post-SetPlayerVictorious:"
                        + " battle="
                          + (battle == null ? "null" : "set")
                        + " BattleState="
                          + PlayerEncounter.BattleState
                        + " WinningSide="
                          + PlayerEncounter.WinningSide
                        + " enc.PlayerSide="
                          + (PlayerEncounter.Current?.PlayerSide
                                .ToString() ?? "n/a")
                        + " Mission.PlayerTeam.Side="
                          + (Mission.PlayerTeam?.Side.ToString() ?? "null")
                        + " MissionResult.PlayerVictory="
                          + (Mission.MissionResult?.PlayerVictory
                                .ToString() ?? "null")
                        + " PlayerIsAttacker="
                          + PlayerEncounter.PlayerIsAttacker);
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
