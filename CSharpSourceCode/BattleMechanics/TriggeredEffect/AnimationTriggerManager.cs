using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
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
        private AnimationTriggerManager() 
        {
            AnimationTriggers = new List<AnimationTrigger>();
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
                    Instance.AnimationTriggers.Add(item);
                }
            }
        }

        public static void ReloadAnimationTriggers()
        {
            Instance.AnimationTriggers.Clear();
            LoadAnimationTriggers();
        }

        public static bool HasTriggersForAction(string actionName)
        {
            return Instance.AnimationTriggers.Any(x => x.ActionName == actionName);
        }

        public static AnimationTrigger GetTriggerForAction(string actionName)
        {
            return Instance.AnimationTriggers.FirstOrDefault(x => x.ActionName == actionName);
        }
    }
}
