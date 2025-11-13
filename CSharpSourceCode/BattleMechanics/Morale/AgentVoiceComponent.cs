using System;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TOR_Core.Utilities;
using static TaleWorlds.MountAndBlade.SkinVoiceManager;

namespace TOR_Core.BattleMechanics.Morale
{
    public class AgentVoiceComponent(Agent agent) : AgentComponent(agent)
    {
        private MissionTime _lastPlayVoiceStartTime;
        //private float _minTimeBetweenVoicePlaybacks = 2.0f;
        private float _playbackDelay = 0.9f;
        private bool _wantsToPlayVoice = false;
        private SkinVoiceType _wantsToPlayVoiceType = VoiceType.Idle;

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
        }

        private void PlayVoice(SkinVoiceType voiceType)
        {
            Agent.MakeVoice(voiceType, CombatVoiceNetworkPredictionType.NoPrediction);
        }
    }
}