using Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TOR_Core.AbilitySystem;
using TOR_Core.AbilitySystem.Spells;
using TOR_Core.BattleMechanics.StatusEffect;
using TOR_Core.CharacterDevelopment;
using TOR_Core.CharacterDevelopment.CareerSystem;
using TOR_Core.CharacterDevelopment.CareerSystem.CareerButton;
using TOR_Core.Extensions;
using TOR_Core.Utilities;

namespace TOR_Core.Models
{
    public class TORAbilityModel : GameModel
    {
        public SkillObject GetRelevantSkillForAbility(AbilityTemplate ability)
        {
            switch (ability.AbilityType)
            {
                case AbilityType.Spell:
                    return TORSkills.SpellCraft;
                case AbilityType.Prayer:
                    return TORSkills.Faith;
                default:
                    return TORSkills.SpellCraft;
            }
        }

        public SkillEffect GetRelevantSkillEffectForAbilityDamage(AbilityTemplate ability)
        {
            switch (ability.AbilityType)
            {
                case AbilityType.Spell:
                    return TORSkillEffects.SpellEffectiveness;
                case AbilityType.Prayer:
                    return TORSkillEffects.SpellEffectiveness;
                default:
                    return null;
            }
        }

        public SkillEffect GetRelevantSkillEffectForAbilityDuration(AbilityTemplate ability)
        {
            switch (ability.AbilityType)
            {
                case AbilityType.Spell:
                    return TORSkillEffects.SpellDuration;
                case AbilityType.Prayer:
                    return TORSkillEffects.SpellDuration;   //Thats a bug: there is no prayer duration amplification by skills
                default:
                    return null;
            }
        }

        public int GetSkillXpForCastingAbility(AbilityTemplate ability)
        {
            if (ability.AbilityType == AbilityType.Prayer)
            {
                return ability.CoolDown * 4;
            }
            return ability.WindsOfMagicCost * 20;
        }

        public int GetSkillXpForAbilityDamage(AbilityTemplate ability, int damageAmount)
        {
            
            return (int)damageAmount /5;
        }

        public float GetSkillEffectivenessForAbilityDamage(CharacterObject character, AbilityTemplate ability)
        {
            ExplainedNumber explainedNumber = new ExplainedNumber(1f, false, null);
            var skill = GetRelevantSkillForAbility(ability);
            if(skill != null)
            {
                var skillValue = character.GetSkillValue(skill);
                var skillEffect = GetRelevantSkillEffectForAbilityDamage(ability);
                if(skillEffect != null) SkillHelper.AddSkillBonusForCharacter(skill, skillEffect, character, ref explainedNumber, skillValue, true, 0);
            }
            return explainedNumber.ResultNumber;
        }

        public float CalculateStatusEffectDurationForAbility(CharacterObject character, AbilityTemplate originAbilityTemplate, float statusEffectDuration)
        {
            
            float skillmultiplier = GetSkillEffectivenessForAbilityDuration(character, originAbilityTemplate);
            float perkmultiplier = 1f;
            if (character.IsHero) perkmultiplier = GetPerkEffectsOnAbilityDuration(character, originAbilityTemplate);
            
            if (character.IsHero&& character.HeroObject == Hero.MainHero)
            {
                var player = character.HeroObject;
                var explainedNumber = new ExplainedNumber(1);
                
                if (originAbilityTemplate.AbilityEffectType == AbilityEffectType.Augment || originAbilityTemplate.AbilityEffectType == AbilityEffectType.Heal)
                {
                    CareerHelper.ApplyBasicCareerPassives(player,ref explainedNumber,PassiveEffectType.BuffDuration, true);
                }
                else if (originAbilityTemplate.AbilityEffectType == AbilityEffectType.Hex)
                {
                    CareerHelper.ApplyBasicCareerPassives(player,ref explainedNumber,PassiveEffectType.DebuffDuration, true);
                }
                
                perkmultiplier += (explainedNumber.ResultNumber -1) ;
            }
            
            
            return statusEffectDuration * skillmultiplier * perkmultiplier;
        }
        
        public float CalculateRadiusForAbility(CharacterObject character, AbilityTemplate originAbilityTemplate, float radius)
        {
            if (character.IsHero&& character.HeroObject == Hero.MainHero)
            {
                var player = character.HeroObject;
                var explainedNumber = new ExplainedNumber(radius);
                
                CareerHelper.ApplyBasicCareerPassives(player,ref explainedNumber,PassiveEffectType.SpellRadius, true);
                
                return explainedNumber.ResultNumber;
            }

            return radius;
        }

        public float GetSkillEffectivenessForAbilityDuration(CharacterObject character, AbilityTemplate ability)
        {
            ExplainedNumber explainedNumber = new ExplainedNumber(1f, false, null);
            var skill = GetRelevantSkillForAbility(ability);
            if (skill != null)
            {
                var skillValue = character.GetSkillValue(skill);
                var skillEffect = GetRelevantSkillEffectForAbilityDuration(ability);
                if (skillEffect != null) SkillHelper.AddSkillBonusForCharacter(skill, skillEffect, character, ref explainedNumber, skillValue, true, 0);
            }
            return explainedNumber.ResultNumber;
        }

        public float GetPerkEffectsOnAbilityDuration(CharacterObject character, AbilityTemplate template)
        {
            ExplainedNumber explainedNumber = new ExplainedNumber(1f, false, null);
            if (character.GetPerkValue(TORPerks.SpellCraft.Selfish) && template.IsSpell)
            {
                PerkHelper.AddPerkBonusForCharacter(TORPerks.SpellCraft.Selfish, character, false, ref explainedNumber);
            }
            return explainedNumber.ResultNumber;
        }

        public float GetPerkEffectsOnAbilityDamage(CharacterObject character, Agent victim, AbilityTemplate abilityTemplate)
        {
            ExplainedNumber explainedNumber = new ExplainedNumber(1f, false, null);
            var victimCharacter = victim.Character as CharacterObject;
            var victimLeader = victim.GetPartyLeaderCharacter();
            var victimCaptain = victim.GetCaptainCharacter();

            if(character != null && abilityTemplate != null)
            {
                if (character.GetPerkValue(TORPerks.SpellCraft.Selfish) && abilityTemplate.IsSpell && abilityTemplate.DoesDamage)
                {
                    if(victimCharacter != null && character == victimCharacter)
                    {
                        PerkHelper.AddPerkBonusForCharacter(TORPerks.SpellCraft.Selfish, character, true, ref explainedNumber);
                    }
                }
                if (character.GetPerkValue(TORPerks.SpellCraft.WellControlled) && abilityTemplate.IsSpell && abilityTemplate.DoesDamage)
                {
                    if(victimLeader != null && character == victimLeader)
                    {
                        PerkHelper.AddPerkBonusForCharacter(TORPerks.SpellCraft.WellControlled, character, true, ref explainedNumber);
                    }
                }
                if (character.IsPlayerCharacter && character.IsHero&& character.HeroObject== Hero.MainHero)
                {
                    CareerHelper.ApplyBasicCareerPassives(Hero.MainHero,ref explainedNumber,PassiveEffectType.SpellEffectiveness, true);
                }
                if (character.GetPerkValue(TORPerks.SpellCraft.OverCaster) && abilityTemplate.IsSpell && abilityTemplate.DoesDamage)
                {
                    PerkHelper.AddPerkBonusForCharacter(TORPerks.SpellCraft.OverCaster, character, true, ref explainedNumber);
                }
                if (character.GetPerkValue(TORPerks.SpellCraft.EfficientSpellCaster) && abilityTemplate.IsSpell && abilityTemplate.DoesDamage)
                {
                    PerkHelper.AddPerkBonusForCharacter(TORPerks.SpellCraft.EfficientSpellCaster, character, true, ref explainedNumber);
                }
                if (character.GetPerkValue(TORPerks.SpellCraft.Dampener) && abilityTemplate.IsSpell && abilityTemplate.DoesDamage)
                {
                    PerkHelper.AddPerkBonusForCharacter(TORPerks.SpellCraft.Dampener, character, true, ref explainedNumber);
                }
                if(victimCaptain != null && victimCaptain.GetPerkValue(TORPerks.SpellCraft.Dampener) && abilityTemplate.IsSpell && abilityTemplate.DoesDamage)
                {
                    explainedNumber.AddFactor(-0.3f);
                }
                
                if(character.HeroObject == Hero.MainHero && victimLeader != null && victimLeader.HeroObject == Hero.MainHero && abilityTemplate.IsSpell && abilityTemplate.DoesDamage)
                {
                    //friendly fire

                    if (Hero.MainHero.HasCareerChoice("ImperialEnchantmentPassive2"))
                    {
                        var choice = TORCareerChoices.GetChoice("ImperialEnchantmentPassive2");
                        explainedNumber.AddFactor(choice.GetPassiveValue());
                    }
                }
            }
            return explainedNumber.ResultNumber;
        }

        public int GetSpellGoldCostForHero(Hero hero, AbilityTemplate spellTemplate)
        {
            ExplainedNumber goldCost = new ExplainedNumber(spellTemplate.GoldCost);
            if(hero.GetPerkValue(TORPerks.SpellCraft.Librarian))
            {
                PerkHelper.AddPerkBonusForCharacter(TORPerks.SpellCraft.Librarian, hero.CharacterObject, false, ref goldCost);
            }
            return (int)goldCost.ResultNumber;
        }

        public int GetEffectiveWindsCost(CharacterObject character, AbilityTemplate template)
        {
            ExplainedNumber cost = new ExplainedNumber(template.WindsOfMagicCost);
            if(character != null && template != null)
            {
                if (character.GetPerkValue(TORPerks.SpellCraft.OverCaster))
                {
                    cost.AddFactor(TORPerks.SpellCraft.OverCaster.SecondaryBonus);
                }
                if (character.GetPerkValue(TORPerks.SpellCraft.EfficientSpellCaster))
                {
                    cost.AddFactor(TORPerks.SpellCraft.EfficientSpellCaster.SecondaryBonus);
                }
                
                if (character.IsPlayerCharacter)
                {
                    var player = Hero.MainHero;
                    
                    CareerHelper.ApplyBasicCareerPassives(player,ref cost,PassiveEffectType.WindsCostReduction, true);
                }
            }
            return (int)cost.ResultNumber;
        }

        public float GetWindsRechargeRate(CharacterObject baseCharacter)
        {
            if (baseCharacter.HeroObject != null && baseCharacter.HeroObject != Hero.MainHero && baseCharacter.HeroObject.Occupation == Occupation.Lord && baseCharacter.HeroObject.IsSpellCaster()) return 2f;
            ExplainedNumber explainedNumber = new ExplainedNumber(1f, false, null);
            SkillHelper.AddSkillBonusForCharacter(TORSkills.SpellCraft, TORSkillEffects.WindsRechargeRate, baseCharacter, ref explainedNumber);

            if ( baseCharacter.HeroObject != null&& baseCharacter.HeroObject.PartyBelongedTo!=null&&  baseCharacter.HeroObject.PartyBelongedTo.IsMainParty )
            {
                CareerHelper.ApplyBasicCareerPassives(baseCharacter.HeroObject, ref explainedNumber, PassiveEffectType.WindsRegeneration, false);

                var weightmalus = baseCharacter.Equipment.GetTotalWeightOfArmor(true) / 25;
                
                explainedNumber.AddFactor(-weightmalus);
            }
                    
            return explainedNumber.ResultNumber;
        }
        public float GetMaximumWindsOfMagic(CharacterObject baseCharacter)
        {
            if (baseCharacter.HeroObject == null || baseCharacter.HeroObject != Hero.MainHero && baseCharacter.HeroObject.Occupation == Occupation.Lord && baseCharacter.HeroObject.IsSpellCaster()) return 100f;
            
                ExplainedNumber explainedNumber = new ExplainedNumber(10f, false, null);
                SkillHelper.AddSkillBonusForCharacter(TORSkills.SpellCraft, TORSkillEffects.MaxWinds, baseCharacter, ref explainedNumber);
                if (Hero.MainHero.HasAnyCareer())
                {
                    if (baseCharacter.HeroObject == Hero.MainHero)
                    {
                        
                        CareerHelper.ApplyBasicCareerPassives(Hero.MainHero,ref  explainedNumber, PassiveEffectType.WindsOfMagic,false);
                        var CareerChoices = Hero.MainHero.GetAllCareerChoices();
                        if (CareerChoices.Contains("DarkVisionPassive4"))
                        {
                            var spellCount = Hero.MainHero.GetExtendedInfo().AcquiredAbilities.Count;
                            var choice = TORCareerChoices.GetChoice("DarkVisionPassive4");
                            explainedNumber.Add(choice.GetPassiveValue() * spellCount);
                        }

                        if (CareerChoices.Contains("DiscipleOfAccursedPassive4"))
                        {
                            var characterEquipment = Hero.MainHero.CharacterObject.GetCharacterEquipment();
                            foreach (var item in characterEquipment)
                            {
                                var choice = TORCareerChoices.GetChoice("DiscipleOfAccursedPassive4");
                                if (item.IsMagicalItem())
                                {
                                    explainedNumber.Add(choice.GetPassiveValue());
                                } 
                            }
                        }

                        if (Hero.MainHero.PartyBelongedTo!=null&&CareerChoices.Contains("ArcaneKnowledgePassive4"))
                        {
                            var heroes = Hero.MainHero.PartyBelongedTo.GetMemberHeroes();

                            heroes.Remove(Hero.MainHero);
                            foreach (var hero in heroes)
                            {
                                if(hero.Culture.StringId != TORConstants.Cultures.EMPIRE) continue;
                                
                                if (hero.IsSpellCaster())
                                {
                                    var choice = TORCareerChoices.GetChoice("ArcaneKnowledgePassive4");
                                    explainedNumber.Add(choice.GetPassiveValue());
                                }
                                
                            }
                        }
                    }
                    else if (baseCharacter.HeroObject.PartyBelongedTo!=null && baseCharacter.HeroObject.PartyBelongedTo.IsMainParty)
                    {
                        if (Hero.MainHero != null)
                        {
                            var choices = Hero.MainHero.GetAllCareerChoices();
                            if (choices.Contains("EnvoyOfTheLadyPassive3"))
                            {
                                var choice = TORCareerChoices.GetChoice("EnvoyOfTheLadyPassive3");
                                explainedNumber.Add(choice.GetPassiveValue());
                            }
                            
                            if (choices.Contains("LieOfLadyPassive2"))
                            {
                                var choice = TORCareerChoices.GetChoice("LieOfLadyPassive2");
                                explainedNumber.Add(choice.GetPassiveValue());
                            }
                            if (choices.Contains("CollegeOrdersPassive2"))
                            {
                                var choice = TORCareerChoices.GetChoice("CollegeOrdersPassive2");
                                explainedNumber.Add(choice.GetPassiveValue());
                            }
                            
                            if (choices.Contains("WellspringOfDharPassive3"))
                            {
                                var choice = TORCareerChoices.GetChoice("WellspringOfDharPassive3");
                                explainedNumber.Add(choice.GetPassiveValue());
                            }
                        }
                    }
                }

                if (Hero.MainHero.HasCareer(TORCareers.ImperialMagister))
                {
                    var stoneBehavior =
                        CareerButtons.Instance.GetCareerButton(TORCareers.ImperialMagister) as
                            ImperialMagisterCareerButtonBehavior;

                    var powerstones = stoneBehavior.GetAllPowerstones();
                    
                    var reserved = powerstones.Sum(pair => (pair.Upkeep));
                    
                    explainedNumber.Add(-reserved);
                }
                return explainedNumber.ResultNumber;
        }

        public bool IsValidLoreForCharacter(Hero hero, LoreObject loreObject)
        {
            if (!hero.IsVampire() && loreObject.IsRestrictedToVampires) return false;

            if (hero.HasCareer(TORCareers.Necrarch))
            {
                if(loreObject.ID == "LoreOfLife" || loreObject.ID == "LoreOfLight") return false;
                if(hero.HasUnlockedCareerChoiceTier(3))
                    if(!hero.HasKnownLore("DarkMagic")&& loreObject.ID!="DarkMagic") 
                        return false;
                
                return true;
            }
            return !loreObject.DisabledForCultures.Contains(hero.Culture.StringId);
        }
    }
}
