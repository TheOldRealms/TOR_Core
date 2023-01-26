using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;

namespace TOR_Core.CharacterDevelopment.CareerSystem
{
    public static class CareerHelper
    {
        public static float AddSkillEffectToValue(CareerChoiceObject careerChoice, Hero hero, List<SkillObject> relevantSkills, float scalingFactor)
        {
            float result = 0f;
            if(hero != null && relevantSkills != null && relevantSkills.Count > 0)
            {
                foreach (var skill in relevantSkills)
                {
                    int skillValue = hero.GetSkillValue(skill);
                    result = skillValue * scalingFactor;
                }
            }
            return result;
        }
    }
}
