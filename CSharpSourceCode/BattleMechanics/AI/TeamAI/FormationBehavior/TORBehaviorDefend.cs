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
            if (Formation.CachedAveragePosition.DistanceSquared(CurrentOrder.GetPosition(Formation)) < 100.0)
            {
                if (Formation.QuerySystem.HasShield)
                    Formation.SetArrangementOrder(ArrangementOrder.ArrangementOrderShieldWall);
                else if (Formation.CachedClosestEnemyFormation?.Formation != null && Formation.CachedAveragePosition.DistanceSquared(Formation.CachedClosestEnemyFormation.Formation.CachedAveragePosition) > 100.0 && Formation.QuerySystem.UnderRangedAttackRatio > 0.200000002980232 - (Formation.ArrangementOrder.OrderEnum == ArrangementOrder.ArrangementOrderEnum.Loose ? 0.100000001490116 : 0.0))
                    Formation.SetArrangementOrder(ArrangementOrder.ArrangementOrderLoose);
                if (TacticalDefendPosition == null)
                    return;

                if (TacticalDefendPosition.TacticalPositionType == TacticalPosition.TacticalPositionTypeEnum.ChokePoint)
                    Formation.SetFormOrder(FormOrder.FormOrderCustom(TacticalDefendPosition.Width));

            }
            else
            {
                Formation.SetArrangementOrder(ArrangementOrder.ArrangementOrderLine);
                Formation.SetFormOrder(FormOrder.FormOrderWide);
                Formation.SetFacingOrder(FacingOrder.FacingOrderLookAtEnemy); //TODO Use position facing in some cases?
            }
        }
    }
}