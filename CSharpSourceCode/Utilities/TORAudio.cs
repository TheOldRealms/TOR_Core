using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NAudio;
using NAudio.Vorbis;
using NAudio.Wave;

namespace TOR_Core.Utilities
{
    public class TORAudio
    {
        private static TORAudio Instance { get; set; }
        public static bool IsPlaying => Instance._outputDevice?.PlaybackState == PlaybackState.Playing;
        private WaveOutEvent _outputDevice;
        private VorbisWaveReader _vorbisWaveReader;

        public static void Initialize()
        {
            Instance = new TORAudio();
        }

        public static bool PlayAudio(string filename)
        {
            var info = new FileInfo(filename);
            if (info.Exists)
            {
                if(info.Extension == ".ogg")
                {
                    
                    Instance._vorbisWaveReader = new VorbisWaveReader(filename);
                    if(Instance._vorbisWaveReader != null)
                    {
                        Instance._outputDevice = new WaveOutEvent();
                        Instance._outputDevice.Init(Instance._vorbisWaveReader);
                        Instance._outputDevice.Play();
                    }
                }
            }
            return false;
        }

        public static void StopAudio()
        {
            Instance._outputDevice?.Stop();
        }

        public static void CleanUp()
        {
            Instance._vorbisWaveReader?.Dispose();
            Instance._outputDevice?.Dispose();
        }
    }
}