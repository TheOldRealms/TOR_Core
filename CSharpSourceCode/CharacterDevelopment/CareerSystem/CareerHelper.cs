using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Helpers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
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
                    int skillValueWielded = 0;
                    if (agent.WieldedWeapon.Item != null)
                    {
                        wieldedWeaponSkill = agent.WieldedWeapon.Item?.PrimaryWeapon?.RelevantSkill;
                        skillValueWielded= agent.GetHero().GetSkillValue(wieldedWeaponSkill);
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


        public static bool DecideMasterlyFighterPerkEffect(Agent attackerAgent, Agent victimAgent, AttackCollisionData collisionData,int threshold=20, int maximumTier = 4)
        {
            if (collisionData.BaseMagnitude < threshold) return false;
            var choices = attackerAgent.GetHero().GetAllCareerChoices();
            if (!choices.IsEmpty())
            {
                if(choices.Contains("DreadKnightPassive4"))
                {
                    if(victimAgent.Character.GetBattleTier()<maximumTier)
                    {
                        return true;
                    }
                }
                        
            }

            return false;
        }

        public static bool StartWithPrayerReady(this Agent agent)
        {
            var hero = agent.GetHero();
            if (hero == null) return false;

            if (hero.HasCareerChoice("ArchLectorPassive1"))
            {
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
    }
}