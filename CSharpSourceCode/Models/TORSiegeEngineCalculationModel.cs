using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade.ComponentInterfaces;

namespace TOR_Core.Models
{
    public class TORSiegeEngineCalculationModel : DefaultSiegeEngineCalculationModel
    {

        public static readonly float BaseCannonReloadSpeed = 20f; 


        public float CalculateCannonReloadSpeed(float cannonReloadSpeed, Hero hero)
        {
            var engineeringSkillValue = hero.GetSkillValue(DefaultSkills.Engineering);
            
            var gunPowderSkillValue = hero.GetSkillValue(DefaultSkills.Engineering);

            var reductionFactor = gunPowderSkillValue / 300 * 0.4f;
            reductionFactor += engineeringSkillValue / 300 * 0.4f;
            
            return cannonReloadSpeed * (reductionFactor*cannonReloadSpeed);
        }
    }
}