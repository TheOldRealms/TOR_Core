using TaleWorlds.MountAndBlade;
using TOR_Core.BattleMechanics.AI.FormationBehavior;

namespace TOR_Core.BattleMechanics.AI.TeamBehavior
{
    public class TORTeamAIGeneral : TeamAIGeneral
    {
        public TORTeamAIGeneral(Mission currentMission, Team currentTeam, float thinkTimerTime = 10, float applyTimerTime = 1) : base(currentMission, currentTeam, thinkTimerTime, applyTimerTime)
        {
        }

        public override void OnUnitAddedToFormationForTheFirstTime(Formation formation)
        {
            base.OnUnitAddedToFormationForTheFirstTime(formation);

            formation.AI.AddAiBehavior(new BehaviorProtectArtillery(formation));
            
            //TODO: Need to create separate formation for engineers if artillery is available.
        }
    }
}