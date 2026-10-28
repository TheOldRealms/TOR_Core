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
    /// Watches retreat-zone presence; sets the behavior's LastAmbushOutcome and
    /// ends the mission accordingly. Also catches player-defeat / player-died
    /// conditions so vanilla BattleEndLogic doesn't close the mission with an
    /// undefined ambush outcome.
    ///
    /// No hard pressure-clock timeout — the pressure is purely in the escalating
    /// reinforcement waves (AmbushReinforcementSpawnTimer). Player retreats to a
    /// zone, dies, or gets wiped. Those are the three end conditions.
    /// </summary>
    public class AmbushEndLogic : MissionLogic
    {
        private float _timeInZone;
        private bool _outcomeDecided;
        private bool _deploymentFinished;
        private float _diagnosticAccumulator;
        private MissionResult _result;
        private TORMissionAmbushModel _model;

        public override void AfterStart()
        {
            TaleWorlds.Library.Debug.Print("[AmbushEndLogic] AfterStart");
            base.AfterStart();
            _model = Campaign.Current?.Models?.GetMissionAmbushModel() ?? new TORMissionAmbushModel();
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
            // is done. Before that, teams can report 0 active human agents simply
            // because the vanilla spawn pipeline hasn't placed them yet, and
            // we'd auto-defeat on frame 1.
            if (!_deploymentFinished) return;

            _diagnosticAccumulator += dt;
            if (_diagnosticAccumulator >= 2f)
            {
                _diagnosticAccumulator = 0f;
                int atk = CountActiveHumans(Mission.AttackerTeam);
                int def = CountActiveHumans(Mission.DefenderTeam);
                var fallback = AmbushRetreatZone.ProceduralFallbackPosition;
                var fallbackStr = fallback.HasValue ? fallback.Value.ToString() : "<none>";
                TaleWorlds.Library.Debug.Print(
                    "[AmbushEndLogic] tick atk=" + atk + " def=" + def
                    + " zones=" + AmbushRetreatZone.ActiveZones.Count
                    + " fallback=" + fallbackStr);
            }

            // Catch player death / player-side wipe early so the right outcome
            // enum is set before vanilla BattleEndLogic ends the mission.
            var mainAgent = Mission.MainAgent;
            if (mainAgent != null && !mainAgent.IsActive())
            {
                SetOutcome(AmbushOutcome.PlayerDied, successful: false);
                return;
            }
            if (IsPlayerSideWiped())
            {
                SetOutcome(AmbushOutcome.PlayerDefeated, successful: false);
                return;
            }

            bool inZone = AmbushRetreatZone.IsAgentInsideAnyZone(mainAgent);
            if (inZone)
            {
                _timeInZone += dt;
                if (_timeInZone >= _model.ZoneHoldDuration)
                {
                    TaleWorlds.Library.Debug.Print("[AmbushEndLogic] zone-hold complete — extraction successful");
                    TORCommon.Say("Extraction successful.");
                    SetOutcome(AmbushOutcome.ExtractionSuccessful, successful: true);
                }
            }
            else
            {
                _timeInZone = 0f;
            }
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

        private void SetOutcome(AmbushOutcome outcome, bool successful)
        {
            _outcomeDecided = true;
            _result = successful ? MissionResult.CreateSuccessful(Mission) : MissionResult.CreateDefeated(Mission);
            var behavior = Campaign.Current?.GetCampaignBehavior<AmbushCampaignBehavior>();
            if (behavior != null) behavior.LastAmbushOutcome = outcome;
        }

        private bool IsPlayerSideWiped()
        {
            // Player is attacker in our mission (sally-out convention: besieger).
            // Count active human agents on the attacker team; wipe = zero.
            if (Mission.AttackerTeam == null) return false;
            foreach (var agent in Mission.AttackerTeam.ActiveAgents)
            {
                if (agent != null && agent.IsHuman && agent.IsActive()) return false;
            }
            return true;
        }
    }
}
