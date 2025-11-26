using HarmonyLib;
using System;
using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.MountAndBlade;
using TOR_Core.Utilities;
using static TaleWorlds.MountAndBlade.SkinVoiceManager;

namespace TOR_Core.BattleMechanics.Voice
{
    public class AgentVoiceComponent(Agent agent) : AgentComponent(agent)
    {
        private MissionTime _lastPlayVoiceStartTime;
        //private float _minTimeBetweenVoicePlaybacks = 2.0f;
        private float _playbackDelay = 0.9f;
        private bool _wantsToPlayVoice = false;
        private SkinVoiceType _wantsToPlayVoiceType = VoiceType.Idle;
        private readonly Dictionary<int, SoundEvent> _activeSounds = [];
        private readonly List<int> _soundsToRemove = [];

        public void SetWantsToPlayVoiceWithDelay(SkinVoiceType voiceType, float delay)
        {
            _wantsToPlayVoice = true;
            _wantsToPlayVoiceType = voiceType;
            _lastPlayVoiceStartTime = MissionTime.Now;
            _playbackDelay = delay + MBRandom.RandomFloatRanged(0f, 0.5f);
        }

        public override void OnTick(float dt)
        {
            if (_wantsToPlayVoice && _lastPlayVoiceStartTime.ToSeconds + _playbackDelay < MissionTime.Now.ToSeconds)
            {
                if (Agent == null)
                {
                    TORCommon.Log("AgentVoiceComponent : agent is null.", NLog.LogLevel.Warn);
                }
                _wantsToPlayVoice = false;
                try
                {
                    if (Agent.IsHuman && Agent.IsActive() && Agent.Health > 1f)
                    {
                        PlayVoice(_wantsToPlayVoiceType);
                    }
                }
                catch (Exception ex)
                {
                    TORCommon.Log("AgentVoiceComponent.OnTickAsAI : error while attempting to play voice. Error: " + ex.Message, NLog.LogLevel.Error);
                }
            }

            _soundsToRemove.Clear();
            foreach (var sound in _activeSounds)
            {
                if (!sound.Value.IsValid || !sound.Value.IsPlaying())
                {
                    _soundsToRemove.Add(sound.Key);
                }
                else if(sound.Value.IsPlaying())
                {
                    // Update sound position to follow agent
                    sound.Value.SetPosition(Agent.Position);
                }
            }
            foreach (int id in _soundsToRemove)
            {
                var sound = _activeSounds[id];
                sound?.Release();
                _activeSounds.Remove(id);
            }
        }

        public override void OnAgentRemoved()
        {
            CleanUp();
        }

        public override void OnComponentRemoved()
        {
            CleanUp();
        }

        private void CleanUp()
        {
            _soundsToRemove.Clear();
            foreach (var sound in _activeSounds)
            {
                sound.Value?.Stop();
                sound.Value?.Release();
            }
            _activeSounds.Clear();
        }

        private void PlayVoice(SkinVoiceType voiceType)
        {
            if (Agent.IsFemale || !TORConfig.UseAlternativeVoiceManager)
            {
                Agent.MakeVoice(voiceType, CombatVoiceNetworkPredictionType.NoPrediction);
            }
            else
            {
                PlayVoiceNonVanilla(voiceType);
            }
        }

        /// <summary>
        /// Plays a voice for an agent at the agent's position.
        /// </summary>
        public void PlayVoiceNonVanilla(SkinVoiceType voiceType)
        {
            if (Agent == null || !Agent.IsActive() || Mission.Current == null)
            {
                return;
            }

            try
            {
                var voiceToPlay = TORVoiceManager.Instance.GetVoiceToPlay(Agent, voiceType);
                if (string.IsNullOrEmpty(voiceToPlay))
                {
                    TORCommon.Log($"VoiceManager: No voice definition found for agent race/character", NLog.LogLevel.Warn);
                    return;
                }

                PlaySound(voiceToPlay);
            }
            catch (Exception ex)
            {
                TORCommon.Log($"VoiceManager.PlayAgentVoice: Error playing voice. {ex.Message}", NLog.LogLevel.Error);
            }
        }

        private void PlaySound(string soundDef)
        {
            // Create and play sound event
            int soundIndex = SoundEvent.GetEventIdFromString(soundDef);
            if (soundIndex < 0)
            {
                // Fallback: try to register the sound dynamically if needed
                TORCommon.Log($"VoiceManager: Sound event '{soundDef}' not registered in engine", NLog.LogLevel.Debug);
                return;
            }

            var soundEvent = SoundEvent.CreateEvent(soundIndex, Mission.Current.Scene);
            if (!soundEvent.IsNullSoundEvent())
            {
                soundEvent.PlayInPosition(Agent.Position);
                _activeSounds.Add(soundEvent.GetSoundId(), soundEvent);
            }
            else
            {
                soundEvent?.Release();
                soundEvent = null;
            }
        }
    }
}