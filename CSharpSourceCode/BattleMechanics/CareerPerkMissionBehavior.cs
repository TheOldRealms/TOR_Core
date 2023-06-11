using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TOR_Core.CharacterDevelopment;
using TOR_Core.Extensions;

namespace TOR_Core.BattleMechanics
{
    public class CareerPerkMissionBehavior : MissionLogic
    {
        public override MissionBehaviorType BehaviorType { get; }


        public override void OnAgentRemoved(Agent affectedAgent, Agent affectorAgent, AgentState agentState, KillingBlow blow)
        {
            if(affectorAgent==null)return;
            if (affectorAgent.IsMainAgent)
            {
                var playerHero = affectorAgent.GetHero();
                var choices = playerHero.GetAllCareerChoices();
                //if(affectedAgent.Health >= 0&& affectedAgent.State!=AgentState.Killed) return;    //Kill perks

                var hitBodyPart = blow.VictimBodyPart;
            
                if ((hitBodyPart == BoneBodyPartType.Head||hitBodyPart== BoneBodyPartType.Abdomen)&& choices.Contains("CourtleyPassive4"))
                {
                    var choice = TORCareerChoices.GetChoice("CourtleyPassive4");
                    if (choice != null)
                    {
                        var value = choice.GetPassiveValue();
                        playerHero.AddWindsOfMagic(value);
                    }
                }
            }
        }
    }
}