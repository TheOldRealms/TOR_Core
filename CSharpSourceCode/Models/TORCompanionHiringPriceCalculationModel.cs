using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameComponents;
using TOR_Core.Extensions;

namespace TOR_Core.Models
{
    public class TORCompanionHiringPriceCalculationModel : DefaultCompanionHiringPriceCalculationModel
    {
        public override int GetCompanionHiringPrice(Hero companion)
        {
            if (companion.Template.IsTORTemplate() && companion.IsWanderer)
            {
                if (companion.IsSpellCaster())
                {
                    return 20000;
                }
                else if(companion.BattleEquipment.GetHumanBodyArmorSum() > 40) 
                {
                    return 15000;
                }
                return 10000;
            }
            else return base.GetCompanionHiringPrice(companion);
        }
    }
}
