using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.LinQuick;
using TaleWorlds.MountAndBlade;
using TaleWorlds.TwoDimension;
using TOR_Core.AbilitySystem;
using TOR_Core.AbilitySystem.Scripts;
using TOR_Core.BattleMechanics.StatusEffect;
using TOR_Core.BattleMechanics.TriggeredEffect;
using TOR_Core.CharacterDevelopment;
using TOR_Core.CharacterDevelopment.CareerSystem;
using TOR_Core.Extensions;
using TOR_Core.Extensions.ExtendedInfoSystem;

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

            if (_zoomKeyEventStarted && Mission.InputManager.IsGameKeyReleased(24))
            {
                ZoomKeyUpEvents();
            }
        }

        private void ZoomKeyDownEvents()
        {
            if (_zoomKeyEventStarted) return;

            if (Mission.Current.IsFriendlyMission)
            {
                return;
            }

            _zoomKeyEventStarted = true;
        }

        private void ZoomKeyUpEvents()
        {
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

            if (Mission.Current.IsSiegeBattle) //Sly : there's probably a better way to handle this than to check every mission tick
            {
                if (!Mission.Current.IsMissionEnding)
                {
                    if (Agent.Main != null && Hero.MainHero.HasCareer(TORCareers.GreyLord))
                    {
                        if (Mission.Current.Teams.PlayerEnemy.ActiveAgents.Count == 0)
                        {
                            //Sly : if the player is in their aoe, they mark themselves and this will kill them on mission end. Idk if the player being marked prevents mission end, but if it doesn't, then the simplest fix would be removing/not adding the MainAgent to the list. To be fixed when I remember and my current list of changes isn't already so long.
                            var agents = Mission.Current.PlayerTeam.ActiveAgents.WhereQ(x => x.HasAttribute(CharacterAttributes.FELLFANG_MARK)).ToList();

                            for (int i = agents.Count - 1; i >= 0; i--)
                            {

                                agents[i].ApplyDamage(2000, agents[i].Position);
                            }
                        }
                    }
                }
            }
        }
        
        public override void OnMeleeHit(Agent attacker, Agent victim, bool isCanceled, AttackCollisionData collisionData)
        {
            if (victim == null || attacker == null) return;
            if (victim.IsMainAgent && victim.BelongsToMainParty() && victim.IsEnemyOf(attacker) && Hero.MainHero.HasCareer(TORCareers.Ironbreaker) && Hero.MainHero.HasCareerChoice("GromrilArmorKeystone"))
            {
                if (Agent.Main.HasAttribute(CharacterAttributes.IMPENETRABLE))
                {
                    GromrilArmorBehavior();
                }
            }
        }

        public override void OnAgentHit(Agent affectedAgent, Agent affectorAgent, in MissionWeapon affectorWeapon, in Blow blow, in AttackCollisionData attackCollisionData)
        {
            if (!CareerHelper.IsValidCareerMissionInteractionBetweenAgents(affectorAgent, affectedAgent)) return;

            if (affectorAgent.BelongsToMainParty() && ((Hero.MainHero.HasCareer(TORCareers.WitchHunter) && affectorAgent.IsMainAgent) ||
                (Agent.Main != null && Agent.Main.IsActive() && Hero.MainHero.HasCareerChoice("GuiltyByAssociationKeystone") && (affectorAgent.IsHero || affectorAgent.Character.StringId.Contains("retinue")))))
            {
                WitchHunterAccusationBehavior(affectorAgent, affectedAgent, blow.InflictedDamage);
            }

            if (affectedAgent.HasAttribute(CharacterAttributes.THORNS))
            {
                var thornsDamage = (int)(blow.InflictedDamage * 0.25f);
                var abilityLogic = Mission.Current?.GetMissionBehavior<AbilityManagerMissionLogic>();
                if (abilityLogic != null)
                {
                    abilityLogic.QueueOnHitSecondaryDamage(affectorAgent, thornsDamage, affectedAgent.Position, originatesFromAbility: true);
                }
                else
                {
                    affectorAgent.ApplyDamage(thornsDamage, affectedAgent.Position);
                }
            }
        }

        private void GromrilArmorBehavior()
        {
            CareerMissionVariables[0] += 1;
        }

        private void WitchHunterAccusationBehavior(Agent affectorAgent, Agent affectedAgent, int inflictedDamge)
        {
            var comp = affectedAgent.GetComponent<StatusEffectComponent>();
            if (comp == null) return;
            var temporaryEffects = comp.GetTemporaryAttributes();
            if (!temporaryEffects.Contains("AccusationMark")) return;

            var choices = Hero.MainHero.GetAllCareerChoices();

            CareerAbility ability = Agent.Main.GetComponent<AbilityComponent>().CareerAbility;

            var reapplyChance = 0.6f;

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
            // Runelord: Teachings of Thungni - runed units reduce career ability cooldown on kill
            if (Hero.MainHero.HasCareer(TORCareers.Runelord))
            {
                if (affectorAgent != null && !affectorAgent.IsHero && affectorAgent.BelongsToMainParty() &&
                    Hero.MainHero.HasCareerChoice("TeachingsOfThungniKeystone"))
                {
                    if (affectorAgent.Character.HasUnitRune() && Agent.Main != null && Agent.Main.IsActive())
                    {
                        var careerAbility = Agent.Main.GetCareerAbility();
                        if (careerAbility != null)
                        {
                            var cooldown = careerAbility.GetCoolDownLeft();
                            careerAbility.SetCoolDown(cooldown - 1);
                        }
                    }
                }
            }

            if (Hero.MainHero.HasCareer(TORCareers.GreyLord))
            {
                if (affectedAgent.HasAttribute(CharacterAttributes.FELLFANG_MARK))
                {
                    if (Hero.MainHero.HasCareerChoice("UnrestrictedMagicKeystone"))
                    {
                        if (Agent.Main == null || !Agent.Main.IsActive()) return;
                        var effect = TriggeredEffectManager.CreateNew("apply_fellfang_explosion");
                        effect.Trigger(affectedAgent.Position, Vec3.Up, Agent.Main, Agent.Main.GetCareerAbility().Template);
                    }
                }
            }


            if (!(affectorAgent != null &&
                  (affectorAgent.HasAttribute(CharacterAttributes.WINDS_LINK) || affectedAgent.HasAttribute(CharacterAttributes.WINDS_DEATH_LINK))) &&
                !CareerHelper.IsValidCareerMissionInteractionBetweenAgents(affectorAgent, affectedAgent)) return;

            var playerHero = affectorAgent.GetHero();

            var hitBodyPart = blow.VictimBodyPart;

            if (Hero.MainHero.HasCareer(TORCareers.Slayer))
            {
                if (affectedAgent.BelongsToMainParty())
                {
                    if (Hero.MainHero.HasCareerChoice("ShameOfTheAncestorsKeystone") && affectedAgent.IsSlayer())
                    {
                        CareerMissionVariables[0]++;
                    }
                }

            }

            // Call of da Green: WoM from linked Greenskin kills/deaths
            if (affectorAgent.HasAttribute(CharacterAttributes.WINDS_LINK))
            {
                Hero.MainHero.AddWindsOfMagic(0.5f);
            }

            if (affectedAgent.HasAttribute(CharacterAttributes.WINDS_DEATH_LINK))
            {
                Hero.MainHero.AddWindsOfMagic(-0.15f);
            }

            if (affectorAgent.IsMainAgent)
            {
                var choices = Hero.MainHero.GetAllCareerChoices();

                if (Hero.MainHero.HasCareer(TORCareers.Slayer) && Agent.Main.HasAttribute(CharacterAttributes.DOOM_SEEKING))
                {
                    CareerMissionVariables[0]++;
                }

                if (hitBodyPart == BoneBodyPartType.Head || hitBodyPart == BoneBodyPartType.Neck)
                {
                    if (blow.WeaponRecordWeaponFlags.HasAllFlags(WeaponFlags.MeleeWeapon) && choices.Contains("CourtleyPassive4"))
                    {
                        var choice = TORCareerChoices.GetChoice("CourtleyPassive4");
                        if (choice != null)
                        {
                            var value = choice.GetPassiveValue();
                            Hero.MainHero.AddWindsOfMagic(value);
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
                            if (affectedAgent.HasAttribute(CharacterAttributes.ACCUSATION_MARK))
                                multiplier *= 2;
                        }

                        var value = ((int)blow.InflictedDamage * multiplier) / 10;
                        Hero.MainHero.AddSkillXp(DefaultSkills.Roguery, value);
                    }
                }
                
                if (choices.Contains("ChiselAndHammerPassive2"))
                {
                    var value = ((int)blow.InflictedDamage) / 10; //Not sure if this is too much can be adjusted
                    if (affectorAgent.WieldedWeapon.Item.HasAnyTrait())
                    {
                        Hero.MainHero.AddSkillXp(TORSkills.Spellcraft, value);
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
                        //career abilities only exist in missions where IsCastingMission is true, eg. not arena fights
                        var careerAbility = (Ability)affectorAgent.GetComponent<AbilityComponent>()?.CareerAbility;

                        if (careerAbility != null && careerAbility.IsActive)
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

                if (affectedAgent.IsUndead() || affectedAgent.Character.IsChaos()|| affectedAgent.Character.IsBeastman())
                {
                    if (choices.Contains("SilverHammerPassive1") ||
                        choices.Contains("TemplarOrdersPassive2"))
                    {

                        Hero.MainHero.AddSkillXp(TORSkills.Faith, 10);
                    }
                }


                if (choices.Contains("AxeOfGrimnirPassive3"))
                {
                    Hero.MainHero.AddSkillXp(TORSkills.Faith, 10);
                }

                if (choices.Contains("SwampRiderPassive3"))
                {
                    var choice = TORCareerChoices.GetChoice("SwampRiderPassive3");

                    if (choice != null)
                    {
                        if (affectedAgent.IsEnemyOf(affectorAgent) && !blow.IsMissile)
                        {
                            Hero.MainHero.AddSkillXp(DefaultSkills.Roguery, blow.InflictedDamage);
                        }
                    }
                }

                if (affectorAgent.HasAttribute(CharacterAttributes.NECROMANCER_CHAMPION))
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

                    Hero.MainHero.AddSkillXp(TORSkills.Spellcraft, 5 * multiplier);
                    if (Hero.MainHero.HasCareerChoice("LiberNecrisPassive2"))
                    {
                        Hero.MainHero.AddSkillXp(DefaultSkills.Roguery, 5 * multiplier);
                    }
                }
                
            }
        }
    }
}