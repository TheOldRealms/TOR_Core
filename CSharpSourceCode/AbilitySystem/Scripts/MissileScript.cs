using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace TOR_Core.AbilitySystem.Scripts
{
    public class MissileScript : AbilityScript
    {
        private const float AGENT_SWEEP_RAY_THICKNESS = 0.05f;
        private const float MINIMUM_SWEEP_DISTANCE_SQUARED = 0.0001f;

        protected override void OnAfterTick(float dt)
        {
            if (!CanCollide ||
                IsFading ||
                Ability.Template.TriggerType != TriggerType.OnCollision)
            {
                return;
            }

            var movementThisTick = CurrentGlobalPosition - LastFrameGlobalPosition;
            if (movementThisTick.LengthSquared <= MINIMUM_SWEEP_DISTANCE_SQUARED)
            {
                return;
            }

            var casterAgentIndex = CasterAgent.Health <= 0 ? -1 : CasterAgent.Index;

            Agent closestAgent;
            float collisionDistance;

            // on low tick speed as meters per tick increase spells theoretically can go through agents, though this shouldn't be happening in a playable battle
            using (new TWSharedMutexReadLock(Scene.PhysicsAndRayCastLock))
            {
                closestAgent = Mission.Current.RayCastForClosestAgent(
                    LastFrameGlobalPosition,
                    CurrentGlobalPosition,
                    casterAgentIndex,
                    AGENT_SWEEP_RAY_THICKNESS,
                    out collisionDistance);
            }

            if (closestAgent == null ||
                closestAgent.Index == CasterAgent.MountAgent?.Index)
            {
                return;
            }

            var movementDirection = movementThisTick.NormalizedCopy();
            var collisionPosition = LastFrameGlobalPosition + movementDirection * collisionDistance;

            HandleCollision(collisionPosition, movementDirection);
        }
    }
}