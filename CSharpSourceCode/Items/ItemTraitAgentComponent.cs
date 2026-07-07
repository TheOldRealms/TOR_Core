using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Engine;
using TaleWorlds.MountAndBlade;
using TOR_Core.Extensions;
using TOR_Core.Utilities;

namespace TOR_Core.Items
{
    public class ItemTraitAgentComponent : AgentComponent
    {
        private readonly List<Tuple<MissionWeapon, ItemTrait, float>> _dynamicTraits = new List<Tuple<MissionWeapon, ItemTrait, float>>();
        private readonly List<Tuple<ItemTrait, float>> _wieldedSlotTraits = new List<Tuple<ItemTrait, float>>();
        private readonly List<TraitParticlePresetState> _currentPresets = new List<TraitParticlePresetState>();

        //weapon swaps can keep the old particle offsets so rebuild it
        private const float WeaponLengthRebuildThreshold = 0.01f;
        private sealed class TraitParticlePresetState
        {
            public WeaponParticlePreset Preset { get; }
            public List<ParticleSystem> Particles { get; }
            public List<GameEntity> ChildEntities { get; }
            public bool UsesWeaponGeometry { get; }
            public bool IsEnabled { get; set; }

            public float CachedWeaponLength { get; }
            public WeaponClass CachedWeaponClass { get; }

            public TraitParticlePresetState(
                WeaponParticlePreset preset,
                List<ParticleSystem> particles,
                List<GameEntity> childEntities,
                float cachedWeaponLength,
                WeaponClass cachedWeaponClass,
                bool usesWeaponGeometry,
                bool isEnabled)
            {
                Preset = preset;
                Particles = particles;
                ChildEntities = childEntities;
                CachedWeaponLength = cachedWeaponLength;
                CachedWeaponClass = cachedWeaponClass;
                UsesWeaponGeometry = usesWeaponGeometry;
                IsEnabled = isEnabled;
            }
        }

        private readonly BasicMissionTimer _missionTimer = new BasicMissionTimer();
        private readonly float _tickInterval = 1f;
        private bool _needsInitialPresetUpdate = true;
        private GameEntity _armorParticleAnchor;

        public ItemTraitAgentComponent(Agent agent) : base(agent) { }

        internal void OnTickAsMainAgent(float dt)
        {
            UpdateLifeTime(dt);
        }

        public override void OnTick(float dt)
        {
            UpdateLifeTime(dt);
        }

        private void UpdateLifeTime(float dt)
        {
            if (_armorParticleAnchor != null && Agent.IsActive() && !Agent.IsFadingOut())
            {
                _armorParticleAnchor.SetGlobalFrame(new MatrixFrame(Mat3.Identity, Agent.GetChestGlobalPosition()));
            }

            if (_needsInitialPresetUpdate)
            {
                _needsInitialPresetUpdate = false;
                UpdatePresets();
            }

            if (_missionTimer.ElapsedTime > _tickInterval)
            {
                _missionTimer.Reset();
                bool removedAnyTrait = false;

                for (int i = _dynamicTraits.Count - 1; i >= 0; i--)
                {
                    var entry = _dynamicTraits[i];
                    float remaining = entry.Item3 - _tickInterval;

                    if (remaining <= 0f)
                    {
                        _dynamicTraits.RemoveAt(i);
                        removedAnyTrait = true;
                        continue;
                    }

                    _dynamicTraits[i] = new Tuple<MissionWeapon, ItemTrait, float>(entry.Item1, entry.Item2, remaining);
                }

                // persist across swaps
                for (int i = _wieldedSlotTraits.Count - 1; i >= 0; i--)
                {
                    var entry = _wieldedSlotTraits[i];
                    float remaining = entry.Item2 - _tickInterval;

                    if (remaining <= 0f)
                    {
                        _wieldedSlotTraits.RemoveAt(i);
                        removedAnyTrait = true;
                        continue;
                    }

                    _wieldedSlotTraits[i] = new Tuple<ItemTrait, float>(entry.Item1, remaining);
                }

                if (removedAnyTrait)
                {
                    UpdatePresets();
                }
    
            }
        }

        public void OnWieldedItemChanged()
        {
            UpdatePresets();
        }

        private static float GetStartOffsetPercentage(WeaponClass weaponClass)
        {
            switch (weaponClass)
            {
                case WeaponClass.OneHandedSword:
                case WeaponClass.TwoHandedSword:
                    return 0.3f;
                case WeaponClass.LowGripPolearm:
                case WeaponClass.OneHandedPolearm:
                case WeaponClass.TwoHandedPolearm:
                    return 0.7f;
                default:
                    return 0.85f;
            }
        }

        private void CreatePresetVisuals(WeaponParticlePreset preset, MissionWeapon weapon, bool usesWeaponGeometry, out List<ParticleSystem> particles, out List<GameEntity> childEntities)
        {
            if (!usesWeaponGeometry)
            {
                particles = new List<ParticleSystem>();
                childEntities = new List<GameEntity>();

                if (!Agent.HasUsableVisuals() || Mission.Current?.Scene == null)
                {
                    return;
                }

                if (_armorParticleAnchor == null)
                {
                    _armorParticleAnchor = GameEntity.CreateEmpty(Mission.Current.Scene, false);
                    _armorParticleAnchor.Name = "_traitParticleAnchor_" + Agent.Index;
                }

                _armorParticleAnchor.SetGlobalFrame(new MatrixFrame(Mat3.Identity, Agent.GetChestGlobalPosition()));

                var effectEntity = GameEntity.CreateEmpty(Mission.Current.Scene, false);
                effectEntity.Name = "_traitParticleEntity_" + Agent.Index + "_" + preset.ParticlePrefab;

                var entityFrame = MatrixFrame.Identity;
                effectEntity.SetFrame(ref entityFrame);
                _armorParticleAnchor.AddChild(effectEntity);

                var particleFrame = MatrixFrame.Identity;
                var particle = ParticleSystem.CreateParticleSystemAttachedToEntity(preset.ParticlePrefab, effectEntity, ref particleFrame);
                if (particle == null)
                {
                    effectEntity.Remove(0);
                    return;
                }

                particle.SetEnable(false);
                particles.Add(particle);
                childEntities.Add(effectEntity);
                return;
            }

            particles = new List<ParticleSystem>();
            childEntities = new List<GameEntity>();

            float weaponLength = weapon.CurrentUsageItem.GetRealWeaponLength();
            WeaponClass weaponClass = weapon.CurrentUsageItem.WeaponClass;

            float startOffsetPercentage = GetStartOffsetPercentage(weaponClass);
            float startOffset = weaponLength * startOffsetPercentage;
            float effectLength = weaponLength - startOffset;

            int particleCount = (int)(effectLength / 0.1f);
            if (particleCount <= 0) particleCount = 1;

            if (preset.IsUniqueSingleCopy)
            {
                var particle = TORParticleSystem.ApplyParticleToAgentBone(Agent, preset.ParticlePrefab, Game.Current.DefaultMonster.MainHandItemBoneIndex, out var entity);
                if (particle != null)
                {
                    particle.SetEnable(false); //enable happens explicitly to avoid any gaps during rebuilds
                    particles.Add(particle);
                    childEntities.Add(entity);
                }
            }
            else
            {
                for (int i = 0; i < particleCount; i++)
                {
                    var particle = TORParticleSystem.ApplyParticleToAgentBone(Agent, preset.ParticlePrefab, Game.Current.DefaultMonster.MainHandItemBoneIndex, out var entity, startOffset + i * 0.1f);
                    if (particle != null)
                    {
                        particle.SetEnable(false);
                        particles.Add(particle);
                        childEntities.Add(entity);
                    }
                }
            }
        }

        private void RemovePresetVisuals(TraitParticlePresetState presetState)
        {
            if (presetState.UsesWeaponGeometry)
            {
                foreach (var particle in presetState.Particles)
                {
                    TORParticleSystem.RemoveParticleFromAgentBone(Agent, particle);
                }
            }
            else
            {
                foreach (var particle in presetState.Particles)
                {
                    particle?.SetRuntimeEmissionRateMultiplier(0f);
                    particle?.SetEnable(false);
                }
            }

            foreach (var entity in presetState.ChildEntities)
            {
                if (entity == null)
                {
                    continue;
                }

                entity.RemoveAllParticleSystems();

                if (presetState.UsesWeaponGeometry)
                {
                    entity.FadeOut(1, true);
                    Agent.TryRemoveChildEntity(entity);
                    continue;
                }

                entity.Remove(0);
            }
        }

        private void SetPresetEnabled(TraitParticlePresetState presetState, bool enable)
        {
            presetState.IsEnabled = enable;
            foreach (var particle in presetState.Particles)
            {
                particle?.SetEnable(enable);
            }
        }
        public override void OnAgentRemoved()
        {
            for (int i = _currentPresets.Count - 1; i >= 0; i--)
            {
                RemovePresetVisuals(_currentPresets[i]);
            }

            _currentPresets.Clear();

            _armorParticleAnchor?.RemoveAllChildren();
            _armorParticleAnchor?.Remove(0);
            _armorParticleAnchor = null;
        }

        public void ApplyParticlePreset(WeaponParticlePreset preset, MissionWeapon weapon, bool usesWeaponGeometry)
        {
            var existing = _currentPresets.FirstOrDefault(x => x.Preset == preset && x.UsesWeaponGeometry == usesWeaponGeometry);
            if (existing != null)
            {
                if (!usesWeaponGeometry)
                {
                    SetPresetEnabled(existing, true);
                    return;
                }

                float weaponLength = weapon.CurrentUsageItem.GetRealWeaponLength();
                WeaponClass weaponClass = weapon.CurrentUsageItem.WeaponClass;

                bool weaponSignatureChanged =
                    existing.CachedWeaponClass != weaponClass ||
                    Math.Abs(existing.CachedWeaponLength - weaponLength) > WeaponLengthRebuildThreshold;

                if (!weaponSignatureChanged)
                {
                    SetPresetEnabled(existing, true);
                    return;
                }

                CreatePresetVisuals(preset, weapon, true, out var newParticles, out var newChildEntities);
                RemovePresetVisuals(existing);

                var rebuiltState = new TraitParticlePresetState(preset, newParticles, newChildEntities, weaponLength, weaponClass, true, true);
                _currentPresets.Remove(existing);
                _currentPresets.Add(rebuiltState);

                SetPresetEnabled(rebuiltState, true);
                return;
            }

            if (usesWeaponGeometry)
            {
                float weaponLength = weapon.CurrentUsageItem.GetRealWeaponLength();
                WeaponClass weaponClass = weapon.CurrentUsageItem.WeaponClass;

                CreatePresetVisuals(preset, weapon, true, out var particles, out var childEntities);
                var presetState = new TraitParticlePresetState(preset, particles, childEntities, weaponLength, weaponClass, true, true);
                _currentPresets.Add(presetState);

                SetPresetEnabled(presetState, true);
                return;
            }

            CreatePresetVisuals(preset, default(MissionWeapon), false, out var armorParticles, out var armorChildEntities);
            var armorPresetState = new TraitParticlePresetState(preset, armorParticles, armorChildEntities, 0f, default(WeaponClass), false, true);
            _currentPresets.Add(armorPresetState);

            SetPresetEnabled(armorPresetState, true);
        }

        public void AddTraitToWeapon(MissionWeapon weapon, ItemTrait trait, float duration)
        {
            if (trait != null && duration > 0)
            {
                if (weapon.CurrentUsageItem != null)
                {
                    if (_dynamicTraits.Any(x => x.Item1.Item == weapon.Item && x.Item2.Equals(trait)))
                    {
                        var match = _dynamicTraits.FirstOrDefault(x => x.Item1.Item == weapon.Item && x.Item2.Equals(trait));
                        if (match != null)
                        {
                            var newTuple = new Tuple<MissionWeapon, ItemTrait, float>(match.Item1, match.Item2, match.Item3 + duration);
                            _dynamicTraits.Remove(match);
                            _dynamicTraits.Add(newTuple);
                        }
                    }
                    else
                    {
                        _dynamicTraits.Add(new Tuple<MissionWeapon, ItemTrait, float>(weapon, trait, duration));
                        UpdatePresets();
                    }
                }
            }
        }
        public void AddTraitToWieldedWeapon(ItemTrait trait, float duration)
        {
            if (trait != null && duration > 0)
            {
                var existing = _wieldedSlotTraits.FirstOrDefault(x => x.Item1.Equals(trait));
                if (existing != null)
                {
                    _wieldedSlotTraits.Remove(existing);
                    _wieldedSlotTraits.Add(new Tuple<ItemTrait, float>(trait, existing.Item2 + duration));
                }
                else
                {
                    _wieldedSlotTraits.Add(new Tuple<ItemTrait, float>(trait, duration));
                    UpdatePresets();
                }
            }

        }


        public void RemoveTraitFromWieldedWeapon(string traitName)
        {
            var slotMatch = _wieldedSlotTraits.FirstOrDefault(x => x.Item1.ItemTraitStringId == traitName);
            if (slotMatch != null)
            {
                _wieldedSlotTraits.Remove(slotMatch);
                UpdatePresets();
                return;
            }
            if (traitName != null)
            {
                var weapon = Agent.WieldedWeapon;
                if (weapon.CurrentUsageItem != null)
                {
                    var match = _dynamicTraits.FirstOrDefault(x => x.Item1.Item == weapon.Item && x.Item2.ItemTraitStringId == traitName);
                    if (match != null)
                    {
                        _dynamicTraits.Remove(match);
                        UpdatePresets();
                    }
                }
            }
        }

        private void UpdatePresets()
        {
            for (int i = 0; i < _currentPresets.Count; i++)
            {
                _currentPresets[i].IsEnabled = false;
            }

            var index = Agent.GetPrimaryWieldedItemIndex();
            if (index != EquipmentIndex.None)
            {
                var weapon = Agent.WieldedWeapon;
                if (weapon.Item != null && weapon.Item.HasAnyTrait(Agent) && !weapon.CurrentUsageItem.IsRangedWeapon)
                {
                    var traits = weapon.Item.GetTraits(Agent);
                    if (traits.Count > 0)
                    {
                        var traitsWithParticles = traits.FindAll(x => x.WeaponParticlePreset != null && x.WeaponParticlePreset.ParticlePrefab != "invalid" && x.WeaponParticlePreset.ParticlePrefab != "none" && !string.IsNullOrEmpty(x.WeaponParticlePreset.ParticlePrefab));
                        foreach (var trait in traitsWithParticles)
                        {
                            ApplyParticlePreset(trait.WeaponParticlePreset, weapon, true);
                        }
                    }
                }
            }

            foreach (var armorItem in Agent.Character.GetCharacterEquipment(EquipmentIndex.ArmorItemBeginSlot, EquipmentIndex.ArmorItemEndSlot))
            {
                if (!armorItem.HasAnyTrait(Agent))
                {
                    continue;
                }

                var traitsWithParticles = armorItem.GetTraits(Agent)
                    .FindAll(x => x.WeaponParticlePreset != null && x.WeaponParticlePreset.ParticlePrefab != "invalid" && x.WeaponParticlePreset.ParticlePrefab != "none" && !string.IsNullOrEmpty(x.WeaponParticlePreset.ParticlePrefab));

                foreach (var trait in traitsWithParticles)
                {
                    ApplyParticlePreset(trait.WeaponParticlePreset, default(MissionWeapon), false);
                }
            }

            RefreshVisuals();
        }

        public void EnableAllParticles(bool enable)
        {
            for (int i = 0; i < _currentPresets.Count; i++)
            {
                _currentPresets[i].IsEnabled = enable;
            }

            RefreshVisuals();
        }

        private void RefreshVisuals()
        {
            foreach (var presetState in _currentPresets)
            {
                bool enable = presetState.IsEnabled;
                foreach (var particle in presetState.Particles)
                {
                    particle?.SetEnable(enable);
                }
            }
        }

        public List<ItemTrait> GetDynamicTraits(ItemObject itemObject)
        {
            List<ItemTrait> list = new List<ItemTrait>();
            foreach (var item in _dynamicTraits)
            {
                if (item.Item1.Item == itemObject) list.Add(item.Item2);
            }
            if (Agent.WieldedWeapon.Item == itemObject && _wieldedSlotTraits.Count > 0)
            {
                foreach (var entry in _wieldedSlotTraits)
                {
                    list.Add(entry.Item1);
                }
            }
            return list;
        }

        public List<string> GetDynamicTraitIds(ItemObject itemObject)
        {
            List<string> list = [];
            foreach (var item in _dynamicTraits)
            {
                if (item.Item1.Item == itemObject) list.Add(item.Item2.ItemTraitStringId);
            }
            return list;
        }
        internal bool HasDynamicTraits(ItemObject itemObject)
        {
            for (int i = 0; i < _dynamicTraits.Count; i++)
            {
                var entry = _dynamicTraits[i];
                if (entry.Item1.Item == itemObject)
                    return true;
            }

            return false;
        }
    }
}