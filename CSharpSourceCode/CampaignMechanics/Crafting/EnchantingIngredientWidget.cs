using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.Library;
using TaleWorlds.TwoDimension;

namespace TOR_Core.CampaignMechanics.Crafting
{
    internal class EnchantingIngredientWidget(UIContext context) : RichTextWidget(context)
    {
        protected override void OnRender(TwoDimensionContext twoDimensionContext, TwoDimensionDrawContext drawContext)
        {
            if (int.TryParse(Text, out int amount) && amount < 0)
            {
                Brush = Context.GetBrush("TorEnchantingIngredientRed");
            }
            Brush.FontSize = 30;
            base.OnRender(twoDimensionContext, drawContext);
        }
    }
}
