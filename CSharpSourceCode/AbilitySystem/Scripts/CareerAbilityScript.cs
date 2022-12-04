using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.Library;
using TOR_Core.BattleMechanics.TriggeredEffect;
using TOR_Core.CharacterDevelopment;
using TOR_Core.Extensions;

namespace TOR_Core.AbilitySystem.Scripts
{
    public class CareerAbilityScript : AbilityScript
    {
        protected override TriggeredEffect GetEffectToTrigger()
        {
            var id = _ability?.Template.TriggeredEffectID;
            if (id != null && string.IsNullOrEmpty(id))
            {
                if(_casterAgent.GetHero() != null)
                {
                    var info = _casterAgent.GetHero().GetExtendedInfo();
                    if(info != null && string.IsNullOrEmpty(info.CareerID))
                    {
                        var career = TORCareers.All.FirstOrDefault(x => x.StringId == info.CareerID);
                        if(career != null)
                        {
                            var template = TriggeredEffectManager.GetTemplateWithId(id).Clone(id + "_modified_" + _casterAgent.Index);
                            career.MutateTriggeredEffect(template, _casterAgent.GetHero());
                            return new TriggeredEffect(template);
                        }
                    }
                }
            }
            return null;
        }
    }
}
