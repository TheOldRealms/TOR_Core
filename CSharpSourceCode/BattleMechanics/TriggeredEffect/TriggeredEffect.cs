﻿using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.Engine;
using System;
using TOR_Core.BattleMechanics.TriggeredEffect.Scripts;
using System.Collections.Generic;
using TOR_Core.AbilitySystem;
using TOR_Core.Utilities;
using TOR_Core.Extensions;
using TaleWorlds.Core;
using TaleWorlds.CampaignSystem;
using Timer = System.Timers.Timer;
using TOR_Core.CharacterDevelopment;
using System.Linq;

namespace TOR_Core.BattleMechanics.TriggeredEffect
{
    public class TriggeredEffect : IDisposable
    {
        private TriggeredEffectTemplate _template;
        private int _soundIndex;
        private SoundEvent _sound;
        private Timer _timer;
        private object _sync = new object();

        public TriggeredEffect(TriggeredEffectTemplate template)
        {
            _template = template;
        }
        
        public void Trigger(Vec3 position, Vec3 normal, Agent triggererAgent, AbilityTemplate originAbilityTemplate = null, MBList<Agent> targets = null)
        {
            if (_template == null) return;
            _timer = new Timer(2000);
            _timer.AutoReset = false;
            _timer.Enabled = false;
            _timer.Elapsed += (s, e) =>
            {
                lock (_sync)
                {
                    Dispose();
                }
            };
            if (_template.SoundEffectLength > 0)
            {
                _timer.Interval = _template.SoundEffectLength * 1000;
            }
            _timer.Start();

            float damageMultiplier = 1f;
            float durationMultiplier = 1f;
            if(Game.Current.GameType is Campaign && originAbilityTemplate != null)
            {
                var model = Campaign.Current.Models.GetSpellcraftModel();
                var character = triggererAgent.Character as CharacterObject;
                if(model != null && character != null)
                {
                    damageMultiplier = model.GetSkillEffectivenessForAbilityDamage(character, originAbilityTemplate);
                    durationMultiplier = model.GetSkillEffectivenessForAbilityDuration(character, originAbilityTemplate);
                    if(character.IsHero) durationMultiplier *= model.GetPerkEffectsOnAbilityDuration(character, originAbilityTemplate);
                }
            }
            //Cause Damage
            if (targets == null && triggererAgent != null)
            {
                targets = new MBList<Agent>();
                if (_template.TargetType == TargetType.Enemy)
                {
                    targets = Mission.Current.GetNearbyEnemyAgents(position.AsVec2, _template.Radius, triggererAgent.Team, targets);
                }
                else if (_template.TargetType == TargetType.Friendly)
                {
                    targets = Mission.Current.GetNearbyAllyAgents(position.AsVec2, _template.Radius, triggererAgent.Team, targets);
                }
                else if (_template.TargetType == TargetType.All)
                {
                    targets = Mission.Current.GetNearbyAgents(position.AsVec2, _template.Radius, targets);
                }
            }
            if (_template.DamageAmount > 0)
            {
                TORMissionHelper.DamageAgents(targets, (int)(_template.DamageAmount * (1 - _template.DamageVariance) * damageMultiplier), (int)(_template.DamageAmount * (1 + _template.DamageVariance)), triggererAgent, _template.TargetType, _template, _template.DamageType, _template.HasShockWave, position, originAbilityTemplate);
            }
            else if (_template.DamageAmount < 0)
            {
                TORMissionHelper.HealAgents(targets, (int)(-_template.DamageAmount * (1 - _template.DamageVariance) * damageMultiplier), (int)(-_template.DamageAmount * (1 + _template.DamageVariance)), triggererAgent, _template.TargetType, originAbilityTemplate);
            }
            //Apply status effects
            if (_template.ImbuedStatusEffectID != "none" && _template.AssociatedStatusEffect != null)
            {
                var triggererCharacter = triggererAgent.Character as CharacterObject;
                if(triggererCharacter != null && triggererCharacter.GetPerkValue(TORPerks.SpellCraft.ArcaneLink) && _template.AssociatedStatusEffect.IsBuffEffect)
                {
                    if(!targets.Contains(triggererAgent)) targets.Append(triggererAgent);
                }
                TORMissionHelper.ApplyStatusEffectToAgents(targets, _template.ImbuedStatusEffectID, triggererAgent, durationMultiplier, _template.ImbuedStatusEffectDuration);
            }
            SpawnVisuals(position, normal);
            PlaySound(position);
            TriggerScript(position, triggererAgent, targets);
        }

        private void SpawnVisuals(Vec3 position, Vec3 normal)
        {
            //play visuals
            if (_template.BurstParticleEffectPrefab != "none")
            {
                var effect = GameEntity.CreateEmpty(Mission.Current.Scene);
                MatrixFrame frame = MatrixFrame.Identity;
                ParticleSystem.CreateParticleSystemAttachedToEntity(_template.BurstParticleEffectPrefab, effect, ref frame);
                var globalFrame = new MatrixFrame(Mat3.CreateMat3WithForward(in normal), position);
                effect.SetGlobalFrame(globalFrame);
                effect.FadeOut(_template.SoundEffectLength, true);
            }
        }

        private void PlaySound(Vec3 position)
        {
            //play sound
            if (_template.SoundEffectId != "none")
            {
                _soundIndex = SoundEvent.GetEventIdFromString(_template.SoundEffectId);
                _sound = SoundEvent.CreateEvent(_soundIndex, Mission.Current.Scene);
                if (_sound != null)
                {
                    _sound.PlayInPosition(position);
                }
            }
        }

        private void TriggerScript(Vec3 position, Agent triggerer, IEnumerable<Agent> triggeredAgents)
        {
            if (_template.ScriptNameToTrigger != "none")
            {
                try
                {
                    var obj = Activator.CreateInstance(Type.GetType(_template.ScriptNameToTrigger));
                    if(obj is PrefabSpawnerScript)
                    {
                        var script = obj as PrefabSpawnerScript;
                        script.OnInit(_template.SpawnPrefabName);
                    }
                    else if(obj is SummonScript && _template.TroopIdToSummon != "none")
                    {
                        var script = obj as SummonScript;
                        script.OnInit(_template.TroopIdToSummon, _template.NumberToSummon);
                    }
                    if (obj is ITriggeredScript)
                    {
                        var script = obj as ITriggeredScript;
                        script.OnTrigger(position, triggerer, triggeredAgents);
                    }
                }
                catch (Exception)
                {
                    TORCommon.Log("Tried to spawn TriggeredScript: " + _template.ScriptNameToTrigger + ", but failed.", NLog.LogLevel.Error);
                }
            }
        }

        public void Dispose()
        {
            CleanUp();
        }

        private void CleanUp()
        {
            if (_sound != null) _sound.Release();
            _sound = null;
            _soundIndex = -1;
            _template = null;
            _timer.Stop();
        }
    }
}
