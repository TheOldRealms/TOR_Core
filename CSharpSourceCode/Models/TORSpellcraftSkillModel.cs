using Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TOR_Core.AbilitySystem;
using TOR_Core.CharacterDevelopment;

namespace TOR_Core.Models
{
    public class TORSpellcraftSkillModel : GameModel
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
            return 100;
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
    }
}
