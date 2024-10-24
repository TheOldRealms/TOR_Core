using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace TOR_Core.AbilitySystem.Scripts
{
    public class WindScript : AbilityScript
    {
        protected override MatrixFrame GetNextGlobalFrame(MatrixFrame oldFrame, float dt)
        {
            var frame = base.GetNextGlobalFrame(oldFrame, dt);
            float heightAtPosition;
            using (new TWSharedMutexReadLock(Scene.PhysicsAndRayCastLock))
            {
                heightAtPosition = Mission.Current.Scene.GetGroundHeightAtPositionMT(frame.origin);
            }
            frame.origin.z = heightAtPosition + Ability.Template.Radius / 2;
            return frame;
        }
    }
}
