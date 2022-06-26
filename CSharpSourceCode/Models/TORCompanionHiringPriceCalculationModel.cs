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
                return 2000;
            }
            else return base.GetCompanionHiringPrice(companion);
        }
    }
}
