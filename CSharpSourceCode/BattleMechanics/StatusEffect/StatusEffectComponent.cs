using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TOR_Core.AbilitySystem;
using TOR_Core.BattleMechanics.DamageSystem;
using TOR_Core.BattleMechanics.SFX;
using TOR_Core.Extensions;
using TOR_Core.Extensions.ExtendedInfoSystem;
using TOR_Core.Utilities;

namespace TOR_Core.BattleMechanics.StatusEffect
{
    public class StatusEffectComponent : AgentComponent, IDisposable
    {
        private GameEntity _dummyEntity;
        private float _updateFrequency = 1;
        private float _deltaSinceLastTick = MBRandom.RandomFloatRanged(0, 0.1f);
        private Dictionary<StatusEffect, EffectData> _currentEffects;
        private readonly Dictionary<string, StatusParticleVisualPoolEntry> _statusParticleVisualPool = new();
        private const float STATUS_PARTICLE_POOL_DORMANT_SECONDS = 21f; // after this many seconds cache for attached particles will be cleared
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
            _dummyEntity = null;
            _baseValues = new Dictionary<DrivenProperty, float>();
        }

        public void SynchronizeBaseValues(bool mountOnly = false)
        {
            if (Agent == null) return;
            if (Agent.AgentDrivenProperties == null) return;
            if (!mountOnly)
            {
                _baseValues.AddOrReplace(DrivenProperty.MaxSpeedMultiplier, this.Agent.AgentDrivenProperties.MaxSpeedMultiplier);
                _baseValues.AddOrReplace(DrivenProperty.SwingSpeedMultiplier, this.Agent.AgentDrivenProperties.SwingSpeedMultiplier);
                _baseValues.AddOrReplace(DrivenProperty.ThrustOrRangedReadySpeedMultiplier, this.Agent.AgentDrivenProperties.ThrustOrRangedReadySpeedMultiplier);
                _baseValues.AddOrReplace(DrivenProperty.ReloadSpeed, this.Agent.AgentDrivenProperties.ReloadSpeed);
                _baseValues.AddOrReplace(DrivenProperty.BipedalRangedReloadSpeedMultiplier, this.Agent.AgentDrivenProperties.BipedalRangedReloadSpeedMultiplier);
            }

            if (!Agent.HasMount) return;
            _baseValues.AddOrReplace(DrivenProperty.MountManeuver, Agent.MountAgent.AgentDrivenProperties.MountManeuver);
            _baseValues.AddOrReplace(DrivenProperty.MountSpeed, this.Agent.MountAgent.AgentDrivenProperties.MountSpeed);
            _baseValues.AddOrReplace(DrivenProperty.MountDashAccelerationMultiplier, this.Agent.MountAgent.AgentDrivenProperties.MountDashAccelerationMultiplier);
        }

        public void RunStatusEffect(string effectId, Agent applierAgent, float duration, bool append, bool isMutated, bool stack, int castId = -1)
        {
            if (Agent == null) return;
            if (_disabled) return;

            StatusEffect effect = _currentEffects.Keys.Where(e => e.Template.StringID.Contains(effectId)).FirstOrDefault();
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
                // Update castId if a new one is provided (for tracking purposes)
                if (castId >= 0)
                {
                    effect.CastId = castId;
                }
                if (stack)
                {
                    var clone = StatusEffectManager.CreateNewStatusEffect(effectId, applierAgent, true, castId);
                    clone.CurrentDuration = duration;
                    AddEffect(clone);
                }
            }
            else
            {
                effect = StatusEffectManager.CreateNewStatusEffect(effectId, applierAgent, isMutated, castId);
                if (applierAgent != null && applierAgent.IsMainAgent)
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

        public bool HasActiveEffects => _currentEffects.Count > 0;
        public bool NeedsStatusEffectTick => _currentEffects.Count > 0 || _statusParticleVisualPool.Count > 0;

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
                    RemoveEffect(effect, false);
                }
            }

            CalculateEffectAggregate();

            if (Agent != null && Agent.IsActive() && !Agent.IsFadingOut())
            {
                if (_effectAggregate == null) return;

                if (_effectAggregate != null && _effectAggregate.WindsOverTime > 0)
                {
                    if (Agent.IsHero && Agent.IsSpellCaster())
                        Agent.GetHero().AddWindsOfMagic(_effectAggregate.WindsOverTime);
                }


                var abilityLogic = Mission.Current?.GetMissionBehavior<AbilityManagerMissionLogic>();
                if (_effectAggregate.DamageOverTime > 0)
                {
                    var damageValue = (int)_effectAggregate.DamageOverTime;
                    var effect = _currentEffects.Keys.FirstOrDefault(x => x.Template.Type == StatusEffectTemplate.EffectType.DamageOverTime && x.ApplierAgent == Agent.Main) ??
                                  _currentEffects.Keys.FirstOrDefault(x => x.Template.Type == StatusEffectTemplate.EffectType.DamageOverTime);

                    var applier = effect?.ApplierAgent;

                    // Career ability charge is now applied once per session in FinalizeSession, not every tick

                    if (abilityLogic != null)
                    {
                        abilityLogic.QueueStatusDotDamage(
                            Agent,
                            damageValue,
                            Agent.Position,
                            applier,
                            effect?.CastId ?? -1);
                    }
                    else
                    {
                        Agent.ApplyDamage(damageValue, Agent.Position, applier, false, false);
                    }
                }
                else if (_effectAggregate.HealthOverTime > 0)
                {
                    var healingValue = (int)_effectAggregate.HealthOverTime;
                    if (abilityLogic != null)
                    {
                        abilityLogic.QueueStatusHealing(Agent, healingValue);

                        if (Agent.HasMount && Agent.HasHorseLink())
                        {
                            abilityLogic.QueueStatusHealing(Agent.MountAgent, healingValue);
                        }
                    }
                    else
                    {
                        Agent.Heal(healingValue);

                        if (Agent.HasMount && Agent.HasHorseLink())
                        {
                            Agent.MountAgent.Heal(healingValue);
                        }
                    }

                    // Career ability charge is now applied once per session in FinalizeSession, not every tick
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

        // PROPOSED (CS0114, should add `new` or `override`): StatusEffectMissionLogic.cs calls this
        // through the concrete `StatusEffectComponent` type (not polymorphically via
        // `AgentComponent`), so `new` would match current behavior. Before adding `override` instead,
        // worth checking whether the engine also auto-invokes AgentComponent.OnTick on every
        // component each frame -- if so, that would double-tick status effects.
        // Randy - @Sly the parent class has an OnTick registration, I don't know if the DI container registers both events though, the parent IS empty though.
#pragma warning disable CS0114
        public void OnTick(float dt)
#pragma warning restore CS0114
        {
            if (_currentEffects.Count > 0)
            {
                _deltaSinceLastTick += dt;
                if (_deltaSinceLastTick > _updateFrequency)
                {
                    _deltaSinceLastTick = MBRandom.RandomFloatRanged(0, 0.1f);
                    OnElapsed(dt);
                }
            }

            UpdateDummyEntity(dt);

            if (_statusParticleVisualPool.Count == 0)
            {
                return;
            }

            if (Agent == null || !Agent.IsActive() || Agent.IsFadingOut() || !Agent.HasUsableVisuals())
            {
                DestroyDormantStatusParticleVisuals();
                return;
            }

            var currentMissionTime = Mission.Current?.CurrentTime ?? 0f;
            foreach (var pair in _statusParticleVisualPool.ToList())
            {
                var poolEntry = pair.Value;
                if (poolEntry == null ||
                    poolEntry.IsInUse ||
                    poolEntry.LastReleasedMissionTime < 0f ||
                    currentMissionTime - poolEntry.LastReleasedMissionTime < STATUS_PARTICLE_POOL_DORMANT_SECONDS)
                {
                    continue;
                }

                DestroyStatusParticleVisuals(poolEntry);
                _statusParticleVisualPool.Remove(pair.Key);
            }
        }

        private void UpdateDummyEntity(float dt)
        {
            if (_dummyEntity == null || Agent == null || !Agent.IsActive() || Agent.IsFadingOut())
            {
                return;
            }

            _dummyEntity.SetGlobalFrame(new MatrixFrame(_dummyEntity.GetFrame().rotation, Agent.GetChestGlobalPosition()));
        }
        private EffectData CreateAttachedStatusParticleData(StatusEffect effect, string particleId)
        {
            var particles = new List<ParticleSystem>();
            var entities = new List<GameEntity>();
            var particleBoneIndexes = new List<sbyte>();

            bool hasVisibleParticle =
                !string.IsNullOrWhiteSpace(particleId) &&
                !particleId.Equals("none", StringComparison.OrdinalIgnoreCase);

            if (!hasVisibleParticle || Agent == null || !Agent.HasUsableVisuals())
            {
                return new EffectData(effect, particles, entities, particleBoneIndexes, GetAttachedVisualSkeleton());
            }

            var poolEntry = AcquireStatusParticleVisuals(
                particleId,
                effect.Template.ParticleIntensity,
                effect.Template.ApplyToRootBoneOnly);

            if (poolEntry != null)
            {
                return new EffectData(
                    effect,
                    poolEntry.Particles,
                    poolEntry.Entities,
                    poolEntry.ParticleBoneIndexes,
                    poolEntry.AttachedSkeleton,
                    poolEntry);
            }

            return CreateUnpooledStatusParticleData(effect, particleId);
        }

        private StatusParticleVisualPoolEntry AcquireStatusParticleVisuals(string particleId, TORParticleSystem.ParticleIntensity intensity, bool rootOnly)
        {
            if (intensity == TORParticleSystem.ParticleIntensity.Undefined)
            {
                return null;
            }

            var poolKey = GetStatusParticleVisualPoolKey(particleId, intensity, rootOnly);
            if (!_statusParticleVisualPool.TryGetValue(poolKey, out var poolEntry))
            {
                poolEntry = CreateStatusParticleVisuals(particleId, intensity, rootOnly);
                if (poolEntry == null)
                {
                    return null;
                }

                _statusParticleVisualPool.Add(poolKey, poolEntry);
            }
            else
            {
                if (poolEntry.IsInUse)
                {
                    return null;
                }

                var currentMissionTime = Mission.Current?.CurrentTime ?? 0f;
                bool dormantPoolEntryExpired =
                    poolEntry.LastReleasedMissionTime >= 0f &&
                    currentMissionTime - poolEntry.LastReleasedMissionTime >= STATUS_PARTICLE_POOL_DORMANT_SECONDS;

                if (dormantPoolEntryExpired || !IsStatusParticleVisualPoolEntryUsable(poolEntry))
                {
                    DestroyStatusParticleVisuals(poolEntry);
                    poolEntry = CreateStatusParticleVisuals(particleId, intensity, rootOnly);
                    if (poolEntry == null)
                    {
                        _statusParticleVisualPool.Remove(poolKey);
                        return null;
                    }

                    _statusParticleVisualPool[poolKey] = poolEntry;
                }
            }

            poolEntry.IsInUse = true;
            poolEntry.LastReleasedMissionTime = -1f;
            EnableStatusParticleVisuals(poolEntry, true);
            return poolEntry;
        }

        private EffectData CreateUnpooledStatusParticleData(StatusEffect effect, string particleId)
        {
            var particles = TORParticleSystem.ApplyParticleToAgent(
                Agent,
                particleId,
                out var entities,
                out var particleBoneIndexes,
                effect.Template.ParticleIntensity,
                effect.Template.ApplyToRootBoneOnly);

            if (particles.Count == 0)
            {
                foreach (var entity in entities)
                {
                    entity?.RemoveAllParticleSystems();
                    entity?.Remove(0);
                }

                entities.Clear();
                particleBoneIndexes.Clear();
            }

            return new EffectData(effect, particles, entities, particleBoneIndexes, GetAttachedVisualSkeleton());
        }

        private StatusParticleVisualPoolEntry CreateStatusParticleVisuals(
            string particleId,
            TORParticleSystem.ParticleIntensity intensity,
            bool rootOnly)
        {
            var particles = TORParticleSystem.ApplyParticleToAgent(
                Agent,
                particleId,
                out var entities,
                out var particleBoneIndexes,
                intensity,
                rootOnly);

            if (particles.Count == 0)
            {
                foreach (var entity in entities)
                {
                    entity?.RemoveAllParticleSystems();
                    entity?.Remove(0);
                }

                return null;
            }

            return new StatusParticleVisualPoolEntry(
                particleId,
                intensity,
                rootOnly,
                particles,
                entities,
                particleBoneIndexes,
                GetAttachedVisualSkeleton());
        }

        private static string GetStatusParticleVisualPoolKey(
            string particleId,
            TORParticleSystem.ParticleIntensity intensity,
            bool rootOnly)
        {
            return particleId.Trim().ToLowerInvariant() + "|" + intensity + "|root=" + rootOnly;
        }

        private static void EnableStatusParticleVisuals(StatusParticleVisualPoolEntry poolEntry, bool enable)
        {
            foreach (var particle in poolEntry.Particles)
            {
                if (particle == null)
                {
                    continue;
                }

                particle.SetRuntimeEmissionRateMultiplier(enable ? 1f : 0f);
                particle.SetEnable(enable);

                if (enable)
                {
                    particle.Restart();
                }
            }
        }

        private static bool IsStatusParticleVisualPoolEntryUsable(StatusParticleVisualPoolEntry poolEntry)
        {
            if (poolEntry == null ||
                poolEntry.AttachedSkeleton == null ||
                !poolEntry.AttachedSkeleton.IsValid ||
                poolEntry.Particles == null ||
                poolEntry.ParticleBoneIndexes == null ||
                poolEntry.Particles.Count == 0 ||
                poolEntry.Particles.Count != poolEntry.ParticleBoneIndexes.Count)
            {
                return false;
            }

            for (int i = 0; i < poolEntry.Particles.Count; i++)
            {
                var particle = poolEntry.Particles[i];
                if (particle == null ||
                    !poolEntry.AttachedSkeleton.HasBoneComponent(poolEntry.ParticleBoneIndexes[i], particle))
                {
                    return false;
                }
            }

            int pairedVisualCount = Math.Min(poolEntry.Particles.Count, poolEntry.Entities?.Count ?? 0);
            for (int i = 0; i < pairedVisualCount; i++)
            {
                var particle = poolEntry.Particles[i];
                var entity = poolEntry.Entities[i];
                if (particle != null && entity != null && !entity.HasComponent(particle))
                {
                    return false;
                }
            }

            return true;
        }

        private void ReleaseStatusParticleVisuals(StatusParticleVisualPoolEntry poolEntry)
        {
            if (poolEntry == null)
            {
                return;
            }

            EnableStatusParticleVisuals(poolEntry, false);
            poolEntry.IsInUse = false;
            poolEntry.LastReleasedMissionTime = Mission.Current?.CurrentTime ?? 0f;
        }

        private void DestroyDormantStatusParticleVisuals()
        {
            foreach (var poolEntry in _statusParticleVisualPool.Values)
            {
                DestroyStatusParticleVisuals(poolEntry);
            }

            _statusParticleVisualPool.Clear();
        }

        private void DestroyStatusParticleVisuals(StatusParticleVisualPoolEntry poolEntry)
        {
            if (poolEntry == null)
            {
                return;
            }

            var skeleton = poolEntry.AttachedSkeleton;
            if (skeleton == null || !skeleton.IsValid)
            {
                skeleton = GetAttachedVisualSkeleton();
            }

            for (int i = 0; i < poolEntry.Particles.Count; i++)
            {
                var particle = poolEntry.Particles[i];
                if (particle == null)
                {
                    continue;
                }

                particle.SetRuntimeEmissionRateMultiplier(0f);
                particle.SetEnable(false);

                bool removedFromRegisteredBone = false;
                if (skeleton != null &&
                    skeleton.IsValid &&
                    i < poolEntry.ParticleBoneIndexes.Count)
                {
                    var boneIndex = poolEntry.ParticleBoneIndexes[i];
                    if (skeleton.HasBoneComponent(boneIndex, particle))
                    {
                        skeleton.RemoveBoneComponent(boneIndex, particle);
                        removedFromRegisteredBone = true;
                    }
                }

                if (!removedFromRegisteredBone)
                {
                    TORParticleSystem.RemoveParticleFromAgentBone(Agent, particle);
                }
            }

            int pairedVisualCount = Math.Min(poolEntry.Particles.Count, poolEntry.Entities.Count);
            for (int i = 0; i < pairedVisualCount; i++)
            {
                var particle = poolEntry.Particles[i];
                var entity = poolEntry.Entities[i];

                if (particle != null && entity != null && entity.HasComponent(particle))
                {
                    entity.RemoveComponent(particle);
                }
            }

            bool canDetachFromAgentVisuals = Agent != null && Agent.HasUsableVisuals();
            foreach (var entity in poolEntry.Entities)
            {
                if (entity == null)
                {
                    continue;
                }

                entity.RemoveAllParticleSystems();

                if (canDetachFromAgentVisuals && Agent.TryRemoveChildEntity(entity))
                {
                    continue;
                }

                entity.Remove(0);
            }

            poolEntry.Particles.Clear();
            poolEntry.Entities.Clear();
            poolEntry.ParticleBoneIndexes.Clear();
            poolEntry.IsInUse = false;
        }

        private void RemoveVisuals(EffectData data)
        {
            if (data == null)
            {
                return;
            }

            if (data.PoolEntry != null)
            {
                ReleaseStatusParticleVisuals(data.PoolEntry);
                return;
            }

            if (data.IsParticleAttachedToAgentSkeleton)
            {
                RemoveAttachedParticlesFromAgentSkeleton(data);
                RemoveAttachedParticlesFromCarrierEntities(data);
            }

            if (data.Entities == null)
            {
                return;
            }

            bool canDetachFromAgentVisuals = Agent != null && Agent.HasUsableVisuals();

            foreach (var entity in data.Entities)
            {
                if (entity == null)
                {
                    continue;
                }

                entity.RemoveAllParticleSystems();

                if (data.IsParticleAttachedToAgentSkeleton &&
                    canDetachFromAgentVisuals &&
                    Agent.TryRemoveChildEntity(entity))
                {
                    continue;
                }

                entity.Remove(0);
            }
        }

        private void RemoveAttachedParticlesFromAgentSkeleton(EffectData data)
        {
            if (data?.Particles == null ||
                !data.IsParticleAttachedToAgentSkeleton)
            {
                return;
            }

            var skeleton = data.AttachedSkeleton;
            if (skeleton == null || !skeleton.IsValid)
            {
                skeleton = GetAttachedVisualSkeleton();
            }

            for (int i = 0; i < data.Particles.Count; i++)
            {
                var particle = data.Particles[i];
                if (particle == null)
                {
                    continue;
                }

                particle.SetRuntimeEmissionRateMultiplier(0f);
                particle.SetEnable(false);

                bool removedFromRegisteredBone = false;
                if (skeleton != null &&
                    skeleton.IsValid &&
                    data.ParticleBoneIndexes != null &&
                    i < data.ParticleBoneIndexes.Count)
                {
                    var boneIndex = data.ParticleBoneIndexes[i];
                    if (skeleton.HasBoneComponent(boneIndex, particle))
                    {
                        skeleton.RemoveBoneComponent(boneIndex, particle);
                        removedFromRegisteredBone = true;
                    }
                }

                if (!removedFromRegisteredBone)
                {
                    TORParticleSystem.RemoveParticleFromAgentBone(Agent, particle);
                }
            }
        }
        private void RemoveAttachedParticlesFromCarrierEntities(EffectData data)
        {
            if (data?.Particles == null ||
                data.Entities == null ||
                !data.IsParticleAttachedToAgentSkeleton)
            {
                return;
            }

            int pairedVisualCount = Math.Min(data.Particles.Count, data.Entities.Count);

            for (int i = 0; i < pairedVisualCount; i++)
            {
                var particle = data.Particles[i];
                var carrierEntity = data.Entities[i];

                if (particle == null)
                {
                    continue;
                }

                particle.SetRuntimeEmissionRateMultiplier(0f);
                particle.SetEnable(false);

                if (carrierEntity != null && carrierEntity.HasComponent(particle))
                {
                    carrierEntity.RemoveComponent(particle);
                }
            }
        }

        private Skeleton GetAttachedVisualSkeleton()
        {
            if (Agent == null || !Agent.HasUsableVisuals())
            {
                return null;
            }

            var skeleton = Agent.AgentVisuals.GetSkeleton();
            return skeleton != null && skeleton.IsValid ? skeleton : null;
        }
      
        private void RemoveEffect(StatusEffect effect, bool refreshStatusState = true)
        {
            if (!_currentEffects.TryGetValue(effect, out var data))
            {
                return;
            }
            RemoveVisuals(data);
            _currentEffects.Remove(effect);

            if (!data.IsParticleAttachedToAgentSkeleton)
            {
                RemoveDetachedDummyIfEmpty();
            }

            if (refreshStatusState)
            {
                RefreshStatusStateAfterRemoval();
            }
        }

        private void RemoveDetachedDummyIfEmpty()
        {
            if (_dummyEntity == null)
            {
                return;
            }
            foreach (var effectData in _currentEffects.Values)
            {
                if (!effectData.IsParticleAttachedToAgentSkeleton &&
                    effectData.Entities != null &&
                    effectData.Entities.Count > 0)
                {
                    return;
                }
            }

            _dummyEntity.RemoveAllChildren();
            _dummyEntity.Remove(0);
            _dummyEntity = null;
        }
        private void RefreshStatusStateAfterRemoval()
        {
            CalculateEffectAggregate();

            ModifiedDrivenProperties =
                _effectAggregate.SpeedProperties != 0 ||
                _effectAggregate.AttackSpeedProperties != 0 ||
                _effectAggregate.ReloadSpeedProperties != 0;

            if (Agent == null || !Agent.IsActive() || Agent.IsFadingOut())
            {
                return;
            }

            if (ModifiedDrivenProperties)
            {
                _restoredBaseValues = false;
                Agent.UpdateAgentProperties();

                if (Agent.HasMount)
                {
                    Agent.MountAgent.UpdateAgentProperties();
                }

                return;
            }

            if (_restoredBaseValues)
            {
                return;
            }

            Agent.UpdateAgentProperties();

            if (Agent.HasMount)
            {
                Agent.MountAgent.UpdateAgentProperties();
            }

            _restoredBaseValues = true;
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

        public float GetDamageOverTimeAggregate()
        {
            if (_effectAggregate == null) _effectAggregate = new EffectAggregate();
            return _effectAggregate.DamageOverTime;
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

        public List<string> GetTemporaryAttributes(bool retrieveDuplicates = false)
        {
            List<string> list = new List<string>();
            foreach (var effect in _currentEffects.Keys)
            {
                foreach (var attribute in effect.Template.TemporaryAttributes)
                {
                    if (retrieveDuplicates || (!list.Contains(attribute)))
                    {
                        list.Add(attribute);
                    }
                }
            }

            return list;
        }

        public int GetActiveEffectCount(string effectId)
        {
            return _currentEffects.Keys.Count(effect =>
                effect.Template.StringID == effectId ||
                effect.Template.StringID.StartsWith(effectId + "*cloned*", StringComparison.Ordinal));
        }

        private void AddEffect(StatusEffect effect)
        {
            bool wasDormant = _currentEffects.Count == 0;
            EffectData data;
            var particleId = effect.Template.ParticleId?.Trim();
            bool hasVisibleParticle =
                !string.IsNullOrWhiteSpace(particleId) &&
                !particleId.Equals("none", StringComparison.OrdinalIgnoreCase);

            if (effect.Template.DoNotAttachToAgentSkeleton)
            {
                var particles = new List<ParticleSystem>();
                var entities = new List<GameEntity>();

                if (hasVisibleParticle)
                {
                    if (_dummyEntity == null)
                    {
                        _dummyEntity = GameEntity.CreateEmpty(Mission.Current.Scene, false);
                        _dummyEntity.Name = "_dummyEntity_" + Agent.Index;
                        _dummyEntity.SetGlobalFrame(new MatrixFrame(Mat3.Identity, Agent.GetChestGlobalPosition()));
                    }

                    var effectEntity = GameEntity.CreateEmpty(Mission.Current.Scene, false);
                    effectEntity.Name = "_statusEffectEntity_" + Agent.Index + "_" + effect.Template.StringID;

                    MatrixFrame entityFrame = MatrixFrame.Identity;
                    effectEntity.SetFrame(ref entityFrame);
                    _dummyEntity.AddChild(effectEntity);

                    MatrixFrame particleFrame = MatrixFrame.Identity;
                    var particle = ParticleSystem.CreateParticleSystemAttachedToEntity(particleId, effectEntity, ref particleFrame);

                    if (particle != null)
                    {
                        particles.Add(particle);
                    }
                    else
                    {
                        effectEntity.Remove(0);
                        effectEntity = null;
                    }

                    if (effectEntity != null)
                    {
                        if (effect.Template.Rotation)
                        {
                            effectEntity.CreateAndAddScriptComponent("TORSpinner", true);
                            effectEntity.GetFirstScriptOfType<TORSpinner>().RotationSpeed = effect.Template.RotationSpeed;
                        }

                        entities.Add(effectEntity);
                    }
                }

                data = new EffectData(effect, particles, entities);
                data.IsParticleAttachedToAgentSkeleton = false;
            }
            else
            {
                data = CreateAttachedStatusParticleData(effect, particleId);
                data.IsParticleAttachedToAgentSkeleton = true;
            }

            _currentEffects.Add(effect, data);

            if (wasDormant)
            {
                //avoid sync newer over time effects on the same delayed tick
                _deltaSinceLastTick = MBRandom.RandomFloatRanged(0, _updateFrequency);
            }
        }

        private void CleanUp()
        {
            foreach (var item in _currentEffects.ToList())
            {
                RemoveVisuals(item.Value);
            }

            _currentEffects.Clear();
            DestroyDormantStatusParticleVisuals();

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

        private sealed class StatusParticleVisualPoolEntry
        {
            public StatusParticleVisualPoolEntry(
                string particleId,
                TORParticleSystem.ParticleIntensity intensity,
                bool rootOnly,
                List<ParticleSystem> particles,
                List<GameEntity> entities,
                List<sbyte> particleBoneIndexes,
                Skeleton attachedSkeleton)
            {
                ParticleId = particleId;
                Intensity = intensity;
                RootOnly = rootOnly;
                Particles = particles ?? new List<ParticleSystem>();
                Entities = entities ?? new List<GameEntity>();
                ParticleBoneIndexes = particleBoneIndexes ?? new List<sbyte>();
                AttachedSkeleton = attachedSkeleton;
            }

            public string ParticleId { get; }
            public TORParticleSystem.ParticleIntensity Intensity { get; }
            public bool RootOnly { get; }
            public List<ParticleSystem> Particles { get; }
            public List<GameEntity> Entities { get; }
            public List<sbyte> ParticleBoneIndexes { get; }
            public Skeleton AttachedSkeleton { get; }
            public bool IsInUse { get; set; }
            public float LastReleasedMissionTime { get; set; } = -1f;
        }

        private class EffectData
        {
            public EffectData(
                StatusEffect effect,
                List<ParticleSystem> particles,
                List<GameEntity> entities,
                List<sbyte> particleBoneIndexes = null,
                Skeleton attachedSkeleton = null,
                StatusParticleVisualPoolEntry poolEntry = null)
            {
                Effect = effect;
                Particles = particles;
                Entities = entities;
                ParticleBoneIndexes = particleBoneIndexes ?? new List<sbyte>();
                AttachedSkeleton = attachedSkeleton;
                PoolEntry = poolEntry;
            }

            public List<ParticleSystem> Particles { get; set; }
            public List<GameEntity> Entities { get; set; }
            public List<sbyte> ParticleBoneIndexes { get; set; }
            public Skeleton AttachedSkeleton { get; set; }
            public StatusEffect Effect { get; set; }
            public StatusParticleVisualPoolEntry PoolEntry { get; set; }
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

        public void RemoveStatusEffect(string StatusEffectID, EffectFlag flag = EffectFlag.Single)
        {
            if (flag == EffectFlag.Single)
            {
                var targetEffect = _currentEffects.Keys.FirstOrDefault(x => x.Template.StringID.Contains(StatusEffectID));

                if (targetEffect == null)
                {
                    return;
                }

                RemoveEffect(targetEffect);
                return;
            }

            var targetEffects = _currentEffects.Keys
                .Where(x => x.Template.StringID.Contains(StatusEffectID))
                .ToList();

            bool removedAnyStatusEffect = false;

            foreach (var statusEffect in targetEffects)
            {
                if (flag == EffectFlag.Enemy && !IsEnemyStatusEffect(statusEffect))
                {
                    continue;
                }

                RemoveEffect(statusEffect, false);
                removedAnyStatusEffect = true;
            }

            if (removedAnyStatusEffect)
            {
                RefreshStatusStateAfterRemoval();
            }
        }

        private static bool IsEnemyStatusEffect(StatusEffect statusEffect)
        {
            var type = statusEffect.Template.Type;

            return type == StatusEffectTemplate.EffectType.DamageOverTime ||
                   statusEffect.Template.BaseEffectValue < 0 &&
                   (type == StatusEffectTemplate.EffectType.DamageAmplification ||
                    type == StatusEffectTemplate.EffectType.Resistance ||
                    type == StatusEffectTemplate.EffectType.ReloadSpeedManipulation ||
                    type == StatusEffectTemplate.EffectType.AttackSpeedManipulation ||
                    type == StatusEffectTemplate.EffectType.MovementManipulation);
        }

        public enum EffectFlag
        {
            Single,
            All,
            Enemy
        }
    }
}