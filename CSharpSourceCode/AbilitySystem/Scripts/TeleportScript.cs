using System.Collections.Generic;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View.MissionViews;
using TOR_Core.BattleMechanics.TriggeredEffect.Scripts;
using TOR_Core.Extensions;
using TOR_Core.Utilities;

namespace TOR_Core.AbilitySystem.Scripts
{
    public class TeleportScript : CareerAbilityScript
    {
        private MissionCameraFadeView _cameraView;
        protected Vec3 targetPosition;

        protected override void OnInit()
        {
            targetPosition = GameEntity.GlobalPosition; //works for what ever reason better
            _cameraView = Mission.Current.GetMissionBehavior<MissionCameraFadeView>();
        }

        protected override void OnAfterTick(float dt)
        {
            if (CasterAgent == null)
            {
                Stop();
                return;
            }

            // Get radius from triggered effects (first effect defines radius, default to 5 if not set)
            var radius = 5f;
            if (EffectsToTrigger != null && EffectsToTrigger.Count > 0)
            {
                radius = EffectsToTrigger[0].EffectRadius <= 0 ? 5f : EffectsToTrigger[0].EffectRadius;
            }

            // Teleport nearby allies within radius
            var troops = Mission.Current.GetNearbyAllyAgents(CasterAgent.Position.AsVec2, radius, CasterAgent.Team, new MBList<Agent>());
            troops.Remove(CasterAgent); // so the player is not affected

            foreach (var troop in troops)
            {
                troop.TeleportToPosition(Mission.Current.GetRandomPositionAroundPoint(targetPosition, 1, 3));
            }

            _cameraView.BeginFadeOutAndIn(0.1f, 0.1f, 0.5f);

            CasterAgent.TeleportToPosition(targetPosition);

            Stop();
        }
    }

    public class DamselTeleportScript : CareerAbilityScript
    {
        private MissionCameraFadeView _cameraView;
        private Vec3 targetPosition;

        protected override void OnInit()
        {
            targetPosition = GameEntity.GlobalPosition;
            _cameraView = Mission.Current.GetMissionBehavior<MissionCameraFadeView>();
        }

        protected override void OnAfterTick(float dt)
        {
            if (CasterAgent == null)
            {
                Stop();
                return;
            }

            // Only teleport allies if the Damsel has the EnvoyOfTheLadyKeystone career choice
            if (CasterAgent.GetHero().HasCareerChoice("EnvoyOfTheLadyKeystone"))
            {
                var troops = Mission.Current.GetNearbyAllyAgents(CasterAgent.Position.AsVec2, 5f, CasterAgent.Team, new MBList<Agent>());
                troops.Remove(CasterAgent);

                foreach (var troop in troops)
                {
                    troop.TeleportToPosition(Mission.Current.GetRandomPositionAroundPoint(targetPosition, 1, 3));
                }
            }

            // Always teleport the caster
            _cameraView.BeginFadeOutAndIn(0.1f, 0.1f, 0.5f);
            CasterAgent.TeleportToPosition(targetPosition);

            Stop();
        }
    }

    /// <summary>
    /// Triggered script for teleportation effects used by spells (e.g., DaHandUvGork).
    /// </summary>
    public class TeleportTriggeredScript : ITriggeredScript
    {
        public void OnTrigger(Vec3 position, Agent triggeredByAgent, IEnumerable<Agent> triggeredAgents, float duration)
        {
            if (triggeredByAgent == null || !triggeredByAgent.IsActive()) return;

            // Camera fade effect for the teleportation
            var cameraView = Mission.Current?.GetMissionBehavior<MissionCameraFadeView>();
            cameraView?.BeginFadeOutAndIn(0.1f, 0.1f, 0.5f);

            // Teleport the caster to the target position
            triggeredByAgent.TeleportToPosition(position);

            // Teleport all affected agents (nearby allies) to random positions around the target
            foreach (var agent in triggeredAgents)
            {
                if (agent != null && agent != triggeredByAgent && agent.IsActive())
                {
                    var randomPos = Mission.Current.GetRandomPositionAroundPoint(position, 1, 3);
                    agent.TeleportToPosition(randomPos);
                }
            }
        }
    }
}