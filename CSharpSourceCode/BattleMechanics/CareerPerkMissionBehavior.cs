using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.TwoDimension;
using TOR_Core.AbilitySystem;
using TOR_Core.AbilitySystem.Scripts;
using TOR_Core.BattleMechanics.StatusEffect;
using TOR_Core.CharacterDevelopment;
using TOR_Core.CharacterDevelopment.CareerSystem;
using TOR_Core.Extensions;

namespace TOR_Core.BattleMechanics
{
    public class CareerPerkMissionBehavior : MissionLogic
    {
        
        public override void OnAgentHit (Agent affectedAgent, Agent affectorAgent, in MissionWeapon affectorWeapon, in Blow blow, in AttackCollisionData attackCollisionData)
        {
            if(!CareerHelper.IsValidCareerMissionInteractionBetweenAgents(affectorAgent, affectedAgent)) return;

            if (( !affectorAgent.IsMainAgent || !affectorAgent.GetHero().HasCareer(TORCareers.WitchHunter) ) &&
                ( !affectorAgent.BelongsToMainParty() || !Hero.MainHero.HasCareerChoice("NoRestAgainstEvilKeystone") )) return;
            
            var comp = affectedAgent.GetComponent<StatusEffectComponent>();
            if(comp==null) return;
            var temporaryEffects = comp.GetTemporaryAttributes();
            if (!temporaryEffects.Contains("AccusationMark")) return;
                
            var choices = Hero.MainHero.GetAllCareerChoices();
                    
            AbilityTemplate ability = affectorAgent.GetComponent<AbilityComponent>().CareerAbility.Template;

            var reapplyChance = 0.5f;

            reapplyChance = Mathf.Clamp(reapplyChance, 0.1f, 1);

            var targets = new MBList<Agent>();

            if (choices.Contains("EndsJustifiesMeansKeystone")&&affectedAgent.HealthLimit <= blow.InflictedDamage)
            {
                var amount = AccusationScript.CalculateAdditonalTargetAmount(ability.ScaleVariable1);
                targets.AddRange(AccusationScript.GetAdditionalAccusationMarkTargets(affectedAgent.Position.AsVec2,amount+1));
            }
                    
            var script = (AccusationScript)affectorAgent.GetComponent<AbilityComponent>().CareerAbility.AbilityScript;
            var triggeredEffect = script.GetEffects()[0];
            targets.Add(affectedAgent);
            if (MBRandom.RandomFloat <= reapplyChance|| choices.Contains("NoRestAgainstEvilKeystone"))
            {
                targets.Add(affectedAgent);
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

        public override void OnAgentRemoved(Agent affectedAgent, Agent affectorAgent, AgentState agentState, KillingBlow blow)
        {
            if (affectorAgent == null) return;
            if (!affectorAgent.IsMainAgent) return;
            
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

                if (affectedAgent.Character != null)
                {
                    var level = affectedAgent.Character.Level;
                    Hero.MainHero.AddSkillXp(TORSkills.SpellCraft,5*level);
                    if (Hero.MainHero.HasCareerChoice("LiberNecrisPassive2"))
                    {
                        Hero.MainHero.AddSkillXp(DefaultSkills.Roguery,5*level);
                    }
                }
            }
        }
    }
}