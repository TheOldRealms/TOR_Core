using System;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TOR_Core.BattleMechanics.AI.TeamAI.FormationBehavior;
using TOR_Core.Utilities;

namespace TOR_Core.BattleMechanics.AI.TeamAI.TeamBehavior
{
    public class TORTeamAIGeneral : TeamAIGeneral
    {
        public TORTeamAIGeneral(Mission currentMission, Team currentTeam, float thinkTimerTime = 10, float applyTimerTime = 1) : base(currentMission, currentTeam, thinkTimerTime, applyTimerTime)
        {
            OnNotifyTacticalDecision += RecieveNewTacticalDecision;
        }

        private void RecieveNewTacticalDecision(in TacticalDecision decision)
        {
            TORCommon.Say(string.Format("Team {0} recieved new tactic: {1}", Team.ToString(), CurrentTactic.ToString() ));
        }

        public override void OnUnitAddedToFormationForTheFirstTime(Formation formation)
        {
            if (GameNetwork.IsServer)
            {
                if (formation.AI.GetBehavior<BehaviorCharge>() != null)
                    return;
                AddFormationBehaviors(formation);
            }
            else
            {
                if (GameNetwork.IsClientOrReplay || formation.AI.GetBehavior<BehaviorCharge>() != null)
                    return;
                AddFormationBehaviors(formation);
            }
        }

        private void AddFormationBehaviors(Formation formation)
        {
            if (formation.FormationIndex == FormationClass.NumberOfRegularFormations)
                formation.AI.AddAiBehavior(new BehaviorGeneral(formation));
            else if (formation.FormationIndex == FormationClass.Bodyguard)
                formation.AI.AddAiBehavior(new BehaviorProtectGeneral(formation));

            formation.AI.AddAiBehavior(new BehaviorCharge(formation));
            formation.AI.AddAiBehavior(new BehaviorPullBack(formation));
            formation.AI.AddAiBehavior(new BehaviorRegroup(formation));
            formation.AI.AddAiBehavior(new BehaviorReserve(formation));
            formation.AI.AddAiBehavior(new BehaviorRetreat(formation));
            formation.AI.AddAiBehavior(new BehaviorStop(formation));
            formation.AI.AddAiBehavior(new BehaviorTacticalCharge(formation));
            formation.AI.AddAiBehavior(new BehaviorAdvance(formation));
            formation.AI.AddAiBehavior(new BehaviorCautiousAdvance(formation));
            formation.AI.AddAiBehavior(new BehaviorCavalryScreen(formation));
            formation.AI.AddAiBehavior(new BehaviorDefensiveRing(formation));
            formation.AI.AddAiBehavior(new BehaviorFireFromInfantryCover(formation));
            formation.AI.AddAiBehavior(new BehaviorFlank(formation));
            formation.AI.AddAiBehavior(new BehaviorHoldHighGround(formation));
            formation.AI.AddAiBehavior(new BehaviorHorseArcherSkirmish(formation));
            formation.AI.AddAiBehavior(new BehaviorMountedSkirmish(formation));
            formation.AI.AddAiBehavior(new BehaviorProtectFlank(formation));
            formation.AI.AddAiBehavior(new BehaviorScreenedSkirmish(formation));
            formation.AI.AddAiBehavior(new BehaviorSkirmish(formation));
            formation.AI.AddAiBehavior(new BehaviorSkirmishBehindFormation(formation));
            formation.AI.AddAiBehavior(new BehaviorSkirmishLine(formation));
            formation.AI.AddAiBehavior(new BehaviorVanguard(formation));

            formation.AI.AddAiBehavior(new TORBehaviorDefend(formation));
            formation.AI.AddAiBehavior(new TORBehaviorProtectArtillery(formation));
        }
    }
}