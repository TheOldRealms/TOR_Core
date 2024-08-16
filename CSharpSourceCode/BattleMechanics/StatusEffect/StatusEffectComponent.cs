using System.Collections.Generic;
using TaleWorlds.MountAndBlade;
using TOR_Core.Extensions;
using TaleWorlds.Engine;
using System.Linq;
using TOR_Core.BattleMechanics.DamageSystem;
using TOR_Core.Utilities;
using TaleWorlds.Library;
using TaleWorlds.Core;
using TOR_Core.Extensions.ExtendedInfoSystem;
using System;
using TaleWorlds.TwoDimension;
using TOR_Core.AbilitySystem;
using TOR_Core.BattleMechanics.SFX;
using TOR_Core.CharacterDevelopment.CareerSystem;
using TaleWorlds.CampaignSystem;

namespace TOR_Core.BattleMechanics.StatusEffect
{
    public class StatusEffectComponent : AgentComponent, IDisposable
    {
        private GameEntity _dummyEntity;
        private float _updateFrequency = 1;
        private float _deltaSinceLastTick = MBRandom.RandomFloatRanged(0, 0.1f);
        private Dictionary<StatusEffect, EffectData> _currentEffects;
        private EffectAggregate _effectAggregate;
        public bool ModifiedDrivenProperties;
        private bool _restoredBaseValues;
        private bool _initBaseValues;
        private bool _disabled;
        private Dictionary<DrivenProperty, float> _baseValues;

        public StatusEffectComponent(Agent agent) : base(agent)
        {
            _currentEffects = new Dictionary<StatusEffect, EffectData>();
            _effectAggregate = new EffectAggregate();
            _dummyEntity = GameEntity.CreateEmpty(Mission.Current.Scene, false);
            _dummyEntity.Name = "_dummyEntity_" + Agent.Index;
            _baseValues = new Dictionary<DrivenProperty, float>();
        }

        public void SynchronizeBaseValues(bool mountOnly = false)
        {
            if (Agent.AgentDrivenProperties == null) return;
            if (!mountOnly)
            {
                _baseValues.AddOrReplace(DrivenProperty.MaxSpeedMultiplier, this.Agent.AgentDrivenProperties.MaxSpeedMultiplier);
                _baseValues.AddOrReplace(DrivenProperty.SwingSpeedMultiplier, this.Agent.AgentDrivenProperties.SwingSpeedMultiplier);
                _baseValues.AddOrReplace(DrivenProperty.ThrustOrRangedReadySpeedMultiplier, this.Agent.AgentDrivenProperties.ThrustOrRangedReadySpeedMultiplier);
                _baseValues.AddOrReplace(DrivenProperty.ReloadSpeed, this.Agent.AgentDrivenProperties.ReloadSpeed);
                _baseValues.AddOrReplace(DrivenProperty.BipedalRangedReloadSpeedMultiplier, this.Agent.AgentDrivenProperties.BipedalRangedReloadSpeedMultiplier);
            }

            if (!this.Agent.HasMount) return;
            _baseValues.AddOrReplace(DrivenProperty.MountManeuver, Agent.MountAgent.AgentDrivenProperties.MountManeuver);
            _baseValues.AddOrReplace(DrivenProperty.MountSpeed, this.Agent.MountAgent.AgentDrivenProperties.MountSpeed);
            _baseValues.AddOrReplace(DrivenProperty.MountDashAccelerationMultiplier, this.Agent.MountAgent.AgentDrivenProperties.MountDashAccelerationMultiplier);
        }

        public void RunStatusEffect(string effectId, Agent applierAgent, float duration, bool append, bool isMutated, bool stack)
        {
            if (Agent == null) return;
            if(_disabled) return;

            StatusEffect effect = _currentEffects.Keys.Where(e => e.Template.StringID.Equals(effectId)).FirstOrDefault();
            if (effect != null)
            {
                if (append)
                {
                    effect.CurrentDuration += duration;
                }
                else
                {
                    effect.CurrentDuration = duration; 
                }
                if (stack)
                {
                    StatusEffectManager.CreateNewStatusEffect(effectId, applierAgent, isMutated);
                    AddEffect(effect);
                }
            }
            else
            {
                effect = StatusEffectManager.CreateNewStatusEffect(effectId, applierAgent, isMutated);
                if (applierAgent!=null&&applierAgent.IsHero)
                {
                    var career = applierAgent.GetHero().GetCareer();
                    if (career != null) career.MutateStatusEffect(effect.Template, applierAgent);
                }

                effect.CurrentDuration = duration;
                AddEffect(effect);
            }
        }

        public bool AreBaseValuesInitialized()
        {
            return _initBaseValues;
        }

        public override void OnAgentRemoved() => CleanUp();
        
        public override void OnComponentRemoved() => CleanUp();

        public void OnElapsed(float dt)
        {
            if (!_initBaseValues)
            {
                SynchronizeBaseValues();
                _initBaseValues = true;
            }

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
            
            if (Agent.IsActive() && Agent != null && !Agent.IsFadingOut())
            {
                if (_effectAggregate == null) return;

                if (_effectAggregate != null && _effectAggregate.WindsOverTime > 0)
                {
                    if (Agent.IsHero && Agent.IsSpellCaster())
                        Agent.GetHero().AddWindsOfMagic(_effectAggregate.WindsOverTime);
                }


                if (_effectAggregate.DamageOverTime > 0)
                {
                    var value = _effectAggregate.DamageOverTime;
                    var effect = _currentEffects.Keys.FirstOrDefault(x => x.Template.Type == StatusEffectTemplate.EffectType.DamageOverTime&& x.ApplierAgent == Agent.Main) ?? 
                                  _currentEffects.Keys.FirstOrDefault(x => x.Template.Type == StatusEffectTemplate.EffectType.DamageOverTime);
                    
                    var applier = effect?.ApplierAgent;

                    if (Campaign.Current != null && applier != null && (applier.IsMainAgent || applier.BelongsToMainParty()))
                    {
                        CareerHelper.ApplyCareerAbilityCharge((int)value,ChargeType.Healed,AttackTypeMask.Spell,applier);
                    }

                    Agent.ApplyDamage((int)_effectAggregate.DamageOverTime, Agent.Position, applier, false, false);
                }
                else if (_effectAggregate.HealthOverTime > 0)
                {
                    
                    var healingValue =(int) _effectAggregate.HealthOverTime;
                    Agent.Heal(healingValue);

                    if (Agent.HasMount && Agent.HasAttribute("HorseLink"))
                    {
                        Agent.MountAgent.Heal(healingValue);
                    }
                    
                    var effect = _currentEffects.Keys.FirstOrDefault(x => x.Template.Type == StatusEffectTemplate.EffectType.HealthOverTime&& x.ApplierAgent == Agent.Main) ?? 
                                  _currentEffects.Keys.FirstOrDefault(x => x.Template.Type == StatusEffectTemplate.EffectType.HealthOverTime);

                    var applier = effect?.ApplierAgent;

                    if (Campaign.Current != null && applier != null && (applier.IsMainAgent || applier.BelongsToMainParty()))
                    {
                        CareerHelper.ApplyCareerAbilityCharge(healingValue, ChargeType.Healed, AttackTypeMask.Spell, applier);
                    }
                    
                }

                if (_effectAggregate == null) return;

                ModifiedDrivenProperties = _effectAggregate.SpeedProperties != 0 || _effectAggregate.AttackSpeedProperties != 0 || _effectAggregate.ReloadSpeedProperties != 0;

                if (!ModifiedDrivenProperties)
                {
                    if (_restoredBaseValues) return;
                    Agent.UpdateAgentProperties();
                    if (Agent.HasMount)
                    {
                        Agent.MountAgent.UpdateAgentProperties();
                    }

                    _restoredBaseValues = true;

                    return;
                }

                _restoredBaseValues = false;
                Agent.UpdateAgentProperties();

                if (Agent.HasMount)
                {
                    Agent.MountAgent.UpdateAgentProperties();
                }

                if (Math.Abs(_effectAggregate.SpeedProperties - (-1)) < 0.01) //if the movement is impaired completely...
                {
                    if (!Agent.HasMount) return;
                    Agent.MountAgent.SetActionChannel(0, ActionIndexCache.Create("act_horse_stand_1"));
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

            UpdateDummyEntity(dt);
        }

        private void UpdateDummyEntity(float dt)
        {
            _dummyEntity?.SetGlobalFrame(new MatrixFrame(_dummyEntity.GetFrame().rotation, Agent.GetChestGlobalPosition()));
        }

        private void RemoveEffect(StatusEffect effect)
        {
            EffectData data = _currentEffects[effect];

            if (data.IsParticleAttachedToAgentSkeleton)
            {
                foreach (var entity in data.Entities)
                {
                    entity.FadeOut(1, true);
                    entity.RemoveAllParticleSystems();
                    Agent.AgentVisuals.RemoveChildEntity(entity, 0);
                }
            }
            _dummyEntity.RemoveAllParticleSystems();

            if (data.Effect.Template.Type == StatusEffectTemplate.EffectType.MovementManipulation)
            {
                if (Mathf.Abs(GetMovementSpeedModifier() - effect.Template.BaseEffectValue) - 0.00001f <= 0f)
                {
                    this.Agent.UpdateAgentProperties();
                }
            }
            
            _currentEffects.Remove(effect);
            foreach (var currEffect in _currentEffects.Keys)
            {
                if (!_currentEffects[currEffect].IsParticleAttachedToAgentSkeleton)
                {
                    _currentEffects[currEffect].Particles.Clear();
                    MatrixFrame frame = MatrixFrame.Identity;
                    _currentEffects[currEffect].Particles.Add(ParticleSystem.CreateParticleSystemAttachedToEntity(currEffect.Template.ParticleId, _dummyEntity, ref frame));
                }
            }
        }

        public float[] GetAmplifiers(AttackTypeMask mask)
        {
            if (_effectAggregate == null) _effectAggregate = new EffectAggregate();
            return _effectAggregate.DamageAmplifications[mask];
        }

        public float[] GetResistances(AttackTypeMask mask)
        {
            if (_effectAggregate == null) _effectAggregate = new EffectAggregate();
            return _effectAggregate.Resistances[mask];
        }

        public float GetMovementSpeedModifier()
        {
            if (_effectAggregate == null) _effectAggregate = new EffectAggregate();
            return _effectAggregate.SpeedProperties;
        }

        public float GetAttackSpeedModifier()
        {
            if (_effectAggregate == null) _effectAggregate = new EffectAggregate();
            return _effectAggregate.AttackSpeedProperties;
        }
        
        public float GetReloadSpeedModifier()
        {
            if (_effectAggregate == null) _effectAggregate = new EffectAggregate();
            return _effectAggregate.ReloadSpeedProperties;
        }


        public float GetLanceSteadinessModifier()
        {
            if (_effectAggregate == null) _effectAggregate = new EffectAggregate();
            return _effectAggregate.LanceSteadinessChance;
        }

        public float GetBaseValueForDrivenProperty(DrivenProperty property)
        {
            if (_baseValues == null)
            {
                _baseValues = new Dictionary<DrivenProperty, float>();
                SynchronizeBaseValues();
            }

            if (!_baseValues.ContainsKey(property))
            {
                throw new ArgumentException("The property was not defined in the status effect system", property.ToString());
            }

            return _baseValues[property];
        }

        public List<string> GetTemporaryAttributes()
        {
            List<string> list = new List<string>();
            foreach (var effect in _currentEffects.Keys)
            {
                foreach (var attribute in effect.Template.TemporaryAttributes)
                {
                    if (!list.Contains(attribute)) list.Add(attribute);
                }
            }

            return list;
        }

        private void AddEffect(StatusEffect effect)
        {
            EffectData data;
            if (effect.Template.DoNotAttachToAgentSkeleton)
            {
                MatrixFrame frame = MatrixFrame.Identity;
                var psys = ParticleSystem.CreateParticleSystemAttachedToEntity(effect.Template.ParticleId, _dummyEntity, ref frame);
                if (effect.Template.Rotation)
                {
                    _dummyEntity.CreateAndAddScriptComponent("TORSpinner");
                    _dummyEntity.GetFirstScriptOfType<TORSpinner>().RotationSpeed = effect.Template.RotationSpeed;
                }

                data = new EffectData(effect, new List<ParticleSystem> { psys }, null);
                data.IsParticleAttachedToAgentSkeleton = false;
            }
            else
            {
                List<GameEntity> entities;
                List<ParticleSystem> particles = TORParticleSystem.ApplyParticleToAgent(Agent, effect.Template.ParticleId, out entities, effect.Template.ParticleIntensity, effect.Template.ApplyToRootBoneOnly);
                data = new EffectData(effect, particles, entities);
            }

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
            _dummyEntity?.RemoveAllChildren();
            _dummyEntity?.Remove(0);
            _dummyEntity = null;
            _disabled = true;
        }

        public void Dispose()
        {
            CleanUp();
        }

        private class EffectData
        {
            public EffectData(StatusEffect effect, List<ParticleSystem> particles, List<GameEntity> entities)
            {
                Effect = effect;
                Particles = particles;
                Entities = entities;
            }

            public List<ParticleSystem> Particles { get; set; }
            public List<GameEntity> Entities { get; set; }
            public StatusEffect Effect { get; set; }
            public bool IsParticleAttachedToAgentSkeleton { get; set; } = true;
        }

        private class EffectAggregate
        {
            public float WindsOverTime { get; set; } = 0;
            public float HealthOverTime { get; set; } = 0;
            public float DamageOverTime { get; set; } = 0;
            public Dictionary<AttackTypeMask, float[]> DamageAmplifications { get; }
            public Dictionary<AttackTypeMask, float[]> Resistances { get; }
            public float SpeedProperties;
            public float AttackSpeedProperties;
            public float ReloadSpeedProperties;
            public float LanceSteadinessChance;

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
                    case StatusEffectTemplate.EffectType.WindsOverTime:
                        WindsOverTime += strength;
                        break;
                    case StatusEffectTemplate.EffectType.LanceSteadiness:
                        LanceSteadinessChance += strength;
                        break;
                    case StatusEffectTemplate.EffectType.DamageAmplification:
                        AddDamageAmplification(template.DamageType, template.AttackTypeMask, strength);
                        break;
                    case StatusEffectTemplate.EffectType.Resistance:
                        AddResistance(template.DamageType, template.AttackTypeMask, strength);
                        break;
                    case StatusEffectTemplate.EffectType.MovementManipulation:
                        SpeedProperties += strength;
                        break;
                    case StatusEffectTemplate.EffectType.AttackSpeedManipulation:
                        AttackSpeedProperties += strength;
                        break;
                    case StatusEffectTemplate.EffectType.ReloadSpeedManipulation:
                        ReloadSpeedProperties += strength;
                        break;
                    case StatusEffectTemplate.EffectType.TemporaryAttributeOnly:
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

        public void RemoveStatusEffect(string StatusEffectID, bool allEffects=false)
        {
            if (allEffects)
            {
                var targetEffects = _currentEffects.Keys.Where(x => x.Template.StringID.Contains(StatusEffectID)).ToList();
                foreach (var statusEffect in targetEffects)
                {
                    TORCommon.Say("remove "+statusEffect.Template.StringID);
                    RemoveEffect(statusEffect);
                }
            }
            var targetEffect = _currentEffects.Keys.FirstOrDefault(x => x.Template.StringID.Contains(StatusEffectID));
            
            if(targetEffect==null) return;

            RemoveEffect(targetEffect);
        }
    }
}