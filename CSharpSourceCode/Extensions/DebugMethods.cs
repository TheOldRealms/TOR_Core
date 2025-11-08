using TaleWorlds.Engine;
using TOR_Core.BattleMechanics.AI.CommonAIFunctions;

namespace TOR_Core.Extensions
{
    public static class DebugMethods
    {
        public static void DebugRender(this Target target, float time = 30, float size = 2)
        {
            if (target.TacticalPosition != null)
            {
                MBDebug.RenderDebugText3D(target.TacticalPosition.Position.GetGroundVec3MT(), target.UtilityValue.ToString(), 4294967295U, 0, 0, time);
                MBDebug.RenderDebugSphere(target.TacticalPosition.Position.GetGroundVec3MT(), size, 4294967295, false, time);
                MBDebug.RenderDebugDirectionArrow(target.TacticalPosition.Position.GetGroundVec3MT(), target.TacticalPosition.Direction.ToVec3());
            }
        }
    }
}