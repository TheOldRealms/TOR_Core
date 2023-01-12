using System.Linq;
using TaleWorlds.MountAndBlade;
using TOR_Core.BattleMechanics.AI.TeamBehavior;

namespace TOR_Core.BattleMechanics.AI.FormationBehavior
{
    public class BehaviorProtectArtilleryCrew : BehaviorComponent
    {
        public BehaviorProtectArtilleryCrew(Formation formation) : base(formation)
        {
        }

        public override void TickOccasionally()
        {
            var artillery = FindArtilleryFormation();
            if (artillery == null) return;
            
            var closestEnemyFormation = artillery.QuerySystem.ClosestEnemyFormation;
            if (closestEnemyFormation != null && closestEnemyFormation.AveragePosition.Distance(artillery.QuerySystem.AveragePosition) < 10)
            {
                CurrentOrder = MovementOrder.MovementOrderChargeToTarget(closestEnemyFormation.Formation);
                Formation.SetMovementOrder(CurrentOrder);
            }

            var targetAgent = artillery.GetMedianAgent(false, true, artillery.GetAveragePositionOfUnits(true, true));
            if (targetAgent != null)
            {
                CurrentOrder = MovementOrder.MovementOrderFollow(targetAgent);
                Formation.SetMovementOrder(CurrentOrder);
            }
        }

        private Formation FindArtilleryFormation()
        {
            return Formation.Team.FormationsIncludingSpecialAndEmpty.ToList().Find(formation => formation.Index == (int) TORFormationClass.Artillery);
        }

        protected override float GetAiWeight() => Formation.Index == (int) TORFormationClass.ArtilleryGuard && FindArtilleryFormation() != null ? 100f : 0.0f;
    }
}