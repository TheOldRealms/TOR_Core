using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace TOR_Core.AbilitySystem.Scripts
{
    public class WindScript : AbilityScript
    {

        protected override MatrixFrame GetNextGlobalFrame(MatrixFrame oldFrame, float dt)
        {
            var frame = base.GetNextGlobalFrame(oldFrame, dt);
            var heightAtPosition = Mission.Current.Scene.GetGroundHeightAtPosition(frame.origin);
            frame.origin.z = heightAtPosition + Ability.Template.Radius / 2;
            return frame;
        }
    }
}
