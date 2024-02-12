using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TOR_Core.Audio
{
    public class CachedSoundInstance : IDisposable, ISampleProvider
    {
        private CachedSound _cachedSound;
        public float CurrentVolume { get; set; }
        public long CurrentPosition { get; set; }
        public float[] AudioData => _cachedSound.AudioData;
        public bool IsLooping { get; set; }
        public bool IsLoaded => _cachedSound.IsLoaded;
        public bool IsPlaying => TORAudioEngine.Instance.Mixer.MixerInputs.Contains(this);
        public string AudioName => _cachedSound.AudioName;
        public WaveFormat WaveFormat { get { return _cachedSound.WaveFormat; } }

        public CachedSoundInstance(CachedSound cachedSound, bool isLooping = false, float startingVolume = 1)
        {
            _cachedSound = cachedSound;
            CurrentVolume = startingVolume;
            IsLooping = isLooping;
            CurrentPosition = 0;
        }

        public int Read(float[] buffer, int offset, int count)
        {
            var availableSamples = _cachedSound.AudioData.Length - CurrentPosition;

            var samplesToCopy = Math.Min(availableSamples, count);

            int destOffset = offset;
            for (int sourceSample = 0; sourceSample < samplesToCopy; sourceSample += 2)
            {
                float outL = _cachedSound.AudioData[CurrentPosition + sourceSample + 0];
                float outR = _cachedSound.AudioData[CurrentPosition + sourceSample + 1];

                buffer[destOffset + 0] = outL * CurrentVolume;
                buffer[destOffset + 1] = outR * CurrentVolume;
                destOffset += 2;
            }

            CurrentPosition += samplesToCopy;

            if(CurrentPosition >= AudioData.Length && IsLooping && samplesToCopy <= count)
            {
                int additionalSamples = (int)(count - samplesToCopy);
                CurrentPosition = 0;
                for (int sourceSample = 0; sourceSample < additionalSamples; sourceSample += 2)
                {
                    float outL = _cachedSound.AudioData[CurrentPosition + sourceSample + 0];
                    float outR = _cachedSound.AudioData[CurrentPosition + sourceSample + 1];

                    buffer[destOffset + 0] = outL * CurrentVolume;
                    buffer[destOffset + 1] = outR * CurrentVolume;
                    destOffset += 2;
                }
                CurrentPosition += additionalSamples;
                samplesToCopy += additionalSamples;
            }

            return (int)samplesToCopy;
        }

        public void RewindToStart() => CurrentPosition = 0;

        public void SkipToEnd()
        {
            if (IsLoaded) CurrentPosition = AudioData.Length;
        }

        public void Play()
        {
            if (IsLoaded) TORAudioEngine.Instance.PlaySound(this);
        }

        public void Remove()
        {
            if (IsPlaying) TORAudioEngine.Instance.RemoveSound(this);
        }

        public void Dispose()
        {
            _cachedSound = null;
        }
    }
}
