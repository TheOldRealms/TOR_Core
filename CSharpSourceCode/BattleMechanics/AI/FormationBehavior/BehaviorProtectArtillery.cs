using TaleWorlds.MountAndBlade;

namespace TOR_Core.BattleMechanics.AI.FormationBehavior
{
    public class BehaviorProtectArtillery : BehaviorComponent
    {
        public BehaviorProtectArtillery(Formation formation) : base(formation)
        {
            CurrentOrder = MovementOrder.MovementOrderFollow(DetermineArtilleryFormation().GetFirstUnit());
        }

        private Formation DetermineArtilleryFormation()
        {
            return Formation.Team.FormationsIncludingSpecialAndEmpty[11];
        }

        protected override float GetAiWeight() => 0f; //TODO: If artillery operational 1 else 0
    }
}