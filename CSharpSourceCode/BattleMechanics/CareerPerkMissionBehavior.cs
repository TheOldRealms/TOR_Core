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
using TOR_Core.Extensions.ExtendedInfoSystem;
using TOR_Core.Utilities;

namespace TOR_Core.BattleMechanics
{
    public class CareerPerkMissionBehavior : MissionLogic
    {
        private float _currentTime;
        private readonly float _frequency=1f;

        private bool init;
        public override void OnMissionTick(float dt)
        {
            _currentTime += dt;
            if (_currentTime > _frequency)
            {
                _currentTime = 0f;
                TickEvents();
            }

            if (!init)
            {
                init = true;
            }
        }
        
        public override void OnAgentBuild(Agent agent, Banner banner)
        {
            base.OnAgentBuild(agent, banner);
            
            
            if (agent.BelongsToMainParty()&& Hero.MainHero.HasCareer(TORCareers.ImperialMagister))
            {
                CareerHelper.PowerstoneEffectAssignment(agent);
            }
        }

    

        private void TickEvents()
        {
            
            if (Agent.Main!=null&&Hero.MainHero.HasCareer(TORCareers.Necrarch))
            {
                if (Hero.MainHero.HasCareerChoice("HungerForKnowledgeKeystone"))
                {
                    var cAbility = Mission.Current.MainAgent.GetComponent<AbilityComponent>();
                    if (cAbility != null)
                    {
                        var value = TORCareers.Necrarch.GetCalculatedCareerAbilityCharge(Mission.Current.MainAgent, null, ChargeType.DamageDone, 15, AttackTypeMask.Spell, CareerHelper.ChargeCollisionFlag.None);
                        cAbility.CareerAbility.AddCharge(value);
                    }
                }
            }
        }

        public override void OnAgentHit(Agent affectedAgent, Agent affectorAgent, in MissionWeapon affectorWeapon, in Blow blow, in AttackCollisionData attackCollisionData)
        {
            if (!CareerHelper.IsValidCareerMissionInteractionBetweenAgents(affectorAgent, affectedAgent)) return;

            if (affectorAgent.BelongsToMainParty()&& ((Hero.MainHero.HasCareer(TORCareers.WitchHunter)&& affectorAgent.IsMainAgent) ||
                (Agent.Main!=null&& Agent.Main.IsActive()&&Hero.MainHero.HasCareerChoice("GuiltyByAssociationKeystone")&& (affectorAgent.IsHero || affectorAgent.Character.StringId.Contains("retinue")))))
            {
                WitchHunterAccusationBehavior(affectorAgent, affectedAgent, blow.InflictedDamage);
            }
        }

        private void WitchHunterAccusationBehavior(Agent affectorAgent, Agent affectedAgent, int inflictedDamge)
        {
            var comp = affectedAgent.GetComponent<StatusEffectComponent>();
            if (comp == null) return;
            var temporaryEffects = comp.GetTemporaryAttributes();
            if (!temporaryEffects.Contains("AccusationMark")) return;
            
            var choices = Hero.MainHero.GetAllCareerChoices();

            CareerAbility ability = Agent.Main.GetComponent<AbilityComponent>().CareerAbility;

            var reapplyChance = 0.5f;

            reapplyChance = Mathf.Clamp(reapplyChance, 0.1f, 1);

            var targets = new MBList<Agent>();

            if (choices.Contains("EndsJustifiesMeansKeystone") && affectedAgent.HealthLimit <= inflictedDamge)
            {
                var amount = AccusationScript.CalculateAdditonalTargetAmount(ability.Template.ScaleVariable1);
                targets.AddRange(AccusationScript.GetAdditionalAccusationMarkTargets(affectedAgent.Position.AsVec2, amount + 1));
            }

            var script = (CareerAbilityScript)(ability.AbilityScript);
            foreach (var triggeredEffect in script.EffectsToTrigger)
            {
                targets.Add(affectedAgent);
                if (MBRandom.RandomFloat <= reapplyChance || choices.Contains("NoRestAgainstEvilKeystone"))
                {
                    targets.Add(affectedAgent);
                }
                else
                {
                    affectedAgent.RemoveStatusEffect("accusation_debuff");
                }

                if (triggeredEffect == null) return;

                foreach (var target in targets)
                {
                    foreach (var statusEffect in triggeredEffect.StatusEffects)
                    {
                        target.ApplyStatusEffect(statusEffect, affectorAgent, triggeredEffect.ImbuedStatusEffectDuration, true, true);
                    }
                }
            }
        }

        public override void OnAgentRemoved(Agent affectedAgent, Agent affectorAgent, AgentState agentState, KillingBlow blow)
        {

            if (!CareerHelper.IsValidCareerMissionInteractionBetweenAgents(affectorAgent, affectedAgent)) return;

            var playerHero = affectorAgent.GetHero();

            var hitBodyPart = blow.VictimBodyPart;

            if (affectorAgent.IsMainAgent)
            {
                var choices = Hero.MainHero.GetAllCareerChoices();
                if (hitBodyPart == BoneBodyPartType.Head || hitBodyPart == BoneBodyPartType.Neck)
                {
                    if (choices.Contains("CourtleyPassive4"))
                    {
                        var choice = TORCareerChoices.GetChoice("CourtleyPassive4");
                        if (choice != null)
                        {
                            var value = choice.GetPassiveValue();
                            playerHero.AddWindsOfMagic(value);
                        }
                    }

                    if (choices.Contains("GuiltyByAssociationPassive4"))
                    {
                        var choice = TORCareerChoices.GetChoice("GuiltyByAssociationPassive4");
                        if (choice != null)
                        {
                            affectorAgent.ApplyStatusEffect("accusation_buff_ats", affectorAgent, choice.GetPassiveValue(), false, false);
                            affectorAgent.ApplyStatusEffect("accusation_buff_rls", affectorAgent, choice.GetPassiveValue(), false, false);
                        }
                    }
                    
                    if (choices.Contains("ToolsOfJudgementPassive4"))
                    {
                        var multiplier = 1;
                        if (affectedAgent.Character != null)
                        {
                            multiplier = affectedAgent.Character.Level;
                            if (affectedAgent.HasAttribute("AccusationMark"))
                                multiplier *= 2;
                        }

                        var value = ((int)blow.InflictedDamage * multiplier) / 10;
                        Hero.MainHero.AddSkillXp(DefaultSkills.Roguery, value);
                    }
                }

                if (choices.Contains("ControlledHungerPassive4"))
                {
                    var choice = TORCareerChoices.GetChoice("ControlledHungerPassive4");

                    if (choice != null)
                    {
                        var damage = blow.InflictedDamage;
                        
                        if (damage >= 200)
                        {
                            damage /= 200;
                            var bonus = Mathf.Clamp(damage, 0, 5);
                            affectorAgent.Heal(bonus);
                        }
                    }
                }

                if (choices.Contains("DreadKnightKeystone"))
                {
                    var choice = TORCareerChoices.GetChoice("DreadKnightKeystone");

                    if (choice != null)
                    {
                        var careerAbility = (Ability)affectorAgent.GetComponent<AbilityComponent>().CareerAbility;

                        if (careerAbility.IsActive)
                        {
                            var script = (RedFuryScript)(careerAbility.AbilityScript);

                            foreach (var effect in script.EffectsToTrigger)
                            {
                                foreach (var statuseffect in effect.StatusEffects)
                                {
                                    affectorAgent.ApplyStatusEffect(statuseffect, affectorAgent, 3);
                                    script.ExtendLifeTime(3);
                                }
                            }
                        }
                    }
                }


                if (choices.Contains("SilverHammerPassive1"))
                {
                    var choice = TORCareerChoices.GetChoice("SilverHammerPassive1");

                    if (choice != null)
                    {
                        if (affectedAgent.IsEnemyOf(affectorAgent) && affectedAgent.Character.Race != 0)
                        {
                            Hero.MainHero.AddSkillXp(TORSkills.Faith, choice.GetPassiveValue());
                        }
                    }
                }
                
                if (choices.Contains ("SwampRiderPassive3"))
                {
                    var choice = TORCareerChoices.GetChoice("SwampRiderPassive3");

                    if (choice != null)
                    {
                        if (affectedAgent.IsEnemyOf(affectorAgent) && !blow.IsMissile)
                        {
                            Hero.MainHero.AddSkillXp(DefaultSkills.Roguery,blow.InflictedDamage);
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

                    var multiplier = 1;
                    if (affectedAgent.Character != null)
                    {
                        multiplier = affectedAgent.Character.Level;
                    }

                    Hero.MainHero.AddSkillXp(TORSkills.SpellCraft, 5 * multiplier);
                    if (Hero.MainHero.HasCareerChoice("LiberNecrisPassive2"))
                    {
                        Hero.MainHero.AddSkillXp(DefaultSkills.Roguery, 5 * multiplier);
                    }
                }

                if (TORSpellBlowHelper.IsSpellBlow(blow))
                {
                    if (Hero.MainHero.HasCareerChoice("FlameOfUlricPassive4"))
                    {
                        affectorAgent.Heal(0.25f);
                    }
                }
            }
        }
    }
}