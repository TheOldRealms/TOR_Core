using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using TOR_Core.Utilities;

namespace TOR_Core.CampaignMechanics.Religion
{
    public class ReligionManager
    {
        private static ReligionManager _instance;
        private static string _religionXmlPath = "tor_religions.xml";
        private static Dictionary<string, ReligionObject> _religions = new Dictionary<string, ReligionObject>();

        private ReligionManager() { }

        public static void LoadXML()
        {
            _instance = new ReligionManager();
            _instance.LoadReligions();
        }

        public static void Initialize()
        {
            _religions.Values.ToList().ForEach(x => x.OnInitialize());
        }

        public static ReligionObject GetReligion(string id)
        {
            ReligionObject obj = null;
            _religions.TryGetValue(id, out obj);
            return obj;
        }

        private void LoadReligions()
        {
            var ser = new XmlSerializer(typeof(List<ReligionObject>), new XmlRootAttribute("Religions"));
            var path = TORPaths.TORCoreModuleExtendedDataPath + _religionXmlPath;
            if (File.Exists(path))
            {
                var list = ser.Deserialize(File.OpenRead(path)) as List<ReligionObject>;
                foreach (var item in list)
                {
                    _religions.Add(item.StringId, item);
                }
            }
        }
    }
}
