using System.Linq;
using TaleWorlds.MountAndBlade;
using TOR_Core.BattleMechanics.AI.TeamAI;
using TOR_Core.Extensions;

namespace TOR_Core.BattleMechanics.AI.TeamAI.FormationBehavior
{
    public class TORBehaviorProtectArtillery : BehaviorComponent
    {
        public TORBehaviorProtectArtillery(Formation formation) : base(formation)
        {
        }

        public override void TickOccasionally()
        {
            var artillery = FindArtilleryFormation();
            if (artillery == null) return;

            var closestEnemyFormation = artillery.CachedClosestEnemyFormation?.Formation;
            if (closestEnemyFormation != null && closestEnemyFormation.CachedAveragePosition.Distance(artillery.CachedAveragePosition) < 30)
            {
                CurrentOrder = MovementOrder.MovementOrderChargeToTarget(closestEnemyFormation);
                Formation.SetMovementOrder(CurrentOrder);
            }

            var targetAgent = artillery.GetMedianAgent(true, true, artillery.GetAveragePositionOfUnits(true, true));
            if (targetAgent != null)
            {
                CurrentOrder = MovementOrder.MovementOrderFollow(targetAgent);
                Formation.SetMovementOrder(CurrentOrder);
            }
        }

        private Formation FindArtilleryFormation()
        {
            return Formation.Team.GetFormationsIncludingSpecial().ToList().Find(formation => formation.Index == (int)TORFormationClass.Artillery);
        }

        protected override float GetAiWeight() => Formation.Index == (int)TORFormationClass.ArtilleryGuard && FindArtilleryFormation() != null ? 100f : 0.0f;
    }
}