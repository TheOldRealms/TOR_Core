using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace TOR_Core.AbilitySystem.Scripts
{
    public class MissileScript : AbilityScript
    {
        override protected bool CollidedWithAgent()
        {
            return base.CollidedWithAgent();
            /*
            var collisionRadius = Ability.Template.Radius;
            var index = CasterAgent.Health <= 0 ? -1 : CasterAgent.Index;
            Agent closestAgent;
            
            using(new TWSharedMutexReadLock(Scene.PhysicsAndRayCastLock))
            {
                closestAgent = Mission.Current.RayCastForClosestAgent(LastFrameGlobalPosition, CurrentGlobalPosition, index, 0.05f, out _);
            }
            
            return closestAgent != null && closestAgent.Index != CasterAgent.MountAgent?.Index;
            */
        }
    }
}