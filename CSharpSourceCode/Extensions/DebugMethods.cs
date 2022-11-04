using TaleWorlds.Engine;
using TOR_Core.BattleMechanics.AI.Decision;

namespace TOR_Core.Extensions
{
    public static class DebugMethods
    {
        public static void DebugRender(this Target target)
        {
            if (target.TacticalPosition != null)
            {
                MBDebug.RenderDebugText3D(target.TacticalPosition.Position.GetGroundVec3(), target.UtilityValue.ToString(), 4294967295U, 0, 0, 30);
                MBDebug.RenderDebugSphere(target.TacticalPosition.Position.GetGroundVec3(), 2, 4294967295, false, 30);
                MBDebug.RenderDebugDirectionArrow(target.TacticalPosition.Position.GetGroundVec3(), target.TacticalPosition.Direction.ToVec3());
            }
        }
    }
}