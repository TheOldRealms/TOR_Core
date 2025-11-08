using NAudio.Mixer;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.MountAndBlade;
using TOR_Core.Utilities;

namespace TOR_Core.Audio
{
    public class TORAudioManager
    {
        public static readonly TORAudioManager Instance = new TORAudioManager();
        private readonly Dictionary<string, CachedSound> _registeredSounds = new Dictionary<string, CachedSound>();

        public bool RegisterSound(string filePath, string audioName)
        {
            var info = new FileInfo(filePath);
            if (info.Exists)
            {
                if(info.Extension == ".ogg")
                {
                    if (!_registeredSounds.ContainsKey(info.Name))
                    {
                        _registeredSounds.Add(audioName, new CachedSound(filePath, audioName));
                        return true;
                    }
                    return false;
                }
                else
                {
                    TORCommon.Log(string.Format("Sound file at path: {0} is not in the Ogg Vorbis file format.", filePath), NLog.LogLevel.Warn);
                    return false;
                }
            }
            else
            {
                TORCommon.Log(string.Format("Sound file at path: {0} not found.", filePath), NLog.LogLevel.Warn);
                return false;
            }
        }

        public static CachedSoundInstance CreateSoundInstance(string audioName, bool isLooping, float startingVolume)
        {
            var file = TORPaths.TORArmoryModuleRootPath + "ModuleSounds/" + audioName + ".ogg";
            if (Instance._registeredSounds.ContainsKey(audioName))
            {
                return new CachedSoundInstance(Instance._registeredSounds[audioName], isLooping, startingVolume);
            }
            if (Instance.RegisterSound(file, audioName))
            {
                return new CachedSoundInstance(Instance._registeredSounds[audioName], isLooping, startingVolume);
            }
            return null;
        }
    }
}
