using TaleWorlds.MountAndBlade;

namespace TOR_Core.BattleMechanics.AI.TeamBehavior.Tactic
{
    public class TacticOffensiveArtillery: TacticRangedHarrassmentOffensive
    {

        public TacticOffensiveArtillery(Team team) : base(team)
        {
        }

        protected override float GetTacticWeight()
        {
            float tacticWeight = base.GetTacticWeight();
            
            return tacticWeight;
        }
    }
}
