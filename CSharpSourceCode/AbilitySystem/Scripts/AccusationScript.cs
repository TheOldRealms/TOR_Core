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

            var chance = _ability.Template.ScaleVariable1;

            var additionalTargetNumber = CalculateAdditonalTargetAmount(chance);
            
            var effects = GetEffectsToTrigger();
            
            var targets = GetAdditionalAccusationMarkTargets(_target.Position.AsVec2, additionalTargetNumber);

            foreach (var effect in effects)
            {
                effect.Trigger(position, normal, _casterAgent, _ability.Template, targets );
            }

            if (Hero.MainHero.HasCareerChoice("NoRestAgainstEvilKeystone"))
            {
                var effect = effects.FirstOrDefault();
                if (effect != null)
                {
                    _casterAgent.ApplyStatusEffect("accusation_buff_penetration", _casterAgent, effect.ImbuedStatusEffectDuration, false);
                }
                
            }
        }

        public static int CalculateAdditonalTargetAmount(float chance)
        {
            var additionalTargetNumber = 0;
            if (chance > 1)
            {
                additionalTargetNumber = 9;
            }
            else
            {
                for (int i = 0; i < 9; i++)
                {
                    var trial = MBRandom.RandomFloatRanged(0f,1f);
                    if (trial <= chance)
                    {
                        additionalTargetNumber++;
                        continue;
                    }
                    break;
                }
            }

            return additionalTargetNumber;
        }


        public static MBList<Agent> GetAdditionalAccusationMarkTargets(Vec2 pos, int limit = 0)
        {
            var targets = Mission.Current.GetNearbyAgents(pos, 5, new MBList<Agent>()).TakeRandom(limit).ToMBList();
            if (limit > 0&&targets.Count < limit)
            {
                List<Agent> list = targets.ToList();
                targets = list.TakeRandom(limit).ToMBList();
            }
            
            for (var index = 0; index < targets.Count; index++)
            {
                var target = targets[index];
                if (target.Team.IsPlayerTeam)
                {
                    targets.Remove(target);
                    continue;
                }
                
                var tempAttributes = target.GetComponent<StatusEffectComponent>().GetTemporaryAttributes();

                if (tempAttributes.Contains("AccusationMark"))
                {
                    targets.Remove(target);
                }
            }
            
            return targets;
        }

        public override void Stop()
        {
            TORCommon.Say("Stop");
            base.Stop();
        }
    }
}