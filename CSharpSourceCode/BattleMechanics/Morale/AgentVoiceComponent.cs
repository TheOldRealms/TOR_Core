using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
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

        public override void OnTickAsAI(float dt)
        {
            if (_wantsToPlayVoice && _lastPlayVoiceStartTime.ToSeconds + _playbackDelay < MissionTime.Now.ToSeconds)
            {
                _wantsToPlayVoice = false;
                if (Agent.IsHuman && Agent.IsActive() && Agent.Health > 1f)
                {
                    PlayVoice(_wantsToPlayVoiceType);
                }
            }
        }

        private void PlayVoice(SkinVoiceType voiceType)
        {
            Agent.MakeVoice(voiceType, CombatVoiceNetworkPredictionType.NoPrediction);
        }
    }
}
