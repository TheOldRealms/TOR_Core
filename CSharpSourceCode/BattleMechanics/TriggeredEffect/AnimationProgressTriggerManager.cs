using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using TaleWorlds.Core;
using TOR_Core.Utilities;

namespace TOR_Core.BattleMechanics.TriggeredEffect
{
    public class AnimationProgressTriggerManager
    {
        private static string _filename = "tor_animation_progress_triggers.xml";
        private static AnimationProgressTriggerManager _instance = null;

        public static AnimationProgressTriggerManager Instance
        {
            get
            {
                if (_instance == null) _instance = new AnimationProgressTriggerManager();
                return _instance;
            }
        }

        public List<AnimationProgressTrigger> Triggers { get; private set; }
        public List<int> RegisteredRaceInts { get; private set; }

        private AnimationProgressTriggerManager()
        {
            Triggers = new List<AnimationProgressTrigger>();
            RegisteredRaceInts = new List<int>();
        }

        public static void LoadTriggers()
        {
            var ser = new XmlSerializer(typeof(List<AnimationProgressTrigger>), new XmlRootAttribute("AnimationProgressTriggers"));
            var path = TORPaths.TORCoreModuleExtendedDataPath + _filename;
            if (File.Exists(path))
            {
                var list = ser.Deserialize(File.OpenRead(path)) as List<AnimationProgressTrigger>;
                if (list != null)
                {
                    foreach (var item in list)
                    {
                        if (!FaceGen.GetRaceNames().Contains(item.RaceId))
                        {
                            TORCommon.Log("AnimationProgressTriggerManager : " + item.RaceId + " does not exist in the _raceNamesArray. The xml entries does not correspond to the Monsters defined in the monsters and skins xml.", NLog.LogLevel.Error);
                        }
                        else
                        {
                            Instance.Triggers.Add(item);

                            int raceInt = FaceGen.GetRaceOrDefault(item.RaceId);
                            if (!Instance.RegisteredRaceInts.Contains(raceInt))
                            {
                                Instance.RegisteredRaceInts.Add(raceInt);
                            }
                        }
                    }
                }
            }
        }

        public static void ReloadTriggers()
        {
            Instance.Triggers.Clear();
            Instance.RegisteredRaceInts.Clear();
            LoadTriggers();
        }
    }
}
