using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.Engine;
using TOR_Core.Utilities;

namespace TOR_Core.Audio
{
    public class TORAudioAmbientSoundEmitter : ScriptComponentBehavior
    {
        public string AudioName;
        public int Range = 25;
        public int MaxVolumePercent = 100;
        public SimpleButton LoadSound;
        public SimpleButton PlaySound;
        public SimpleButton StopSound;
        private CachedSoundInstance _cachedSound;

        protected override void OnEditorInit() => SetScriptComponentToTick(GetTickRequirement());

        public override TickRequirement GetTickRequirement() => TickRequirement.Tick;

        protected override void OnInit()
        {
            SetScriptComponentToTick(GetTickRequirement());
            if(!string.IsNullOrEmpty(AudioName))
            {
                _cachedSound = TORAudioManager.CreateSoundInstance(AudioName, true, 0);
                if (_cachedSound != null) _cachedSound.Play();
            }
        }

        protected override void OnTick(float dt)
        {
            if(_cachedSound != null && _cachedSound.IsLoaded)
            {
                var cameraPos = Scene.LastFinalRenderCameraPosition;
                var soundPos = GameEntity.GlobalPosition;

                var distance = (soundPos - cameraPos).Length;
                if (distance <= Range)
                {
                    //linear attenuation for now
                    var volume = distance / Range;
                    volume = 1 - volume;
                    volume = Math.Max(0, volume);
                    _cachedSound.CurrentVolume = volume * (MaxVolumePercent / 100);
                }
                else _cachedSound.CurrentVolume = 0;
            }
        }

        protected override void OnEditorTick(float dt) => OnTick(dt);

        protected override void OnEditorVariableChanged(string variableName)
        {
            base.OnEditorVariableChanged(variableName);
            if (variableName == "LoadSound") DoLoadSound();
            if (variableName == "PlaySound") DoPlaySound();
            if (variableName == "StopSound") DoStopSound();
        }

        protected override void OnRemoved(int removeReason) => DoStopSound();

        private void DoStopSound()
        {
            if (_cachedSound != null && _cachedSound.IsPlaying) _cachedSound.Remove();
        }

        private void DoPlaySound()
        {
            if(_cachedSound != null && _cachedSound.IsLoaded) _cachedSound.Play();
        }

        private void DoLoadSound()
        {
            DoStopSound();
            _cachedSound = TORAudioManager.CreateSoundInstance(AudioName, true, 0);
        }
    }
}
