using System.Linq;
using System.Windows.Input;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.LinQuick;
using TaleWorlds.MountAndBlade;
using TaleWorlds.TwoDimension;
using TOR_Core.AbilitySystem;
using TOR_Core.AbilitySystem.Scripts;
using TOR_Core.BattleMechanics.DamageSystem;
using TOR_Core.BattleMechanics.StatusEffect;
using TOR_Core.BattleMechanics.TriggeredEffect;
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
        private readonly float _frequency = 1f;
        private bool _zoomKeyEventStarted;
        private const int TimeRequestId = 10001;
        public float[] CareerMissionVariables = new float[5];
        
        public override void AfterStart()
        {
            CareerMissionVariables = new float[5];
            
            base.AfterStart();
        }

        public override void OnMissionTick(float dt)
        {
            _currentTime += dt;
            if (_currentTime > _frequency)
            {
                _currentTime = 0f;
                TickEvents();
            }

            if (Mission.InputManager.IsGameKeyDown(24))
            {
                ZoomKeyDownEvents();
            }
            
            if(_zoomKeyEventStarted && Mission.InputManager.IsGameKeyReleased(24))
            {
                ZoomKeyUpEvents();
            }
        }

        private void ZoomKeyDownEvents()
        {
            if(_zoomKeyEventStarted) return;

            if (Mission.Current.IsFriendlyMission)
            {
                return;
            }
            
            if (Hero.MainHero.HasCareer(TORCareers.Waywatcher) && Hero.MainHero.HasCareerChoice("HawkeyedPassive4"))
            {
                if (Agent.Main != null && Agent.Main.GetComponent<AbilityComponent>().CareerAbility.ChargeLevel < 0.1f)
                {
                    return;
                }
           
                var timeRequest = new Mission.TimeSpeedRequest (0.60f, TimeRequestId);
                Mission.Current.AddTimeSpeedRequest (timeRequest);
            }
            
            _zoomKeyEventStarted = true;
        }

        private void ZoomKeyUpEvents()
        {
            if (Hero.MainHero.HasCareer(TORCareers.Waywatcher))
            {
                if (Mission.Current.GetRequestedTimeSpeed(TimeRequestId, out float _))
                {
                    Mission.Current.RemoveTimeSpeedRequest(TimeRequestId);
                }
            }
            
            _zoomKeyEventStarted = false;
        }


        private void TickEvents()
        {

            if (Agent.Main != null && Hero.MainHero.HasCareer(TORCareers.Necrarch))
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

            if (Agent.Main != null && Hero.MainHero.HasCareer(TORCareers.Waywatcher))
            {
                if(Hero.MainHero.HasCareerChoice("StarfireEssencePassive4"))
                {
                    CareerMissionVariables[2] ++;
                
                    if (Hero.MainHero.HasCareerChoice("EyeOfTheHunterPassive4"))
                    {
                        CareerMissionVariables[2] ++;
                    }
                }
                
                if( Hero.MainHero.HasCareerChoice("HailOfArrowsPassive4"))
                {
                    CareerMissionVariables[0] = Mathf.Max(0, CareerMissionVariables[0] - 0.10f);
                }

                if (_zoomKeyEventStarted && Hero.MainHero.HasCareerChoice("HawkeyedPassive4"))
                {
                    var lose = 100;
                    Agent.Main.GetComponent<AbilityComponent>().CareerAbility.AddCharge(-lose);
                }
            }
            
            

            if (Mission.Current.IsSiegeBattle)
            {
                if (!Mission.Current.IsMissionEnding)
                {
                    if (Agent.Main != null && Hero.MainHero.HasCareer(TORCareers.GreyLord))
                    {
                        if (Mission.Current.Teams.PlayerEnemy.ActiveAgents.Count == 0)
                        {
                            var agents = Mission.Current.PlayerTeam.ActiveAgents.WhereQ(x => x.HasAttribute("FellfangMark")).ToList();

                            for (int i = agents.Count - 1; i >= 0; i--)
                            {
                                
                                agents[i].ApplyDamage(2000,agents[i].Position);
                            }
               
                        }
                    }
                }
            }
        }

        public override void OnMissileHit(Agent attacker, Agent victim, bool isCanceled, AttackCollisionData collisionData)
        {
            if (victim == null) return;
            if (attacker.IsMainAgent && Hero.MainHero.HasCareer(TORCareers.Waywatcher) && Agent.Main!=null)
            {
                CareerMissionVariables[2] = 0;
                
                if (Hero.MainHero.HasCareerChoice("HailOfArrowsPassive3"))
                {
                    var agentDirection = victim.LookDirection;
                    var attackerDirection = collisionData.WeaponBlowDir.NormalizedCopy();
                    var isStealthAttack = false;
                    if (agentDirection.Length != 0 && attackerDirection.Length != 0)
                    {
                        var degree = Vec3.AngleBetweenTwoVectors(agentDirection, attackerDirection).ToDegrees();
                        isStealthAttack = degree < 90;
                    }


                    if (isStealthAttack || !victim.AIStateFlags.HasFlag(Agent.AIStateFlag.Alarmed))
                    {
                        InformationManager.DisplayMessage(new InformationMessage("Stealth Attack!", new TaleWorlds.Library.Color(255, 165, 85)));
                        victim.ApplyDamage((int)(collisionData.InflictedDamage * 0.5f), victim.Position);
                    }
                }

                if (Hero.MainHero.HasCareerChoice("HawkeyedPassive3") && Agent.Main!=null)
                {
                    CareerMissionVariables[1]++;

                    var hitCount = 6;

                    if (Hero.MainHero.HasCareerChoice("EyeOfTheHunterPassive4"))
                    {
                        hitCount /= 2;
                    }
                    
                    if (CareerMissionVariables[1] >= hitCount)
                    {
                        victim.ApplyStatusEffect("hawkeyed_debuff",Agent.Main,6,false);
                        victim.ApplyStatusEffect("hawkeyed_debuff2",Agent.Main,6,false);
                        CareerMissionVariables[1] = 0;
                    }
                }
                
                if (Hero.MainHero.HasCareerChoice("HailOfArrowsPassive4") && Agent.Main!=null)
                {
                    CareerMissionVariables[0]++;
                    
                    if (Hero.MainHero.HasCareerChoice("EyeOfTheHunterPassive4"))
                    {
                        CareerMissionVariables[0] ++;
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
            
            if(affectedAgent.HasAttribute("Thorns"))
            {
                affectorAgent.ApplyDamage((int)(blow.InflictedDamage*0.25f),affectedAgent.Position);
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
            if (Hero.MainHero.HasCareer(TORCareers.GreyLord))
            {
                if (affectedAgent.HasAttribute("FellfangMark"))
                {
                    if (Hero.MainHero.HasCareerChoice("UnrestrictedMagicKeystone"))
                    {
                        if (Agent.Main == null || !Agent.Main.IsActive()) return;
                        var effect = TriggeredEffectManager.CreateNew("apply_fellfang_explosion");
                        effect.Trigger(affectedAgent.Position,Vec3.Up,Agent.Main, Agent.Main.GetCareerAbility().Template);
                    }
                }
            }
            
            
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

                if (affectorAgent.HasAttribute("WindsLink"))
                {
                    Hero.MainHero.AddWindsOfMagic(1);
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