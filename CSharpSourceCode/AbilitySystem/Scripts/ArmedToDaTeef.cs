using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.LinQuick;
using TaleWorlds.MountAndBlade;
using TOR_Core.BattleMechanics.TriggeredEffect;
using TOR_Core.Extensions;

namespace TOR_Core.AbilitySystem.Scripts
{
    public class ArmedToDaTeef : CareerAbilityScript
    {
        protected override List<TriggeredEffect> GetEffectsToTrigger()
        {

            List<TriggeredEffect> result = base.GetEffectsToTrigger();

            if (CasterAgent == null || CasterAgent.GetHero() == null) return result;

            var hero = CasterAgent.GetHero();
            var info = hero.GetExtendedInfo();

            if (info == null || string.IsNullOrEmpty(info.CareerID)) return result;

            var career = hero.GetCareer();
            if (career == null) return result;


            if (CasterAgent.WieldedWeapon.IsEmpty)
                return  result;

            if (CasterAgent.WieldedWeapon.CurrentUsageItem.RelevantSkill == DefaultSkills.Polearm)
            {
                TriggeredEffectTemplate dismountTemplate = (TriggeredEffectTemplate)TriggeredEffectManager.GetTemplateWithId("armed_to_da_teef_dismount").Clone("armed_to_da_teef_dismount" + "*cloned*" + CasterAgent.Index);
                var dismount = new TriggeredEffect(dismountTemplate);
                var targets = new MBList<Agent>();
                var riders = Mission.Current.GetNearbyEnemyAgents( this.CurrentGlobalPosition.AsVec2, 4 , CasterAgent.Team.GetEnemyTeams().FirstOrDefault(),targets).WhereQ(x=> x.HasMount).ToMBList();
                
                dismount.Trigger(this.CurrentGlobalPosition,Vec3.Up, CasterAgent,null,riders);
            }
            
            if (Hero.MainHero.HasCareerChoice("GetToDaChoppasKeystone"))
            {
                string buffEffectId = "get_to_da_choppas_buff";
                TriggeredEffectTemplate buffTemplate = (TriggeredEffectTemplate)TriggeredEffectManager.GetTemplateWithId(buffEffectId).Clone(buffEffectId + "*cloned*" + CasterAgent.Index);
                result.Add(new TriggeredEffect(buffTemplate));
            }

            return result;
        }
    }
}
