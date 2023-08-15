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
using TOR_Core.BattleMechanics.StatusEffect;
using TOR_Core.CharacterDevelopment;
using TOR_Core.CharacterDevelopment.CareerSystem;
using TOR_Core.Extensions;

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
                    return null;
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
                    return TORSkillEffects.SpellDuration;
                default:
                    return null;
            }
        }

        public int GetSkillXpForCastingAbility(AbilityTemplate ability)
        {
            if (ability.AbilityType == AbilityType.Prayer)
            {
                return ability.CoolDown * 2;
            }
            return ability.WindsOfMagicCost * 20;
        }

        public int GetSkillXpForAbilityDamage(AbilityTemplate ability, int damageAmount)
        {
            return damageAmount;
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
            return statusEffectDuration * skillmultiplier * perkmultiplier;
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
    }
}
