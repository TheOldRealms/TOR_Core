using TaleWorlds.MountAndBlade;

namespace TOR_Core.BattleMechanics.AI.FormationBehavior
{
    public class BehaviorProtectArtillery : BehaviorComponent
    {
        private readonly TacticComponent _relatedTactic;
        public Formation TargetFormation { get; set; }


        public BehaviorProtectArtillery(Formation formation, Formation targetFormation, TacticComponent relatedTactic) : base(formation)
        {
        
            _relatedTactic = relatedTactic;
            TargetFormation = targetFormation;
        }

        public override void TickOccasionally()
        {
            // MovementOrder.MovementOrderFollowEntity() //TODO: Follow artillery entity instead?
            var targetAgent = TargetFormation.GetFirstUnit();
            MovementOrder.MovementOrderFollow(targetAgent);
        }
        public float GetAIWeight() => Formation.Team.TeamAI.IsCurrentTactic(_relatedTactic) ? 100f : 0.0f;
        protected override float GetAiWeight() => Formation.Team.TeamAI.IsCurrentTactic(_relatedTactic) ? 100f : 0.0f;
    }
}