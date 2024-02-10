using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace TOR_Core.BattleMechanics.AI.TeamAI.FormationBehavior
{
    public class TORBehaviorDefend : BehaviorDefend
    {
        public TORBehaviorDefend(Formation formation) : base(formation)
        {
        }

        public override void TickOccasionally()
        {
            CalculateCurrentOrder();
            Formation.SetMovementOrder(CurrentOrder);
            if (Formation.QuerySystem.AveragePosition.DistanceSquared(CurrentOrder.GetPosition(Formation)) < 100.0)
            {
                if (Formation.QuerySystem.HasShield)
                    Formation.ArrangementOrder = ArrangementOrder.ArrangementOrderShieldWall;
                else if (Formation.QuerySystem.ClosestSignificantlyLargeEnemyFormation != null && Formation.QuerySystem.AveragePosition.DistanceSquared(Formation.QuerySystem.ClosestSignificantlyLargeEnemyFormation.MedianPosition.AsVec2) > 100.0 && Formation.QuerySystem.UnderRangedAttackRatio > 0.200000002980232 - (Formation.ArrangementOrder.OrderEnum == ArrangementOrder.ArrangementOrderEnum.Loose ? 0.100000001490116 : 0.0))
                    Formation.ArrangementOrder = ArrangementOrder.ArrangementOrderLoose;
                if (TacticalDefendPosition == null)
                    return;

                if (TacticalDefendPosition.TacticalPositionType == TacticalPosition.TacticalPositionTypeEnum.ChokePoint)
                    Formation.FormOrder = FormOrder.FormOrderCustom(TacticalDefendPosition.Width);

            }
            else
            {
                Formation.ArrangementOrder = ArrangementOrder.ArrangementOrderLine;
                Formation.FormOrder = FormOrder.FormOrderWide;
                Formation.FacingOrder = FacingOrder.FacingOrderLookAtEnemy; //TODO Use position facing in some cases?
            }
        }
    }
}