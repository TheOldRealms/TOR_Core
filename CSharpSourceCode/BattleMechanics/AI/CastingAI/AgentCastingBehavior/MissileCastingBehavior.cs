using System;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TOR_Core.AbilitySystem;
using TOR_Core.BattleMechanics.AI.CommonAIFunctions;

namespace TOR_Core.BattleMechanics.AI.CastingAI.AgentCastingBehavior
{
    public class MissileCastingBehavior : AbstractAgentCastingBehavior
    {
        public MissileCastingBehavior(Agent agent, AbilityTemplate template, int abilityIndex) : base(agent, template,
            abilityIndex)
        {
            Hysteresis = 0.1f;
        }

        protected override Target UpdateTarget(Target target)
        {
            var targetFormation = CurrentTarget.Formation;
            var medianAgent = targetFormation?.GetMedianAgent(true, false, targetFormation.GetAveragePositionOfUnits(true, false));
            if (medianAgent == null) return target;

            if (targetFormation.CountOfUnits > 10)
            {
                medianAgent = CommonAIFunctions.CommonAIFunctions.GetRandomAgent(targetFormation);
                target.Agent = medianAgent;
                Vec3 adjustedPosition = medianAgent.Position;
                adjustedPosition += ComputeSpellAngleVelocityCorrection(medianAgent.Position, medianAgent.Velocity);
                target.SelectedWorldPosition = adjustedPosition;
            }

            target.Agent = medianAgent;
            return target;
        }

        protected override bool HaveLineOfSightToTarget(Target target)
        {
            var targetPoint = target.GetPositionPrioritizeCalculated();
            targetPoint.z += 0.75f;
            Agent collidedAgent;
            float distance;

            using(new TWSharedMutexReadLock(Scene.PhysicsAndRayCastLock))
            {
                collidedAgent = Mission.Current.RayCastForClosestAgent(Agent.Position + new Vec3(z: Agent.GetEyeGlobalHeight()), targetPoint, out float _, Agent.Index, 0.4f);
                Mission.Current.Scene.RayCastForClosestEntityOrTerrainMT(Agent.Position + new Vec3(z: Agent.GetEyeGlobalHeight()), targetPoint, out distance, out GameEntity _, 0.4f);
            }

            return Agent.GetChestGlobalPosition().Distance(targetPoint) > 1 && (distance is Single.NaN || distance > 1) &&
                   (collidedAgent == null || collidedAgent.IsEnemyOf(Agent) || collidedAgent.GetChestGlobalPosition().Distance(targetPoint) < 4) &&
                   (float.IsNaN(distance) || Math.Abs(distance - targetPoint.Distance(Agent.Position)) < 0.3);
        }
    }
}