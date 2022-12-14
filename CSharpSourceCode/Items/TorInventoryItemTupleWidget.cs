using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.Core;
using TaleWorlds.GauntletUI;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.GauntletUI.Widgets.Inventory;
using TaleWorlds.ObjectSystem;
using TaleWorlds.TwoDimension;
using TOR_Core.Extensions;

namespace TOR_Core.Items
{
    public class TorInventoryItemTupleWidget : InventoryItemTupleWidget
    {
        private Brush _magicBrush;

        public TorInventoryItemTupleWidget(UIContext context) : base(context)
        {
            _magicBrush = context.GetBrush("TorInventoryMagicItemTupleBrush");
        }

        protected override void OnRender(TwoDimensionContext twoDimensionContext, TwoDimensionDrawContext drawContext)
        {
            if (!MainContainer.Brush.IsCloneRelated(CharacterCantUseBrush) && _magicBrush != null)
            {
                if(ItemID != null)
                {
                    var item = MBObjectManager.Instance.GetObject<ItemObject>(ItemID);
                    if (item != null && item.IsMagicalItem())
                    {
                        MainContainer.Brush = _magicBrush;
                    }
                }
            }
            base.OnRender(twoDimensionContext, drawContext);
        }
    }
}
