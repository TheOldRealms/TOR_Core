using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.TwoDimension;
using TOR_Core.AbilitySystem;
using TOR_Core.CharacterDevelopment;
using TOR_Core.CharacterDevelopment.CareerSystem;
using TOR_Core.Extensions;
using TOR_Core.Extensions.ExtendedInfoSystem;
using TOR_Core.HarmonyPatches;
using TOR_Core.Utilities;

namespace TOR_Core.BattleMechanics
{
    public class CareerPerkMissionBehavior : MissionLogic
    {
        
        
        public void OnAgentHealed(Agent affectorAgent, Agent affectedAgent, int totalAmountOfHeal)
        {
            if ((affectorAgent == Agent.Main || affectorAgent.GetOriginMobileParty().IsMainParty) && affectorAgent.IsHero)
            {
                if (Agent.Main.GetHero().HasAnyCareer())
                {
                    if (Agent.Main.GetHero().GetCareer() == TORCareers.GrailDamsel)
                    {
                        var choices = Agent.Main.GetHero().GetAllCareerChoices();
                        if (affectorAgent.IsMainAgent || affectorAgent.IsSpellCaster() && choices.Contains("InspirationOfTheLadyKeystone"))
                        {
                            var cAbility = Agent.Main.GetComponent<AbilityComponent>();
                            if (cAbility != null)
                            {
                                var value = CareerHelper.CalculateChargeForCareer(ChargeType.DamageDone, totalAmountOfHeal, AttackTypeMask.Spell);
                                
                                cAbility.CareerAbility.AddCharge(value);
                            }
                        }
                    }
                }
            }
        }
        public override void OnAgentHit(Agent affectedAgent, Agent affectorAgent, in MissionWeapon affectorWeapon, in Blow blow, in AttackCollisionData attackCollisionData)
        {
            if(affectorAgent.IsMount) return;
            if (affectorAgent.GetOriginMobileParty().IsMainParty && Agent.Main!=null&&  affectorAgent!= Agent.Main && affectorAgent.IsHero)
            {
                
                var choices = Agent.Main.GetHero().GetAllCareerChoices();
                if (choices.Contains("InspirationOfTheLadyKeystone"))
                {
                    if (!affectorAgent.IsSpellCaster()) return;
                    
                    AttackTypeMask mask= DamagePatch.DetermineMask(blow);
                    if (mask == AttackTypeMask.Spell)
                    {
                        var value = CareerHelper.CalculateChargeForCareer(ChargeType.DamageDone, blow.InflictedDamage, mask);
                        var cAbility = Agent.Main.GetComponent<AbilityComponent>();
                        if (cAbility != null)
                        {
                            cAbility.CareerAbility.AddCharge(value);
                        }
                        
                    }
                    
                }
            }
        }

        public override void OnAgentRemoved(Agent affectedAgent, Agent affectorAgent, AgentState agentState, KillingBlow blow)
        {
            if (affectorAgent == null) return;
            if (affectorAgent.IsMainAgent)
            {
                var playerHero = affectorAgent.GetHero();
                var choices = playerHero.GetAllCareerChoices();
                var hitBodyPart = blow.VictimBodyPart;

                if ((hitBodyPart == BoneBodyPartType.Head || hitBodyPart == BoneBodyPartType.Abdomen) && choices.Contains("CourtleyPassive4"))
                {
                    var choice = TORCareerChoices.GetChoice("CourtleyPassive4");
                    if (choice != null)
                    {
                        var value = choice.GetPassiveValue();
                        playerHero.AddWindsOfMagic(value);
                    }
                }

                if (choices.Contains ("AvatarOfDeathPassive4"))
                {
                    var choice = TORCareerChoices.GetChoice("AvatarOfDeathPassive4");

                    if (choice != null)
                    {
                        var threshold = 500;
                        var damage = blow.InflictedDamage - threshold;
                        if (damage >= 0)
                        { 
                            var bonus =  Mathf.Clamp(damage / 100,0, 5);
                            affectorAgent.Heal (bonus);
                       
                        }
                        
                    }
                }
            }
        }
    }
}