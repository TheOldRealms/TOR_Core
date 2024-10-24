using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace TOR_Core.AbilitySystem.Scripts
{
    public class MissileScript : AbilityScript
    {
        override protected bool CollidedWithAgent()
        {
            var collisionRadius = Ability.Template.Radius;
            var index = CasterAgent.Health <= 0 ? -1 : CasterAgent.Index;
            Agent closestAgent;
            
            using(new TWSharedMutexReadLock(Scene.PhysicsAndRayCastLock))
            {
                closestAgent = Mission.Current.RayCastForClosestAgent(LastFrameGlobalPosition, CurrentGlobalPosition, out float _, index, collisionRadius);
            }
            
            return closestAgent != null && closestAgent.Index != CasterAgent.MountAgent?.Index;
        }
    }
}