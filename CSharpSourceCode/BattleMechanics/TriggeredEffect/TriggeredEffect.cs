using TaleWorlds.Library;
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
    public class TriggeredEffect(TriggeredEffectTemplate template, bool isTemplateMutated = false) : IDisposable
    {
        private TriggeredEffectTemplate _template = template;
        private int _soundIndex;
        private SoundEvent _sound;
        private Timer _timer;
        private readonly object _sync = new();
        private readonly bool _isTemplateMutated = isTemplateMutated;

        public float EffectRadius => _template.Radius;
        public string SummonedTroopId => _template.TroopIdToSummon;
        public float ImbuedStatusEffectDuration => _template.ImbuedStatusEffectDuration;
        public List<string> StatusEffects => _template.ImbuedStatusEffects;
        public void Trigger(Vec3 position, Vec3 normal, Agent triggererAgent, AbilityTemplate originAbilityTemplate = null, MBList<Agent> targets = null)
        {
            if (_template == null || !triggererAgent.IsActive()) return;
            _timer = new Timer(2000)
            {
                AutoReset = false,
                Enabled = false
            };
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
            float statusEffectDuration = _template.ImbuedStatusEffectDuration;
            float radius = _template.Radius;
            if(Game.Current.GameType is Campaign && originAbilityTemplate != null)
            {
                var model = Campaign.Current.Models.GetAbilityModel();
                if (model != null && triggererAgent.Character is CharacterObject character)
                {
                    damageMultiplier = model.GetSkillEffectivenessForAbilityDamage(character, originAbilityTemplate);
                    statusEffectDuration = model.CalculateStatusEffectDurationForAbility(character, originAbilityTemplate, statusEffectDuration);
                    radius = model.CalculateRadiusForAbility(character, originAbilityTemplate, radius);
                }
            }
            //Determine targets
            if (targets == null && triggererAgent != null)
            {
                targets = [];
                if(_template.TargetType == TargetType.Self)
                {
                    targets.Add(triggererAgent);
                }
                else if (_template.TargetType == TargetType.Enemy)
                {
                    targets = Mission.Current.GetNearbyEnemyAgents(position.AsVec2, radius, triggererAgent.Team, targets);
                }
                else if (_template.TargetType == TargetType.Friendly)
                {
                    targets = Mission.Current.GetNearbyAllyAgents(position.AsVec2, radius, triggererAgent.Team, targets);
                }
                else if (_template.TargetType == TargetType.All)
                {
                    targets = Mission.Current.GetNearbyAgents(position.AsVec2, radius, targets);
                }
            }
            //Cause Damage
            if (_template.DamageAmount > 0)
            {
                TORMissionHelper.DamageAgents(targets, (int)(_template.DamageAmount * (1 - _template.DamageVariance) * damageMultiplier), (int)(_template.DamageAmount * (1 + _template.DamageVariance)), triggererAgent, _template.TargetType, _template, _template.DamageType, _template.HasShockWave, position, originAbilityTemplate);
            }
            else if (_template.DamageAmount < 0)
            {
                TORMissionHelper.HealAgents(targets, (int)(-_template.DamageAmount * (1 - _template.DamageVariance) * damageMultiplier), (int)(-_template.DamageAmount * (1 + _template.DamageVariance)), triggererAgent, _template.TargetType, originAbilityTemplate);
            }
            //Apply status effects
            if (_template.AssociatedStatusEffects != null && _template.AssociatedStatusEffects.Count > 0)
            {
                foreach (var effect in _template.AssociatedStatusEffects)
                {
                    if (triggererAgent.Character is CharacterObject triggererCharacter && triggererCharacter.GetPerkValue(TORPerks.SpellCraft.ArcaneLink) && effect.IsBuffEffect)
                    {
                        if (!targets.Contains(triggererAgent)) targets.Append(triggererAgent);
                    }
                    TORMissionHelper.ApplyStatusEffectToAgents(targets, effect.StringID, triggererAgent, statusEffectDuration, true, _isTemplateMutated);
                }
            }
            if (_template.DoNotAlignParticleEffectPrefabOnImpact)
            {
                var groundPos = new Vec3(position.x, position.y, position.z - 5f);
                using (new TWSharedMutexReadLock(Scene.PhysicsAndRayCastLock))
                {
                    Mission.Current.Scene.RayCastForClosestEntityOrTerrainMT(position, groundPos, out float distance, 0.01f, BodyFlags.CommonCollisionExcludeFlagsForAgent);
                    if (distance >= 0.0000001f)
                    {
                        position = new Vec3(position.x, position.y, position.z - distance);
                    }
                }
                normal = Vec3.Forward;
            }

            SpawnVisuals(position, normal);
            PlaySound(position);
            TriggerScript(position, triggererAgent, targets, statusEffectDuration);
        }

        private void SpawnVisuals(Vec3 position, Vec3 normal)
        {
            //play visuals
            if (_template!=null&&_template.BurstParticleEffectPrefab != "none")
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
            if (_template!=null&&_template.SoundEffectId != "none")
            {
                _soundIndex = SoundEvent.GetEventIdFromString(_template.SoundEffectId);
                _sound = SoundEvent.CreateEvent(_soundIndex, Mission.Current.Scene);
                _sound?.PlayInPosition(position);
            }
        }

        private void TriggerScript(Vec3 position, Agent triggerer, IEnumerable<Agent> triggeredAgents, float duration)
        {
            if (_template!=null&&_template.ScriptNameToTrigger != "none")
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
                        script.OnTrigger(position, triggerer, triggeredAgents, duration);
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
            _sound?.Release();
            _sound = null;
            _soundIndex = -1;
            _template = null;
            _timer.Stop();
        }
    }
}
