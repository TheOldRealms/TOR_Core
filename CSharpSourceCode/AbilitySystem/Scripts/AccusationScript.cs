using System.Collections.Generic;
using System.Linq;
using Ink.Parsed;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TOR_Core.BattleMechanics.StatusEffect;
using TOR_Core.BattleMechanics.TriggeredEffect;
using TOR_Core.Extensions;
using TOR_Core.Utilities;

namespace TOR_Core.AbilitySystem.Scripts
{
    public class AccusationScript : CareerAbilityScript
    {
        private Agent _target;

        private List<TriggeredEffect> _effects;
        protected override void OnInit()
        {
            base.OnInit();
            
            
        }

        public List<TriggeredEffect> GetEffects()
        {
            return _effects;
        }

        public override void SetExplicitSingleTarget(Agent agent)
        {
            base.SetExplicitSingleTarget(agent);
            _target = agent;
        }

        protected override void TriggerEffects(Vec3 position, Vec3 normal)
        {
            if (GetEffectsToTrigger().Any())
            {
                _effects= GetEffectsToTrigger();
            }
            base.TriggerEffects(position,normal);

            if (_target == null) return;
            if(!Hero.MainHero.HasCareerChoice("GuiltyByAssociationKeystone")) return;
            
            var effects = GetEffectsToTrigger();
            
            var targets = GetAdditionalAccusationMarkTargets(_target.Position.AsVec2);

            foreach (var effect in effects)
            {
                effect.Trigger(position, normal, _casterAgent, _ability.Template, targets );
            }
        }


        public static MBList<Agent> GetAdditionalAccusationMarkTargets(Vec2 pos, int limit = 0)
        {
            var targets = Mission.Current.GetNearbyAgents( pos, 5, new MBList<Agent>());
            for (var index = 0; index < targets.Count; index++)
            {
                var target = targets[index];
                var tempAttributes = target.GetComponent<StatusEffectComponent>().GetTemporaryAttributes();

                if (tempAttributes.Contains("AccusationMark"))
                {
                    targets.Remove(target);
                }
            }
            
            targets.Remove(Agent.Main);
            

            return limit == 0 ? targets.Take(MBRandom.RandomInt(1, 2)).ToMBList() : targets.Take(MBRandom.RandomInt(1, limit)).ToMBList();
        }

        public override void Stop()
        {
            TORCommon.Say("Stop");
            base.Stop();
        }
    }
}