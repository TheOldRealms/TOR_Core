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
        public List<TriggeredEffect> EffectsToTrigger => GetEffectsToTrigger();

        protected override List<TriggeredEffect> GetEffectsToTrigger()
        {
            List<TriggeredEffect> result = new List<TriggeredEffect>();
            var effects = Ability?.Template.TriggeredEffects;
            if (effects == null) return result;
            foreach (var effect in effects)
            {
                if (effect == null || string.IsNullOrEmpty(effect)) continue;
                if (CasterAgent.GetHero() == null) continue;
                
                var info = CasterAgent.GetHero().GetExtendedInfo();
                if (info != null && !string.IsNullOrEmpty(info.CareerID))
                {
                    var career = CasterAgent.GetHero().GetCareer();
                    if (career != null)
                    {
                        TriggeredEffectTemplate template = (TriggeredEffectTemplate)TriggeredEffectManager.GetTemplateWithId(effect).Clone(effect + "*cloned*" + CasterAgent.Index);
                        career.MutateTriggeredEffect(template, CasterAgent);
                        result.Add(new TriggeredEffect(template, true));
                    }
                }
            }
            return result;
        }

        protected override void OnBeforeTick(float dt)
        {
            if (!CasterAgent.IsActive()) Stop();
        }

        protected override bool ShouldMove() => true;

        protected override MatrixFrame GetNextGlobalFrame(MatrixFrame oldFrame, float dt)
        {
            return new MatrixFrame(CasterAgent.LookRotation, CasterAgent.GetChestGlobalPosition());
        }
    }
}
