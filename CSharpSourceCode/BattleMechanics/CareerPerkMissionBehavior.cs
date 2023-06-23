using System;
using System.Linq;
using Helpers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TOR_Core.CharacterDevelopment;
using TOR_Core.Extensions;
using TOR_Core.Utilities;

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

            if (affectorAgent.IsMainAgent || affectorAgent.GetOriginMobileParty() == MobileParty.MainParty)
            {
                var playerHero = affectorAgent.GetHero();
                var choices = playerHero.GetAllCareerChoices();
                
                if ((choices.Contains("HeadhunterPassive3")))
                {
                    var isHighValueTarget = false;
                    if (affectedAgent.Character.Culture.IsBandit)
                    {
                        if(Mission.Current.Mode == MissionMode.Tournament) return;
                        
                        var cultureObject = affectedAgent.Character.GetCultureObject();
                        if (cultureObject != null && cultureObject.BanditBoss == affectedAgent.Character)
                        {
                            isHighValueTarget = true;
                        }
                    }
                    else
                    {
                        if(playerHero.PartyBelongedTo.ActualClan.MapFaction.IsKingdomFaction&&playerHero.Clan.IsUnderMercenaryService && 
                           affectedAgent.IsHero && affectedAgent.GetHero().Occupation == Occupation.Lord)
                        {
                            isHighValueTarget = true;
                        }
                    }
                    
                    if(!isHighValueTarget)return;
                    
                    var choice = TORCareerChoices.GetChoice("HeadhunterPassive3");
                    if (choice != null)
                    {
                        var value = (int)choice.GetPassiveValue();
                        playerHero.Gold += value;
                        InformationManager.DisplayMessage(new InformationMessage($"Contract Complete. You earned {value} from a bounty", Color.FromUint(16744448)));
                    }
                }
            }
        }
    }
}