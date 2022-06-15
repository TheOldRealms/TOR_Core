using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace TOR_Core.AbilitySystem.Scripts
{
    public class WindScript : AbilityScript
    {

        protected override MatrixFrame GetNextFrame(MatrixFrame oldFrame, float dt)
        {
            var frame = base.GetNextFrame(oldFrame, dt);
            var heightAtPosition = Mission.Current.Scene.GetGroundHeightAtPosition(frame.origin);
            frame.origin.z = heightAtPosition + base._ability.Template.Radius / 2;
            return frame;
        }
    }
}
