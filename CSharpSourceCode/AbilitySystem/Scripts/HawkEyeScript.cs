using TaleWorlds.Library;

namespace TOR_Core.AbilitySystem.Scripts;

public class HawkEyeScript : CareerAbilityScript
{
    protected override MatrixFrame GetNextGlobalFrame(MatrixFrame oldFrame, float dt)
    {
        return oldFrame;
    }
}