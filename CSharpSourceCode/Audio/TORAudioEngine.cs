using NAudio.Wave.SampleProviders;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.InputSystem;

namespace TOR_Core.Audio
{
    public class TORAudioEngine : IDisposable
    {
        private readonly IWavePlayer _outputDevice;
        private readonly TORMixingSampleProvider _mixer;
        public int SampleRate { get; private set; }
        public int Channels { get; private set; }
        public TORMixingSampleProvider Mixer => _mixer;

        public static readonly TORAudioEngine Instance = new TORAudioEngine(48000, 2);

        public TORAudioEngine(int sampleRate = 48000, int channelCount = 2)
        {
            SampleRate = sampleRate;
            Channels = channelCount;
            _outputDevice = new WaveOutEvent();
            _mixer = new TORMixingSampleProvider(WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, channelCount));
            _mixer.ReadFully = true;
            _outputDevice.Init(_mixer);
            _outputDevice.Play();
        }

        public void PlaySound(CachedSoundInstance sound) => _mixer?.AddMixerInput(sound);

        public void RemoveSound(CachedSoundInstance sound) => _mixer?.RemoveMixerInput(sound);

        public void ClearSounds() => _mixer?.RemoveAllMixerInputs();

        public void Dispose()
        {
            _outputDevice?.Dispose();
        }
    }
}
