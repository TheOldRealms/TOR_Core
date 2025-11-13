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

            if (targetFormation.CountOfUnitsWithoutDetachedOnes < 1) return target; //test to skip extra calculations for median agent (up to 3 calculations for formations > 10 troops

            if (targetFormation.CountOfUnitsWithoutDetachedOnes > 10) //GetRandomAgent uses excludeDetachedUnits = true - using the wrong count can cause a null to be set for target.Agent
            {
                var medianAgent = CommonAIFunctions.CommonAIFunctions.GetRandomAgent(targetFormation);
                if (medianAgent != null)
                {
                    target.Agent = medianAgent;
                    Vec3 adjustedPosition = medianAgent.Position;
                    adjustedPosition += ComputeSpellAngleVelocityCorrection(medianAgent.Position, medianAgent.Velocity);
                    target.SelectedWorldPosition = adjustedPosition;
                }
            }
            else
            {
                var medianAgent = targetFormation?.GetMedianAgent(true, false, targetFormation.GetAveragePositionOfUnits(true, false));
                if (medianAgent != null)
                    target.Agent = medianAgent;
            }

            return target;
        }

        protected override bool HaveLineOfSightToTarget(Target target)
        {
            var targetPoint = target.GetPositionPrioritizeCalculated();
            if (targetPoint == Vec3.Invalid) return false;
            targetPoint.z += 0.75f;
            Agent collidedAgent;
            float distance;

            using (new TWSharedMutexReadLock(Scene.PhysicsAndRayCastLock))
            {
                collidedAgent = Mission.Current.RayCastForClosestAgent(Agent.Position + new Vec3(z: Agent.GetEyeGlobalHeight()), targetPoint, Agent.Index, 0.25f, out _);
                Mission.Current.Scene.RayCastForClosestEntityOrTerrain(Agent.Position + new Vec3(z: Agent.GetEyeGlobalHeight()), targetPoint, out distance, out _, out _, 0.25f);
            }

            return Agent.GetChestGlobalPosition().Distance(targetPoint) > 1 && (distance is Single.NaN || distance > 1) &&
                   (collidedAgent == null || collidedAgent.IsEnemyOf(Agent) || collidedAgent.GetChestGlobalPosition().Distance(targetPoint) < 4) &&
                   (float.IsNaN(distance) || Math.Abs(distance - targetPoint.Distance(Agent.Position)) < 0.3);
        }
    }
}