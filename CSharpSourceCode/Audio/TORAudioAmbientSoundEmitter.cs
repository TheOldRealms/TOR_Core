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

        protected override void OnEditorInit() => SetScriptComponentToTick(GetTickRequirement());

        public override TickRequirement GetTickRequirement() => TickRequirement.Tick;

        protected override void OnInit()
        {
            SetScriptComponentToTick(GetTickRequirement());
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
            if (distance <= Range)
            {
                _ambientSound.SetPosition(soundPosition, Range);
                if (!_ambientSound.IsActive)
                {
                    _ambientSound.Play();
                }
            }
            else if (_ambientSound.IsActive)
            {
                _ambientSound.Remove();
            }
        }

        protected override void OnEditorTick(float dt) => OnTick(dt);

        protected override void OnEditorVariableChanged(string variableName)
        {
            base.OnEditorVariableChanged(variableName);
            if (variableName == "LoadSound") LoadAmbientSound();
            if (variableName == "PlaySound") PlayAmbientSound();
            if (variableName == "StopSound") StopAmbientSound();
        }

        protected override void OnRemoved(int removeReason)
        {
            _ambientSound?.Dispose();
            _ambientSound = null;
        }

        private void StopAmbientSound()
        {
            _ambientSound?.Remove();
        }

        private void PlayAmbientSound()
        {
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
            _ambientSound?.Dispose();
            _ambientSound = null;

            if (string.IsNullOrEmpty(AudioName))
            {
                return;
            }

            _ambientSound = TORAudioManager.CreateSoundInstance(AudioName, true, scene: Scene, is3D: true);
        }
    }
}