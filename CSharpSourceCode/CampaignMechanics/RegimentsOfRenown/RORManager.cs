using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using TaleWorlds.Library;
using TOR_Core.Utilities;

namespace TOR_Core.CampaignMechanics.RegimentsOfRenown
{
    public static class RORManager
    {
        private static readonly Dictionary<string, RORSettlementTemplate> _rorSettlementTemplates = [];
        private static readonly string _templateFileName = "tor_ror_settlement_templates.xml";

        public static RORSettlementTemplate GetTemplateFor(string settlementId)
        {
            if (_rorSettlementTemplates.TryGetValue(settlementId, out RORSettlementTemplate ror)) return ror;
            else return null;
        }

        public static void LoadTemplates()
        {
            var ser = new XmlSerializer(typeof(List<RORSettlementTemplate>));
            var path = TORPaths.TORCoreModuleExtendedDataPath + _templateFileName;
            if (File.Exists(path))
            {
                var list = ser.Deserialize(File.OpenRead(path)) as List<RORSettlementTemplate>;
                foreach (var item in list)
                {
                    _rorSettlementTemplates.Add(item.SettlementId, item);
                }
            }
        }
    }
}
