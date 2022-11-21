using System;
using System.Collections.Generic;
using TaleWorlds.MountAndBlade;
using TOR_Core.Extensions;
using TaleWorlds.Engine;
using System.Linq;
using TOR_Core.BattleMechanics.DamageSystem;
using TOR_Core.Utilities;
using TaleWorlds.Core;
using TOR_Core.Extensions.ExtendedInfoSystem;

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

        public void RunStatusEffect(string id, Agent applierAgent, float multiplier)
        {
            if (Agent == null)
                return;

            StatusEffect effect = _currentEffects.Keys.Where(e => e.Template.Id.Equals(id)).FirstOrDefault();
            if (effect != null)
            {
                effect.CurrentDuration += (int)(effect.Template.BaseDuration * multiplier);
            }
            else
            {
                effect = StatusEffectManager.CreateNewStatusEffect(id);
                effect.CurrentDuration = (int)(effect.Template.BaseDuration * multiplier);
                AddEffect(effect, applierAgent);
            }
        }

        
        public void OnElapsed(float dt)
        {
            foreach (StatusEffect effect in _currentEffects.Keys)
            {
                effect.CurrentDuration--;
                if (effect.CurrentDuration <= 0)
                {
                    RemoveEffect(effect);
                    return;
                }
            }
            CalculateEffectAggregate();
            StatusEffect dotEffect = _currentEffects.Keys.Where(x => x.Template.Type == StatusEffectTemplate.EffectType.DamageOverTime).FirstOrDefault();
            EffectData data = null;
            if (dotEffect != null)
            {
                data = _currentEffects[dotEffect];
            }

            //Temporary method for applying effects from the aggregate. This needs to go to a damage manager/calculator which will use the 
            //aggregated information to determine how much damage to apply to the agent
            if (Agent.IsActive() && Agent != null && !Agent.IsFadingOut())
            {
                if (_effectAggregate.DamageOverTime > 0 && data != null)
                {
                    Agent.ApplyDamage((int)_effectAggregate.DamageOverTime, Agent.Position, data.ApplierAgent, false, false);
                }
                else if (_effectAggregate.HealthOverTime > 0)
                {
                    Agent.Heal((int)_effectAggregate.HealthOverTime);
                }

                if (_effectAggregate.WindsofMagicOverTime > 0)
                {
                    Agent.ChangeCurrentWind(_effectAggregate.WindsofMagicOverTime);
                }
                
                if (_effectAggregate.CooldownReduction > 0)     //Cooldown reduction atleast 2 seconds duration for ticking once
                {
                    var list = Agent.GetAllSelectedAbilities();
                    foreach (var ability in list)
                    {
                        if (ability.IsOnCooldown())
                        {
                            ability.ReduceCoolDown(_effectAggregate.CooldownReduction);
                        }
                    }
                }
                
            }
        }

        private void CalculateEffectAggregate()
        {
            _effectAggregate = new EffectAggregate();
            foreach (var effect in _currentEffects.Keys)
            {
                _effectAggregate.AddEffect(effect);
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

        public float[] GetAmplifiers(AttackType mask)
        {
            var array= Enumerable.Range(0,(int) DamageType.All+1).Select(x => _effectAggregate.DamageAmplification[x,(int)mask - 1]).ToArray();
            return array;
            // _effectAggregate.DamageAmplification[]

            //  return _effectAggregate.DamageAmplification;
        }

        public float[] GetResistances(AttackType mask)
        {
            var array = Enumerable.Range(0, (int)DamageType.All + 1).Select(x => _effectAggregate.Resistance[x, (int)mask - 1]).ToArray();
            return array;
           // return _effectAggregate.Resistance;
        }

        private void AddEffect(StatusEffect effect, Agent applierAgent)
        {
            List<GameEntity> childEntities;
            TORParticleSystem.ApplyParticleToAgent(Agent, effect.Template.ParticleId, out childEntities, effect.Template.ParticleIntensity, effect.Template.ApplyToRootBoneOnly);

            EffectData data = new EffectData(effect, childEntities, applierAgent);
            data.ParticleEntities = childEntities;

            _currentEffects.Add(effect, data);
        }

        private class EffectData
        {
            public EffectData(StatusEffect effect, List<GameEntity> particleEntities, Agent applierAgent)
            {
                Effect = effect;
                ParticleEntities = particleEntities;
                ApplierAgent = applierAgent;
            }

            public List<GameEntity> ParticleEntities { get; set; }
            public StatusEffect Effect { get; set; }
            public Agent ApplierAgent { get; set; }
        }

        private class EffectAggregate
        {
            public float CooldownReduction { get; set; } = 0;
            public float WindsofMagicOverTime { get; set; } = 0;
            public float HealthOverTime { get; set; } = 0;
            public float DamageOverTime { get; set; } = 0;
            
            public readonly float[,] DamageAmplification = new float[(int)DamageType.All + 1, 3];
            public readonly float[,] Resistance = new float[(int)DamageType.All + 1,3];

            public void AddEffect(StatusEffect effect)
            {
                var template = effect.Template;
                switch (template.Type)
                {
                    case StatusEffectTemplate.EffectType.DamageOverTime:
                        DamageOverTime += template.ChangePerTick;
                        break;
                    case StatusEffectTemplate.EffectType.HealthOverTime:
                        HealthOverTime += template.ChangePerTick;
                        break;
                    case StatusEffectTemplate.EffectType.DamageAmplification :
                        var dmgMask = template.DamageAmplifier.AttackType;

                        switch (dmgMask)
                        {
                            case AttackType.Melee:
                                DamageAmplification[(int)template.DamageAmplifier.AmplifiedDamageType, 0] += template.DamageAmplifier.DamageAmplifier;
                                break;
                            case AttackType.Range:
                                DamageAmplification[(int)template.DamageAmplifier.AmplifiedDamageType, 1] += template.DamageAmplifier.DamageAmplifier;
                                break;
                            case AttackType.Spell:
                                DamageAmplification[(int)template.DamageAmplifier.AmplifiedDamageType, 2] += template.DamageAmplifier.DamageAmplifier;
                                break;
                            case AttackType.All:
                                DamageAmplification[(int)template.DamageAmplifier.AmplifiedDamageType, 0] += template.DamageAmplifier.DamageAmplifier;
                                DamageAmplification[(int)template.DamageAmplifier.AmplifiedDamageType, 1] += template.DamageAmplifier.DamageAmplifier;
                                DamageAmplification[(int)template.DamageAmplifier.AmplifiedDamageType, 2] += template.DamageAmplifier.DamageAmplifier;
                                break;
                            case AttackType.None:
                                break;
                            default:
                                throw new ArgumentOutOfRangeException();
                        }

                        break;
                    case StatusEffectTemplate.EffectType.Resistance:
                        var resMask = template.Resistance.AttackType;
                        
                        switch (resMask)
                        {
                            case AttackType.Melee:
                                Resistance[(int)template.Resistance.ResistedDamageType, 0] += template.Resistance.ReductionPercent;
                                break;
                            case AttackType.Range:
                                Resistance[(int)template.Resistance.ResistedDamageType, 1] += template.Resistance.ReductionPercent;
                                break;
                            case AttackType.Spell:
                                Resistance[(int)template.Resistance.ResistedDamageType, 2] += template.Resistance.ReductionPercent;
                                break;
                            case AttackType.All:
                                Resistance[(int)template.Resistance.ResistedDamageType, 0] += template.Resistance.ReductionPercent;
                                Resistance[(int)template.Resistance.ResistedDamageType, 1] += template.Resistance.ReductionPercent;
                                Resistance[(int)template.Resistance.ResistedDamageType, 2] += template.Resistance.ReductionPercent;
                                break;
                            case AttackType.None:
                                break;
                            default:
                                throw new ArgumentOutOfRangeException();
                        }
                        break;
                    case StatusEffectTemplate.EffectType.WindsRegeneration:
                        WindsofMagicOverTime += template.ChangePerTick;
                        break;
                    case StatusEffectTemplate.EffectType.CoolDownReduction:
                        CooldownReduction += template.ChangePerTick;
                        break;
                }
            }
        }
    }
}