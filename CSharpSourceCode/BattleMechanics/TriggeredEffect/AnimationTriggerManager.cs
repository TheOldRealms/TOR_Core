using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using TaleWorlds.Core;
using TOR_Core.Utilities;

namespace TOR_Core.BattleMechanics.TriggeredEffect
{
    public class AnimationTriggerManager
    {
        private static string _filename = "tor_animation_triggers.xml";
        private static AnimationTriggerManager _instance = null;

        public static AnimationTriggerManager Instance
        {
            get
            {
                if (_instance == null) _instance = new AnimationTriggerManager();
                return _instance;
            }
        }

        public List<AnimationTrigger> AnimationTriggers { get; private set; }
        public List<int> RegisteredRaceInts { get; private set; }

        private AnimationTriggerManager()
        {
            AnimationTriggers = new List<AnimationTrigger>();
            RegisteredRaceInts = new List<int>();
        }

        public static void LoadAnimationTriggers()
        {
            var ser = new XmlSerializer(typeof(List<AnimationTrigger>), new XmlRootAttribute("AnimationTriggers"));
            var path = TORPaths.TORCoreModuleExtendedDataPath + _filename;
            if (File.Exists(path))
            {
                var list = ser.Deserialize(File.OpenRead(path)) as List<AnimationTrigger>;
                foreach (var item in list)
                {
                    if (!FaceGen.GetRaceNames().Contains(item.RaceId))
                    {
                        TORCommon.Log("AnimationTriggerManager : " + item.RaceId + " does not exist in the _raceNamesArray. The xml entries does not correspond to the Monsters defined in the monsters and skins xml.", NLog.LogLevel.Error);
                    }
                    else
                    {  
                        Instance.AnimationTriggers.Add(item);

                        int raceInt = FaceGen.GetRaceOrDefault(item.RaceId);
                        if (!Instance.RegisteredRaceInts.Contains(raceInt))
                        {
                            Instance.RegisteredRaceInts.Add(raceInt);
                        }
                    }

                }
            }
        }

        public static void ReloadAnimationTriggers()
        {
            Instance.AnimationTriggers.Clear();
            Instance.RegisteredRaceInts.Clear();
            LoadAnimationTriggers();
        }

        public static string GetEffectForRace(string characterId)
        {
            var triggeredEffectId = Instance.AnimationTriggers.FirstOrDefault(x => x.RaceId == characterId)?.TriggeredEffectId;
            return triggeredEffectId;
        }
    }
}