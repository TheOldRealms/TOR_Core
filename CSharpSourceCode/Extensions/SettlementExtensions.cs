using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem.Settlements;
using TOR_Core.CampaignMechanics.RegimentsOfRenown;

namespace TOR_Core.Extensions
{
    public static class SettlementExtensions
    {

        public static bool IsBloodKeep(this Settlement settlement)
        {
            return settlement.StringId == "castle_BK1";
        }
        
        public static bool IsRoRSettlement(this Settlement settlement)
        {
            return RORManager.GetTemplateFor(settlement.StringId) != null;
        }

        public static RORSettlementTemplate GetRoRTemplate(this Settlement settlement)
        {
            return RORManager.GetTemplateFor(settlement.StringId);
        }
    }
}
