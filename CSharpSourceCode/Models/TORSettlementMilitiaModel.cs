using Helpers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Localization;
using TOR_Core.CharacterDevelopment;
using TOR_Core.Extensions;
using TOR_Core.Utilities;

namespace TOR_Core.Models
{
    public class TORSettlementMilitiaModel : DefaultSettlementMilitiaModel
    {
     

        public override ExplainedNumber CalculateMilitiaChange(Settlement settlement, bool includeDescriptions = false)
        {
            var result = base.CalculateMilitiaChange(settlement, includeDescriptions);
            if (settlement.IsCastle)
            {
                if (settlement.IsBloodKeep() && settlement.Owner.IsVampire()&&!settlement.IsUnderSiege)
                {
                    if (settlement.Militia < 2000)
                    {
                        result.LimitMin(10);
                    }
                    //result.Add(10f,new TextObject("Blood Keep bonus"));
                    return result;
                }

                if (settlement.StringId == "castle_BK2")
                {
                    if (settlement.Militia < 2000)
                    {
                        result.LimitMin(10);
                    }
                }
                
                switch (settlement.OwnerClan.Culture.StringId)
                {
                    case TORConstants.Cultures.SYLVANIA:
                        result.Add(2f, new TextObject("Bonus"));
                        break;
                    case TORConstants.Cultures.EMPIRE:
                        result.Add(1f, new TextObject("Bonus"));
                        break;
                    default:
                        result.Add(1f, new TextObject("Bonus"));
                        break;
                }
            }
            else if (settlement.IsTown)
            {
                switch (settlement.OwnerClan.Culture.StringId)
                {
                    case TORConstants.Cultures.SYLVANIA:
                        result.Add(4f, new TextObject("Bonus"));
                        break;
                    case TORConstants.Cultures.EMPIRE:
                        result.Add(3f, new TextObject("Bonus"));
                        break;
                    default:
                        result.Add(2f, new TextObject("Bonus"));
                        break;
                }
            }
            if(settlement.OwnerClan != null && settlement.OwnerClan.Leader != null)
            {
                PerkHelper.AddPerkBonusForCharacter(TORPerks.Faith.DivineMission, settlement.OwnerClan.Leader.CharacterObject, false, ref result);
            }
            return result;
        }
    }
}
