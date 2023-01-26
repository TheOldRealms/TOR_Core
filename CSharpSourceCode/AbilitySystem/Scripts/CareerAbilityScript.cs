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
        protected override List<TriggeredEffect> GetEffectsToTrigger()
        {
            List<TriggeredEffect> result = new List<TriggeredEffect>();
            var effects = _ability?.Template.TriggeredEffects;
            foreach (var effect in effects)
            {
                if (effect != null && !string.IsNullOrEmpty(effect))
                {
                    if (_casterAgent.GetHero() != null)
                    {
                        var info = _casterAgent.GetHero().GetExtendedInfo();
                        if (info != null && !string.IsNullOrEmpty(info.CareerID))
                        {
                            var career = _casterAgent.GetHero().GetCareer();
                            if (career != null)
                            {
                                TriggeredEffectTemplate template = (TriggeredEffectTemplate)TriggeredEffectManager.GetTemplateWithId(effect).Clone(effect + "*cloned*" + _casterAgent.Index);
                                career.MutateTriggeredEffect(template, _casterAgent);
                                result.Add(new TriggeredEffect(template, true));
                            }
                        }
                    }
                }
            }
            return result;
        }

        protected override void OnTick(float dt)
        {
            if (!_casterAgent.IsActive()) Stop();
            base.OnTick(dt);
        }

        protected override bool ShouldMove() => true;

        protected override MatrixFrame GetNextFrame(MatrixFrame oldFrame, float dt)
        {
            return new MatrixFrame(_casterAgent.LookRotation, _casterAgent.GetChestGlobalPosition());
        }
    }
}
