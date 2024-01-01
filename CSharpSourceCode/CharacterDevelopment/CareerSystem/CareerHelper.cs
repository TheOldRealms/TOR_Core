using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Helpers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TOR_Core.AbilitySystem;
using TOR_Core.CharacterDevelopment.CareerSystem.Choices;
using TOR_Core.Extensions;
using TOR_Core.Extensions.ExtendedInfoSystem;
using TOR_Core.Utilities;

namespace TOR_Core.CharacterDevelopment.CareerSystem
{
    public static class CareerHelper
    {
        public static float AddSkillEffectToValue(CareerChoiceObject careerChoice, Agent agent, List<SkillObject> relevantSkills, float scalingFactor, bool highestOnly = false , bool onlyWielded=false)
        {
            SkillObject wieldedWeaponSkill = null;
            
            float skillValue=0f;
            if (agent != null && agent.IsHero && relevantSkills != null && relevantSkills.Count > 0)
            {
                if (onlyWielded)
                {
                    var getWeaponEquipment = agent.Character.GetCharacterEquipment(EquipmentIndex.Weapon0, EquipmentIndex.Weapon3);
                    int skillValueWielded = 0;
                    if (!getWeaponEquipment.IsEmpty())
                    {
                        var value = 0;
                        foreach (var weapon in getWeaponEquipment)
                        {
                            var skill = weapon.PrimaryWeapon.RelevantSkill;
                            if (relevantSkills.Contains(skill)&& value<agent.GetHero().GetSkillValue(skill))
                            {
                                value = agent.GetHero().GetSkillValue(skill);
                            }
                        }
                        
                        skillValueWielded= value;
                    }
                    skillValue= skillValueWielded;
                }
                else 
                if (highestOnly)
                {
                    skillValue = relevantSkills.Max(x => agent.GetHero().GetSkillValue(x));
                }
                else
                    foreach (var skill in relevantSkills)
                    {
                        skillValue += agent.GetHero().GetSkillValue(skill);
                 
                    }
                
                if (careerChoice == TORCareerChoices.GetChoice("ProtectorOfTheWeakKeyStone"))
                {
                    if (agent.WieldedWeapon.Item?.PrimaryWeapon?.SwingDamageType != DamageTypes.Blunt) return 0f;
                }
            }

            return skillValue*scalingFactor;
        }
        
        
        
        
        public static void ApplyBasicCareerPassives(Hero hero, ref ExplainedNumber number, PassiveEffectType passiveEffectType, AttackTypeMask mask,bool asFactor = false)
        {
            var choices = hero.GetAllCareerChoices();
            foreach (var choiceID in choices)
            {
                var choice = TORCareerChoices.GetChoice(choiceID);
                if (choice == null)
                    continue;

                if (choice.Passive != null && choice.Passive.PassiveEffectType == passiveEffectType)
                {
                    var attackMask = choice.Passive.AttackTypeMask;
                    if ((mask & attackMask) == 0) //if mask does NOT contains attackmask
                        continue;
                    
                    
                    var value = choice.Passive.EffectMagnitude;
                    if (choice.Passive.InterpretAsPercentage)
                    {
                        value /= 100;
                    }
                    if (asFactor)
                    {
                        number.AddFactor(value, new TextObject(choice.BelongsToGroup.Name.ToString()));
                        return;
                    }
                    number.Add(value, new TextObject(choice.BelongsToGroup.Name.ToString()));
                }
            }
        }
        
        public static bool IsValidCareerMissionInteractionBetweenAgents(Agent affectorAgent , Agent affectedAgent)
        {
            if (!Hero.MainHero.HasAnyCareer()) return false;
            if(Agent.Main==null)return false;
            
            if(affectorAgent==null)return false;
            if(affectorAgent.IsMount||affectedAgent.IsMount) return false;

            return affectorAgent.BelongsToMainParty() || affectedAgent.BelongsToMainParty();
        }
        
        public static void ApplyCareerAbilityCharge( int amount, ChargeType chargeType, AttackTypeMask attackTypeMask,Agent affector=null,Agent affected =null, AttackCollisionData collisionData = new AttackCollisionData())
        {
            if(Agent.Main==null) return;
            var cAbility = Agent.Main.GetComponent<AbilityComponent>();
            if (cAbility != null)
            {
                var value = CalculateChargeForCareer(chargeType, amount, affector,affected, attackTypeMask, collisionData);
                if (value > 0)
                {
                    cAbility.CareerAbility.AddCharge(value);
                }
                
            }
        }

        public static float CalculateChargeForCareer(ChargeType chargeType, int chargeValue, Agent agent, Agent affected, AttackTypeMask mask, AttackCollisionData collisionData)
        {
            ChargeCollisionFlag flag = ChargeCollisionFlag.None;

            if (collisionData.AttackBlockedWithShield)
                flag |= ChargeCollisionFlag.HitShield;

            if (collisionData.VictimHitBodyPart == BoneBodyPartType.Head || collisionData.VictimHitBodyPart == BoneBodyPartType.Neck)
            {
                flag |= ChargeCollisionFlag.HeadShot;
            }
            
            var heroCareer = Hero.MainHero.GetCareer();


            var result = heroCareer.GetCalculatedCareerAbilityCharge(agent,affected, chargeType, chargeValue, mask, flag);


            if (!result.ApproximatelyEqualsTo(0))
            {
                return  result;
            }

            return 0;
        }
        
        
        public static void ApplyBasicCareerPassives(Hero hero, ref ExplainedNumber number, PassiveEffectType passiveEffectType, bool asFactor = false)
        {
            var choices = hero.GetAllCareerChoices();
            foreach (var choiceID in choices)
            {
                var choice = TORCareerChoices.GetChoice(choiceID);
                if (choice == null)
                    continue;

                if (choice.Passive != null && choice.Passive.PassiveEffectType == passiveEffectType)
                {
                    var value = choice.Passive.EffectMagnitude;
                    if (choice.Passive.InterpretAsPercentage)
                    {
                        value /= 100;
                    }
                    if (asFactor)
                    {
                        number.AddFactor(value, new TextObject(choice.BelongsToGroup.Name.ToString()));
                        return;
                    }
                    number.Add(value, new TextObject(choice.BelongsToGroup.Name.ToString()));
                }
            }
        }
        
        public static AgentPropertyContainer AddBasicCareerPassivesToPropertyContainerForMainAgent(Agent agent, AgentPropertyContainer propertyContainer, AttackTypeMask attackTypeMaskmask, PropertyMask mask)
        {
            if (!agent.GetHero().HasAnyCareer()) return propertyContainer;

            var damageValues = propertyContainer.AdditionalDamagePercentages;
            var resistanceValues = propertyContainer.ResistancePercentages;
            if (mask == PropertyMask.Attack)
            {
                ApplyCareerPassivesForDamageValues(agent, ref damageValues, attackTypeMaskmask);
            }

            if (mask == PropertyMask.Defense)
            {
                ApplyCareerPassivesForResistanceValues(agent, ref resistanceValues, attackTypeMaskmask);
            }
            return new AgentPropertyContainer(propertyContainer.DamageProportions, propertyContainer.DamagePercentages, resistanceValues, damageValues);
        }

        private static void ApplyCareerPassivesForDamageValues(Agent agent, ref float[] damageAmplifications, AttackTypeMask attackMask)
        {
            var choices = agent.GetHero().GetAllCareerChoices();
            foreach (var choiceID in choices)
            {
                var choice = TORCareerChoices.GetChoice(choiceID);
                if (choice == null)
                    continue;

                if (choice.Passive != null && (choice.Passive.PassiveEffectType == PassiveEffectType.Damage))
                {
                    var passive = choice.Passive;
                    var mask = passive.AttackTypeMask;
                    if ((mask & attackMask) == 0) //if mask does NOT contains attackmask
                        continue;

                    var damageType = passive.DamageProportionTuple.DamageType;
                    damageAmplifications[(int)damageType] += (passive.DamageProportionTuple.Percent / 100);
                }
            }
        }

        private static void ApplyCareerPassivesForResistanceValues(Agent agent, ref float[] resistancePropotions, AttackTypeMask attackMask)
        {
            var choices = agent.GetHero().GetAllCareerChoices();
            foreach (var choiceID in choices)
            {
                var choice = TORCareerChoices.GetChoice(choiceID);
                if (choice == null)
                    continue;

                if (choice.Passive != null && (choice.Passive.PassiveEffectType == PassiveEffectType.Resistance))
                {
                    var passive = choice.Passive;
                    var mask = passive.AttackTypeMask;
                    if ((mask & attackMask) == 0) //if mask does NOT contains attackmask
                        continue;
                    var damageType = passive.DamageProportionTuple.DamageType;
                    resistancePropotions[(int)damageType] += (passive.DamageProportionTuple.Percent / 100);
                }
            }
        }


        public static float CalculateTroopWageCareerPerkEffect(TroopRosterElement troop, string careerPerkID, out TextObject description)
        {
            float value = 0;
            description = new TextObject();
            var choice = TORCareerChoices.GetChoice(careerPerkID);
            if (choice != null)
            {
                float effect = choice.GetPassiveValue();
                value = (troop.Character.TroopWage*troop.Number) *effect;
                description = choice.BelongsToGroup.Name;
            }

            return value;
        }

        public static  bool PlayerOwnsMagicCareer()
        {
            if (Hero.MainHero.HasAnyCareer())
            {
               var career = Hero.MainHero.GetCareer();

               if (Hero.MainHero.GetExtendedInfo().CareerID == "Mercenary" || Hero.MainHero.GetExtendedInfo().CareerID == "MinorVampire" || Hero.MainHero.GetExtendedInfo().CareerID == "GrailDamsel")
                   return true;
            }

            return false;
        }

        public static bool PrayerCooldownIsNotShared(this Agent agent)
        {
            var hero = agent.GetHero();
            if (hero == null) return false;

            if (hero.HasCareerChoice("ArchLectorPassive4"))
            {
                return true;
            }
            return false;
        }


        public static bool ConditionsMetToShowSuperButton(CharacterObject viewedCharacter)
        {
            var career= Hero.MainHero.GetCareer();
            if (career != null)
            {
                return TORCareerChoices.Instance.GetCareerChoices(career).ConditionsAreMetToShowButton(viewedCharacter);
            }

            return false;
        }
        
        public static bool ConditionsMetToEnableSuperButton(CharacterObject viewedCharacter, out TextObject disableReason)
        {
            var career= Hero.MainHero.GetCareer();
            if (career != null)
            {
                return TORCareerChoices.Instance.GetCareerChoices(career).ConditionsAreMetToEnableButton(viewedCharacter, out  disableReason);
            }

            disableReason = new TextObject();
            return false;
        }
        
        public static string GetButtonSprite()
        {
            var career= Hero.MainHero.GetCareer();
            if (career != null)
            {
                return TORCareerChoices.Instance.GetCareerChoices(career).CareerButtonIcon;
            }

            return "";
        }
        
        
        public enum ChargeCollisionFlag
        {
            None,
            HitShield,
            HeadShot
        }
    }
}