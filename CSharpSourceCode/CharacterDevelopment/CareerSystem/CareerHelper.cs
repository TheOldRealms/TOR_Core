using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TOR_Core.Extensions;
using TOR_Core.Extensions.ExtendedInfoSystem;

namespace TOR_Core.CharacterDevelopment.CareerSystem
{
    public static class CareerHelper
    {
        public static float AddSkillEffectToValue(CareerChoiceObject careerChoice, Agent agent, List<SkillObject> relevantSkills, float scalingFactor, bool highestOnly = false)
        {
            float retVal = 0f;
            if(agent != null && agent.IsHero && relevantSkills != null && relevantSkills.Count > 0)
            {
                foreach (var skill in relevantSkills)
                {
                    int skillValue = agent.GetHero().GetSkillValue(skill);
                    var result = skillValue * scalingFactor;
                    if (highestOnly && result > retVal) retVal = result;
                    else retVal += result;
                }
                if(careerChoice == TORCareerChoices.GetChoice("ProtectorOfTheWeakKeyStone"))
                {
                    if (agent.WieldedWeapon.Item?.PrimaryWeapon?.SwingDamageType != DamageTypes.Blunt) return 0f;
                }
            }
            return retVal;
        }


        public static void ApplyBasicCareerPassives(Hero hero, ref ExplainedNumber number, PassiveEffectType passiveEffectType, bool asFactor=false)
        {
            var choices = hero.GetAllCareerChoices();
            foreach (var choiceID in choices)
            {
                var choice =  TORCareerChoices.GetChoice(choiceID);
                if(choice==null)
                    continue;

                if (choice.Passive != null&& choice.Passive.PassiveEffectType == passiveEffectType)
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
            
            var  damageValues = propertyContainer.AdditionalDamagePercentages;
            var resistanceValues = propertyContainer.ResistancePercentages;
            if (mask == PropertyMask.Attack)
            {
                ApplyCareerPassivesForDamageValues(agent, ref damageValues, attackTypeMaskmask);
            }
            if (mask == PropertyMask.Defense)
            {
                ApplyCareerPassivesForResistanceValues(agent, ref resistanceValues, attackTypeMaskmask);
            }
            
            

            return new AgentPropertyContainer(propertyContainer.DamageProportions, damageValues, resistanceValues, propertyContainer.AdditionalDamagePercentages);
        }

        private static void ApplyCareerPassivesForDamageValues(Agent agent, ref float[] damageAmplifications, AttackTypeMask attackMask)
        {
            var choices = agent.GetHero().GetAllCareerChoices();
            foreach (var choiceID in choices)
            {
                var choice =  TORCareerChoices.GetChoice(choiceID);
                if(choice==null)
                    continue;

                if (choice.Passive != null&& (choice.Passive.PassiveEffectType == PassiveEffectType.Damage))
                {
                    var passive = choice.Passive;
                    var mask = passive.AttackTypeMask;
                    if((mask & attackMask)==0) //if mask does NOT contains attackmask
                        continue;
                    
                    var damageType = passive.DamageProportionTuple.DamageType;
                    damageAmplifications[(int)damageType]+=(passive.DamageProportionTuple.Percent/100);
                }
                
               
            }
        }
        
        private static void ApplyCareerPassivesForResistanceValues(Agent agent, ref float[] resistancePropotions, AttackTypeMask attackMask)
        {
            var choices = agent.GetHero().GetAllCareerChoices();
            foreach (var choiceID in choices)
            {
                var choice =  TORCareerChoices.GetChoice(choiceID);
                if(choice==null)
                    continue;

                if (choice.Passive != null&& (choice.Passive.PassiveEffectType == PassiveEffectType.Resistance))
                {
                    var passive = choice.Passive;
                    var mask = passive.AttackTypeMask;
                    if((mask&attackMask)==0)    //if mask does NOT contains attackmask
                        continue;
                    var damageType = passive.DamageProportionTuple.DamageType;
                    resistancePropotions[(int)damageType]+=(passive.DamageProportionTuple.Percent/100);
                }
                
               
            }
        }
    }
}
