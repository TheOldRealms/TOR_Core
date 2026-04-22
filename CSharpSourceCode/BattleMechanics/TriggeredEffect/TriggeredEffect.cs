using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TOR_Core.AbilitySystem;
using TOR_Core.BattleMechanics.StatusEffect;
using TOR_Core.BattleMechanics.TriggeredEffect.Scripts;
using TOR_Core.CharacterDevelopment;
using TOR_Core.Extensions;
using TOR_Core.Utilities;

namespace TOR_Core.BattleMechanics.TriggeredEffect
{
    public class TriggeredEffect(TriggeredEffectTemplate template, bool isTemplateMutated = false) : IDisposable
    {
        private TriggeredEffectTemplate _template = template;
        private int _soundIndex;
        private SoundEvent _sound;
        private readonly bool _isTemplateMutated = isTemplateMutated;
        private static readonly object _pendingDisposeLock = new();

        private static readonly List<PendingDisposal> _pendingDisposals = new();

        public float EffectRadius => _template.Radius;
        public string SummonedTroopId => _template.TroopIdToSummon;
        public float ImbuedStatusEffectDuration => _template.ImbuedStatusEffectDuration;
        public List<string> StatusEffects => _template.ImbuedStatusEffects;

        private struct PendingDisposal
        {
            public TriggeredEffect Effect;
            public float DisposeAtMissionTime;

            public PendingDisposal(TriggeredEffect effect, float disposeAtMissionTime)
            {
                Effect = effect;
                DisposeAtMissionTime = disposeAtMissionTime;
            }
        }

        internal static void ProcessPendingDisposals(float currentMissionTime)
        {
            lock (_pendingDisposeLock)
            {
                for (int i = _pendingDisposals.Count - 1; i >= 0; i--)
                {
                    var pending = _pendingDisposals[i];
                    if (currentMissionTime < pending.DisposeAtMissionTime)
                        continue;

                    _pendingDisposals.RemoveAt(i);
                    pending.Effect.Dispose();
                }
            }
        }

        internal static void ClearPendingDisposals(float currentMissionTime)
        {
            _ = currentMissionTime;

            lock (_pendingDisposeLock)
            {
                for (int i = _pendingDisposals.Count - 1; i >= 0; i--)
                {
                    var pending = _pendingDisposals[i];
                    _pendingDisposals.RemoveAt(i);
                    pending.Effect.Dispose();
                }
            }
        }
        public void Trigger(Vec3 position, Vec3 normal, Agent triggererAgent, AbilityTemplate originAbilityTemplate = null, MBList<Agent> targets = null, int castId = -1)
        {
            if (_template == null || !triggererAgent.IsActive()) return;

            float damageMultiplier = 1f;
            float statusEffectDuration = _template.ImbuedStatusEffectDuration;
            float radius = _template.Radius;
            if (Game.Current.GameType is Campaign && originAbilityTemplate != null)
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
                if (_template.TargetType == TargetType.Self)
                {
                    targets.Add(triggererAgent);
                }
                else if (_template.TargetType == TargetType.Enemy)
                {
                    //Check triggererAgenTeam. It can be null in fastforward mode
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
                TORMissionHelper.DamageAgents(targets, (int)(_template.DamageAmount * (1 - _template.DamageVariance) * damageMultiplier), (int)(_template.DamageAmount * (1 + _template.DamageVariance)), triggererAgent, _template.TargetType, _template, _template.DamageType, _template.HasShockWave, position, originAbilityTemplate, castId);
            }
            else if (_template.DamageAmount < 0)
            {
                TORMissionHelper.HealAgents(targets, (int)(-_template.DamageAmount * (1 - _template.DamageVariance) * damageMultiplier), (int)(-_template.DamageAmount * (1 + _template.DamageVariance)), triggererAgent, _template.TargetType, originAbilityTemplate, castId);
            }
            //Apply status effects
            if (_template.AssociatedStatusEffects != null && _template.AssociatedStatusEffects.Count > 0)
            {
                var logic = Mission.Current?.GetMissionBehavior<AbilityManagerMissionLogic>();

                foreach (var effect in _template.AssociatedStatusEffects)
                {
                    if (triggererAgent.Character is CharacterObject triggererCharacter && triggererCharacter.GetPerkValue(TORPerks.Spellcraft.ArcaneLink) && effect.IsBuffEffect && (_template.TargetType == TargetType.Friendly || _template.TargetType == TargetType.FriendlyHero))
                    {
                        if (!targets.Contains(triggererAgent)) targets.Add(triggererAgent);
                    }
                    TORMissionHelper.ApplyStatusEffectToAgents(targets, effect.StringID, triggererAgent, statusEffectDuration, true, _isTemplateMutated, castId);

                    // Book status effects and expected DOT/HOT immediately
                    if (castId >= 0 && logic != null)
                    {
                        int expectedTicks = (int)statusEffectDuration;
                        int expectedValuePerTarget = (int)(expectedTicks * effect.BaseEffectValue);

                        foreach (var target in targets)
                        {
                            if (target == null) continue;

                            // Book status effect application for XP
                            logic.BookSpellStatusEffect(castId, target);

                            // Book expected DOT/HOT values based on duration × value per tick
                            if (effect.Type == StatusEffectTemplate.EffectType.DamageOverTime)
                            {
                                logic.BookSpellDamage(castId, target, expectedValuePerTarget, 0, effect.DamageType);
                            }
                            else if (effect.Type == StatusEffectTemplate.EffectType.HealthOverTime)
                            {
                                logic.BookSpellHealing(castId, target, expectedValuePerTarget);
                            }
                        }

                        // Extend session collect time to wait for status effects to expire (for kill tracking)
                        logic.ExtendSessionCollectTime(castId, statusEffectDuration);
                    }
                }
            }
            if (_template.DoNotAlignParticleEffectPrefabOnImpact)
            {
                var groundPos = new Vec3(position.x, position.y, position.z - 5f);
                using (new TWSharedMutexReadLock(Scene.PhysicsAndRayCastLock))
                {
                    Mission.Current.Scene.RayCastForClosestEntityOrTerrain(position, groundPos, out float distance, 0.01f, BodyFlags.CommonCollisionExcludeFlagsForAgent);
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
            if (_sound != null)
            {
                float disposeDelaySeconds = _template.SoundEffectLength > 0f ? _template.SoundEffectLength : 2f;
                lock (_pendingDisposeLock)
                {
                    _pendingDisposals.Add(new PendingDisposal(this, Mission.Current.CurrentTime + disposeDelaySeconds));
                }
            }
            else
            {
                Dispose();
            }
        }


        private void SpawnVisuals(Vec3 position, Vec3 normal)
        {
            //play visuals
            var burstPrefab = _template?.BurstParticleEffectPrefab?.Trim();
            if (!string.IsNullOrEmpty(burstPrefab) && burstPrefab != "none")
            {
                var effect = GameEntity.CreateEmpty(Mission.Current.Scene);
                MatrixFrame frame = MatrixFrame.Identity;
                ParticleSystem.CreateParticleSystemAttachedToEntity(burstPrefab, effect, ref frame);
                var globalFrame = new MatrixFrame(Mat3.CreateMat3WithForward(in normal), position);
                effect.SetGlobalFrame(globalFrame);
                effect.FadeOut(_template.SoundEffectLength, true);
            }
        }

        private void PlaySound(Vec3 position)
        {
            // play sound
            if (_template == null) return;

            var soundEffectId = _template.SoundEffectId;
            if (string.IsNullOrWhiteSpace(soundEffectId))
                return;

            soundEffectId = soundEffectId.Trim();
            if (soundEffectId == "none")
                return;

            var soundIndex = SoundEvent.GetEventIdFromString(soundEffectId);
            if (soundIndex < 0)
            {
                throw new InvalidOperationException(
                    $"[TOR] Missing sound event '{soundEffectId}' for triggered effect '{_template.StringID}'.");
            }

            _soundIndex = soundIndex;
            _sound = SoundEvent.CreateEvent(_soundIndex, Mission.Current.Scene);
            _sound?.PlayInPosition(position);
        }

        private void TriggerScript(Vec3 position, Agent triggerer, IEnumerable<Agent> triggeredAgents, float duration)
        {
            var scriptNameToTrigger = _template?.ScriptNameToTrigger?.Trim();
            if (!string.IsNullOrEmpty(scriptNameToTrigger) && scriptNameToTrigger != "none")
            {
                try
                {
                    var scriptType = Type.GetType(scriptNameToTrigger, throwOnError: false);
                    if (scriptType == null)
                    {
                        TORCommon.Log("Tried to spawn TriggeredScript: " + scriptNameToTrigger + ", but type couldnt be resolved.", NLog.LogLevel.Error);
                        return;
                    }


                    var obj = Activator.CreateInstance(scriptType);
                    if (obj is PrefabSpawnerScript)
                    {
                        var script = obj as PrefabSpawnerScript;
                        script.OnInit(_template.SpawnPrefabName);
                    }
                    else if (obj is AnvilOfDoomSpawnerScript)
                    {
                        var script = obj as AnvilOfDoomSpawnerScript;
                        script.OnInit(_template.SpawnPrefabName);
                    }
                    else if (obj is SummonScript && _template.TroopIdToSummon != "none")
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
                    TORCommon.Log("Tried to spawn TriggeredScript: " + scriptNameToTrigger + ", but failed.", NLog.LogLevel.Error);
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
        }
    }
}