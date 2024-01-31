using NAudio.Vorbis;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TOR_Core.Utilities;

namespace TOR_Core.Audio
{
    public class CachedSound
    {
        public float[] AudioData { get; private set; }
        public WaveFormat WaveFormat { get; private set; }
        public bool IsLoaded => AudioData != null && AudioData.Length > 0;
        public string SoundFilePath { get; private set; }
        public string FilePath { get; }
        public string AudioName { get; }

        public CachedSound(string audioFileName, string audioName, bool isLooping = false)
        {
            SoundFilePath = audioFileName;
            AudioName = audioName;
            var info = new FileInfo(audioFileName);
            if (info.Exists)
            {
                if (info.Extension == ".ogg")
                {
                    using (var audioFileReader = new VorbisWaveReader(audioFileName))
                    {
                        if (!EnsureSampleRateAndChannels(audioFileReader))
                        {
                            return;
                        }

                        WaveFormat = audioFileReader.WaveFormat;

                        var wholeFile = new List<float>((int)(audioFileReader.Length / 4));
                        var readBuffer = new float[TORAudioEngine.Instance.SampleRate * TORAudioEngine.Instance.Channels];
                        int samplesRead;
                        while ((samplesRead = audioFileReader.Read(readBuffer, 0, readBuffer.Length)) > 0)
                        {
                            wholeFile.AddRange(readBuffer.Take(samplesRead));
                        }
                        AudioData = wholeFile.ToArray();
                    }
                }
                else TORCommon.Log(string.Format("Sound file at path: {0} must have an .ogg file extension. Sound not loaded.", SoundFilePath), NLog.LogLevel.Warn);
            }
        }

        private bool EnsureSampleRateAndChannels(ISampleProvider provider)
        {
            if(provider.WaveFormat.SampleRate != TORAudioEngine.Instance.SampleRate || 
                provider.WaveFormat.Channels != TORAudioEngine.Instance.Channels)
            {
                TORCommon.Log(string.Format("Sound file at path: {0} is either mono or not sampled at 44100. Sound not loaded.", SoundFilePath), NLog.LogLevel.Warn);
                return false;
            }
            return true;
        }
    }
}
