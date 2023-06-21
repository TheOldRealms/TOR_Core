using System.Linq;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TOR_Core.AbilitySystem;
using TOR_Core.BattleMechanics.DamageSystem;
using TOR_Core.BattleMechanics.SFX;
using TOR_Core.BattleMechanics.TriggeredEffect;
using TOR_Core.CharacterDevelopment;
using TOR_Core.CharacterDevelopment.CareerSystem;
using TOR_Core.Extensions;
using TOR_Core.Extensions.ExtendedInfoSystem;
using TOR_Core.Models;
using TOR_Core.Utilities;

namespace TOR_Core.HarmonyPatches
{
    [HarmonyPatch]
    public static class DamagePatch
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(Agent), "HandleBlow")]
        public static bool PreHandleBlow(ref Blow b, Agent __instance)
        {
            Agent attacker = b.OwnerId != -1 ? Mission.Current.FindAgentWithIndex(b.OwnerId) : __instance;
            Agent victim = __instance;

            if (!victim.IsHuman || !attacker.IsHuman)
            {
                return true;
            }

            if (attacker.IsMainAgent)
            {
                TORCommon.Say("hello"); //TODO Remove
            }

            AttackTypeMask attackTypeMask = DetermineMask(b);

            float[] damageCategories = new float[(int)DamageType.All + 1];
            var attackerPropertyContainer = attacker.GetProperties(PropertyMask.Attack, attackTypeMask);
            var victimPropertyContainer = victim.GetProperties(PropertyMask.Defense, attackTypeMask);
            //attack properties;
            var damageProportions = attackerPropertyContainer.DamageProportions;
            var damageAmplifications = attackerPropertyContainer.DamagePercentages;
            var additionalDamagePercentages = attackerPropertyContainer.AdditionalDamagePercentages;
            //defense properties
            var resistancePercentages = victimPropertyContainer.ResistancePercentages;
            var resultDamage = 0;
            var wardSaveFactor = 1f;
            if (Game.Current.GameType is Campaign)
            {
                var model = MissionGameModels.Current.AgentApplyDamageModel;
                if(model != null && model is TORAgentApplyDamageModel)
                {
                    var torModel = model as TORAgentApplyDamageModel;
                    wardSaveFactor = torModel.CalculateWardSaveFactor(victim, attackTypeMask);
                }

                if (attacker.GetOriginMobileParty()==MobileParty.MainParty)
                {
                    var choices = Hero.MainHero.GetAllCareerChoices();

                    if ((victim.Character.Race == 0||victim.Character.IsCultist()) && choices.Contains("MartiallePassive3"))        //other humans should be added if applicable
                    {
                        var choice = TORCareerChoices.GetChoice("MartiallePassive3");
                        if (choice != null)
                        {
                            var value = choice.GetPassiveValue();
                            additionalDamagePercentages[(int)DamageType.Physical] += value;
                        }
                    }
                    
                    if (victim.Character.Race != 0 && choices.Contains("HolyPurgePassive3"))
                    {
                        var choice = TORCareerChoices.GetChoice("HolyPurgePassive3");
                        if (choice != null)
                        {
                            var value = choice.GetPassiveValue();
                            additionalDamagePercentages[(int)DamageType.Physical] += value;
                        }
                        
                    }
                    if (!attacker.IsHero&&attacker.HasMount&&choices.Contains("DreadKnightPassive3"))
                    {
                        var choice = TORCareerChoices.GetChoice("DreadKnightPassive3");
                        if (choice != null)
                        {
                            var value = choice.GetPassiveValue();
                            additionalDamagePercentages[(int)DamageType.Physical] += value;
                        }
                        
                    }    
                }
                
            }

            string abilityName = "";
            if (attackTypeMask==AttackTypeMask.Ranged)
            {
                var blow = b;
                var magicSpellMissile = Mission.Current.Missiles.FirstOrDefault(x => x.Index == blow.OwnerId);
                if (magicSpellMissile != null)
                {
                    var template =  AbilityFactory.GetTemplate(magicSpellMissile.Weapon.Item.ToString());

                    if (template != null)
                    {
                        attackTypeMask = AttackTypeMask.Spell;
                        abilityName = magicSpellMissile.Weapon.Item.ToString();
                    }

                }
            }

            //calculating spell damage
            if (TORSpellBlowHelper.IsSpellBlow(b)|| (attackTypeMask ==AttackTypeMask.Spell&&abilityName!="") )      //checking the Attackmask should be enough IsSpellBlow is checked before!
            {
                var abilityId="";
                int damageType = 0;
                if (abilityName != "")
                {
                    var template = AbilityFactory.GetTemplate(abilityName);
                    var triggeredEffectTemplate = TriggeredEffectManager.GetTemplateWithId(template.TriggeredEffects[0]);
                    abilityId = abilityName;
                    damageType = triggeredEffectTemplate==null? (int)DamageType.Physical:(int) triggeredEffectTemplate.DamageType;
                    damageCategories[damageType] = triggeredEffectTemplate.DamageAmount * b.BaseMagnitude;
                }
                else
                {
                    var spellInfo = TORSpellBlowHelper.GetSpellBlowInfo(victim.Index, attacker.Index);
                    damageType = (int)spellInfo.DamageType;
                    abilityId = spellInfo.OriginAbilityTemplateId;
                    damageCategories[damageType] = b.InflictedDamage;
                }
                
                damageAmplifications[damageType] -= resistancePercentages[damageType];
                damageCategories[damageType] *= 1 + damageAmplifications[damageType];
                resultDamage = (int)damageCategories[damageType];
                
                if(Game.Current.GameType is Campaign)
                {
                    var abilityTemplate = AbilityFactory.GetTemplate(abilityId);
                    if (attacker.IsHero && abilityTemplate != null)
                    {
                        var hero = attacker.GetHero();
                        var model = Campaign.Current.Models.GetAbilityModel();
                        if (model != null)
                        {
                            resultDamage = (int)(resultDamage * model.GetPerkEffectsOnAbilityDamage(hero.CharacterObject, victim, abilityTemplate));
                            var skill = model.GetRelevantSkillForAbility(abilityTemplate);
                            var amount = model.GetSkillXpForAbilityDamage(abilityTemplate, resultDamage);
                            hero.AddSkillXp(skill, amount);
                        }
                    }
                }
                resultDamage = (int)(resultDamage * wardSaveFactor);
                b.InflictedDamage = resultDamage;
                b.BaseMagnitude = resultDamage;
                if (attacker == Agent.Main || victim == Agent.Main)
                    TORDamageDisplay.DisplaySpellDamageResult((DamageType) damageType, resultDamage, damageAmplifications[damageType]);                
                return true;
            }

            //calculating non-spell damage
            for (int i = 0; i < damageCategories.Length - 1; i++)
            {
                damageProportions[i] += additionalDamagePercentages[i];
                damageCategories[i] = b.InflictedDamage * damageProportions[i];
                damageCategories[i] += damageCategories[(int)DamageType.All] / (int)DamageType.All;
                if (damageCategories[i] > 0)
                {
                    damageAmplifications[i] -= resistancePercentages[i];
                    damageCategories[i] *= 1 + damageAmplifications[i];
                    resultDamage += (int)damageCategories[i];
                }
            }
            resultDamage = (int)(resultDamage * wardSaveFactor * (1 + damageAmplifications[(int)DamageType.All])); 
            var originalDamage = b.InflictedDamage;
            b.InflictedDamage = resultDamage;
            //b.BaseMagnitude = resultDamage;       this shouldn't be the case

            if (victim.GetAttributes().Contains("Unstoppable")||(victim.IsDamageShruggedOff(b.InflictedDamage)))
            {
                b.BlowFlag |= BlowFlags.ShrugOff;
            }
            
            /*if ( b.BaseMagnitude > 25f)
            {
                if (attacker.IsMainAgent)
                {
d                    var choices = attacker.GetHero().GetAllCareerChoices();
                    if (!choices.IsEmpty())
                    {
                        if(choices.Contains("DreadKnightPassive4"))
                        {
                            if(victim.Character.GetBattleTier()<4)
                            {
                                b.BlowFlag |= BlowFlags.KnockDown;
                                b.BlowFlag |= BlowFlags.CanDismount;
                            }

                            if (victim.Character.GetBattleTier() >4 &&victim.Character.GetBattleTier() < 8)
                            {
                                b.BlowFlag |= BlowFlags.MakesRear;
                                b.BlowFlag |= BlowFlags.KnockBack;
                            }
                        }
                        
                    }
                }
            }*/

            /*if (attackTypeMask == AttackTypeMask.Melee && IsMightyBlow(attacker, b.InflictedDamage, attacker.WieldedWeapon))
            {
                b.BlowFlag |= BlowFlags.CrushThrough;
            }*/
            
            

            if (b.InflictedDamage > 0)
            {
                if (attacker == Agent.Main || victim == Agent.Main)
                {
                    var isVictim = victim == Agent.Main;
                    var resultBonus = damageAmplifications;
                    
                    for (int i = 0; i < resultBonus.Length; i++)
                    {
                        resultBonus[i] += additionalDamagePercentages[i];
                    }
                    
                    TORDamageDisplay.DisplayDamageResult(resultDamage, damageCategories, resultBonus, isVictim);
                    /*if(attacker == Agent.Main)
                    {
                        double damageIncrease = 0f;
                        if (originalDamage > 0) damageIncrease = b.InflictedDamage / originalDamage;
                        TORCommon.Say(string.Format("Modified damage by {0}", damageIncrease.ToString("P")));
                    }*/
                    
                }
            }
            return true;
        }
        
        
        
        public static bool IsMightyBlow(Agent agent, int damage, MissionWeapon weapon)
        {
            if (!agent.IsMainAgent || !agent.GetHero().HasAnyCareer()) return false;
            if (!agent.GetHero().GetAllCareerChoices().Contains("MartiallePassive4")) return false;     //or other related skills
                
            var weaponSkill = weapon.Item.WeaponComponent.PrimaryWeapon.RelevantSkill;
            if (weaponSkill == null) return false;
            var threshold = 75 - damage - (agent.GetHero().GetSkillValue(weaponSkill)*0.12f);
            if (threshold <= 0)
            {
                return true;
            }

            return false;
        }

        public static AttackTypeMask DetermineMask(Blow blow)
        {
            if (TORSpellBlowHelper.IsSpellBlow(blow)) return AttackTypeMask.Spell;
            if (blow.IsMissile)
            {
                return AttackTypeMask.Ranged;
            } 
                
            
            return AttackTypeMask.Melee;
        }
    }
}
