using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TOR_Core.Extensions;

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
    }
}
