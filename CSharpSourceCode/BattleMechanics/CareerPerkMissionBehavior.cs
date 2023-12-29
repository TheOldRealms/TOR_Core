using System;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;
using TaleWorlds.TwoDimension;
using TOR_Core.AbilitySystem;
using TOR_Core.AbilitySystem.Scripts;
using TOR_Core.BattleMechanics.StatusEffect;
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
                            Agent.Main.CalculateCareerAbilityCharge(totalAmountOfHeal,ChargeType.DamageDone,AttackTypeMask.Spell);
                        }
                    }
                    if (Agent.Main.GetHero().GetCareer() == TORCareers.Necromancer)
                    {
                        Agent.Main.CalculateCareerAbilityCharge(totalAmountOfHeal,ChargeType.DamageDone,AttackTypeMask.Spell);
                    }
                }
            }
        }
        public override void OnAgentHit(Agent affectedAgent, Agent affectorAgent, in MissionWeapon affectorWeapon, in Blow blow, in AttackCollisionData attackCollisionData)
        {
            if(affectedAgent==null) return;
            
            if(affectedAgent.IsMount) return;
            
            if(affectorAgent == null) return;
            if(affectorAgent.IsMount) return;

            if ((blow.InflictedDamage>=affectedAgent.Health)&&affectedAgent.HasAttribute("NecromancerChampion") && affectedAgent.IsMainAgent )
            {
                var agent = Mission.Current.Agents.FirstOrDefault(x => x.IsHero && x.GetHero() == Hero.MainHero);

                if (agent != null) agent.Controller = Agent.ControllerType.Player;
            }
            
            
            if (affectorAgent.GetOriginMobileParty()!=null&&affectorAgent.GetOriginMobileParty().IsMainParty && Agent.Main!=null&&  affectorAgent!= Agent.Main && affectorAgent.IsHero)
            {
                var choices = Agent.Main.GetHero().GetAllCareerChoices();
                if (choices.Contains("InspirationOfTheLadyKeystone"))
                {
                    if (!affectorAgent.IsSpellCaster()) return;
                    
                    AttackTypeMask mask= DamagePatch.DetermineMask(blow);
                    if (mask == AttackTypeMask.Spell)
                    {
                        Agent.Main.CalculateCareerAbilityCharge(blow.InflictedDamage,ChargeType.DamageDone, mask);
                    }
                }
            }
            
            
            
            

            if( affectorAgent.IsHero 
                                    && (affectorAgent.IsMainAgent && affectorAgent.GetHero().HasCareer(TORCareers.WitchHunter) || affectorAgent.IsHero&&
                                        (affectorAgent.GetHero().PartyBelongedTo == MobileParty.MainParty && Hero.MainHero.HasCareerChoice("NoRestAgainstEvilKeystone"))))
            {
                var comp = affectedAgent.GetComponent<StatusEffectComponent>();
                if(comp==null) return;
                var temporaryEffects = comp.GetTemporaryAttributes();

                if (temporaryEffects.Contains("AccusationMark"))
                {
                    var choices = Hero.MainHero.GetAllCareerChoices();
                    
                    AbilityTemplate ability = affectorAgent.GetComponent<AbilityComponent>().CareerAbility.Template;

                    var chance = ability.ScaleVariable1;

                    chance = Mathf.Clamp(chance, 0.1f, 1);

                    var targets = new MBList<Agent>();

                    if (choices.Contains("EndsJustifiesMeansKeystone")&&affectedAgent.HealthLimit <= blow.InflictedDamage)
                    {
                        targets.AddRange(AccusationScript.GetAdditionalAccusationMarkTargets(affectedAgent.Position.AsVec2,1));
                    }
                    
                    var script = (AccusationScript)affectorAgent.GetComponent<AbilityComponent>().CareerAbility.AbilityScript;
                    var triggeredEffect = script.GetEffects()[0];
                    TORCommon.Say("chance was"+ chance);
                    if (MBRandom.RandomFloat <= chance)
                    {
                        
                        targets.Add(affectedAgent);
                        
                        if (choices.Contains("GuiltyByAssociationKeystone"))
                        {
                            targets.AddRange(AccusationScript.GetAdditionalAccusationMarkTargets(affectedAgent.Position.AsVec2));
                        }
                    }
                    else
                    {
                        affectedAgent.GetComponent<StatusEffectComponent>().RemoveStatusEffect("accusation_debuff",true);
                    }
                    
                    if(triggeredEffect==null) return;
                    
                    foreach (var target in targets)
                    {
                        foreach (var statusEffect in triggeredEffect.StatusEffects)
                        {
                            target.GetComponent<StatusEffectComponent>().RunStatusEffect(statusEffect, affectorAgent, triggeredEffect.ImbuedStatusEffectDuration,true,true);
                        }
                    }

                    
                }
            }


            if (affectorAgent.GetOriginMobileParty() == MobileParty.MainParty && affectorAgent.IsUndead())
            {
                Agent.Main.CalculateCareerAbilityCharge(blow.InflictedDamage,ChargeType.DamageDone,AttackTypeMask.Melee);
            }


            
        }

        public override void OnAgentRemoved(Agent affectedAgent, Agent affectorAgent, AgentState agentState, KillingBlow blow)
        {
            if (affectorAgent == null) return;
            if(affectedAgent.IsMount) return;
            
            if (affectorAgent.IsMainAgent)
            {
                var playerHero = affectorAgent.GetHero();
                var choices = Hero.MainHero.GetAllCareerChoices();
                var hitBodyPart = blow.VictimBodyPart;

                if (hitBodyPart == BoneBodyPartType.Head || hitBodyPart == BoneBodyPartType.Neck)
                {
                    if(choices.Contains("CourtleyPassive4"))
                    {
                        var choice = TORCareerChoices.GetChoice("CourtleyPassive4");
                        if (choice != null)
                        {
                            var value = choice.GetPassiveValue();
                            playerHero.AddWindsOfMagic(value);
                        }
                    }
                    
                    if(choices.Contains("GuiltyByAssociationPassive4"))
                    {
                        var choice = TORCareerChoices.GetChoice("GuiltyByAssociationKeystone");
                        if (choice != null)
                        {
                            affectorAgent.ApplyStatusEffect("accusation_buff_ats",affectorAgent,choice.GetPassiveValue(),false,false);
                            affectorAgent.ApplyStatusEffect("accusation_buff_rls",affectorAgent,choice.GetPassiveValue(),false,false);
                        }
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
                
                if (choices.Contains ("SilverHammerPassive1"))
                {
                    var choice = TORCareerChoices.GetChoice("SilverHammerPassive1");

                    if (choice != null)
                    {
                        if (affectedAgent.IsEnemyOf(affectorAgent) && affectedAgent.Character.Race != 0)
                        {
                            Hero.MainHero.AddSkillXp(TORSkills.Faith,choice.GetPassiveValue());
                        }
                    }
                }

                if (affectorAgent.HasAttribute("NecromancerChampion"))
                {
                    if (choices.Contains("GrimoireNecrisKeystone"))
                    {
                        affectorAgent.Heal(2);
                    } 
                    
                    if (choices.Contains("BooksOfNagashKeystone"))
                    {
                        Hero.MainHero.AddWindsOfMagic(1);
                    } 
                    
                    Hero.MainHero.AddSkillXp(TORSkills.SpellCraft,20); 
                }

              
                
            }
            
            if (agentState == AgentState.Killed && affectedAgent.IsUndead() && affectedAgent.GetOriginMobileParty()== MobileParty.MainParty && Hero.MainHero.HasCareerChoice("BooksOfNagashPassive2"))
            {
                var level = affectedAgent.Character.Level;
                if (level > 1)
                {

                    var lowerSkeleton = MBObjectManager.Instance.CreateObject<CharacterObject>("tor_vc_summoned_skeleton");

                    if (lowerSkeleton != null)
                    {
                        var leaderAgent = Mission.Current.MainAgent;
                        var spawnCount = level / 7;
                        
                        if(spawnCount>1)

                        {
                            for (int i = 0; i < spawnCount; i++)
                            {
                                var data = TORSummonHelper.GetAgentBuildData(leaderAgent, "tor_vc_summoned_skeleton");
                            
                                var position = Mission.Current.GetRandomPositionAroundPoint(affectedAgent.Position, 1, 3, false);
                                TORSummonHelper.SpawnAgent(data, position);
                            }
                        }
                    }
                }
            }
        }

        
        
        
    }
}