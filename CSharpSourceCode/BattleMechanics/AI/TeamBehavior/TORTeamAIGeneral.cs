using TaleWorlds.MountAndBlade;
using TOR_Core.BattleMechanics.AI.FormationBehavior;

namespace TOR_Core.BattleMechanics.AI.TeamBehavior
{
    public class TORTeamAIGeneral : TeamAIGeneral
    {
        public TORTeamAIGeneral(Mission currentMission, Team currentTeam, float thinkTimerTime = 10, float applyTimerTime = 1) : base(currentMission, currentTeam, thinkTimerTime, applyTimerTime)
        {
        }

        protected override void Tick(float dt)
        {
            base.Tick(dt);
        }
    }
}