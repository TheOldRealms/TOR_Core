using System.Linq;
using TaleWorlds.MountAndBlade;
using TOR_Core.AbilitySystem;
using TOR_Core.Extensions;

namespace TOR_Core.BattleMechanics.AI.TeamBehavior.Tactic
{
    //TODO: Create proper offensive artillery logic
    public class TacticOffensiveArtillery : TacticRangedHarrassmentOffensive
    {
        private readonly ArtilleryPositioningComponent _artilleryPositioningComponent;

        public TacticOffensiveArtillery(Team team) : base(team)
        {
            _artilleryPositioningComponent = new ArtilleryPositioningComponent(team);
        }

        protected override float GetTacticWeight()
        {
            if (!_artilleryPositioningComponent.CanArtilleryBePlaced()) return 0.0f;

            float tacticWeight = base.GetTacticWeight();
            return tacticWeight * 1.1f;
        }
    }
}
