using NAudio.Utils;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TOR_Core.Audio
{
    public class TORMixingSampleProvider : ISampleProvider
    {
        private readonly List<CachedSoundInstance> sources;
        private float[] sourceBuffer;
        private const int MaxInputs = 1024; // protect ourselves against doing something silly

        public TORMixingSampleProvider(WaveFormat waveFormat)
        {
            if (waveFormat.Encoding != WaveFormatEncoding.IeeeFloat)
            {
                throw new ArgumentException("Mixer wave format must be IEEE float");
            }
            sources = new List<CachedSoundInstance>();
            WaveFormat = waveFormat;
        }

        public IEnumerable<CachedSoundInstance> MixerInputs => sources;

        public bool ReadFully { get; set; }

        public void AddMixerInput(CachedSoundInstance mixerInput)
        {
            // we'll just call the lock around add since we are protecting against an AddMixerInput at
            // the same time as a Read, rather than two AddMixerInput calls at the same time
            lock (sources)
            {
                if (sources.Count >= MaxInputs)
                {
                    throw new InvalidOperationException("Too many mixer inputs");
                }
                sources.Add(mixerInput);
            }
            if (WaveFormat == null)
            {
                WaveFormat = mixerInput.WaveFormat;
            }
            else
            {
                if (WaveFormat.SampleRate != mixerInput.WaveFormat.SampleRate ||
                    WaveFormat.Channels != mixerInput.WaveFormat.Channels)
                {
                    throw new ArgumentException("All mixer inputs must have the same WaveFormat");
                }
            }
        }

        public void RemoveMixerInput(CachedSoundInstance mixerInput)
        {
            lock (sources)
            {
                sources.Remove(mixerInput);
            }
        }

        public void RemoveAllMixerInputs()
        {
            lock (sources)
            {
                sources.Clear();
            }
        }

        public WaveFormat WaveFormat { get; private set; }

        public int Read(float[] buffer, int offset, int count)
        {
            int outputSamples = 0;
            sourceBuffer = BufferHelpers.Ensure(sourceBuffer, count);
            lock (sources)
            {
                int index = sources.Count - 1;
                while (index >= 0)
                {
                    var source = sources[index];
                    int samplesRead = source.Read(sourceBuffer, 0, count);
                    int outIndex = offset;
                    for (int n = 0; n < samplesRead; n++)
                    {
                        if (n >= outputSamples)
                        {
                            buffer[outIndex++] = sourceBuffer[n];
                        }
                        else
                        {
                            buffer[outIndex++] += sourceBuffer[n];
                        }
                    }
                    outputSamples = Math.Max(samplesRead, outputSamples);
                    if (samplesRead < count)
                    {
                        if(!source.IsLooping) sources.RemoveAt(index);
                    }
                    index--;
                }
            }
            // optionally ensure we return a full buffer
            if (ReadFully && outputSamples < count)
            {
                int outputIndex = offset + outputSamples;
                while (outputIndex < offset + count)
                {
                    buffer[outputIndex++] = 0;
                }
                outputSamples = count;
            }
            return outputSamples;
        }
    }
}
