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
        private TORModuleSound _ambientSound;
        private bool _playbackEnabled;

        protected override void OnEditorInit() => SetScriptComponentToTick(GetTickRequirement());

        public override TickRequirement GetTickRequirement() => TickRequirement.Tick;

        protected override void OnInit()
        {
            SetScriptComponentToTick(GetTickRequirement());
            _playbackEnabled = true;
            LoadAmbientSound();
        }

        protected override void OnTick(float dt)
        {
            if (_ambientSound == null)
            {
                return;
            }

            var soundPosition = GameEntity.GlobalPosition;
            var distance = (soundPosition - Scene.LastFinalRenderCameraPosition).Length;
            if (_playbackEnabled && distance <= Range)
            {
                _ambientSound.SetPosition(soundPosition, Range);
                if (!_ambientSound.IsPlaybackRequested)
                {
                    _ambientSound.Play();
                }
            }
            else if (_ambientSound.IsPlaybackRequested)
            {
                _ambientSound.Remove();
            }
        }

        protected override void OnEditorTick(float dt) => OnTick(dt);

        protected override void OnEditorVariableChanged(string variableName)
        {
            base.OnEditorVariableChanged(variableName);
            if (variableName == "LoadSound")
            {
                _playbackEnabled = false;
                LoadAmbientSound();
            }
            if (variableName == "PlaySound") PlayAmbientSound();
            if (variableName == "StopSound") StopAmbientSound();
        }

        protected override void OnRemoved(int removeReason)
        {
            _ambientSound?.Remove();
            _ambientSound = null;
        }

        private void StopAmbientSound()
        {
            _playbackEnabled = false;
            _ambientSound?.Remove();
        }

        private void PlayAmbientSound()
        {
            _playbackEnabled = true;
            if (_ambientSound == null)
            {
                LoadAmbientSound();
            }

            if (_ambientSound != null)
            {
                _ambientSound.SetPosition(GameEntity.GlobalPosition, Range);
                _ambientSound.Play();
            }
        }

        private void LoadAmbientSound()
        {
            _ambientSound?.Remove();
            _ambientSound = null;

            if (string.IsNullOrEmpty(AudioName))
            {
                return;
            }

            _ambientSound = TORAudioManager.CreateSoundInstance(AudioName, true, scene: Scene, is3D: true);
        }
    }
}