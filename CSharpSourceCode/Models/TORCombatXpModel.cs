using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.Core;

namespace TOR_Core.Models
{
    public class TORCombatXpModel : DefaultCombatXpModel
    {
        public override SkillObject GetSkillForWeapon(WeaponComponentData weapon, bool isSiegeWeaponHit)
        {
            SkillObject result = DefaultSkills.Athletics;
            var baseResult = base.GetSkillForWeapon(weapon, isSiegeWeaponHit);
            if (baseResult != null) result = baseResult;
            return result;
        }
    }
}
