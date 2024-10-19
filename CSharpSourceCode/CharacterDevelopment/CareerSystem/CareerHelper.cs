using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Helpers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TOR_Core.AbilitySystem;
using TOR_Core.AbilitySystem.SpellBook;
using TOR_Core.AbilitySystem.Spells;
using TOR_Core.AbilitySystem.Spells.Prayers;
using TOR_Core.BattleMechanics.DamageSystem;
using TOR_Core.BattleMechanics.StatusEffect;
using TOR_Core.BattleMechanics.TriggeredEffect;
using TOR_Core.BattleMechanics.TriggeredEffect.Scripts;
using TOR_Core.CharacterDevelopment.CareerSystem.Button;
using TOR_Core.CharacterDevelopment.CareerSystem.CareerButton;
using TOR_Core.CharacterDevelopment.CareerSystem.Choices;
using TOR_Core.Extensions;
using TOR_Core.Extensions.ExtendedInfoSystem;
using TOR_Core.Items;
using TOR_Core.Utilities;

namespace TOR_Core.CharacterDevelopment.CareerSystem
{
    public static class CareerHelper
    {
        public static float AddSkillEffectToValue(CareerChoiceObject careerChoice, Agent agent, List<SkillObject> relevantSkills, float scalingFactor, bool highestOnly = false , bool onlyWielded=false)
        {
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


        
        
        public static void ApplyBasicCareerPassives(Hero hero, ref ExplainedNumber number, PassiveEffectType passiveEffectType, AttackTypeMask mask, bool asFactor = false)
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
            if (Campaign.Current == null) return false;
            if (!Hero.MainHero.HasAnyCareer()) return false;
            if (affectorAgent == null) return false;
            if (Hero.MainHero.HasCareer(TORCareers.Necromancer) && affectorAgent.HasAttribute("NecromancerChampion")) return true;
            if (Agent.Main == null) return false;
            
            
            if(affectorAgent.IsMount||affectedAgent.IsMount) return false;

            return affectorAgent.BelongsToMainParty() || affectedAgent.BelongsToMainParty();
        }
        
        public static void ApplyCareerAbilityCharge( int amount, ChargeType chargeType, AttackTypeMask attackTypeMask,Agent affector=null,Agent affected =null, AttackCollisionData collisionData = new AttackCollisionData())
        {
            if (Agent.Main == null) return;
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

            if (Hero.MainHero == null || Hero.MainHero.GetCareer() == null) return 0;

            var heroCareer = Hero.MainHero.GetCareer();

            var result = heroCareer.GetCalculatedCareerAbilityCharge(agent, affected, chargeType, chargeValue, mask, flag);

            if (!result.ApproximatelyEqualsTo(0))
            {
                return result;
            }

            return 0;
        }
        
        
        public static void ApplyBasicCareerPassives(Hero hero, ref ExplainedNumber number, PassiveEffectType passiveEffectType, bool asFactor = true, CharacterObject characterObject=null)
        {
            var choices = hero.GetAllCareerChoices();
            foreach (var choiceID in choices)
            {
                var choice = TORCareerChoices.GetChoice(choiceID);

                if (choice?.Passive == null || choice.Passive.PassiveEffectType != passiveEffectType) continue;
                
                if (characterObject == null)
                {
                    characterObject = hero.CharacterObject;
                }
                    
                var passive = choice.Passive;
                
                if(!passive.IsValidCharacterObject(characterObject)) continue;
                    
                if (passive.WithFactorFlatSwitch)
                {
                    asFactor = !asFactor;
                }
                    
                var value = passive.EffectMagnitude;
                var text = choice.BelongsToGroup.Name;
                    
                if (passive.InterpretAsPercentage)
                {
                    value /= 100;
                }
                    
                if (asFactor)
                {
                    number.AddFactor(value, text);
                    continue;
                }
                number.Add(value, text);
            }
        }

        public static float[] AddCareerPassivesForDamageValues(Agent attacker, Agent victim, AttackTypeMask attackTypeMask, PropertyMask mask)
        {
            var damageValues = new float[(int)DamageType.All + 1];

            switch (mask)
            {
                case PropertyMask.Attack:
                    if (attacker.IsHero && attacker.IsMainAgent)
                    {
                        ApplyCareerPassivesForDamageValues(attacker, victim, ref damageValues, attackTypeMask, PassiveEffectType.Damage);
                    }
                    else
                    {
                        ApplyCareerPassivesForDamageValues(attacker, victim, ref damageValues, attackTypeMask, PassiveEffectType.TroopDamage);
                    }
                    return damageValues;
                case PropertyMask.Defense:
                    if (victim.IsHero && victim.IsMainAgent)
                    {
                        ApplyCareerPassivesForDamageValues(attacker, victim, ref damageValues, attackTypeMask, PassiveEffectType.Resistance);
                    }
                    else
                    {
                        ApplyCareerPassivesForDamageValues(attacker, victim, ref damageValues, attackTypeMask, PassiveEffectType.TroopResistance);
                    }
                    
                    return damageValues;
                default:
                    return null;
            }
        }

        private static void ApplyCareerPassivesForDamageValues(Agent agent,Agent victim, ref float[] values, AttackTypeMask attackMask, PassiveEffectType type)
        {
            if (type != PassiveEffectType.Damage &&
                type != PassiveEffectType.TroopDamage &&
                type != PassiveEffectType.Resistance &&
                type != PassiveEffectType.TroopResistance) return;
            
            var choices = Hero.MainHero.GetAllCareerChoices();
            foreach (var choiceID in choices)
            {
                var choice = TORCareerChoices.GetChoice(choiceID);
                if (choice == null)
                    continue;

                if (choice.Passive != null && (choice.Passive.PassiveEffectType == type))
                {
                    if(!choice.Passive.IsValidCombatInteraction(agent, victim, attackMask)) continue;
                    var passive = choice.Passive;
                    var mask = passive.AttackTypeMask;
                    if ((mask & attackMask) == 0) //if mask does NOT contains attackmask
                        continue;

                    var damageType = passive.DamageProportionTuple.DamageType;
                    values[(int)damageType] += (passive.DamageProportionTuple.Percent / 100);
                }
            }
        }


        public static float CalculateTroopWageCareerPerkEffect(TroopRosterElement troop, CareerChoiceObject careerPerk, out TextObject description)
        {
            float value = 0;
            description = new TextObject("");
            if (careerPerk == null) return 0;
            
            float effect = careerPerk.GetPassiveValue();
            value = (troop.Character.TroopWage*troop.Number) * effect;
            description = careerPerk.BelongsToGroup.Name;

            return value;
        }

        public static bool IsMagicCapableCareer(CareerObject career)
        {
            if (career == null) return false;
            
            if (career == TORCareers.Mercenary|| 
                career == TORCareers.MinorVampire || 
                career == TORCareers.GrailDamsel ||
                career == TORCareers.Necromancer||
                career == TORCareers.Necrarch)
                return true;

            return false;
        }

        public static List<(string PrayerID,int Rank) > GetBattlePrayerList(CareerObject career)
        {
            List<(string PrayerID, int Rank)> prayers = new List<(string, int)>();
            
            
            if (career == TORCareers.WarriorPriest)
            {
                prayers.Add(("HealingHand",2));
                prayers.Add(("ArmourOfRighteousness",3));
                prayers.Add(("Vanquish",3));
                prayers.Add(( "CometOfSigmar", 4));
                return prayers;
            }

            if (career == TORCareers.GrailDamsel)
            {
                prayers.Add(("AuraOfTheLady", 2));
                prayers.Add(("ShieldOfCombat", 3));
                prayers.Add(("LadysFavour", 3));
                prayers.Add(( "AerialShield", 4));
                return prayers;
            }

            if (career == TORCareers.WarriorPriestUlric)
            {
                prayers.Add(("UlricsGift",2));
                prayers.Add(("HeartOfTheWolf",3));
                prayers.Add(("IceStorm",3));
                prayers.Add(( "SnowKingDecree", 4));
                return prayers;
            }

            return prayers;
        }

        public static bool PrayerCooldownIsNotShared(this Agent agent)
        {
            var hero = agent.GetHero();
            if (hero == null) return false;

            if (hero.HasCareerChoice("RelentlessFanaticPassive4"))
            {
                return true;
            }
            return false;
        }

        public static CareerButtonBehaviorBase GetCareerButton()
        {
            
            var career = Hero.MainHero.GetCareer();

            if (career != null)
            {
                return CareerButtons.Instance.GetCareerButton(career);
            }

            return null;
        }
        
        public static string GetButtonSprite()
        {
            var career= Hero.MainHero.GetCareer();
            if (career == null) return "";
            
            var button = GetCareerButton();
            if (button != null)
            {
                return CareerButtons.Instance.GetCareerButton(career).CareerButtonIcon;
            }

            return "";
        }
        
        
        public static void PowerstoneEffectAssignment(Agent agent)
        {
            var statuseffectComponent = agent.GetComponent<StatusEffectComponent>();

            var button =  CareerHelper.GetCareerButton() as ImperialMagisterCareerButtonBehavior;
            if (statuseffectComponent != null && button!=null)
            {
                var powerstone = button.GetPowerstone(agent.Character as CharacterObject);

                if (powerstone!=null)
                {
                    AddMissionPermanentEffect(agent, powerstone.EffectId);
                }
            }
        }

        public static void AddDefaultPermanentMissionEffect(Agent agent, string effectID)
        {
            var statuseffectComponent = agent.GetComponent<StatusEffectComponent>();
            
            if (statuseffectComponent != null)
            {
                AddMissionPermanentEffect(agent, effectID);
            }
        }
        

        private static void AddMissionPermanentEffect(Agent agent, string effectID)
        {
            var template = TriggeredEffectManager.GetTemplateWithId(effectID);
                    
            if(template==null) return;
                    
            foreach (var effect in template.ImbuedStatusEffects)
            {
                agent.ApplyStatusEffect(effect,Agent.Main,99999);
            }

            if (template!=null&&template.ScriptNameToTrigger != "none")
            {
                try
                {
                    var obj = Activator.CreateInstance(Type.GetType(template.ScriptNameToTrigger));
                    if (obj is ITriggeredScript)
                    {
                        var script = obj as ITriggeredScript;
                        script.OnTrigger(agent.Position, Agent.Main, new List<Agent>(){agent}, 9999);
                    }
                }
                catch (Exception)
                {
                    TORCommon.Log("Tried to spawn TriggeredScript: " + template.ScriptNameToTrigger + ", but failed.", NLog.LogLevel.Error);
                }
            }
        }
        
        
        public enum ChargeCollisionFlag
        {
            None,
            HitShield,
            HeadShot
        }

        public static bool IsPriestCareer(CareerObject career)
        {

            return career == TORCareers.WarriorPriest ||
                   career == TORCareers.WarriorPriestUlric ||
                   career == TORCareers.GrailDamsel;
        }
        
        public static string GetGodCareerIsDevotedTo(CareerObject careerObject)
        {
            if (careerObject == TORCareers.GrailDamsel) return "cult_of_lady";
            if (careerObject == TORCareers.WarriorPriest) return "cult_of_sigmar";
            if (careerObject == TORCareers.WarriorPriestUlric) return "cult_of_ulric";

            return "-";
        }

        public static void RemovePowerstone(List<string> attributes)
        {
            var button =  CareerHelper.GetCareerButton() as ImperialMagisterCareerButtonBehavior;

            if (button != null)
            {
                var stones = button.AvailablePowerStones;
                
                foreach (var attribute in attributes)
                {
                    var removedStone = stones.FirstOrDefault(x => x.Id == attribute);

                    if (removedStone != null)
                    {
                        Hero.MainHero.AddCustomResource("Prestige",removedStone.ScrapPrestigeGain);
                        break;
                    }
                }
                
                
            }

        }

        public static void RemoveCareerRelatedTroopAttributes(MobileParty mobileParty, string troopId,
            MobilePartyExtendedInfo mobilePartyinfo)
        {
            if(!mobileParty.IsMainParty) return;
            
            if(Hero.MainHero.HasCareer(TORCareers.ImperialMagister))
            {
                RemovePowerstone(mobilePartyinfo.TroopAttributes[troopId]);
            }
        }
    }
}