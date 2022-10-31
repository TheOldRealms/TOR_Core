using TaleWorlds.MountAndBlade;

namespace TOR_Core.BattleMechanics.AI.FormationBehavior
{
    public class BehaviorProtectArtilleryCrew : BehaviorComponent
    {
        private readonly TacticComponent _relatedTactic;
        public Formation TargetFormation { get; set; }


        public BehaviorProtectArtilleryCrew(Formation formation, Formation targetFormation, TacticComponent relatedTactic) : base(formation)
        {
            _relatedTactic = relatedTactic;
            TargetFormation = targetFormation;
        }

        public override void TickOccasionally()
        {
            var closestEnemyFormation = TargetFormation.QuerySystem.ClosestEnemyFormation;
            if (closestEnemyFormation != null && closestEnemyFormation.AveragePosition.Distance(TargetFormation.QuerySystem.AveragePosition) < 10)
            {
                CurrentOrder = MovementOrder.MovementOrderChargeToTarget(closestEnemyFormation.Formation);
                Formation.SetMovementOrder(CurrentOrder);
            }

            var targetAgent = TargetFormation.GetMedianAgent(false, true, TargetFormation.GetAveragePositionOfUnits(true, true));
            if (targetAgent != null)
            {
                CurrentOrder = MovementOrder.MovementOrderFollow(targetAgent);
                Formation.SetMovementOrder(CurrentOrder);
            }
        }

        public new float GetAIWeight() => Formation.Team.TeamAI.IsCurrentTactic(_relatedTactic) ? 100f : 0.0f;
        protected override float GetAiWeight() => Formation.Team.TeamAI.IsCurrentTactic(_relatedTactic) ? 100f : 0.0f;
    }
}