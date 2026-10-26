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
        private MissionResult _result;
        private TORMissionAmbushModel _model;

        public override void AfterStart()
        {
            base.AfterStart();
            _model = Campaign.Current?.Models?.GetMissionAmbushModel() ?? new TORMissionAmbushModel();
        }

        public override void OnMissionTick(float dt)
        {
            base.OnMissionTick(dt);
            if (_outcomeDecided) return;

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
                    TORCommon.Say("Extraction successful.");
                    SetOutcome(AmbushOutcome.ExtractionSuccessful, successful: true);
                }
            }
            else
            {
                _timeInZone = 0f;
            }
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
