using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace TOR_Core.BattleMechanics.AI.FormationBehavior
{
    public class TorBehaviorDefend:BehaviorDefend
    {
        public TorBehaviorDefend(Formation formation) : base(formation)
        {
        }

        public override void TickOccasionally()
        {
            CalculateCurrentOrder();
            Formation.SetMovementOrder(CurrentOrder);
            Formation.FacingOrder = CurrentFacingOrder;
            if (Formation.QuerySystem.AveragePosition.DistanceSquared(CurrentOrder.GetPosition(Formation)) < 100.0)
            {
                if (Formation.QuerySystem.HasShield)
                    Formation.ArrangementOrder = ArrangementOrder.ArrangementOrderShieldWall;
                else if (Formation.QuerySystem.ClosestSignificantlyLargeEnemyFormation != null && Formation.QuerySystem.AveragePosition.DistanceSquared(Formation.QuerySystem.ClosestSignificantlyLargeEnemyFormation.MedianPosition.AsVec2) > 100.0 && Formation.QuerySystem.UnderRangedAttackRatio > 0.200000002980232 - (Formation.ArrangementOrder.OrderEnum == ArrangementOrder.ArrangementOrderEnum.Loose ? 0.100000001490116 : 0.0))
                    Formation.ArrangementOrder = ArrangementOrder.ArrangementOrderLoose;
                // if (TacticalDefendPosition == null)
                //     return;
                // float customWidth;
                // if (TacticalDefendPosition.TacticalPositionType == TacticalPosition.TacticalPositionTypeEnum.ChokePoint)
                // {
                //     customWidth = TacticalDefendPosition.Width;
                // }
                // else
                // {
                //     int countOfUnits = Formation.CountOfUnits;
                //     customWidth = MathF.Min(TacticalDefendPosition.Width, (float) (Formation.Interval * (double) (countOfUnits - 1) + Formation.UnitDiameter * (double) countOfUnits) / 3f);
                // }
                // Formation.FormOrder = FormOrder.FormOrderCustom(customWidth);
            }
            else
            {
                Formation.ArrangementOrder = ArrangementOrder.ArrangementOrderLine;
                Formation.FormOrder = FormOrder.FormOrderWide;
            }
        }
    }
}