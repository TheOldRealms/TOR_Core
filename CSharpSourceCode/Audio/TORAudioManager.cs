using System.Collections.Generic;
using System;
using System.IO;
using IOPath = System.IO.Path;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TOR_Core.Utilities;

namespace TOR_Core.Audio
{
    public static class TORAudioManager
    {
        private const string EXTERNAL_SOUND_EVENT = "event:/Extra/voiceover";
        private static readonly List<TORModuleSound> _activeSounds = [];

        public static TORModuleSound CreateSoundInstance(string audioName, bool isLooping, float volume = 1f, Scene scene = null, bool is3D = false)
        {
            var moduleSoundsPath = TORPaths.TORArmoryModuleRootPath + "ModuleSounds/";
            var soundFilePath = IOPath.Combine(moduleSoundsPath, audioName);
            if (!IOPath.HasExtension(soundFilePath))
            {
                soundFilePath += ".ogg";
            }

            if (!File.Exists(soundFilePath) && IOPath.GetExtension(soundFilePath).Equals(".ogg", StringComparison.OrdinalIgnoreCase))
            {
                soundFilePath = IOPath.ChangeExtension(soundFilePath, ".wav");
            }

            if (!File.Exists(soundFilePath))
            {
                TORCommon.Log($"Sound file not found: {soundFilePath}", NLog.LogLevel.Warn);
                return null;
            }

            return new TORModuleSound(audioName, soundFilePath, isLooping, volume, scene, is3D);
        }

        public static void Tick(float dt)
        {
            // managed audio wrappers missing completion callback
            for (var index = _activeSounds.Count - 1; index >= 0; index--)
            {
                if (!_activeSounds[index].UpdatePlayback())
                {
                    _activeSounds.RemoveAt(index);
                }
            }
        }

        public static void StopAll()
        {
            var activeSounds = _activeSounds.ToArray();
            foreach (var sound in activeSounds)
            {
                sound.Remove();
            }
            _activeSounds.Clear();
        }

        internal static bool StartSound(TORModuleSound sound)
        {
            sound.ReleasePlayback();

            var soundEvent = SoundEvent.CreateEventFromExternalFile(EXTERNAL_SOUND_EVENT, sound.FilePath, sound.Scene, sound.Is3D, isBlocking: false);
            if (soundEvent == null || !soundEvent.IsValid)
            {
                sound.MarkStopped();
                return false;
            }

            sound.AttachEvent(soundEvent);
            if (sound.Is3D)
            {
                sound.ApplySpatialState();
            }

            if (!soundEvent.Play())
            {
                sound.ReleasePlayback();
                sound.MarkStopped();
                return false;
            }

            sound.MarkStarted();
            if (!_activeSounds.Contains(sound))
            {
                _activeSounds.Add(sound);
            }

            return true;
        }

        internal static void StopSound(TORModuleSound sound)
        {
            sound.ReleasePlayback();
            sound.MarkStopped();
            _activeSounds.Remove(sound);
        }
    }

    public sealed class TORModuleSound : IDisposable
    {
        private SoundEvent _soundEvent;
        private bool _playbackRequested;
        private Vec3 _position;
        private float _range = 25f;

        internal string FilePath { get; }
        internal Scene Scene { get; }
        internal bool Is3D { get; }
        internal float Volume { get; }
        public string AudioName { get; }
        public bool IsLooping { get; }
        public bool IsActive => _playbackRequested;
        public bool IsPlaying => _soundEvent != null && _soundEvent.IsValid && _soundEvent.IsPlaying();

        internal TORModuleSound(string audioName, string filePath, bool isLooping, float volume, Scene scene, bool is3D)
        {
            AudioName = audioName;
            FilePath = filePath;
            IsLooping = isLooping;
            Volume = volume;
            Scene = scene;
            Is3D = is3D;
        }

        public bool Play()
        {
            return TORAudioManager.StartSound(this);
        }

        public void Remove()
        {
            TORAudioManager.StopSound(this);
        }

        public void SetPosition(Vec3 position, float range)
        {
            _position = position;
            _range = range;
            ApplySpatialState();
        }

        public void Dispose()
        {
            Remove();
        }

        internal void AttachEvent(SoundEvent soundEvent)
        {
            _soundEvent = soundEvent;
        }

        internal void ApplySpatialState()
        {
            if (_soundEvent == null || !_soundEvent.IsValid)
            {
                return;
            }

            _soundEvent.SetPosition(_position);
            _soundEvent.SetEventMinMaxDistance(new Vec3(1f, _range, 0f));
        }

        internal bool UpdatePlayback()
        {
            if (!_playbackRequested)
            {
                return false;
            }

            if (_soundEvent != null && _soundEvent.IsValid && !_soundEvent.IsStopped())
            {
                return true;
            }

            if (IsLooping)
            {
                return TORAudioManager.StartSound(this);
            }

            ReleasePlayback();
            MarkStopped();
            return false;
        }

        internal void ReleasePlayback()
        {
            _soundEvent?.Release();
            _soundEvent = null;
        }

        internal void MarkStarted()
        {
            _playbackRequested = true;
        }

        internal void MarkStopped()
        {
            _playbackRequested = false;
        }
    }
}