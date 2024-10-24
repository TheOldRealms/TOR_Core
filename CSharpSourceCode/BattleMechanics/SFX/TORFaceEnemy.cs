using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TOR_Core.AbilitySystem;
using TOR_Core.Extensions;
using TOR_Core.Utilities;

namespace TOR_Core.BattleMechanics.SFX
{
    public class TORFaceArmy: ScriptComponentBehavior
    {
        public bool faceAllied;
        public string CultureId;
        private bool _init;
        
        private void Init()
        {
            var teams = Mission.Current.Teams;
            var alliedTeam = teams.FirstOrDefault(x => x.Leader?.Character.Culture.StringId.ToString() == CultureId);
            if (alliedTeam == null) return;
            var enemyTeam =  Mission.Current.GetEnemyTeamsOf(alliedTeam).FirstOrDefault();
            if (enemyTeam == null) return;
            var target = enemyTeam.GetMedianPosition(enemyTeam.GetAveragePosition()).GetGroundVec3MT();
            var direction = (target - GameEntity.GlobalPosition).NormalizedCopy();
            var rotationNew = faceAllied? Mat3.CreateMat3WithForward(direction):  Mat3.CreateMat3WithForward(-direction);
            var position = GameEntity.GlobalPosition;
            var frameNew = new MatrixFrame(rotationNew, position);
            GameEntity.SetGlobalFrameMT(frameNew);
        }
        
        protected override void OnTick(float dt)
        {
            base.OnTick(dt);
            if (_init) return;
            Init();
            _init = true;
        }
        
        public override TickRequirement GetTickRequirement() => TickRequirement.Tick | base.GetTickRequirement();
    }
}