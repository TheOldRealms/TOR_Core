using System.Collections.Generic;
using TaleWorlds.MountAndBlade;
using TOR_Core.Extensions;
using TaleWorlds.Engine;
using System.Linq;
using TOR_Core.BattleMechanics.DamageSystem;
using TOR_Core.Utilities;
using TaleWorlds.Library;
using TaleWorlds.Core;
using TaleWorlds.CampaignSystem;
using TOR_Core.Extensions.ExtendedInfoSystem;
using System;

namespace TOR_Core.BattleMechanics.StatusEffect
{
    public class StatusEffectComponent : AgentComponent
    {
        private float _updateFrequency = 1;
        private float _deltaSinceLastTick = MBRandom.RandomFloatRanged(0, 0.1f);
        private Dictionary<StatusEffect, EffectData> _currentEffects;
        private EffectAggregate _effectAggregate;

        public StatusEffectComponent(Agent agent) : base(agent)
        {
            _currentEffects = new Dictionary<StatusEffect, EffectData>();
            _effectAggregate = new EffectAggregate();
        }

        public void RunStatusEffect(string effectId, Agent applierAgent, float duration, bool append, bool isMutated)
        {
            if (Agent == null)
                return;

            StatusEffect effect = _currentEffects.Keys.Where(e => e.Template.Id.Equals(effectId)).FirstOrDefault();
            if (effect != null)
            {
                if (append) effect.CurrentDuration += duration;
            }
            else
            {
                effect = StatusEffectManager.CreateNewStatusEffect(effectId, applierAgent, isMutated);
                if (applierAgent.IsHero)
                {
                    var career = applierAgent.GetHero().GetCareer();
                    if(career != null) career.MutateStatusEffect(effect, applierAgent.GetHero());
                }
                effect.CurrentDuration = duration;
                AddEffect(effect);
            }
        }

        public override void OnAgentRemoved() => CleanUp();

        public void OnElapsed(float dt)
        {
            foreach (var item in _currentEffects.ToList())
            {
                StatusEffect effect = item.Key;
                effect.CurrentDuration--;
                if (effect.CurrentDuration <= 0)
                {
                    RemoveEffect(effect);
                }
            }
            CalculateEffectAggregate();
            StatusEffect dotEffect = _currentEffects.Keys.Where(x => x.Template.Type == StatusEffectTemplate.EffectType.DamageOverTime).FirstOrDefault();

            //Temporary method for applying effects from the aggregate. This needs to go to a damage manager/calculator which will use the 
            //aggregated information to determine how much damage to apply to the agent
            if (Agent.IsActive() && Agent != null && !Agent.IsFadingOut())
            {
                if (_effectAggregate.DamageOverTime > 0)
                {
                    Agent.ApplyDamage((int)_effectAggregate.DamageOverTime, Agent.Position, dotEffect.ApplierAgent, false, false);
                }
                else if (_effectAggregate.HealthOverTime > 0)
                {
                    Agent.Heal((int)_effectAggregate.HealthOverTime);
                }
            }
        }

        private void CalculateEffectAggregate()
        {
            _effectAggregate = new EffectAggregate();
            foreach (var item in _currentEffects)
            {
                _effectAggregate.AddEffect(item);
            }
        }

        public void OnTick(float dt)
        {
            _deltaSinceLastTick += dt;
            if (_deltaSinceLastTick > _updateFrequency)
            {
                _deltaSinceLastTick = MBRandom.RandomFloatRanged(0, 0.1f);
                OnElapsed(dt);
            }
        }

        private void RemoveEffect(StatusEffect effect)
        {
            EffectData data = _currentEffects[effect];

            data.ParticleEntities.ForEach(pe =>
            {
                pe.RemoveAllParticleSystems();
                pe = null;
            });

            _currentEffects.Remove(effect);
        }

        public float[] GetAmplifiers(AttackTypeMask mask)
        {
            return _effectAggregate.DamageAmplifications[mask];
        }

        public float[] GetResistances(AttackTypeMask mask)
        {
            return _effectAggregate.Resistances[mask];
        }

        public List<string> GetTemporaryAttributes()
        {
            List<string> list = new List<string>();
            foreach(var effect in _currentEffects.Keys)
            {
                foreach(var attribute in effect.Template.TemporaryAttributes)
                {
                    if (!list.Contains(attribute)) list.Add(attribute);
                }
            }
            return list;
        }

        private void AddEffect(StatusEffect effect)
        {
            List<GameEntity> childEntities;
            TORParticleSystem.ApplyParticleToAgent(Agent, effect.Template.ParticleId, out childEntities, effect.Template.ParticleIntensity, effect.Template.ApplyToRootBoneOnly);

            EffectData data = new EffectData(effect, childEntities);
            data.ParticleEntities = childEntities;

            _currentEffects.Add(effect, data);
        }

        private void CleanUp()
        {
            foreach (var item in _currentEffects.ToList())
            {
                RemoveEffect(item.Key);
            }
            _currentEffects.Clear();
            _effectAggregate = null;
        }

        private class EffectData
        {
            public EffectData(StatusEffect effect, List<GameEntity> particleEntities)
            {
                Effect = effect;
                ParticleEntities = particleEntities;
            }

            public List<GameEntity> ParticleEntities { get; set; }
            public StatusEffect Effect { get; set; }
        }

        private class EffectAggregate
        {
            public float HealthOverTime { get; set; } = 0;
            public float DamageOverTime { get; set; } = 0;
            public Dictionary<AttackTypeMask, float[]> DamageAmplifications { get; }
            public Dictionary<AttackTypeMask, float[]> Resistances { get; }

            public EffectAggregate()
            {
                DamageAmplifications = new Dictionary<AttackTypeMask, float[]>();
                Resistances = new Dictionary<AttackTypeMask, float[]>();
                foreach (AttackTypeMask item in Enum.GetValues(typeof(AttackTypeMask)))
                {
                    DamageAmplifications.Add(item, new float[(int)DamageType.All + 1]);
                    Resistances.Add(item, new float[(int)DamageType.All + 1]);
                }
            }

            public void AddEffect(KeyValuePair<StatusEffect, EffectData> effect)
            {
                var template = effect.Key.Template;
                var strength = template.BaseEffectValue;
                
                switch (template.Type)
                {
                    case StatusEffectTemplate.EffectType.DamageOverTime:
                        DamageOverTime += strength;
                        break;
                    case StatusEffectTemplate.EffectType.HealthOverTime:
                        HealthOverTime += strength;
                        break;
                    case StatusEffectTemplate.EffectType.DamageAmplification :
                        AddDamageAmplification(template.DamageType, template.AttackTypeMask, strength);
                        break;
                    case StatusEffectTemplate.EffectType.Resistance:
                        AddResistance(template.DamageType, template.AttackTypeMask, strength);
                        break;
                }
            }

            private void AddResistance(DamageType damageType, AttackTypeMask attackMask, float value)
            {
                if (attackMask.HasAnyFlag(AttackTypeMask.Ranged))
                {
                    Resistances[AttackTypeMask.Ranged][(int)damageType] += value;
                }
                if (attackMask.HasAnyFlag(AttackTypeMask.Spell))
                {
                    Resistances[AttackTypeMask.Spell][(int)damageType] += value;
                }
                if (attackMask.HasAnyFlag(AttackTypeMask.Melee))
                {
                    Resistances[AttackTypeMask.Melee][(int)damageType] += value;
                }
            }

            private void AddDamageAmplification(DamageType damageType, AttackTypeMask attackMask, float value)
            {
                if (attackMask.HasAnyFlag(AttackTypeMask.Ranged))
                {
                    DamageAmplifications[AttackTypeMask.Ranged][(int)damageType] += value;
                }
                if (attackMask.HasAnyFlag(AttackTypeMask.Spell))
                {
                    DamageAmplifications[AttackTypeMask.Spell][(int)damageType] += value;
                }
                if (attackMask.HasAnyFlag(AttackTypeMask.Melee))
                {
                    DamageAmplifications[AttackTypeMask.Melee][(int)damageType] += value;
                }
            }
        }
    }
}