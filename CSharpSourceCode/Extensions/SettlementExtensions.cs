using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TOR_Core.CampaignMechanics.Assimilation;
using TOR_Core.CampaignMechanics.RegimentsOfRenown;
using TOR_Core.CampaignMechanics.TORCustomSettlement;

namespace TOR_Core.Extensions
{
    public static class SettlementExtensions
    {
        public static CultureObject OriginalCulture(this Settlement settlement)
        {
            return AssimilationCampaignBehavior.GetOriginalCultureForSettlement(settlement);
        }

        public static bool IsBloodKeep(this Settlement settlement)
        {
            if(settlement!=null)
                return settlement.StringId == "castle_BK1";

            return false;
        }
        
        public static bool IsRoRSettlement(this Settlement settlement)
        {
            return RORManager.GetTemplateFor(settlement.StringId) != null;
        }

        public static RORSettlementTemplate GetRoRTemplate(this Settlement settlement)
        {
            return RORManager.GetTemplateFor(settlement.StringId);
        }

        public static bool IsBretonnianMajorSettlement(this Settlement settlement)
        {
            if (settlement.IsVillage) return false;

            if (settlement.IsCastle || settlement.IsTown)
            {
                var endString = settlement.StringId.Substring(settlement.StringId.Length - 3, 3);
                
                if (endString.Contains("MS")) return true;
                if (endString.Contains("PA")) return true;
                if (endString.Contains("MO")) return true;
                if (endString.Contains("BA")) return true;
                if (endString.Contains("BL")) return true;
                if (endString.Contains("AQ")) return true;
                if (endString.Contains("BE")) return true;
                if (endString.Contains("CC")) return true;
                if (endString.Contains("QU")) return true;
                if (endString.Contains("GX")) return true;
                if (endString.Contains("LY")) return true;
                if (endString.Contains("LA")) return true;
                if (endString.Contains("CO")) return true;
            }
            
            return false;
        }

        public static bool IsOakOfTheAges(this Settlement settlement)
        {
            return settlement.SettlementComponent is OakOfAgesComponent;
        }
    }
}
