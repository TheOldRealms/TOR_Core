using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.MountAndBlade;
using TOR_Core.Extensions;
using TOR_Core.Utilities;

namespace TOR_Core.Items
{
    public class ItemTraitAgentComponent : AgentComponent
    {
        private readonly List<Tuple<MissionWeapon, ItemTrait, float>> _dynamicTraits = new List<Tuple<MissionWeapon, ItemTrait, float>>();
        private readonly List<Tuple<WeaponParticlePreset, List<ParticleSystem>, bool>> _currentPresets = new List<Tuple<WeaponParticlePreset, List<ParticleSystem>, bool>>();
        private readonly BasicMissionTimer _missionTimer = new BasicMissionTimer();
        private readonly float _tickInterval = 1f;

        public ItemTraitAgentComponent(Agent agent) : base(agent) { }

        internal void OnTickAsMainAgent(float dt)
        {
            UpdateLifeTime(dt);
        }

        public override void OnTickAsAI(float dt)
        {
            base.OnTickAsAI(dt);
            UpdateLifeTime(dt);
        }

        private void UpdateLifeTime(float dt)
        {
            if (_missionTimer.ElapsedTime > _tickInterval)
            {
                _missionTimer.Reset();
                if (_dynamicTraits.Count > 0)
                {
                    for (int i = 0; i < _dynamicTraits.Count; i++)
                    {
                        var itemTrait = _dynamicTraits[i];
                        _dynamicTraits[i] = new Tuple<MissionWeapon, ItemTrait, float>(itemTrait.Item1, itemTrait.Item2, itemTrait.Item3 - _tickInterval);
                        if (itemTrait.Item3 < 0)
                        {
                            _dynamicTraits.RemoveAt(i);
                            UpdatePresets();
                        }
                    }
                }
            }
        }

        public void OnWieldedItemChanged()
        {
            UpdatePresets();
        }

        public void ApplyParticlePreset(WeaponParticlePreset preset, MissionWeapon weapon)
        {
            if (_currentPresets.Any(x => x.Item1 == preset))
            {
                for(int i = 0; i < _currentPresets.Count; i++)
                {
                    var tuple = _currentPresets[i];
                    if(tuple.Item1 == preset && tuple.Item2 != null && tuple.Item2.Count > 0 && tuple.Item3 == false)
                    {
                        _currentPresets[i] = new Tuple<WeaponParticlePreset, List<ParticleSystem>, bool>(tuple.Item1, tuple.Item2, true);
                    }
                }
            }
            else
            {
                var length = weapon.CurrentUsageItem.GetRealWeaponLength();
                float startOffsetPrc = 0;
                switch (weapon.CurrentUsageItem.WeaponClass)
                {
                    case WeaponClass.OneHandedSword:
                    case WeaponClass.TwoHandedSword:
                        startOffsetPrc = 0.3f;
                        break;
                    case WeaponClass.LowGripPolearm:
                    case WeaponClass.OneHandedPolearm:
                    case WeaponClass.TwoHandedPolearm:
                        startOffsetPrc = 0.7f;
                        break;
                    default:
                        startOffsetPrc = 0.85f;
                        break;
                }
                float startOffset = length * startOffsetPrc;
                float effectlength = length - startOffset;
                int num = (int)(effectlength / 0.1f);
                if (num <= 0) num = 1;
                GameEntity entity;
                List<ParticleSystem> particles = new List<ParticleSystem>();
                if (preset.IsUniqueSingleCopy)
                {
                    var particle = TORParticleSystem.ApplyParticleToAgentBone(Agent, preset.ParticlePrefab, Game.Current.DefaultMonster.MainHandItemBoneIndex, out entity);
                    particles.Add(particle);
                }
                else
                {
                    for (int j = 0; j < num; j++)
                    {
                        var particle = TORParticleSystem.ApplyParticleToAgentBone(Agent, preset.ParticlePrefab, Game.Current.DefaultMonster.MainHandItemBoneIndex, out entity, startOffset + j * 0.1f);
                        particles.Add(particle);
                    }
                }
                _currentPresets.Add(new Tuple<WeaponParticlePreset, List<ParticleSystem>, bool>(preset, particles, true));
            }
        }

        public void AddTraitToWeapon(MissionWeapon weapon, ItemTrait trait, float duration)
        {
            if (trait != null && duration > 0)
            {
                if(weapon.CurrentUsageItem != null)
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
                var weapon = Agent.WieldedWeapon;
                if(weapon.CurrentUsageItem != null)
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

        private void UpdatePresets()
        {
            var index = Agent.GetWieldedItemIndex(Agent.HandIndex.MainHand);
            if (index == EquipmentIndex.None) return;
            for(int i = 0; i < _currentPresets.Count; i++)
            {
                var item = _currentPresets[i];
                _currentPresets[i] = new Tuple<WeaponParticlePreset, List<ParticleSystem>, bool>(item.Item1, item.Item2, false);
            }
            var weapon = Agent.WieldedWeapon;
            if (weapon.Item != null && weapon.Item.HasTrait(Agent) && !weapon.CurrentUsageItem.IsRangedWeapon)
            {
                var info = weapon.Item.GetTorSpecificData(Agent);
                if (info != null)
                {
                    var traitsWithParticles = info.ItemTraits.FindAll(x => x.WeaponParticlePreset != null && x.WeaponParticlePreset.ParticlePrefab != "invalid" && x.WeaponParticlePreset.ParticlePrefab != "none" && !string.IsNullOrEmpty(x.WeaponParticlePreset.ParticlePrefab));
                    foreach (var trait in traitsWithParticles)
                    {
                        ApplyParticlePreset(trait.WeaponParticlePreset, weapon);
                    }
                }
            }
            RefreshVisuals();
        }

        public void EnableAllParticles(bool enable)
        {
            if (enable)
            {
                for (int i = 0; i < _currentPresets.Count; i++)
                {
                    var item = _currentPresets[i];
                    if(_currentPresets[i].Item3 == false) _currentPresets[i] = new Tuple<WeaponParticlePreset, List<ParticleSystem>, bool>(item.Item1, item.Item2, true);
                }
            }
            else
            {
                for (int i = 0; i < _currentPresets.Count; i++)
                {
                    var item = _currentPresets[i];
                    if (_currentPresets[i].Item3 == true) _currentPresets[i] = new Tuple<WeaponParticlePreset, List<ParticleSystem>, bool>(item.Item1, item.Item2, false);
                }
            }
            RefreshVisuals();
        }

        private void RefreshVisuals()
        {
            foreach(var item in _currentPresets.Where(x => x.Item3 == true))
            {
                if(item.Item2.Count > 0)
                {
                    foreach(var ps in item.Item2)
                    {
                        if (ps != null)
                        {
                            ps.SetEnable(true);
                        }
                
                    }
                }
            }
            foreach (var item in _currentPresets.Where(x => x.Item3 == false))
            {
                if (item.Item2.Count > 0)
                {
                    foreach (var ps in item.Item2)
                    {
                        ps.SetEnable(false);
                    }
                }
            }
        }

        public List<ItemTrait> GetDynamicTraits(ItemObject itemObject)
        {
            List<ItemTrait> list = new List<ItemTrait>();
            foreach (var item in _dynamicTraits)
            {
                if(item.Item1.Item == itemObject) list.Add(item.Item2);
            }
            return list;
        }
    }
}
