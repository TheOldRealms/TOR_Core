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
        private const int PENDING_SOUND_DISPOSALS_PER_TICK = 16;

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
            List<TriggeredEffect> effectsToDispose = null;

            lock (_pendingDisposeLock)
            {
                for (int i = _pendingDisposals.Count - 1; i >= 0; i--)
                {
                    if (effectsToDispose?.Count >= PENDING_SOUND_DISPOSALS_PER_TICK)
                    {
                        break;
                    }

                    var pending = _pendingDisposals[i];
                    if (currentMissionTime < pending.DisposeAtMissionTime)
                    {
                        continue;
                    }

                    effectsToDispose ??= new List<TriggeredEffect>();
                    effectsToDispose.Add(pending.Effect);
                    _pendingDisposals.RemoveAt(i);
                }
            }

            if (effectsToDispose == null)
            {
                return;
            }

            foreach (var effect in effectsToDispose)
            {
                effect.Dispose();
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
            if (targets == null)
            {
                targets = new MBList<Agent>();

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

            targets = NormalizeTriggeredTargets(targets);

            var effectPosition = position;

            var effectNormal = normal;

            if (_template.DoNotAlignParticleEffectPrefabOnImpact)
            {
                var groundPos = new Vec3(effectPosition.x, effectPosition.y, effectPosition.z - 5f);
                using (new TWSharedMutexReadLock(Scene.PhysicsAndRayCastLock))
                {
                    Mission.Current.Scene.RayCastForClosestEntityOrTerrain(effectPosition, groundPos, out float distance, 0.01f, BodyFlags.CommonCollisionExcludeFlagsForAgent);
                    if (distance >= 0.0000001f)
                    {
                        effectPosition = new Vec3(effectPosition.x, effectPosition.y, effectPosition.z - distance);
                    }
                }

                effectNormal = Vec3.Forward;
            }

            var logic = Mission.Current?.GetMissionBehavior<AbilityManagerMissionLogic>();
            // trigger returning before queued damage is applied requires associated status to stay pending until the matching damage resolves to preserve their intended order
            var resolutionId = logic != null &&
                               _template.DamageAmount != 0 &&
                               _template.AssociatedStatusEffects?.Count > 0
                ? logic.BeginTriggeredEffectResolution()
                : -1;

            if (logic != null)
            {
                logic.QueueTriggeredEffectSound(_template.StringID, _template.SoundEffectId, effectPosition, castId);
            }
            else
            {
                PlaySound(effectPosition);
            }
            //Cause Damage
            if (_template.DamageAmount > 0)
            {
                TORMissionHelper.DamageAgents(targets, (int)(_template.DamageAmount * (1 - _template.DamageVariance) * damageMultiplier), (int)(_template.DamageAmount * (1 + _template.DamageVariance)), triggererAgent, _template.TargetType, _template, _template.DamageType, _template.HasShockWave, position, originAbilityTemplate, castId, resolutionId);
            }
            else if (_template.DamageAmount < 0)
            {
                TORMissionHelper.HealAgents(targets, (int)(-_template.DamageAmount * (1 - _template.DamageVariance) * damageMultiplier), (int)(-_template.DamageAmount * (1 + _template.DamageVariance)), triggererAgent, _template.TargetType, originAbilityTemplate, castId, resolutionId);
            }
            //Apply status effects
            if (_template.AssociatedStatusEffects != null && _template.AssociatedStatusEffects.Count > 0)
            {

                foreach (var effect in _template.AssociatedStatusEffects)
                {
                    if (triggererAgent.Character is CharacterObject triggererCharacter &&
                        triggererCharacter.GetPerkValue(TORPerks.Spellcraft.ArcaneLink) &&
                        effect.IsBuffEffect &&
                        (_template.TargetType == TargetType.Friendly || _template.TargetType == TargetType.FriendlyHero))
                    {
                        if (!targets.Contains(triggererAgent))
                        {
                            targets.Add(triggererAgent);
                        }
                    }

                    var statusTargets = new MBList<Agent>();
                    foreach (var target in targets)
                    {
                        if (CanReceiveTriggeredStatusEffect(target))
                        {
                            statusTargets.Add(target);
                        }
                    }

                    if (statusTargets.Count == 0)
                    {
                        continue;
                    }

                    foreach (var target in statusTargets)
                    {
                        if (logic != null)
                        {
                            logic.QueueTriggeredStatusEffect(target, effect, triggererAgent, statusEffectDuration, true, _isTemplateMutated, castId, resolutionId);
                        }
                        else
                        {
                            TORMissionHelper.ApplyStatusEffectToAgent( target, effect.StringID, triggererAgent, statusEffectDuration, true, _isTemplateMutated, false, castId);
                        }
                    }
                }
            }
            if (logic != null)
            {
                logic.QueueTriggeredEffectVisual(_template.StringID, _template.BurstParticleEffectPrefab, _template.SoundEffectLength, effectPosition, effectNormal, castId);
            }
            else
            {
                SpawnVisuals(effectPosition, effectNormal);
            }
            TriggerScript(effectPosition, triggererAgent, targets, statusEffectDuration);
            Dispose();
        }
        private static MBList<Agent> NormalizeTriggeredTargets(IEnumerable<Agent> rawTargets)
        {
            var result = new MBList<Agent>();
            if (rawTargets == null)
            {
                return result;
            }

            var mission = Mission.Current;
            var seen = new HashSet<int>();

            foreach (var candidate in rawTargets)
            {
                if (candidate == null || candidate.Health < 1f)
                {
                    continue;
                }

                if (mission != null && mission.FindAgentWithIndex(candidate.Index) != candidate)
                {
                    continue;
                }

                if (!candidate.IsActive() || candidate.IsFadingOut())
                {
                    continue;
                }

                if (!seen.Add(candidate.Index))
                {
                    continue;
                }

                result.Add(candidate);
            }

            return result;
        }

        private static bool CanReceiveTriggeredStatusEffect(Agent agent)
        {
            if (agent == null || !agent.IsHuman || !agent.IsActive() || agent.Health < 1f || agent.IsFadingOut())
            {
                return false;
            }

            var mission = Mission.Current;
            return mission == null || mission.FindAgentWithIndex(agent.Index) == agent;
        }

        private void SpawnVisuals(Vec3 position, Vec3 normal)
        {
            //play visuals
            var burstPrefab = _template?.BurstParticleEffectPrefab?.Trim();
            if (!string.IsNullOrWhiteSpace(burstPrefab) &&
                !burstPrefab.Equals("none", StringComparison.OrdinalIgnoreCase))
            {
                var effect = GameEntity.CreateEmpty(Mission.Current.Scene);
                MatrixFrame frame = MatrixFrame.Identity;
                ParticleSystem.CreateParticleSystemAttachedToEntity(burstPrefab, effect, ref frame);
                var effectForward = normal;
                if (Math.Abs(effectForward.x) + Math.Abs(effectForward.y) + Math.Abs(effectForward.z) < 0.0001f)
                {
                    effectForward = Vec3.Forward;
                }

                var globalFrame = new MatrixFrame(Mat3.CreateMat3WithForward(in effectForward), position);
                effect.SetGlobalFrame(globalFrame);
                effect.FadeOut(_template.SoundEffectLength, true);
            }
        }

        private void PlaySound(Vec3 position)
        {
            // play sound
            if (_template == null)
            {
                return;
            }

            var soundEffectId = _template.SoundEffectId;
            if (string.IsNullOrWhiteSpace(soundEffectId))
            {
                return;
            }

            soundEffectId = soundEffectId.Trim();
            if (soundEffectId.Equals("none", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var soundIndex = SoundEvent.GetEventIdFromString(soundEffectId);
            if (soundIndex < 0)
            {
                TORCommon.Log(
                    "missing triggered effect sound" +  " | effect=" + _template.StringID + " | sound=" + soundEffectId,
                    NLog.LogLevel.Warn);
                return;
            }

            Mission.Current.MakeSound(
                soundIndex,
                position,
                soundCanBePredicted: false,
                isReliable: false,
                relatedAgent1: -1,
                relatedAgent2: -1);

            _soundIndex = -1;
            _sound = null;
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