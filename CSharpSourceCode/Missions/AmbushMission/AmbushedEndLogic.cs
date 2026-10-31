using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TOR_Core.CampaignMechanics.Ambush;
using TOR_Core.Extensions;
using TOR_Core.Models;

namespace TOR_Core.Missions.AmbushMission
{
    /// <summary>
    /// Phase F counterpart to <see cref="AmbushEndLogic"/>. Role-reversed:
    /// AttackerTeam is the AI ambusher force, DefenderTeam is the player.
    ///
    /// Priority order (first match wins):
    ///  1. Hero (MainAgent) inactive                                 → AmbushedHeroWounded
    ///  2. DefenderTeam (player) 0 active + reinforcements drained   → AmbushedWiped
    ///  3. AttackerTeam (AI) 0 active + reinforcements drained       → AmbushRepelled
    ///  4. Player across soft border for countdown seconds           → AmbushedFled
    ///
    /// Full design in TORTasks/ambush-feature/ai-ambushes-player-design.md § 3.
    /// </summary>
    public class AmbushedEndLogic : MissionLogic
    {
        private bool _outcomeDecided;
        private bool _deploymentFinished;
        private MissionResult _result;
        private TORMissionAmbushModel _model;
        private DefaultBattleMissionAgentSpawnLogic _spawnLogic;

        private float _outsideBoundarySeconds;

        public override void AfterStart()
        {
            base.AfterStart();
            _model = Campaign.Current?.Models?.GetMissionAmbushModel() ?? new TORMissionAmbushModel();
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
            // Same reasoning as Phase D: before deployment finishes the engine
            // reports 0 active humans while agents are still being spawned.
            if (!_deploymentFinished) return;

            var mainAgent = Mission.MainAgent;

            // Priority 1 — hero down.
            if (mainAgent != null && !mainAgent.IsActive())
            {
                SetOutcome(AmbushedOutcome.AmbushedHeroWounded, successful: false,
                    reason: "hero inactive");
                return;
            }

            // Priority 2 — player (defender) team wiped. Needs the
            // reinforcement queue drained too, mirroring Phase D's logic:
            // don't call it a defeat while queued waves are seconds away.
            if (CountActiveHumans(Mission.DefenderTeam) == 0
                && !AreReinforcementsStillPending(BattleSideEnum.Defender))
            {
                SetOutcome(AmbushedOutcome.AmbushedWiped, successful: false,
                    reason: "defender (player) wiped + reinforcement queue drained");
                return;
            }

            // Priority 3 — AI ambusher team wiped. AI sits on AttackerTeam
            // (matches the ambusher = Attacker invariant). AI reinforcement
            // is deferred (design doc § 4) but the check reads IsSideDepleted
            // anyway so the gate stays honest when the model lands later.
            if (CountActiveHumans(Mission.AttackerTeam) == 0
                && !AreReinforcementsStillPending(BattleSideEnum.Attacker))
            {
                SetOutcome(AmbushedOutcome.AmbushRepelled, successful: true,
                    reason: "attacker (AI) wiped + reinforcement queue drained");
                return;
            }

            // Priority 4 — soft border crossed. Accumulate time while the
            // hero's position is outside the mission boundary; reset to 0 the
            // moment they step back in. Fire the outcome when the countdown
            // expires. We only engage the countdown after the hero is spawned
            // (MainAgent != null) so a null main agent during the first tick
            // doesn't count against us.
            if (mainAgent != null)
            {
                if (!Mission.IsPositionInsideBoundaries(mainAgent.Position.AsVec2))
                    _outsideBoundarySeconds += dt;
                else
                    _outsideBoundarySeconds = 0f;

                if (_outsideBoundarySeconds >= _model.AmbushedFledCountdownSeconds)
                {
                    SetOutcome(AmbushedOutcome.AmbushedFled, successful: true,
                        reason: "soft border countdown elapsed ("
                                + _model.AmbushedFledCountdownSeconds + "s)");
                    return;
                }
            }
        }

        private bool AreReinforcementsStillPending(BattleSideEnum side)
        {
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

        public override bool MissionEnded(ref MissionResult missionResult)
        {
            if (!_outcomeDecided) return false;
            missionResult = _result;
            return true;
        }

        /// <summary>
        /// AmbushRepelled is the only outcome that nudges the vanilla
        /// post-battle state machine — same pattern as Phase D's TotalVictory,
        /// since the player's party wins and should see loot / cleanup.
        /// AmbushedWiped goes through vanilla defeat; AmbushedHeroWounded and
        /// AmbushedFled leave PlayerEncounter alive so the campaign-side
        /// aftermath (reveal window, follow-up battle, wounded troops) can
        /// run.
        /// </summary>
        public override void OnMissionResultReady(MissionResult missionResult)
        {
            base.OnMissionResultReady(missionResult);
            if (missionResult == null) return;
            var behavior = Campaign.Current?.GetCampaignBehavior<AmbushCampaignBehavior>();
            var outcome = behavior?.LastAmbushedOutcome ?? AmbushedOutcome.None;
            if (outcome == AmbushedOutcome.AmbushRepelled)
            {
                if (PlayerEncounter.Current != null)
                    PlayerEncounter.SetPlayerVictorious();
                if (behavior != null) behavior.PendingPostAmbushCleanup = true;
            }
        }

        private void SetOutcome(AmbushedOutcome outcome, bool successful, string reason)
        {
            _outcomeDecided = true;
            _result = successful
                ? MissionResult.CreateSuccessful(Mission)
                : MissionResult.CreateDefeated(Mission);
            var behavior = Campaign.Current?.GetCampaignBehavior<AmbushCampaignBehavior>();
            if (behavior != null) behavior.LastAmbushedOutcome = outcome;
            TaleWorlds.Library.Debug.Print(
                "[AmbushedEndLogic] outcome=" + outcome + " successful=" + successful
                + " (" + reason + ")");
        }
    }
}
