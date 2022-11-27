using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using TaleWorlds.Core;
using TOR_Core.AbilitySystem;
using TOR_Core.Utilities;

namespace TOR_Core.CampaignMechanics.Career
{
    public class CareerFactory
    {
        private static Dictionary<string, CareerTemplate> _templates = new Dictionary<string, CareerTemplate>();
        
        private static string _filename = "tor_careertemplates.xml";
        public static IEnumerable<string> ListAvailableCareers()
        {
            return (from object career in Enum.GetValues(typeof(CareerId)) select career.ToString()).ToList();
        }
        
        public static void LoadTemplates()
        {
            var ser = new XmlSerializer(typeof(List<CareerTemplate>));
            var path = TORPaths.TORCoreModuleExtendedDataPath + _filename;
            if (File.Exists(path))
            {
                var list = ser.Deserialize(File.OpenRead(path)) as List<CareerTemplate>;
                foreach (var item in list)
                {
                    _templates.Add(item.CareerId.ToString(), item);
                }

                TORCommon.Say(_templates.Count.ToString());
            }
        }
        
        
        
        
        
    }
}