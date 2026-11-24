using NLog;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Missions.MissionLogics;
using TaleWorlds.MountAndBlade.Missions.Objectives;
using TOR_Core.CampaignMechanics.Ambush;
using TOR_Core.Extensions;
using TOR_Core.Models.Ambush;
using TOR_Core.Utilities;

namespace TOR_Core.Missions.AmbushMission
{
    /// <summary>
    /// Shared mission-logic scaffolding for the ambush missions. Owns the
    /// outcome-decided latch, deployment-finished gate, model + reinforcement
    /// + objective handles, and the SetOutcome plumbing. Subclasses implement
    /// the role-specific state machine in <see cref="OnMissionTickReady"/>
    /// and write role-specific flags in <see cref="WriteRoleFlags"/>.
    /// </summary>
    public abstract class AmbushMissionLogicBase : MissionLogic
    {
        protected bool _outcomeDecided;
        protected bool _deploymentFinished;
        protected MissionResult _result;
        protected TORMissionAmbushModel _model;
        protected AmbushReinforcementMissionLogic _reinforcementLogic;
        protected MissionObjectiveLogic _objectiveLogic;
        protected AmbushObjective _objective;

        /// <summary>Prefix for log lines emitted from the shared SetOutcome path.</summary>
        protected abstract string LogTag { get; }

        public override void AfterStart()
        {
            base.AfterStart();
            _model = Campaign.Current?.Models?.GetMissionAmbushModel() ?? new TORMissionAmbushModel();
            _reinforcementLogic = Mission.GetMissionBehavior<AmbushReinforcementMissionLogic>();
            _objectiveLogic = Mission.GetMissionBehavior<MissionObjectiveLogic>();
            OnAfterStarted();
        }

        /// <summary>Role-specific init: objective start, roster snapshot, etc.</summary>
        protected virtual void OnAfterStarted() { }

        public override void OnDeploymentFinished()
        {
            base.OnDeploymentFinished();
            _deploymentFinished = true;
        }

        public sealed override void OnMissionTick(float dt)
        {
            base.OnMissionTick(dt);
            if (_outcomeDecided) return;
            // Deployment not finished: engine reports 0 active humans while agents are spawning.
            if (!_deploymentFinished) return;
            OnMissionTickReady(dt);
        }

        /// <summary>Role-specific outcome state machine. Called once per tick after deployment is live and before an outcome is latched.</summary>
        protected abstract void OnMissionTickReady(float dt);

        public override bool MissionEnded(ref MissionResult missionResult)
        {
            if (!_outcomeDecided) return false;
            missionResult = _result;
            return true;
        }

        /// <summary>
        /// Record the ambush resolution on the campaign behavior, build the
        /// MissionResult, and let the subclass react before logging.
        /// </summary>
        protected void SetOutcome(AmbushResolution resolution, bool successful, string reason,
                                   bool leaderDown = false)
        {
            _outcomeDecided = true;
            _result = successful
                ? MissionResult.CreateSuccessful(Mission)
                : MissionResult.CreateDefeated(Mission);
            var behavior = Campaign.Current?.GetCampaignBehavior<AmbushCampaignBehavior>();
            if (behavior != null)
            {
                behavior.LastResolution = resolution;
                WriteRoleFlags(behavior, leaderDown);
            }
            OnOutcomeSet(resolution, successful, reason, behavior);
            TORCommon.Log(LogLevel.Info,
                LogTag + " resolution=" + resolution + " successful=" + successful
                + " (" + reason + ")");
        }

        /// <summary>
        /// Write the role flags on the campaign behavior (which side the player
        /// was on and which LeaderDown field the <paramref name="leaderDown"/>
        /// bit maps to).
        /// </summary>
        protected abstract void WriteRoleFlags(AmbushCampaignBehavior behavior, bool leaderDown);

        /// <summary>
        /// Hook for role-specific side effects at outcome time (eg.
        /// SetPlayerVictorious on the ambusher's TotalVictory). Behavior
        /// may be null if the ambush campaign behavior wasn't found.
        /// </summary>
        protected virtual void OnOutcomeSet(AmbushResolution resolution, bool successful, string reason,
                                             AmbushCampaignBehavior behavior) { }

        /// <summary>True while the reinforcement wave timer may still deliver another batch.</summary>
        protected bool AreReinforcementsStillPending()
        {
            if (_reinforcementLogic == null) return false;
            return !_reinforcementLogic.IsPoolDepleted;
        }

        /// <summary>Count the live human agents on a team.</summary>
        protected static int CountActiveHumans(Team team)
        {
            if (team == null) return 0;
            int n = 0;
            foreach (var a in team.ActiveAgents)
                if (a != null && a.IsHuman && a.IsActive()) n++;
            return n;
        }
    }
}
