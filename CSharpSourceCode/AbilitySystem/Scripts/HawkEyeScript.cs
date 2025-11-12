using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace TOR_Core.AbilitySystem.Scripts;

public class HawkEyeScript : CareerAbilityScript
{
    protected override MatrixFrame GetNextGlobalFrame(MatrixFrame oldFrame, float dt)
    {
        return oldFrame;
    }
}