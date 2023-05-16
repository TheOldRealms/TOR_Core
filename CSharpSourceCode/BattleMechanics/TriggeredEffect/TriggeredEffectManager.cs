using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using TOR_Core.Utilities;

namespace TOR_Core.BattleMechanics.TriggeredEffect
{
    public class TriggeredEffectManager
    {
        private static Dictionary<string, TriggeredEffectTemplate> _dictionary = new Dictionary<string, TriggeredEffectTemplate>();
        private static string _filename = "tor_triggeredeffects.xml";

        internal static TriggeredEffect CreateNew(string id)
        {
            TriggeredEffect effect = null;
            if (_dictionary.ContainsKey(id))
            {
                effect = new TriggeredEffect(_dictionary[id]);
            }
            return effect;
        }

        public static void LoadTemplates()
        {
            var ser = new XmlSerializer(typeof(List<TriggeredEffectTemplate>), new XmlRootAttribute("TriggeredEffectTemplates"));
            var path = TORPaths.TORCoreModuleExtendedDataPath + _filename;
            if (File.Exists(path))
            {
                var list = ser.Deserialize(File.OpenRead(path)) as List<TriggeredEffectTemplate>;
                foreach (var item in list)
                {
                    _dictionary.Add(item.StringID, item);
                }
            }
        }

        internal static TriggeredEffectTemplate GetTemplateWithId(string id)
        {
            TriggeredEffectTemplate template = null;
            _dictionary.TryGetValue(id, out template);
            return template;
        }

        public static List<TriggeredEffectTemplate> GetTemplatesWithIds(List<string> ids)
        {
            List<TriggeredEffectTemplate> result = new List<TriggeredEffectTemplate>();
            foreach(var id in ids)
            {
                TriggeredEffectTemplate template = null;
                if(_dictionary.TryGetValue(id, out template))
                {
                    result.Add(template);
                }
            }
            return result;
        }
    }
}
