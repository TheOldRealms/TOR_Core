using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Helpers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;
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

        public static float CalculateChargeForCareer(ChargeType chargeType, int chargeValue, AttackTypeMask mask, AttackCollisionData collisionData)
        {
            var blocked = collisionData.AttackBlockedWithShield;
            return CalculateChargeForCareer(chargeType, chargeValue, mask, blocked);
        }

        public static float CalculateChargeForCareer(ChargeType chargeType, int chargeValue, AttackTypeMask mask = AttackTypeMask.Melee, bool blocked = false) 
        {
            var heroCareer = Hero.MainHero.GetCareer();
            
            
            ExplainedNumber explainedNumber = new ExplainedNumber();
            var careerScaleFactor = ModifyChargeAmount();
            
            switch (chargeType)
            {
                case ChargeType.DamageDone:
                {
                    if (heroCareer == TORCareers.GrailDamsel&& mask == AttackTypeMask.Spell)
                    { 
                        explainedNumber.Add(chargeValue);
                       
                        explainedNumber.AddFactor(-0.9f);
                    }
                    
                    if (heroCareer == TORCareers.MinorVampire)
                    {
                        explainedNumber.Add(chargeValue);

                        if (mask == AttackTypeMask.Spell && Agent.Main.GetHero().GetAllCareerChoices().Contains("ArkayneKeystone"))
                        {
                            explainedNumber.AddFactor(-0.9f);
                        }
                           
                    }
                    
                    if (Agent.Main.GetHero().GetAllCareerChoices().Contains("BookOfSigmarKeystone"))
                    {
                        explainedNumber.Add(chargeValue);
                        
                        if (mask == AttackTypeMask.Spell ||  mask == AttackTypeMask.Melee)
                        {
                            explainedNumber.AddFactor(-0.9f);
                        } 
                    }
                   
                    break;
                }
                case ChargeType.DamageTaken:
                {
                    if (heroCareer == TORCareers.WarriorPriest)
                    {
                        explainedNumber.Add((float) chargeValue / Hero.MainHero.MaxHitPoints);      //proportion of lost health 
                        
                        if (blocked)
                        {
                            explainedNumber.AddFactor(-0.85f);
                        }
                        
                    }
                    break;
                }
                    
                case ChargeType.NumberOfKills:
                    
                    break;
            }

            explainedNumber.AddFactor(1-careerScaleFactor);
            return explainedNumber.ResultNumber;
        }
        
        private static float ModifyChargeAmount()
        {
            var number = new ExplainedNumber(1);
            if (Agent.Main.GetHero().HasAnyCareer())
            {

                if (Agent.Main.GetHero().GetAllCareerChoices().Contains("DreadKnightKeystone"))
                {
                    var choice = TORCareerChoices.GetChoice("DreadKnightKeystone");
                    if (choice != null)
                    {
                        var value = choice.GetPassiveValue();
                        number.AddFactor(value);
                    }
                }

                if (Agent.Main.GetHero().GetAllCareerChoices().Contains("NewBloodKeystone"))
                {
                    var choice = TORCareerChoices.GetChoice("NewBloodKeystone");
                    if (choice != null)
                    {
                        var value = choice.GetPassiveValue();
                        number.AddFactor(value);
                    }
                }

                if (Agent.Main.GetHero().GetAllCareerChoices().Contains("MartialleKeystone"))
                {
                    var choice = TORCareerChoices.GetChoice("MartialleKeystone");
                    if (choice != null)
                    {
                        var value = choice.GetPassiveValue();
                        number.AddFactor(value);
                    }
                }
                
                if (Agent.Main.GetHero().GetAllCareerChoices().Contains("VividVisionsKeystone"))
                {
                    var choice = TORCareerChoices.GetChoice("VividVisionsKeystone");
                    if (choice != null)
                    {
                        var value = choice.GetPassiveValue();
                        number.AddFactor(value);
                    }
                }
                if (Agent.Main.GetHero().GetAllCareerChoices().Contains("InspirationOfTheLadyKeystone"))
                {
                    var choice = TORCareerChoices.GetChoice("InspirationOfTheLadyKeystone");
                    if (choice != null)
                    {
                        number.AddFactor(-0.05f);           // Originally only 10% of charge is taken into account, now it would be 5% 
                    }
                }
            }

            return number.ResultNumber-1;
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
    }
}